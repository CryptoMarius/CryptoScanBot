namespace CryptoScanBot.Commands;

public static class CommandHelper
{

    public static void AddSeperator(this ContextMenuStrip menuStrip) => menuStrip.Items.Add(new ToolStripSeparator());

    public static void AddSeperator(this ToolStripMenuItem menuStrip) => menuStrip.DropDownItems.Add(new ToolStripSeparator());

    public static ToolStripMenuItemCommand AddCommand(this ContextMenuStrip menuStrip, CryptoDataGrid? dataGrid, string text, Command command, EventHandler? click = null)
    {
        ToolStripMenuItemCommand menuItem = new()
        {
            Command = command,
            DataGrid = dataGrid,
            Text = text
        };
        if (click == null)
            menuItem.Click += click ?? CommandTools.ExecuteCommand;
        else 
            menuItem.Click += click;
        menuStrip.Items.Add(menuItem);
        return menuItem;
    }

    public static ToolStripMenuItemCommand AddCommand(this ToolStripMenuItem menuStrip, CryptoDataGrid? dataGrid, string text, Command command, EventHandler? click = null)
    {
        ToolStripMenuItemCommand menuItem = new()
        {
            Command = command,
            DataGrid = dataGrid,
            Text = text            
        };
        menuItem.Click += click ?? CommandTools.ExecuteCommand;        
        menuStrip.DropDownItems.Add(menuItem);
        return menuItem;
    }
}
