using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Commands;

public class CommandCopySymbolInfo : CommandBase
{
    public override string CommandName()
        => "Copy";

    public override void Execute(object sender)
    {
        string text = "";
        if (sender is CryptoSymbol symbol)
            text = symbol.Name;

        if (text == "")
            Clipboard.Clear();
        else
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
    }
}
