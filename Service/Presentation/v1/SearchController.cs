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

        public HashSet<Guid>? Tags { get; set; }
        
        public TagHandling TagHandling { get; set; } = TagHandling.Or;
    }

    public enum TagHandling
    {
        And,
        Or
    }

    [HttpPost("/api/v1/search")]
    public async Task<ActionResult<SearchResult[]>> Search([FromBody] SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Ok(Array.Empty<SearchResult>());
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

        if (request.Tags is { Count: > 0 })
        {
            search = search.Include(x => x.Document.Tags)
                           .Include(x => x.Document.ScanPath.Tags);
            search = search.Where(x => x.Document.Tags.Any(t => request.Tags.Contains(t.Id)) ||
                                       x.Document.ScanPath.Tags.Any(t => request.Tags.Contains(t.Id)));
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
            foreach (var document in item.GroupBy(x => x.Document))
            {
                if (request.Tags is { Count: > 0 })
                {
                    var tags = new HashSet<Guid>();
                    tags.UnionWith(document.Key.ScanPath.Tags.Select(x => x.Id));
                    tags.UnionWith(document.Key.Tags.Select(x => x.Id));

                    switch (request.TagHandling)
                    {
                        case TagHandling.Or:
                            if (!tags.Any(t => request.Tags.Contains(t)))
                            {
                                continue;
                            }

                            break;
                        case TagHandling.And:
                            if (!request.Tags.All(t => tags.Contains(t)))
                            {
                                continue;
                            }

                            break;
                        default:
                            return BadRequest("Invalid TagHandling value");
                    }
                }

                result.Locations.Add(new SearchResult.Location
                {
                    DocumentId = document.Key.Id,
                    DocumentName = document.Key.Name,
                    LinkPath = $"/api/v1/documents/{document.Key.Id}",
                    Pages = document.Select(x => x.Page).Distinct().Order().ToList()
                });
            }

            results.Add(result);
        }

        return results.ToArray();
    }
}