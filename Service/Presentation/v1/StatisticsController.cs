using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Infrastructure.Persistence;

namespace PdfMasterIndex.Service.Presentation.v1;

[ApiController]
public class StatisticsController(MasterIndexDbContext context) : ControllerBase
{
    public class ScanPath
    {
        public string Name { get; set; } = "";
        public Document[] Documents { get; set; } = [];
    }

    public class Document
    {
        public string Name { get; set; } = "";
        public int Pages { get; set; } = 0;
        public int Words { get; set; } = 0;
        public int DistinctWords { get; set; } = 0;
    }
    
    [HttpGet("/api/v1/statistics")]
    public async Task<ActionResult<IEnumerable<ScanPath>>> GetStatistics()
    {
        var scanPaths = await context.ScanPaths
            .Include(sp => sp.Documents)
            .ThenInclude(doc => doc.Content)
            .Select(sp => new ScanPath
            {
                Name = sp.Name,
                Documents = sp.Documents.Select(doc => new Document
                {
                    Name = doc.Name,
                    Pages = doc.Content.Any() ? doc.Content.Max(o => o.Page) + 1 : 0,
                    Words = doc.Content.Count,
                    DistinctWords = doc.Content.Select(o => o.Word.Id).Distinct().Count()
                }).ToArray()
            }).ToListAsync();

        return Ok(scanPaths);
    }
}