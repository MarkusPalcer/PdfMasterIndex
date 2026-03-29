using PdfMasterIndex.Service.Infrastructure.Persistence.Models;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Word = UglyToad.PdfPig.Content.Word;

namespace PdfMasterIndex.Service.Application.Scanning;

public static class WordSplitter
{
    private static readonly char[] Trims =
    [
        '(', ')', '"', ' ', '.', ',', '’', '‘', ';', '/', '“', '”', '!', '[', ']', '´', '`',
        '0','1','2','3','4','5','6','7','8','9', '+', '»', ' ', '%', '‚', '–', '×', '=', '—', '…',
        ':', '*', '­'
    ];

    public static IEnumerable<PageWord> SplitWords(IEnumerable<Page> pages)
    {
        var extractedWords = pages.SelectMany(GetWordsFromPage);
        var wordPrefix = "";
        
        uint positionInDocument = 0;
        foreach (var extractedWord in extractedWords)
        {
            var preTrim = extractedWord.Word;
            var trimmed = extractedWord.Word.Trim(Trims);
            while (trimmed != preTrim || trimmed.StartsWith('-'))
            {
                preTrim = trimmed;
                trimmed = trimmed.Trim(Trims);
                trimmed = trimmed.TrimStart('-');
            }
            
            var result = extractedWord with
            {
                Word = $"{wordPrefix}{trimmed}",
                PositionInDocument = positionInDocument
            };
            
            wordPrefix = "";
            positionInDocument++;
            if (result.Word.EndsWith('-'))
            {
                wordPrefix = result.Word.Trim('-');
                continue;
            }

            if (result.Word.Length == 0) continue;
            
            if (!result.Word.Any(char.IsLetterOrDigit))
            {
                continue;
            }
            
            yield return result;
        }
    }

    public static IEnumerable<PageWord> GetWordsFromPage(Page page)
    {
        var text = ContentOrderTextExtractor.GetText(page).ToLowerInvariant();
        
        uint positionInPage = 0;
        foreach (var word in text.Split())
        {
            yield return new PageWord(word, page.Number, positionInPage, 0);
            positionInPage++;
        }
    }
    
    public record PageWord(string Word, int Page, uint PositionInPage, uint PositionInDocument);
}