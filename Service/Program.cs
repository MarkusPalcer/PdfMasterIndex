using System.Reflection;
using AutoInterfaceAttributes;
using Microsoft.EntityFrameworkCore;
using PdfMasterIndex.Service.Attributes;
using PdfMasterIndex.Service.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5083);
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

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
app.UseAuthorization();
app.MapControllers();
app.Run();