namespace CryptoScanner.Commands;

public class CommandLogCopyText : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        //System.Diagnostics.Debug.WriteLine($"Copyy symbing cells to clipboard");

        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.LogViewModel != null && dto.parentWindow != null)
        {
            string text = dto.LogViewModel.Text;

            var clipboard = dto.parentWindow?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
            }
        }
    }
}
