using System.Text.Json;

namespace CryptoScanner.Core.Services;

public interface IJsonSerializerService
{
    JsonSerializerOptions DefaultOptions { get; }
    JsonSerializerOptions IndentedOptions { get; }
}
