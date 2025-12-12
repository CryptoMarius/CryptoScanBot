using System.Text.Json;

namespace CryptoScanner.Services;

public interface IJsonSerializerService
{
    JsonSerializerOptions DefaultOptions { get; }
    JsonSerializerOptions IndentedOptions { get; }
}
