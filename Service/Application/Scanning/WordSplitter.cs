using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace PdfMasterIndex.Service.Application.Scanning;

public static class WordSplitter
{
    public static void SplitWords(IEnumerable<Page> pages, WordHandler wordHandler)
    {
        SplitWordsInternal(pages, wordHandler);
    }

    private static void SplitWordsInternal(IEnumerable<Page> pages, WordHandler wordHandler)
    {
        void CommitWord(ReadOnlySpan<char> text, int start, int end, Page page, ref uint positionInPage, ref uint positionInDocument,
                        ref string prefix)
        {
            if (prefix.Length > 0)
            {
                var word = text[start..end];
                var buffer = new char[prefix.Length + word.Length];
                prefix.CopyTo(buffer);
                word.CopyTo(buffer.AsSpan(prefix.Length));
                wordHandler(buffer.AsSpan(), page.Number, positionInPage, positionInDocument);
                prefix = string.Empty;
            }
            else
            {
                wordHandler(text[start..end], page.Number, positionInPage, positionInDocument);
            }

            positionInPage++;
            positionInDocument++;
        }

        var prefix = "";
        uint positionInDocument = 0;

        foreach (var page in pages)
        {
            var text = ContentOrderTextExtractor.GetText(page).ToLowerInvariant().AsSpan();
            uint positionInPage = 0;
            var start = 0;
            var inWord = false;

            // Scan the whole page
            for (var i = 0; i < text.Length; i++)
            {
                // If we haven't started a word yet, we start one if we encounter a letter
                // else we ignore the current character
                if (!inWord)
                {
                    if (char.IsLetter(text[i]))
                    {
                        start = i;

                        // Special case: A letter is at the end of a page: Then this letter is treated as a word
                        if (i == text.Length - 1)
                        {
                            CommitWord(text, start, i, page, ref positionInPage, ref positionInDocument, ref prefix);
                        }
                        else
                        {
                            inWord = true;
                        }
                    }

                    continue;
                }

                // But in the special case of a hyphen, the word might be split over multiple lines
                // so we will need to check if the hyphen is at the end of a line or of the whole page
                // _then_ we need to remember the part of the word _before_ the hyphen for the next encountered word
                // and prepend it to the first word of the next line or page

                // We are in a word. We read a letter => continue word unless it's the last letter of the page
                if (char.IsLetter(text[i]))
                {
                    // Special case: A letter is at the end of a page: Then this marks the end of the word
                    if (i == text.Length - 1)
                    {
                        CommitWord(text, start, i, page, ref positionInPage, ref positionInDocument, ref prefix);
                        inWord = false;
                    }

                    continue;
                }

                // We are in a word. We read a non-letter which is not a hyphen => finish word
                if (text[i] != '-')
                {
                    CommitWord(text, start, i, page, ref positionInPage, ref positionInDocument, ref prefix);
                    inWord = false;
                    continue;
                }

                // We read a hyphen. If this is the end of the page or the end of a line, we store the word so far for the next page
                if (i == text.Length - 1 || FollowedByNewLine(text, i))
                {
                    prefix = text[start..i].ToString();
                }


                inWord = false;
            }
        }
    }

    /// <summary>
    /// Returns true if the character at the given index is followed by a new line (depending on the current environment) 
    /// </summary>
    private static bool FollowedByNewLine(ReadOnlySpan<char> text, int index)
    {
        // Skip current character
        index++;

        // Skip potential spaces after hyphen before newline
        while (index < text.Length && text[index] == ' ')
        {
            index++;
        }

        if (index >= text.Length)
        {
            return false;
        }

        // Check for \r\n, \n, or \r
        if (text[index] == '\n' || text[index] == '\r')
        {
            return true;
        }

        return false;
    }

    public delegate void WordHandler(ReadOnlySpan<char> word, int page, uint positionInPage, uint positionInDocument);
}