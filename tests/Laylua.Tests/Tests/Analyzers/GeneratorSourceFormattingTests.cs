using System.Text.RegularExpressions;

namespace Laylua.Tests;

public sealed class GeneratorSourceFormattingTests
{
    [Test]
    public void GeneratorSource_DoesNotUseAdjacentNonBlankWriterLineCalls()
    {
        // Arrange
        var repositoryRoot = FindRepositoryRoot();
        var toolsDirectory = Path.Combine(repositoryRoot, "tools");
        var offenders = new List<string>();

        // Act
        foreach (var path in Directory.EnumerateFiles(toolsDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(path);
            for (var lineIndex = 0; lineIndex < lines.Length - 1; lineIndex++)
            {
                if (IsNonBlankWriterLineCall(lines[lineIndex]) && IsNonBlankWriterLineCall(lines[lineIndex + 1]))
                {
                    offenders.Add($"{Path.GetRelativePath(repositoryRoot, path)}:{lineIndex + 1}");
                }
            }
        }

        // Assert
        Assert.That(offenders, Is.Empty, string.Join(Environment.NewLine, offenders));
    }

    [Test]
    public void GeneratorCodeSnippets_DoNotRelyOnExternalCloseBraceForSnippetOwnedBlocks()
    {
        // Arrange
        var repositoryRoot = FindRepositoryRoot();
        var toolsDirectory = Path.Combine(repositoryRoot, "tools");
        var snippetPattern = new Regex(
            "(?<receiver>\\w+)\\.Code\\(\\$*\"\"\"\\r?\\n(?<body>.*?)\\r?\\n\\s*\"\"\"\\);",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        var offenders = new List<string>();

        // Act
        foreach (var path in Directory.EnumerateFiles(toolsDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            foreach (Match match in snippetPattern.Matches(text))
            {
                if (CountStructuralBraceBalance(match.Groups["body"].Value) <= 0)
                {
                    continue;
                }

                var receiver = match.Groups["receiver"].Value;
                var remainingText = text[(match.Index + match.Length)..];
                var nextNonBlankLine = remainingText
                    .Split(["\r\n", "\n", "\r"], StringSplitOptions.None)
                    .Select(static line => line.Trim())
                    .FirstOrDefault(static line => line.Length > 0);
                if (nextNonBlankLine == $"{receiver}.CloseBrace();")
                {
                    var lineNumber = text[..match.Index].Count(static character => character == '\n') + 1;
                    offenders.Add($"{Path.GetRelativePath(repositoryRoot, path)}:{lineNumber}");
                }
            }
        }

        // Assert
        Assert.That(offenders, Is.Empty, string.Join(Environment.NewLine, offenders));
    }

    [Test]
    public void GeneratorCodeSnippets_StandaloneClosingBraces_AreVisuallyDedented()
    {
        // Arrange
        var repositoryRoot = FindRepositoryRoot();
        var toolsDirectory = Path.Combine(repositoryRoot, "tools");
        var snippetPattern = new Regex(
            "(?<receiver>\\w+)\\.Code\\(\\$*\"\"\"\\r?\\n(?<body>.*?)\\r?\\n\\s*\"\"\"\\);",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        var offenders = new List<string>();

        // Act
        foreach (var path in Directory.EnumerateFiles(toolsDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            foreach (Match match in snippetPattern.Matches(text))
            {
                var bodyLines = match.Groups["body"].Value.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
                for (var lineIndex = 0; lineIndex < bodyLines.Length - 1; lineIndex++)
                {
                    if (!IsStatementLikeLine(bodyLines[lineIndex]))
                    {
                        continue;
                    }

                    if (bodyLines[lineIndex + 1].Trim() != "}")
                    {
                        continue;
                    }

                    if (GetIndentation(bodyLines[lineIndex + 1]) >= GetIndentation(bodyLines[lineIndex]))
                    {
                        var fileLineNumber = text[..match.Index].Count(static character => character == '\n') + lineIndex + 2;
                        offenders.Add($"{Path.GetRelativePath(repositoryRoot, path)}:{fileLineNumber}");
                    }
                }
            }
        }

        // Assert
        Assert.That(offenders, Is.Empty, string.Join(Environment.NewLine, offenders));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Laylua.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.That(directory, Is.Not.Null, "Repository root could not be found from the test output directory.");

        return directory!.FullName;
    }

    private static bool IsNonBlankWriterLineCall(string line)
    {
        var trimmedLine = line.Trim();
        var dotIndex = trimmedLine.IndexOf('.');
        if (dotIndex <= 0 || !trimmedLine.EndsWith(");"))
        {
            return false;
        }

        return trimmedLine[(dotIndex + 1)..].StartsWith("Line(")
            && trimmedLine[(dotIndex + 1)..] != "Line();";
    }

    private static int CountStructuralBraceBalance(string text)
    {
        var balance = 0;
        foreach (var character in text)
        {
            if (character == '{')
            {
                balance++;
            }
            else if (character == '}')
            {
                balance--;
            }
        }

        return balance;
    }

    private static bool IsStatementLikeLine(string line)
    {
        var trimmedLine = line.Trim();
        if (trimmedLine.Length == 0)
        {
            return false;
        }

        return trimmedLine is not "{" and not "}" and not "else" and not "catch" and not "finally";
    }

    private static int GetIndentation(string line)
    {
        return line.Length - line.TrimStart().Length;
    }
}
