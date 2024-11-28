using System.Text.Encodings.Web;
using System.Text.Json;

namespace CryptoScanBot.Core.Json;

public class JsonTools
{
    public static readonly JsonSerializerOptions JsonSerializerIndented = new()
    { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true, IncludeFields = true };

    public static readonly JsonSerializerOptions JsonSerializerNotIndented = new()
    { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false };

}
