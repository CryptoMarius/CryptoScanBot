using CryptoScanBot.Core.Model;
using CryptoScanBot.ZoneVisualisation;

namespace CryptoScanBot.Commands;

public class CommandShowGraph : CommandBase
{
    private static CryptoVisualisation? CryptoVisualisationForm = null;

    public override void Execute(ToolStripMenuItemCommand item, object? sender)
    {
        if (sender != null)
        {
            CryptoVisualisation? dialog = null;
            try
            {
                if (CryptoVisualisationForm != null && !CryptoVisualisationForm.IsDisposed)
                    dialog = CryptoVisualisationForm;
            }
            catch (ObjectDisposedException)
            {
                CryptoVisualisationForm = null;
            }

            dialog ??= new()
            {
                StartPosition = FormStartPosition.CenterParent
            };

            dialog.StartWithSymbolAsync(sender);
            dialog.Show();
            if (CryptoVisualisationForm != null)
                dialog.BringToFront();

            CryptoVisualisationForm = dialog;
        }
    }

}
