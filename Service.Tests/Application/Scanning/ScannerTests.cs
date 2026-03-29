using FluentAssertions;
using PdfMasterIndex.Service.Application.Scanning;

namespace Service.Tests.Application.Scanning;

public class ScannerTests
{
    [Test]
    [TestCase("This is a test", new[] { "This", "is", "a", "test" })]
    [TestCase("Test 123", new[] { "Test" })]
    [TestCase("((lep + int))", new[] { "lep", "int" })]
    [TestCase("(‘altgüldenländisch’)", new[] { "altgüldenländisch" })]
    [TestCase("(‘altgüldenländisch;", new[] { "altgüldenländisch" })]
    [TestCase("(‘herr/", new[] { "herr" })]
    [TestCase("(“fass!”)", new[] { "fass" })]
    public void GetWords_SplitsCorrectly(string input, string[] expected)
    {
        WordSplitter.SplitWords(input).Should().BeEquivalentTo(expected);
    }
}