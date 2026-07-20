using System.Text;

namespace DotService.Packaging;

/// <summary>Builds Windows command lines using the CommandLineToArgvW escaping rules.</summary>
public static class WindowsCommandLine
{
    /// <summary>Combines an executable and arguments into an SCM-safe binary path.</summary>
    public static string Build(string executablePath, IEnumerable<string>? arguments = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        if (executablePath.IndexOfAny(['\0', '"']) >= 0)
        {
            throw new ArgumentException("Executable paths cannot contain quote or NUL characters.", nameof(executablePath));
        }

        IEnumerable<string> values = arguments ?? [];
        return string.Join(' ', new[] { $"\"{executablePath}\"" }.Concat(values.Select(QuoteArgument)));
    }

    /// <summary>Quotes one argument without invoking a shell.</summary>
    public static string QuoteArgument(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);
        if (argument.Contains('\0'))
        {
            throw new ArgumentException("Command-line arguments cannot contain NUL characters.", nameof(argument));
        }

        if (argument.Length > 0 && !argument.Any(static character => char.IsWhiteSpace(character) || character == '"'))
        {
            return argument;
        }

        StringBuilder result = new(argument.Length + 2);
        result.Append('"');
        int backslashes = 0;

        foreach (char character in argument)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '"')
            {
                result.Append('\\', (backslashes * 2) + 1);
                result.Append('"');
                backslashes = 0;
                continue;
            }

            result.Append('\\', backslashes);
            backslashes = 0;
            result.Append(character);
        }

        result.Append('\\', backslashes * 2);
        result.Append('"');
        return result.ToString();
    }
}
