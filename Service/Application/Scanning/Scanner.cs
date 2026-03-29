using AutoInterfaceAttributes;
using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Attributes;
using PdfMasterIndex.Service.Infrastructure.Persistence;
using PdfMasterIndex.Service.Infrastructure.Persistence.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace PdfMasterIndex.Service.Application.Scanning;

[AutoInterface]
[Lifetime(ServiceLifetime.Singleton)]
public class Scanner(IScanStatus status, IServiceScopeFactory scopeFactory, ILogger<Scanner> logger) : IScanner
{
    public IScanStatus Status { get; } = status;
    private Task _scanTask = Task.CompletedTask;
    private CancellationTokenSource _cancellationSource = new();

    private readonly (string, string)[] _textReplacements =
    [
        ($"\n{Environment.NewLine}", Environment.NewLine),
    ];

    public void Start()
    {
        if (Status.IsRunning)
        {
            throw new InvalidOperationException("Scanner is already scanning.");
        }

        Status.CurrentStep = ScanStep.ScanForFiles;

        _cancellationSource = new CancellationTokenSource();
        _scanTask = ScanAsync();
    }

    public async Task Cancel()
    {
        if (!Status.IsRunning)
        {
            throw new InvalidOperationException("Scanner is not scanning.");
        }

        Status.CurrentStep = ScanStep.Cancelling;
        await _cancellationSource.CancelAsync();
        await _scanTask;
        Status.CurrentStep = ScanStep.Idle;
    }

    public async Task ScanAsync()
    {
        try
        {
            await ScanForFiles();
            await ProcessFiles();
            Status.CurrentStep = ScanStep.Idle;
        }
        catch (OperationCanceledException)
        {
            logger.Cancelled();
        }
        catch (Exception ex)
        {
            logger.UncaughtException(ex);
        }
    }

    private async Task ScanForFiles()
    {
        Status.CurrentStep = ScanStep.ScanForFiles;
        Status.CurrentStepProgress = 0;

        Guid[] paths;
        using (var scope = scopeFactory.CreateScope())
        {
            paths = scope.ServiceProvider.GetRequiredService<IRepository>().ScanPaths.Select(x => x.Id).ToArray();
        }

        var count = 0;
        foreach (var scanPath in paths)
        {
            await ScanForFiles(scanPath);
            count++;
            Status.CurrentStepProgress = count / (double)paths.Length;
            _cancellationSource.Token.ThrowIfCancellationRequested();
        }
    }

    private async Task ScanForFiles(Guid scanPathId)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository>();
        var path = repository.ScanPaths.Include(x => x.Documents).Single(x => x.Id == scanPathId);

        var knownFiles = path.Documents.ToDictionary(x => x.FilePath);

        var files = new DirectoryInfo(path.InternalPath).GetFiles("*.pdf", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            logger.ScanningFile(file.FullName);
            _cancellationSource.Token.ThrowIfCancellationRequested();

            var relativePath = file.FullName[(path.InternalPath.Length + 1)..];
            if (!knownFiles.Remove(relativePath, out var document))
            {
                document = new Document
                {
                    ScanPath = path,
                    FilePath = relativePath,
                    Name = file.Name,
                    Hash = new Hash { Algorithm = Algorithm.Sha256 }
                };

                await repository.AddAsync(document);
                logger.NewFile();

                continue;
            }

            if (document.Hash.Value.Length == 0)
            {
                logger.ChangedFile();
            }
            else
            {
                Hash hash;
                try
                {
                    hash = await HashFileAsync(file);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.ScanningFailed(file.FullName, ex);
                    continue;
                }
                
                if (!document.Hash.Value.SequenceEqual(hash.Value))
                {
                    logger.ChangedFile();
                }
            }
        }

        foreach (var x in knownFiles.Values.ToList())
        {
            logger.RemovedFile(x.FilePath);
            repository.Remove(x);
        }

        await repository.SaveChangesAsync();
    }

    private async Task<Hash> HashFileAsync(FileInfo file)
    {
        await using var stream = File.OpenRead(file.FullName);
        return new Hash
        {
            Algorithm = Algorithm.Sha256,
            Value = await System.Security.Cryptography.SHA256.HashDataAsync(stream, _cancellationSource.Token)
        };
    }

    private async Task ProcessFiles()
    {
        Status.CurrentStep = ScanStep.ParseFiles;
        Status.CurrentStepProgress = 0;

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository>();

        var documentsToScan = await repository.Documents
                                              .Include(x => x.ScanPath)
                                              .Where(x => x.Hash.Value.Length == 0)
                                              .ToArrayAsync();

        var documentCount = documentsToScan.Length;
        var scanned = 0;

        logger.ImportStarted();

        foreach (var document in documentsToScan)
        {
            _cancellationSource.Token.ThrowIfCancellationRequested();

            try
            {
                await ProcessFile(document, repository);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                logger.ImportFailed(document.FilePath, ex);
            }

            scanned++;
            Status.CurrentStepProgress = scanned / (double)documentCount;
        }
        
        logger.ImportFinished();
    }

    private async Task ProcessFile(Document document, IRepository repository)
    {
        Status.CurrentFileProgress = 0;
        logger.ImportProgress(document.FilePath);

        await repository.ClearDocumentAsync(document);
        var words = await repository.Words.ToDictionaryAsync(x => x.Value);

        using var pdf = PdfDocument.Open(Path.Combine(document.ScanPath.InternalPath, document.FilePath));
        uint positionInDocument = 0;

        foreach (var page in pdf.GetPages())
        {
            _cancellationSource.Token.ThrowIfCancellationRequested();
            var text = ContentOrderTextExtractor.GetText(page).ToLowerInvariant();
            foreach (var (toReplace, replaceWith) in _textReplacements)
            {
                while (text.Contains(toReplace))
                {
                    text = text.Replace(toReplace, replaceWith);
                }
            }

            uint positionInPage = 0;
            var splitString = text.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in splitString)
            {
                _cancellationSource.Token.ThrowIfCancellationRequested();
                if (!words.TryGetValue(word, out var wordEntity))
                {
                    wordEntity = new Word
                    {
                        Value = word
                    };
                    await repository.AddAsync(wordEntity);
                    words[word] = wordEntity;
                }

                var occurrence = new Occurrence
                {
                    Document = document,
                    DocumentPosition = positionInDocument,
                    Page = page.Number,
                    PagePosition = positionInPage,
                    Word = wordEntity
                };
                await repository.AddAsync(occurrence);

                positionInDocument++;
                positionInPage++;
            }

            Status.CurrentFileProgress = page.Number / (double)pdf.NumberOfPages;
        }

        _cancellationSource.Token.ThrowIfCancellationRequested();
        document.Hash = await HashFileAsync(new FileInfo(Path.Combine(document.ScanPath.InternalPath, document.FilePath)));
        await repository.SaveChangesAsync();
    }
}