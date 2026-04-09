using AutoInterfaceAttributes;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using PdfMasterIndex.Service.Attributes;
using PdfMasterIndex.Service.Domain.Settings;
using PdfMasterIndex.Service.Presentation.v1;

namespace PdfMasterIndex.Service.Infrastructure.Persistence;

[AutoInterface]
[Lifetime(ServiceLifetime.Singleton)]
public class SettingsRepository(ILogger<SettingsRepository> logger, IHubContext<SettingsHub> hubContext) : ISettingsRepository
{
    public bool SettingsWritable
    {
        get => field;
        private set
        {
            field = value;
            _ = hubContext.Clients.All.SendAsync("SettingsWritableChanged", value);
        }
    } = true;

    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        Formatting = Formatting.Indented,
    };
    
    private readonly string _fileName = Environment.GetEnvironmentVariable("CONFIG_PATH") ?? Path.Combine(".", "config.json");

    public async Task<Settings> GetSettingsAsync()
    {
        try
        {
            return JsonConvert.DeserializeObject<Settings>(await File.ReadAllTextAsync(_fileName), JsonSerializerSettings) ?? new Settings();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cannot load settings");
            return new Settings();
        }
    }

    public async Task SaveSettingsAsync(Settings settings)
    {
        try
        {
            await File.WriteAllTextAsync(_fileName, JsonConvert.SerializeObject(settings, JsonSerializerSettings));
            SettingsWritable = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cannot save settings");
            SettingsWritable = false;
        }
    }
}