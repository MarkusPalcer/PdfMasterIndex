using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Infrastructure.Persistence;
using PdfMasterIndex.Service.Infrastructure.Persistence.Models;

namespace PdfMasterIndex.Service.Presentation.v1;

[ApiController]
public class SearchController(IRepository repository) : ControllerBase
{
    [HttpGet("/api/v1/search")]
    public IGrouping<string, Occurrence>[] Search([FromBody] string query)
    {
        return repository.Occurrences
                         .Include(x => x.Word)
                         .Where(x => x.Word.Value.Contains(query))
                         .Include(x => x.Document)
                         .OrderBy(x => x.Word.Value)
                         .AsEnumerable()
                         .GroupBy(x => x.Word.Value)
                         .Take(100)
                         .ToArray();
    }
}