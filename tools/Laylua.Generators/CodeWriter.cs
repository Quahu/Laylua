using System.Collections.Generic;
using System.Text;

namespace Laylua.Generators;

/// <summary>
///     Wraps StringBuilder with automatic indentation tracking for code generation.
/// </summary>
internal sealed class CodeWriter
{
    private const string NewLine = "\r\n";

    private readonly StringBuilder _sb = new();
    private int _indent;
    private bool _pendingStatementSeparator;

    public void Line(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Line();
            return;
        }

        WriteLineCore(text, indent: true);
    }

    public void Line()
    {
        _sb.Append(NewLine);
        _pendingStatementSeparator = false;
    }

    public void Code(string code)
    {
        var lines = GetLogicalLines(code);
        var startIndex = 0;
        while (startIndex < lines.Count && string.IsNullOrWhiteSpace(lines[startIndex]))
        {
            startIndex++;
        }

        var endIndex = lines.Count - 1;
        while (endIndex >= startIndex && string.IsNullOrWhiteSpace(lines[endIndex]))
        {
            endIndex--;
        }

        for (var index = startIndex; index <= endIndex; index++)
        {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                Line();
                continue;
            }

            var trimmedLine = line.TrimStart();
            var leadingCloseBraceCount = CountLeadingCloseBraces(trimmedLine);
            _indent -= leadingCloseBraceCount;
            Line(trimmedLine);
            _indent += CountStructuralOpenBraces(trimmedLine) - CountStructuralCloseBraces(trimmedLine) + leadingCloseBraceCount;
        }
    }

    public void OpenBrace()
    {
        Line("{");
        _indent++;
    }

    public void CloseBrace()
    {
        _indent--;
        Line("}");
    }

    public void Indent()
    {
        _indent++;
    }

    public void Dedent()
    {
        _indent--;
    }

    public void DisableWarning(string warningId)
    {
        WriteLineCore($"#pragma warning disable {warningId}", indent: false);
    }

    public void RestoreWarning(string warningId)
    {
        WriteLineCore($"#pragma warning restore {warningId}", indent: false);
    }

    public override string ToString()
    {
        return _sb.ToString();
    }

    private static List<string> GetLogicalLines(string text)
    {
        var lines = new List<string>();
        var position = 0;
        while (position < text.Length)
        {
            var lineStart = position;
            while (position < text.Length && text[position] is not '\r' and not '\n')
            {
                position++;
            }

            lines.Add(text.Substring(lineStart, position - lineStart));
            if (position >= text.Length)
            {
                break;
            }

            if (text[position] == '\r' && position + 1 < text.Length && text[position + 1] == '\n')
            {
                position += 2;
            }
            else
            {
                position++;
            }
        }

        return lines;
    }

    private void WriteLineCore(string text, bool indent)
    {
        if (_pendingStatementSeparator && ShouldInsertStatementSeparatorBefore(text))
        {
            _sb.Append(NewLine);
        }

        if (indent)
        {
            _sb.Append(' ', _indent * 4);
        }

        _sb.Append(text).Append(NewLine);
        _pendingStatementSeparator = IsStandaloneBlockTerminator(text);
    }

    private static bool ShouldInsertStatementSeparatorBefore(string text)
    {
        var trimmedText = text.TrimStart();
        if (trimmedText.Length == 0)
        {
            return false;
        }

        if (trimmedText[0] is '{' or '}' or ')' or ']' or ',' or ';' or '.')
        {
            return false;
        }

        return !trimmedText.StartsWith("else")
            && !trimmedText.StartsWith("catch")
            && !trimmedText.StartsWith("finally");
    }

    private static bool IsStandaloneBlockTerminator(string text)
    {
        return text.Trim() == "}";
    }

    private static int CountLeadingCloseBraces(string text)
    {
        var count = 0;
        while (count < text.Length && text[count] == '}')
        {
            count++;
        }

        return count;
    }

    private static int CountStructuralOpenBraces(string text)
    {
        var count = 0;
        foreach (var character in EnumerateCodeCharacters(text))
        {
            if (character == '{')
            {
                count++;
            }
        }

        return count;
    }

    private static int CountStructuralCloseBraces(string text)
    {
        var count = 0;
        foreach (var character in EnumerateCodeCharacters(text))
        {
            if (character == '}')
            {
                count++;
            }
        }

        return count;
    }

    private static IEnumerable<char> EnumerateCodeCharacters(string text)
    {
        var inString = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (inString)
            {
                if (character == '\\')
                {
                    index++;
                    continue;
                }

                if (character == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (character == '"')
            {
                inString = true;
                continue;
            }

            if (character == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                yield break;
            }

            yield return character;
        }
    }
}
