using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PdfMasterIndex.Service.Infrastructure.Persistence;
using PdfMasterIndex.Service.Infrastructure.Persistence.Models;

namespace PdfMasterIndex.Service.Presentation.v1;

[ApiController]
public class ScanPathController(IRepository repository) : ControllerBase
{
    [HttpGet("/api/v1/scanpaths")]
    public async Task<ScanPath[]> Get() => await repository.ScanPaths.ToArrayAsync();
    
    [HttpGet("/api/v1/scanpaths/{id}")]
    public async Task<ActionResult<ScanPath>> Get(Guid id)
    {
        var result = await repository.ScanPaths.SingleOrDefaultAsync(x => x.Id == id);

        if (result == null)
        {
            return NotFound();
        }
        
        return result;
    }

    [HttpPost("/api/v1/scanpaths")]
    public async Task<ActionResult<ScanPath>> Post(ScanPath scanPath)
    {
        if (scanPath.Id != Guid.Empty)
        {
            var exists = await repository.ScanPaths.AnyAsync(x => x.Id == scanPath.Id);
            if (exists)
            {
                return Conflict($"A ScanPath with ID {scanPath.Id} already exists.");
            }
        }

        if (scanPath.Path.IsNullOrEmpty())
        {
            return BadRequest("Path is required.");
        }

        if (scanPath.Name.IsNullOrEmpty())
        {
            scanPath.Name = scanPath.Path;
        }
        
        await repository.AddAsync(scanPath);
        await repository.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = scanPath.Id }, scanPath);
    }

    [HttpPut("/api/v1/scanpaths/{id}")]
    public async Task<IActionResult> Put(Guid id, ScanPath scanPath)
    {
        var existing = await repository.ScanPaths.SingleOrDefaultAsync(x => x.Id == id);

        if (existing == null)
        {
            return NotFound();
        }

        if (!scanPath.Name.IsNullOrEmpty())
        {
            existing.Name = scanPath.Name;
        }
        
        if (!scanPath.Path.IsNullOrEmpty())
        {
            existing.Path = scanPath.Path;
        }

        repository.Update(existing);
        await repository.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("/api/v1/scanpaths/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await repository.ScanPaths.SingleOrDefaultAsync(x => x.Id == id);

        if (existing != null)
        {
            repository.Remove(existing);
            await repository.SaveChangesAsync();
        }

        return NoContent();
    }
}
