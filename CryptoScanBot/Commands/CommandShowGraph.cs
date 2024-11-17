using CryptoScanBot.Core.Model;
using CryptoScanBot.ZoneVisualisation;

namespace CryptoScanBot.Commands;

public class CommandShowGraph : CommandBase
{
    public override void Execute(ToolStripMenuItemCommand item, object sender)
    {
        if (sender is CryptoSymbol symbol)
        {
            CryptoVisualisation dialog = new()
            {
                StartPosition = FormStartPosition.CenterParent
            };
            //ChangeTheme(Main.theme, dialog);
            dialog.StartWithSymbolAsync(symbol);

            //if (dialog.ShowDialog() != DialogResult.OK)
            //    return;

            Form parent = null;
            if (item.DataGrid != null)
                parent = item.DataGrid.Grid.FindForm();
            //var x = item.GetCurrentParent().Owner;
            //Form parent = x!.FindForm(item);
            dialog.Show(parent);
        }
    }

}
