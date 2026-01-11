namespace CryptoScanner.Core.Services;

public interface IStringProtectorService
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}
