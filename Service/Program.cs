using System.Reflection;
using AutoInterfaceAttributes;
using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Attributes;
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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    services.GetRequiredService<ILogger<Program>>().LogInformation("Migrating database...");
    await services.GetRequiredService<MasterIndexDbContext>().Database.MigrateAsync();
    services.GetRequiredService<ILogger<Program>>().LogInformation("Database migration completed...");
}

app.MapOpenApi();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.MapHub<ScanHub>("/scan-hub");
app.MapHub<LogHub>("/log-hub");
app.Run();