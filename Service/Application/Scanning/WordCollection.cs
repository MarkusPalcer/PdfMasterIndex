using System.Diagnostics.CodeAnalysis;
using PdfMasterIndex.Service.Domain.Index;

namespace PdfMasterIndex.Service.Application.Scanning;

public class WordCollection
{
    private readonly Dictionary<string, Word> _dictionary;

    public WordCollection(IEnumerable<Word> initialData)
    {
        _dictionary = initialData.ToDictionary(x => x.Value, x => x, StringComparer.Ordinal);
    }

    public void Add(Word word)
    {
        _dictionary[word.Value] = word;
    }

    public bool TryGetValue(ReadOnlySpan<char> word, [MaybeNullWhen(false)] out Word wordEntity)
    {
        var alternateLookup = _dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        return alternateLookup.TryGetValue(word, out wordEntity);
    }
}
