using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoScanBot;

public class SecureStringConverter : JsonConverter<string>
{
    private const string prefix = "DPAPI:";

    public static string Protect(string stringToEncrypt, string? optionalEntropy, DataProtectionScope scope)
    {
        return Convert.ToBase64String(
            ProtectedData.Protect(
                Encoding.UTF8.GetBytes(stringToEncrypt)
                , optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null
                , scope));
    }
    public static string Unprotect(string encryptedString, string? optionalEntropy, DataProtectionScope scope)
    {
        return Encoding.UTF8.GetString(
            ProtectedData.Unprotect(
                Convert.FromBase64String(encryptedString)
                , optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null
                , scope));
    }


    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.String)
        {
            string text = reader.GetString();
            if (!string.IsNullOrEmpty(text))
            {
                if (text.StartsWith(prefix))
                {
                    return Unprotect(text[prefix.Length..], null, DataProtectionScope.LocalMachine);
                }
            }
        }

        return JsonSerializer.Deserialize<string>(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        string output;

        if (value == string.Empty)
        {
            output = string.Empty;
        }
        else if (value.EndsWith('='))
        {
            output = value;
        }
        else
        {
            output = prefix + Protect(value, null, DataProtectionScope.LocalMachine);
        }

        writer.WriteStringValue(output);
    }
}