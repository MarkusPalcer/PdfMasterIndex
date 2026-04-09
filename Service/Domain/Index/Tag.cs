namespace PdfMasterIndex.Service.Domain.Index;

public class Tag
{
    public Guid Id { get; set; }
    public required string Value { get; set; }
    
    public List<ScanPath> ScanPaths { get; set; } = [];
    public List<Document> Documents { get; set; } = [];
}
