namespace CryptoScanBot.Commands;

public class CommandCopySignalInfo : CommandBase
{
    public override string CommandName()
        => "Copy";

    public override void Execute(object sender)
    {
        string text = "";
        if (sender is ListView listview)
        {
            if (listview.SelectedItems.Count > 0)
            {
                for (int index = 0; index < listview.SelectedItems.Count; index++)
                {
                    ListViewItem item = listview.SelectedItems[index];

                    text += item.Text + ";";
                    foreach (ListViewItem.ListViewSubItem i in item.SubItems)
                    {
                        text += i.Text + ";";
                    }
                    text += "\r\n";

                }
            }
        }
        if (text == "")
            Clipboard.Clear();
        else
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
    }
}
