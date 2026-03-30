using System.Text.Json.Serialization;

namespace PdfMasterIndex.Service.Infrastructure.Persistence.Models;

public class ScanPath
{
    /// <summary>
    /// The persistence-id of the scan path.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// A human-readable name for this entry which is shown in the UI overviews
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// The path to the folder that contains the documents to be indexed as seen within the docker container
    /// </summary>
    public string Path { get; set; } = "";
    
    /// <summary>
    /// The documents that have been scanned within this scan path.
    /// </summary>
    [JsonIgnore] public List<Document> Documents { get; set; } = [];
}