namespace PdfMasterIndex.Service.Infrastructure.Persistence.Models;

/// <summary>
/// A file-hash
/// </summary>
/// <remarks>
/// This is saved as complex property of a document
/// </remarks>
public class Hash
{
    /// <summary>
    /// The base64 encoded hash value.
    /// </summary>
    public byte[] Value { get; set; } = [];
    
    /// <summary>
    /// The hash-algorithm used to calculate the hash.
    /// </summary>
    public Algorithm Algorithm { get; set; }
    
    public override string ToString()
    {
        return $"{Algorithm}: {Convert.ToBase64String(Value)}";
    }
}