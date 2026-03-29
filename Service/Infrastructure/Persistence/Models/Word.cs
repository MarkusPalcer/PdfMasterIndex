using System.Text.Json.Serialization;

namespace PdfMasterIndex.Service.Infrastructure.Persistence.Models;

public class Word
{
    public Guid Id { get; set; }
    public string Value { get; set; } = "";
    public bool Ignored { get; set; } = false;
    [JsonIgnore] public List<Occurrence> Occurrences { get; set; } = [];
}