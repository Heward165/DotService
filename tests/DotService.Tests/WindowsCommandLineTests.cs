using DotService.Packaging;

namespace DotService.Tests;

public sealed class WindowsCommandLineTests
{
    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("", "\"\"")]
    [InlineData("two words", "\"two words\"")]
    [InlineData("a\"b", "\"a\\\"b\"")]
    [InlineData("C:\\folder with space\\", "\"C:\\folder with space\\\\\"")]
    public void QuoteArgumentUsesWindowsEscapingRules(string input, string expected)
    {
        Assert.Equal(expected, WindowsCommandLine.QuoteArgument(input));
    }

    [Fact]
    public void BuildQuotesExecutableAndArgumentsIndependently()
    {
        string commandLine = WindowsCommandLine.Build(
            @"C:\Program Files\Acme\worker.exe",
            ["--contentRoot", @"C:\Program Data\Acme"]);

        Assert.Equal(
            "\"C:\\Program Files\\Acme\\worker.exe\" --contentRoot \"C:\\Program Data\\Acme\"",
            commandLine);
    }

    [Fact]
    public void QuoteArgumentRejectsEmbeddedNullCharacters()
    {
        Assert.Throws<ArgumentException>(() => WindowsCommandLine.QuoteArgument("unsafe\0tail"));
    }
}
