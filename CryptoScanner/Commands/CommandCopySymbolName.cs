using Avalonia.Controls;

using CryptoScanner.Core.Model;

namespace CryptoScanner.Commands;

public class CommandCopySymbolName : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        //System.Diagnostics.Debug.WriteLine($"Copyy symbing cells to clipboard");

        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null && dto.parentWindow != null)
        {
            string text = dto.symbol.Name;

            var clipboard = dto.parentWindow?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
            }
        }
    }
}
