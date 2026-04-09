namespace PdfMasterIndex.Service.Domain.Settings;

public class ScanPath
{
    public string Name { get; set; }
    public string[] Tags { get; set; }

    public ScanPath(Index.ScanPath scanPath)
    {
        Name = scanPath.Name;
        Tags = scanPath.Tags.Select(t => t.Value).ToArray();
    }

    public ScanPath()
    {
        Name = string.Empty;
        Tags = [];
    }
}