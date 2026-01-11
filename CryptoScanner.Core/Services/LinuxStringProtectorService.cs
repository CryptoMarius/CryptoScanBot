using System.Text;

using System.Security.Cryptography;

using Tmds.DBus;
using CryptoScanner.Core.Services.Linux;
using System.Runtime.Versioning;

namespace CryptoScanner.Core.Services;

[SupportedOSPlatform("linux")]
public class LinuxStringProtectorService : IStringProtectorService
{
    private const string ServiceName = "org.freedesktop.secrets";
    private static readonly ObjectPath ServicePath = new("/org/freedesktop/secrets");
    private static readonly ObjectPath DefaultCollectionPath = new("/org/freedesktop/secrets/collection/login");
    private const string KeyLabel = "CryptoScanner JsonEncryptionKey";

    // Synchronous API voor interface‑consistentie
    public string Protect(string plaintext)
    {
        var key = GetOrCreateKey().GetAwaiter().GetResult();

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        using var encryptor = aes.CreateEncryptor();
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Unprotect(string ciphertext)
    {
        var key = GetOrCreateKey().GetAwaiter().GetResult();
        var full = Convert.FromBase64String(ciphertext);

        using var aes = Aes.Create();
        aes.Key = key;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[full.Length - iv.Length];

        Buffer.BlockCopy(full, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(full, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private async Task<byte[]> GetOrCreateKey()
    {
        if (await TryReadKey() is { } keyBytes)
            return keyBytes;

        var newKey = RandomNumberGenerator.GetBytes(32);
        await SaveKey(newKey);
        return newKey;
    }

    private async Task<byte[]?> TryReadKey()
    {
        var connection = new Connection(Address.Session);
        await connection.ConnectAsync();

        var service = connection.CreateProxy<ISecretService>(ServiceName, ServicePath);

        var (sessionPath, _) = await service.OpenSessionAsync("plain", "");

        var collection = connection.CreateProxy<ISecretCollection>(ServiceName, DefaultCollectionPath);

        var searchAttrs = new Dictionary<string, string>
        {
            { "label", KeyLabel }
        };

        var (unlocked, locked) = await collection.SearchItemsAsync(searchAttrs);
        var all = unlocked.Length > 0 ? unlocked : locked;

        if (all.Length == 0)
            return null;

        var item = connection.CreateProxy<ISecretItem>(ServiceName, all[0]);
        var secret = await item.GetSecretAsync(sessionPath);

        return secret.Content;
    }

    private async Task SaveKey(byte[] key)
    {
        var connection = new Connection(Address.Session);
        await connection.ConnectAsync();

        var service = connection.CreateProxy<ISecretService>(ServiceName, ServicePath);
        var (sessionPath, _) = await service.OpenSessionAsync("plain", "");

        var secret = new Secret
        {
            Session = sessionPath,
            Content = key,
            ContentType = "application/octet-stream"
        };

        var props = new Dictionary<string, object>
        {
            { "org.freedesktop.Secret.Item.Label", KeyLabel },
            {
                "org.freedesktop.Secret.Item.Attributes",
                new Dictionary<string, string>
                {
                    { "label", KeyLabel }
                }
            }
        };

        await service.CreateItemAsync(
            DefaultCollectionPath,
            props,
            secret,
            replace: true);
    }
}
