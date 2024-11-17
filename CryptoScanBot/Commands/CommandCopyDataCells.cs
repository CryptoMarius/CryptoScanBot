using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Commands;

public class CommandCopyDataCells : CommandBase
{
    public override void Execute(ToolStripMenuItemCommand item, object sender)
    {
        string text = "";
        if (sender is CryptoDataGrid grid)
        {
            if (grid.Grid.SelectedRows.Count > 0)
            {
                int rowIndex = grid.Grid.SelectedRows[0].Index;

                for (int columnIndex = 0; columnIndex < grid.Grid.ColumnCount - 1; columnIndex++)
                {
                    //var item = grid.Grid.Rows[rowIndex].Cells[columnIndex];
                    DataGridViewCellValueEventArgs e = new(columnIndex, rowIndex);
                    grid.GetTextFunction(sender, e);
                    text += e.Value?.ToString() + ";";
                }
                text += "\r\n";
            }
        }

        if (text == "")
            Clipboard.Clear();
        else
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
    }
}
