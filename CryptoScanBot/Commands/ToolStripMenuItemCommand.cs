namespace CryptoScanBot.Commands;

public class ToolStripMenuItemCommand : ToolStripMenuItem
{
    public new Command Command { get; set; }
    public CryptoDataGrid? DataGrid { get; set; }
}
