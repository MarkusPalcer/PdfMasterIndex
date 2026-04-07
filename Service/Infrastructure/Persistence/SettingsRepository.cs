using AutoInterfaceAttributes;
using Newtonsoft.Json;
using PdfMasterIndex.Service.Attributes;
using PdfMasterIndex.Service.Domain.Settings;

namespace PdfMasterIndex.Service.Infrastructure.Persistence;

[AutoInterface]
[Lifetime(ServiceLifetime.Singleton)]
public class SettingsRepository(ILogger<SettingsRepository> logger) : ISettingsRepository
{
    private static JsonSerializerSettings _jsonSerializerSettings = new()
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
            return JsonConvert.DeserializeObject<Settings>(await File.ReadAllTextAsync(_fileName), _jsonSerializerSettings) ?? new Settings();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cannot load settings");
            return new Settings();
        }
    }

    public async Task SaveSettingsAsync(Settings settings)
    {
        await File.WriteAllTextAsync(_fileName, JsonConvert.SerializeObject(settings, _jsonSerializerSettings));
    }
}