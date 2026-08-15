using CryptoScanner.Core.Helpers;

namespace CryptoScanner.Commands;

public class CommandOpenWiki : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Open via the external (system) browser, github refuses to be loaded inside a frame
        ExternalLinkHelper.OpenSystemBrowser("https://github.com/CryptoMarius/CryptoScanBot/wiki");
    }
}
