using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Json;

public class SecureStringConverter : JsonConverter<string>
{
    private const string prefix = "DPAPI:";

    public static string Protect(string stringToEncrypt, string? optionalEntropy, DataProtectionScope scope)
    {
#if WINDOWS
        return Convert.ToBase64String(
            ProtectedData.Protect(
                Encoding.UTF8.GetBytes(stringToEncrypt)
                , optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null
                , scope));
#else
        return stringToEncrypt;
#endif
    }

    public static string Unprotect(string encryptedString, string? optionalEntropy, DataProtectionScope scope)
    {
#if WINDOWS
        return Encoding.UTF8.GetString(
            ProtectedData.Unprotect(
                Convert.FromBase64String(encryptedString)
                , optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null
                , scope));
#else
        return encryptedString;
#endif
    }

    public override bool HandleNull => true;


    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.String)
        {
            string? text = reader.GetString();
            if (!string.IsNullOrEmpty(text))
            {
#if WINDOWS
                if (text.StartsWith(prefix))
                {
                    return Unprotect(text[prefix.Length..], null, DataProtectionScope.LocalMachine);
                }
#else
                return text;
#endif
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
#if WINDOWS
            output = prefix + Protect(value, null, DataProtectionScope.LocalMachine);
#else
            output = value;
#endif
        }

        writer.WriteStringValue(output);
    }
}