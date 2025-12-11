namespace CryptoScanner.Services;

public interface IPlatformService
{
    string GetDataDirectory();
    Task<bool> OpenExternalApp(string appName);
    Task<bool> OpenFile(string filePath);
    string PlatformName { get; }
}