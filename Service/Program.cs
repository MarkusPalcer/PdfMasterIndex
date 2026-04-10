using System.Reflection;
using AutoInterfaceAttributes;
using System.Text.Json.Serialization;
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
builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
builder.Services.AddSignalR()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
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
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, proxy-revalidate, max-age=0");
        ctx.Context.Response.Headers.Append("Pragma", "no-cache");
        ctx.Context.Response.Headers.Append("Expires", "0");
    }
});
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, proxy-revalidate, max-age=0");
    context.Response.Headers.Append("Pragma", "no-cache");
    context.Response.Headers.Append("Expires", "0");
    await next();
});
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.MapHub<ScanHub>("/scan-hub");
app.MapHub<LogHub>("/log-hub");
app.MapHub<SettingsHub>("/settings-hub");
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

    var databaseEntries = await dataRepo.ScanPaths
                                        .Include(x => x.Tags)
                                        .ToDictionaryAsync(x => x.Path);
    
    var settings = await settingsRepo.GetSettingsAsync();
    foreach (var settingsEntry in settings.ScanPaths)
    {
        if (!databaseEntries.TryGetValue(settingsEntry.Key, out var databaseEntry))
        {
            databaseEntry = new ScanPath
            {
                Path = settingsEntry.Key,
            };
            await dataRepo.AddAsync(databaseEntry);
        }
        databaseEntry.Name = settingsEntry.Value.Name;
        databaseEntry.Tags = await dataRepo.ProcessTags(settingsEntry.Value.Tags);
    }
    await dataRepo.SaveChangesAsync();
    
    var newExistingScanPaths = await dataRepo.ScanPaths.ToArrayAsync();
    settings.ScanPaths = newExistingScanPaths.ToDictionary(x => x.Path, x=> new PdfMasterIndex.Service.Domain.Settings.ScanPath(x));
    await settingsRepo.SaveSettingsAsync(settings);
}