using CryptoScanBot.Model;

namespace CryptoScanBot.Commands;

public class CommandCopySymbolInfo : CommandBase
{
    public override string CommandName()
        => "Copy";

    public override void Execute(object sender)
    {
        string text = "";

        if (sender is CryptoSymbol symbol)
        {
            Clipboard.SetText(symbol.Name, TextDataFormat.UnicodeText);
        }

        if (text == "")
            Clipboard.Clear();
        else
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
    }
}
