namespace PdfMasterIndex.Service.Infrastructure.Persistence.Models;

/// <summary>
/// A document that is being indexed.
/// </summary>
public class Document
{
    /// <summary>
    /// The persistence-id of the document.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The display name of the document
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// The path of the document within the <see cref="ScanPath"/>
    /// </summary>
    public required string FilePath { get; set; }
    
    /// <summary>
    /// The <see cref="ScanPath"/> this document was found in
    /// </summary>
    public required ScanPath ScanPath { get; set; }

    public required Hash Hash { get; set; } = new();
    
    public List<Occurrence> Content { get; set; } = []; 
}