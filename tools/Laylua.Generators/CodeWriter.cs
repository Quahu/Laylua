using System.Text;

namespace Laylua.Generators;

/// <summary>
///     Wraps StringBuilder with automatic indentation tracking for code generation.
/// </summary>
internal sealed class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;

    public void Line(string text)
    {
        _sb.Append(' ', _indent * 4).AppendLine(text);
    }

    public void Line()
    {
        _sb.AppendLine();
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
        _sb.AppendLine($"#pragma warning disable {warningId}");
    }

    public void RestoreWarning(string warningId)
    {
        _sb.AppendLine($"#pragma warning restore {warningId}");
    }

    public override string ToString()
    {
        return _sb.ToString();
    }
}
