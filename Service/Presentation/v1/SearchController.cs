using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Domain.Index;
using PdfMasterIndex.Service.Infrastructure.Persistence;

namespace PdfMasterIndex.Service.Presentation.v1;

[ApiController]
public class SearchController(IRepository repository) : ControllerBase
{
    public class SearchRequest
    {
        public string Query { get; set; } = "";
        public List<Guid>? SearchPaths { get; set; }
    }

    [HttpPost("/api/v1/search")]
    public async Task<SearchResult[]> Search([FromBody] SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return [];
        }


        IQueryable<Occurrence> search = repository.Occurrences
                                                  .Include(x => x.Word)
                                                  .Where(x => x.Word.Value.Contains(request.Query))
                                                  .Include(x => x.Document)
                                                  .Include(x => x.Document.ScanPath);
        if (request.SearchPaths != null)
        {
            search = search.Where(x => request.SearchPaths.Contains(x.Document.ScanPath.Id));
        }
        
        var searchResult = search.OrderByDescending(x => x.Word.Value == request.Query)
                       .ThenBy(x => x.Word.Value)
                       .AsAsyncEnumerable()
                       .GroupBy(x => x.Word.Value);

        var results = new List<SearchResult>();

        await foreach (var item in searchResult)
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
                    Pages = documents.Select(x => x.Page).Distinct().Order().ToList()
                });
            }

            results.Add(result);
        }

        return results.ToArray();
    }
}