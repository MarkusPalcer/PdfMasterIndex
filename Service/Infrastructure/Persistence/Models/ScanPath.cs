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
    /// <remarks>
    /// This will be used internally.
    /// </remarks>
    public string InternalPath { get; set; } = "";
    
    /// <summary>
    /// The path to the folder that contains the documents to be indexed as seen outside the docker container
    /// </summary>
    /// <remarks>
    /// This will be displayed in the UI.
    /// </remarks>
    public string ExternalPath { get; set; } = "";
    
    /// <summary>
    /// The documents that have been scanned within this scan path.
    /// </summary>
    [JsonIgnore] public List<Document> Documents { get; set; } = [];
}