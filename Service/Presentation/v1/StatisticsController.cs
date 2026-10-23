using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Infrastructure.Persistence;

namespace PdfMasterIndex.Service.Presentation.v1;

[ApiController]
public class StatisticsController(IRepository repository)
{
    public record Stats(long Documents, long UniqueWords, long Occurrences);
    
    [HttpGet("/stats")]
    public async Task<Stats> GetStats()
    {
        var documents = await repository.Documents.CountAsync();
        var uniqueWords = await repository.Words.CountAsync();
        var occurrences = await repository.Occurrences.CountAsync();

        return new Stats(documents, uniqueWords, occurrences);
    }
}