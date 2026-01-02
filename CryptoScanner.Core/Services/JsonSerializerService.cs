using System.Text.Json;

namespace CryptoScanner.Services;

public class JsonSerializerService : IJsonSerializerService
{
    public JsonSerializerOptions DefaultOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JsonSerializerOptions IndentedOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
}