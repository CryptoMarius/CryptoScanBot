using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CryptoScanner.Core.Json;

public class JsonTools
{
    public static readonly JsonSerializerOptions JsonSerializerIndented = new()
    {
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IncludeFields = true,
        IgnoreReadOnlyFields = true,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new ColorConverter()}
    };

    public static readonly JsonSerializerOptions JsonSerializerNotIndented = new()
    {
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IncludeFields = true,
        IgnoreReadOnlyFields = true,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        Converters = { new ColorConverter() }
    };

    public static readonly JsonSerializerOptions DeSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IncludeFields = true,
        IgnoreReadOnlyFields = true,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new ColorConverter() }
    };


    private const string INDENT_STRING = "  ";

    public static string FormatJson(string text)
    {
        var indent = 0;
        var quoted = false;
        var sb = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            switch (ch)
            {
                case '{':
                case '[':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.AppendLine();
                        Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                    }
                    break;
                case '}':
                case ']':
                    if (!quoted)
                    {
                        sb.AppendLine();
                        Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                    }
                    sb.Append(ch);
                    break;
                case '"':
                    sb.Append(ch);
                    bool escaped = false;
                    var index = i;
                    while (index > 0 && text[--index] == '\\')
                        escaped = !escaped;
                    if (!escaped)
                        quoted = !quoted;
                    break;
                case ',':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.AppendLine();
                        Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                    }
                    break;
                case ':':
                    sb.Append(ch);
                    if (!quoted)
                        sb.Append(" ");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }
        return sb.ToString();
    }
}

static class Extensions
{
    public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
    {
        foreach (var i in ie)
        {
            action(i);
        }
    }
}