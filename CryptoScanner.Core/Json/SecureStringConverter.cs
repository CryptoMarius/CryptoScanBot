using System.Text.Json;
using System.Text.Json.Serialization;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

namespace CryptoScanner.Core.Json;


public class SecureStringConverter : JsonConverter<string>
{
    private const string prefix = "DPAPI:";
    private readonly IStringProtectorService _stringProtectorService;

    public SecureStringConverter()
    {
        _stringProtectorService = GlobalData.GetService<IStringProtectorService>()
            ?? throw new InvalidOperationException("IStringProtectorService not registered");
    }


    public override bool HandleNull => true;


    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.String)
        {
            string? text = reader.GetString();
            if (!string.IsNullOrEmpty(text))
            {
                if (text.StartsWith(prefix, StringComparison.Ordinal)) 
                { 
                    var payload = text[prefix.Length..]; 
                    return _stringProtectorService.Unprotect(payload); 
                } 
                
                // It was not encrypted
                return text;
            }
        }

        return JsonSerializer.Deserialize<string>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        string output;

        if (value == string.Empty)
        {
            output = string.Empty;
        }
        else if (value.StartsWith(prefix))
        {
            output = value;
        }
        else
        {
            output = prefix + _stringProtectorService.Protect(value); 
        }

        writer.WriteStringValue(output);
    }
}