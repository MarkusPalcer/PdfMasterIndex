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
    private Task _scanTask = Task.CompletedTask;
    private CancellationTokenSource _cancellationSource = new();

    private readonly List<(Occurrence Occurence, string Word)> _newOccurrences = [];

    private IRepository _repository = null!;

    public void Start()
    {
        if (status.IsRunning)
        {
            throw new InvalidOperationException("Scanner is already scanning.");
        }

        status.CurrentStep = ScanStep.ScanForFiles;

        _cancellationSource = new CancellationTokenSource();
        _scanTask = ScanAsync();
    }

    public async Task Cancel()
    {
        if (!status.IsRunning)
        {
            throw new InvalidOperationException("Scanner is not scanning.");
        }

        status.CurrentStep = ScanStep.Cancelling;
        await _cancellationSource.CancelAsync();
        await _scanTask;
        status.CurrentStep = ScanStep.Idle;
    }

    public async Task ScanAsync()
    {
        try
        {
            await ScanForFiles();
            await ProcessFiles();
            status.CurrentStep = ScanStep.Idle;
        }
        catch (OperationCanceledException)
        {
            status.CurrentStep = ScanStep.Idle;
            logger.Cancelled();
        }
        catch (Exception ex)
        {
            status.CurrentStep = ScanStep.Idle;
            logger.UncaughtException(ex);
        }
    }

    private async Task ScanForFiles()
    {
        status.CurrentStep = ScanStep.ScanForFiles;
        status.CurrentStepProgress = 0;

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
            status.CurrentStepProgress = count / (double)paths.Length;
            _cancellationSource.Token.ThrowIfCancellationRequested();
        }
    }

    private async Task ScanForFiles(Guid scanPathId)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository>();
        var path = repository.ScanPaths.Include(x => x.Documents).Single(x => x.Id == scanPathId);

        status.CurrentStepMessage = $"Scanning {path.Name} for new/changed files";

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
        status.CurrentStep = ScanStep.ParseFiles;
        status.CurrentStepProgress = 0;
        status.CurrentStepMessage = "Parsing files";

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
                status.CurrentStepProgress = scanned / (double)documentCount;
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
        status.CurrentFileProgress = 0;
        var fullFilePath = Path.Combine(document.ScanPath.Path, document.FilePath);
        status.CurrentFileMessage = $"Parsing {fullFilePath}";
        
        logger.ImportProgress(fullFilePath);

        await _repository.ClearDocumentAsync(document);
        
        var words = new WordCollection(await _repository.Words.Select(x => x.Value).ToListAsync());
        
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
            
            var wordAsString = words.GetOrAdd(word);
            
            var occurrence = new Occurrence
            {
                Id = Guid.NewGuid(),
                Document = document,
                DocumentPosition = positionInDocument,
                Page = page,
                PagePosition = positionInPage,
                Word = null!
            };

            _newOccurrences.Add((occurrence, wordAsString));

            status.CurrentFileProgress = page / (double)numberOfPages;
        });

        status.CurrentFileMessage = $"Saving {fullFilePath}";

        var wordEntityIds = await SaveAndGetWordIdsAsync(words);

        foreach (var newOccurrence in _newOccurrences)
        {
            newOccurrence.Occurence.WordId = wordEntityIds[newOccurrence.Word];
        }
        
        await _repository.BulkInsertAsync(_newOccurrences.Select(x=> x.Occurence), p => status.CurrentFileProgress = (double)p * 0.5 + 0.5);

        document.Hash = await HashFileAsync(new FileInfo(fullFilePath));
        document.PageCount = numberOfPages;
        document.WordCount = _newOccurrences.Count;
        await _repository.SaveChangesAsync();
    }

    private async Task<Dictionary<string, Guid>> SaveAndGetWordIdsAsync(
        WordCollection words)
    {
        var inputWords = CreateInputWords(words);

        await ClearInputWordsAsync();
        
        await _repository.BulkInsertAsync(inputWords, p => status.CurrentFileProgress = (double)p * 0.5);

        await InsertMissingWordsAsync();

        await ResolveWordIdsAsync();

        var wordIds = await ReadWordIdsAsync();

        await ClearInputWordsAsync();
        
        return wordIds;
    }

    private static List<InputWord> CreateInputWords(WordCollection words)
    {
        return words
            .Select(word => new InputWord
            {
                Id = Guid.CreateVersion7(),
                Value = word
            })
            .ToList();
    }

    private async Task InsertMissingWordsAsync()
    {
        await _repository.ExecuteSqlRawAsync(
            """
            INSERT INTO dbo.Words (Value, Ignored)
            SELECT iw.Value, 0
            FROM dbo.InputWords AS iw
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM dbo.Words AS w
                WHERE w.Value = iw.Value
            )
            GROUP BY iw.Value;
            """);
    }
    
    private async Task ResolveWordIdsAsync()
    {
        await _repository.ExecuteSqlRawAsync(
            """
            UPDATE iw
            SET WordId = w.Id
            FROM dbo.InputWords AS iw
            INNER JOIN dbo.Words AS w
                ON w.Value = iw.Value;
            """);
    }
    
    private async Task<Dictionary<string, Guid>> ReadWordIdsAsync()
    {
        return await _repository.InputWords
            .AsNoTracking()
            .ToDictionaryAsync(
                x => x.Value,
                x => x.WordId!.Value,
                StringComparer.Ordinal);
    }
    
    private async Task ClearInputWordsAsync()
    {
        await _repository.ExecuteSqlRawAsync("TRUNCATE TABLE dbo.InputWords;");
    }
}