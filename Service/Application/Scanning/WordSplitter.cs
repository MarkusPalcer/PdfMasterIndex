namespace PdfMasterIndex.Service.Application.Scanning;

public static class WordSplitter
{
    private static readonly (string, string)[] TextReplacements =
    [
        ($"\n{Environment.NewLine}", Environment.NewLine),
    ];

    private static readonly char[] Trims = ['(', ')', '"', ' ', '.', ',', '’', '‘', ';', '/', '“', '”', '!'];

    public static IEnumerable<string> SplitWords(string text)
    {
        foreach (var (toReplace, replaceWith) in TextReplacements)
        {
            while (text.Contains(toReplace))
            {
                text = text.Replace(toReplace, replaceWith);
            }
        }

        return text.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries)
                   .Where(word => word.Any(char.IsLetter))
                   .Select(word => word.Trim(Trims));
    }
}