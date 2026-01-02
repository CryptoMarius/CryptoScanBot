using Avalonia.Controls;

using CryptoScanner.Core.Model;

namespace CryptoScanner.Commands;

public class CommandCopyDataCells : CommandBase
{
    public async Task Execute(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"Copying cells to clipboard");

        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.datagrid != null && dto.parentWindow != null)
        {
            string text = "Hello World";

            var clipboard = dto.parentWindow.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
            }
        }
    }

}
