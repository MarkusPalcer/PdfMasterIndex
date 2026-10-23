using System.Collections;

namespace PdfMasterIndex.Service.Application.Scanning;

public class WordCollection : IEnumerable<string>
{
    private readonly HashSet<string> _set;
    private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _spanLookup;

    public WordCollection(IEnumerable<string> initialData)
    {
        _set = initialData.ToHashSet(StringComparer.Ordinal);
        _spanLookup = _set.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    public string GetOrAdd(ReadOnlySpan<char> word)
    {
        if (_spanLookup.TryGetValue(word, out var value))
            return value;

        value = word.ToString();
        _set.Add(value);
        return value;
    }

    public IEnumerator<string> GetEnumerator()
    {
        return _set.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_set).GetEnumerator();
    }
}
