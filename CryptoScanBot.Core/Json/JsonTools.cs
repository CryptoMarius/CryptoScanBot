using System.Text.Encodings.Web;
using System.Text.Json;

namespace CryptoScanBot.Core.Json;

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
    };

    public static readonly JsonSerializerOptions JsonSerializerNotIndented = new()
    {
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IncludeFields = true,
        IgnoreReadOnlyFields = true,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public static readonly JsonSerializerOptions DeSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IncludeFields = true,
        IgnoreReadOnlyFields = true,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };
}
