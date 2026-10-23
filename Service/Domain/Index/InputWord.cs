namespace PdfMasterIndex.Service.Domain.Index;

public class InputWord
{
    public Guid Id { get; set; }

    public string Value { get; set; } = null!;

    public Guid? WordId { get; set; }
}