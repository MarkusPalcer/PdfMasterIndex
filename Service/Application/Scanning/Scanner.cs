using AutoInterfaceAttributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PdfMasterIndex.Service.Attributes;
using PdfMasterIndex.Service.Domain.Index;
using PdfMasterIndex.Service.Infrastructure.Persistence;
using PdfMasterIndex.Service.Infrastructure.Persistence.Configurations;
using UglyToad.PdfPig;

namespace PdfMasterIndex.Service.Application.Scanning;

[AutoInterface]
[Lifetime(ServiceLifetime.Singleton)]
public class Scanner(IScanStatus status, IServiceScopeFactory scopeFactory, ILogger<Scanner> logger) : IScanner
{
    public IScanStatus Status { get; } = status;
    private Task _scanTask = Task.CompletedTask;
    private CancellationTokenSource _cancellationSource = new();

    private readonly List<Word> _newWords = [];
    private readonly List<Occurrence> _newOccurrences = [];

    private IRepository _repository = null!;

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

        Status.CurrentStepMessage = $"Scanning {path.Name} for new/changed files...";

        var knownFiles = path.Documents.ToDictionary(x => x.FilePath);

        var files = new DirectoryInfo(path.Path).GetFiles("*.pdf", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            logger.ScanningFile(file.FullName);
            _cancellationSource.Token.ThrowIfCancellationRequested();

            var relativePath = file.FullName[(path.Path.Length + 1)..];
            if (!knownFiles.Remove(relativePath, out var document))
            {
                document = new Document
                {
                    ScanPath = path,
                    FilePath = relativePath,
                    Name = file.Name,
                    Hash = string.Empty
                };

                await repository.AddAsync(document);
                logger.NewFile();

                continue;
            }

            if (document.Hash.IsNullOrEmpty())
            {
                logger.ChangedFile();
            }
            else
            {
                string hash;
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

                if (document.Hash != hash)
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

    private async Task<string> HashFileAsync(FileInfo file)
    {
        await using var stream = File.OpenRead(file.FullName);
        var hash = await System.Security.Cryptography.SHA256.HashDataAsync(stream, _cancellationSource.Token);
        return Convert.ToBase64String(hash);
    }

    private async Task ProcessFiles()
    {
        Status.CurrentStep = ScanStep.ParseFiles;
        Status.CurrentStepProgress = 0;

        using var scope = scopeFactory.CreateScope();
        _repository = scope.ServiceProvider.GetRequiredService<IRepository>();

        try
        {
            var documentsToScan = await _repository.Documents
                                                   .Include(x => x.ScanPath)
                                                   .Where(x => x.Hash == string.Empty)
                                                   .ToArrayAsync();

            var documentCount = documentsToScan.Length;
            var scanned = 0;

            logger.ImportStarted();

            foreach (var document in documentsToScan)
            {
                _cancellationSource.Token.ThrowIfCancellationRequested();

                try
                {
                    await ProcessFile(document);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.ImportFailed(document.FilePath, ex);
                }

                scanned++;
                Status.CurrentStepProgress = scanned / (double)documentCount;
            }

            logger.ImportFinished();
            await _repository.DeleteOrphanedWordsAsync();
        }
        finally
        {
            _repository = null!;
        }
    }

    private async Task ProcessFile(Document document)
    {
        Status.CurrentFileProgress = 0;
        var fullFilePath = Path.Combine(document.ScanPath.Path, document.FilePath);
        Status.CurrentStepMessage = $"Parsing {fullFilePath}...";
        
        logger.ImportProgress(fullFilePath);

        await _repository.ClearDocumentAsync(document);
        var words = new WordCollection(await _repository.Words.ToListAsync());
        _newWords.Clear();
        _newOccurrences.Clear();

        using var pdf = PdfDocument.Open(fullFilePath);

        var numberOfPages = pdf.NumberOfPages;

        WordSplitter.SplitWords(pdf.GetPages(), (word, page, positionInPage, positionInDocument) =>
        {
            _cancellationSource.Token.ThrowIfCancellationRequested();

            if (word.Length > WordConfiguration.MaxWordLength)
            {
                logger.WordTooLong(word.ToString(), page);
                return;
            }

            if (!words.TryGetValue(word, out var wordEntity))
            {
                wordEntity = new Word
                {
                    Id = Guid.NewGuid(),
                    Value = word.ToString()
                };
                _newWords.Add(wordEntity);
                words.Add(wordEntity);
            }

            var occurrence = new Occurrence
            {
                Id = Guid.NewGuid(),
                Document = document,
                DocumentPosition = positionInDocument,
                Page = page,
                PagePosition = positionInPage,
                Word = wordEntity
            };

            _newOccurrences.Add(occurrence);

            Status.CurrentFileProgress = page / (double)numberOfPages;
        });

        Status.CurrentStepMessage = $"Saving {fullFilePath}...";
        
        await _repository.BulkInsertAsync(_newWords);
        await _repository.BulkInsertAsync(_newOccurrences);

        document.Hash = await HashFileAsync(new FileInfo(fullFilePath));
        document.PageCount = numberOfPages;
        document.WordCount = _newOccurrences.Count;
        await _repository.SaveChangesAsync();
    }
}