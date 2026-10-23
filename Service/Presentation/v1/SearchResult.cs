namespace PdfMasterIndex.Service.Presentation.v1;

public record SearchResponse(SearchResult[] Results, double DurationMs);

public class SearchResult
{
    public string Word { get; set; } = "";
    public List<Location> Locations { get; set; } = [];

    public class Location
    {
        public Guid DocumentId { get; set; }
        public string DocumentName { get; set; } = "";
        public string LinkPath { get; set; } = "";
        public List<int> Pages { get; set; } = [];
    }
}