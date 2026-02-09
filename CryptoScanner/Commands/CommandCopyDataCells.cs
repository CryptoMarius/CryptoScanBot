namespace CryptoScanner.Commands;

public class CommandCopyDataCells : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"Copying cells to clipboard");

        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.listBox != null && dto.parentWindow != null)
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
