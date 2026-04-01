namespace PdfMasterIndex.Service.Infrastructure.Persistence.Models;

public class Occurrence
{
    /// <summary>
    /// The persistence-id of the occurrence.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The word that is being indexed.
    /// </summary>
    public required Word Word { get; set; }
    
    /// <summary>
    /// The document that contains the word.
    /// </summary>
    public required Document Document { get; set; }
    
    /// <summary>
    /// The zero-based page that contains the word.
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// The zero-based position of the word on the page.
    /// </summary>
    public uint PagePosition { get; set; }
    
    /// <summary>
    /// The zero-based position of the word in the document.
    /// </summary>
    public uint DocumentPosition { get; set; }
}