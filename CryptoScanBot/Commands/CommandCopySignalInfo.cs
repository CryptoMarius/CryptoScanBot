namespace CryptoScanBot.Commands;

public class CommandCopySignalInfo : CommandBase
{
    public override string CommandName()
        => "Copy information";

    public override void Execute(object sender)
    {
        string text = "";
        // Victum of the grid change
        //if (sender is DataGridView listview)
        //{
        //    if (listview.SelectedRows.Count > 0)
        //    {
        //        int rowIndex = listview.SelectedRows[0].Index;

        //        for (int index = 0; index < listview.ColumnCount - 1; index++)
        //        {
        //            var item = listview.Rows[rowIndex].Cells[index];

        //            text += item.Text + ";";
        //            foreach (ListViewItem.ListViewSubItem i in item.SubItems)
        //            {
        //                text += i.Text + ";";
        //            }
        //            text += "\r\n";

        //        }
        //    }
        //}
        if (text == "")
            Clipboard.Clear();
        else
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
    }
}
