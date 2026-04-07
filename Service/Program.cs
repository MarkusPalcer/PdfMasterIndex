using System.Reflection;
using AutoInterfaceAttributes;
using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Attributes;
using PdfMasterIndex.Service.Domain.Index;
using PdfMasterIndex.Service.Infrastructure.Logging;
using PdfMasterIndex.Service.Infrastructure.Persistence;
using PdfMasterIndex.Service.Presentation.v1;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "WebRoot"
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddHistory();

builder.Services.Scan(scan =>
                          scan.FromAssemblyOf<Program>()
                           .AddClasses(c =>
                                           c.WithAttribute<AutoInterfaceAttribute>())
                           .AsImplementedInterfaces()
                           .WithLifetime(type => type.GetCustomAttribute<LifetimeAttribute>()?.Lifetime ?? ServiceLifetime.Transient));

builder.Services.AddDbContext<MasterIndexDbContext>(x => x.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

await MigrateDatabaseAsync();
await ApplySettingsAsync();

app.MapOpenApi();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.MapHub<ScanHub>("/scan-hub");
app.MapHub<LogHub>("/log-hub");
app.Run();

return;

async Task MigrateDatabaseAsync()
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    services.GetRequiredService<ILogger<Program>>().LogInformation("Migrating database...");
    await services.GetRequiredService<MasterIndexDbContext>().Database.MigrateAsync();
    services.GetRequiredService<ILogger<Program>>().LogInformation("Database migration completed...");
}

async Task ApplySettingsAsync()
{
    using var scope = app.Services.CreateScope();
    var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
    var dataRepo = scope.ServiceProvider.GetRequiredService<IRepository>();

    var existingScanPaths = await dataRepo.ScanPaths.ToDictionaryAsync(x => x.Path);
    
    var settings = await settingsRepo.GetSettingsAsync();
    foreach (var scanPath in settings.ScanPaths)
    {
        if (existingScanPaths.TryGetValue(scanPath.Key, out var existingScanPath))
        {
            existingScanPath.Name = scanPath.Value.Name;
        }
        else
        {
            await dataRepo.AddAsync(new ScanPath
            {
                Path = scanPath.Key,
                Name = scanPath.Value.Name,
            });
        }
    }
    await dataRepo.SaveChangesAsync();
    
    var newExistingScanPaths = await dataRepo.ScanPaths.ToArrayAsync();
    settings.ScanPaths = newExistingScanPaths.ToDictionary(x => x.Path, x=> new PdfMasterIndex.Service.Domain.Settings.ScanPath()
    {
        Name = x.Name
    });
    await settingsRepo.SaveSettingsAsync(settings);
}