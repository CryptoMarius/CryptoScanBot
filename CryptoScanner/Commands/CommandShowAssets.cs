using Avalonia.Controls;

using CryptoScanner.Views;

namespace CryptoScanner.Commands;

/// <summary>
/// Opens the paper-assets window: shows the balances and lets them be corrected or reset.
/// </summary>
public class CommandShowAssets : CommandBase
{
    public override void Execute(object? parameter)
    {
        AssetWindow window = new();
        if (parameter is Window owner)
            window.ShowDialog(owner);
        else
            window.Show();
    }
}
