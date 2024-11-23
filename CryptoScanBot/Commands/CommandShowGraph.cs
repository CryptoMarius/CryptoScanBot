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


            Form? parent = null;
            if (item.DataGrid != null && item.DataGrid.Grid != null)
                parent = item.DataGrid.Grid.FindForm();
            if (parent != null)
            {
                dialog.StartPosition = FormStartPosition.Manual;
                dialog.Location = new Point(
                    (parent.Location.X + parent.Width / 2) - (dialog.Width / 2),
                    (parent.Location.Y + parent.Height / 2) - (dialog.Height / 2));
            }
            dialog.Show();
        }
    }

}
