using Avalonia.Controls;

using CryptoScanner.Views;

namespace CryptoScanner.Commands;

public class CommandShowAbout : CommandBase
{
    public override void Execute(object? parameter)
    {
        if (parameter is not Window parentWindow)
            return;

        var dialog = new AboutWindow
        {
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        dialog.Show(parentWindow);
    }
}
