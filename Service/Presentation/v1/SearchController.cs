using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Infrastructure.Persistence;
using PdfMasterIndex.Service.Infrastructure.Persistence.Models;

namespace PdfMasterIndex.Service.Presentation.v1;

public class SearchResult
{
    public string Word { get; set; } = "";
    public List<Location> Locations { get; set; } = [];

    public class Location
    {
        public Guid DocumentId { get; set; }
        public string DocumentName { get; set; } = "";
        public string LinkPath { get; set; } = "";
        public List<int> Pages { get; set; } = [];
    }
}

[ApiController]
public class SearchController(IRepository repository) : ControllerBase
{
    [HttpGet("/api/v1/search")]
    public async Task<SearchResult[]> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var search = repository.Occurrences
                               .Include(x => x.Word)
                               .Where(x => x.Word.Value.Contains(query))
                               .Include(x => x.Document)
                               .Include(x => x.Document.ScanPath)
                               .OrderBy(x => x.Word.Value)
                               .AsAsyncEnumerable()
                               .GroupBy(x => x.Word.Value);

        var results = new List<SearchResult>();

        await foreach (var item in search)
        {
            var result = new SearchResult
            {
                Word = item.Key,
            };
            foreach (var documents in item.GroupBy(x => x.Document))
            {
                result.Locations.Add(new SearchResult.Location
                {
                    DocumentId = documents.Key.Id,
                    DocumentName = documents.Key.Name,
                    LinkPath = $"/api/v1/documents/{documents.Key.Id}",
                    Pages = documents.Select(x => x.Page).Distinct().ToList()
                });
            }
            results.Add(result);
        }
        
        return results.ToArray();
    }
}