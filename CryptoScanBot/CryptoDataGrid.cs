using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using CryptoScanBot.Commands;
using CryptoScanBot.Intern;
using CryptoScanBot.Settings;

namespace CryptoScanBot;

// Virtual DataGrid Base for displaying objects (symbol, signal, positions)

public static class ControlHelper
{
    [DllImport("user32.dll", EntryPoint = "SendMessageA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
    private const int WM_SETREDRAW = 0xB;


    public static void SuspendDrawing(this Control target)
    {
        SendMessage(target.Handle, WM_SETREDRAW, 0, 0);
    }

    public static void ResumeDrawing(this Control target) { ResumeDrawing(target, true); }
    public static void ResumeDrawing(this Control target, bool redraw)
    {
        SendMessage(target.Handle, WM_SETREDRAW, 1, 0);

        if (redraw)
        {
            target.Refresh();
        }
    }
}

// extra basis class omdat we anders niet bij de Selected kunnen komen
// (de compiler doet wat moeilijk over die generics)

public class CryptoDataGrid
{
    // references
    internal DataGridView Grid;
    internal Object SelectedObject;
    internal int SelectedObjectIndex;
    internal SortedList<string, ColumnSetting> ColumnList;
}


public abstract class CryptoDataGrid<T>: CryptoDataGrid
{
    internal readonly List<T> List;
    internal ContextMenuStrip MenuStripCells = new();
    internal ContextMenuStrip MenuStripHeader = new();
    //public (T Selected, int RowIndex) MySelection = new();

    // sorting
    internal int SortColumn = 0;
    internal SortOrder SortOrder = SortOrder.None;
    internal CaseInsensitiveComparer ObjectCompare = new();

    // background colors
    internal Color VeryLightGray1 = Color.FromArgb(0xf1, 0xf1, 0xf1);
    internal Color VeryLightGray2 = Color.FromArgb(0xa1, 0xa1, 0xa1);


    public CryptoDataGrid(DataGridView grid, List<T> list, SortedList<string, ColumnSetting> columnList)
    {
        Grid = grid;
        List = list;
        ColumnList = columnList;

        InitializeDataGrid();
        InitializeHeaders();
        ShowSortIndicator();
        FillColumnPopup();
    }

    public abstract void InitializeHeaders();
    public abstract void InitializeCommands(ContextMenuStrip menuStrip);
    public abstract void GetTextFunction(object sender, DataGridViewCellValueEventArgs e);
    public abstract void CellFormattingEvent(object sender, DataGridViewCellFormattingEventArgs e);
    public abstract void SortFunction();

    internal T GetSelectedObject(out int rowIndex)
    {
        rowIndex = SelectedObjectIndex;
        return (T)SelectedObject;
    }

    internal T GetCellObject(int rowIndex)
    {
        if (rowIndex >= 0 && rowIndex < List.Count)
            return List[rowIndex];
        else
            return default;
    }

    public void AdjustObjectCount()
    {
        Grid.RowCount = List.Count;
    }

    internal DataGridViewTextBoxColumn CreateColumn(string headerText, Type type, string format, DataGridViewContentAlignment align, int width = 0)
    {
        DataGridViewTextBoxColumn c = new();
        c.HeaderText = headerText;
        c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        if (width > 0)
        {
            c.Width = width;
            c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // NotSet; // AllCellsExceptHeader; // AllCells; //
        }
        else
        {
            c.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader; // AllCells; // NotSet; // AllCellsExceptHeader; // AllCells; //
            c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // NotSet; // AllCellsExceptHeader; // AllCells; //
        }
        c.ValueType = type;
        c.DefaultCellStyle.Format = format;
        c.DefaultCellStyle.Alignment = align;

        Grid.Columns.Add(c);
        return c;
    }

    private void InitializeDataGrid()
    {
        // https://stackoverflow.com/questions/35214250/c-sharp-using-icomparer-to-sort-x-number-of-columns

        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(Grid, true, null);

        //DefaultFont = new Font(Grid.DefaultCellStyle.Font.Name, Grid.DefaultCellStyle.Font.Size);
        //SelectedFont = new Font(Grid.DefaultCellStyle.Font.Name, Grid.DefaultCellStyle.Font.Size, FontStyle.Underline); // | FontStyle.Bold | FontStyle.Italic

        // https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/implementing-virtual-mode-wf-datagridview-control?view=netframeworkdesktop-4.8&redirectedfrom=MSDN
        Grid.ReadOnly = true;
        Grid.VirtualMode = true;
        
        Grid.RowHeadersWidth = 20;
        Grid.RowHeadersVisible = true; // the first column to select rows
        Grid.RowHeadersDefaultCellStyle.BackColor = Grid.DefaultCellStyle.BackColor;

        Grid.AllowUserToAddRows = false;
        Grid.AutoGenerateColumns = false;
        Grid.MultiSelect = false;
        Grid.AllowUserToResizeRows = false;
        Grid.AllowUserToResizeColumns = true;
        Grid.AllowUserToOrderColumns = true;

        // Hide header cell highlight stuff
        Grid.EnableHeadersVisualStyles = false;
        Grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Grid.ColumnHeadersDefaultCellStyle.BackColor;
        Grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Grid.ColumnHeadersDefaultCellStyle.ForeColor;

        Grid.GridColor = VeryLightGray1;
        Grid.BackgroundColor = Grid.DefaultCellStyle.BackColor;
        Grid.BorderStyle = BorderStyle.None; // Fixed3D, FixedSingle;
        Grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
        Grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single; // Single;
        Grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single; // Raised;

        Grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        Grid.DefaultCellStyle.SelectionBackColor = Grid.DefaultCellStyle.BackColor;
        Grid.DefaultCellStyle.SelectionForeColor = Grid.DefaultCellStyle.ForeColor;

        //Grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.Fill; // DisplayedCells; ?
        Grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; // AllCellsExceptHeader; // AllCells; // DisplayedCells;
        Grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        Grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;

        Grid.CellFormatting += CellFormattingEvent;
        Grid.CellValueNeeded += new DataGridViewCellValueEventHandler(GetTextFunction);
        Grid.ColumnHeaderMouseClick += HeaderClick;
        Grid.DoubleClick += GridDoubleClick;
        Grid.SelectionChanged += ClearSelection;
        Grid.MouseUp += ShowPopupMenu;

        // Commands
        Grid.ContextMenuStrip = MenuStripCells;
        MenuStripCells.Opening += MenuStripOpening;
        InitializeCommands(MenuStripCells);

        // Bind to a textbox
        //txtModel.DataBindings.Add("Text", bs, "Model");

        //if (cell.RowIndex == Grid.CurrentCell.RowIndex)
        //    cell.Style.Font = SelectedFont;
        //else
        //    cell.Style.Font = DefaultFont;

        // Make the font italic for row four.
        //Grid.Columns[4].DefaultCellStyle.Font = new Font(DataGridView.DefaultFont, FontStyle.Italic);
    }

    internal void FillColumnPopup()
    {
        foreach (DataGridViewColumn column in Grid.Columns)
        {
            if (column.HeaderText != "")
            {
                if (!ColumnList.TryGetValue(column.HeaderText, out ColumnSetting columnSetting))
                {
                    columnSetting = new();
                    ColumnList.Add(column.HeaderText, columnSetting);
                }

                ToolStripMenuItem item = new()
                {
                    Tag = column,
                    Text = column.HeaderText,
                    Size = new Size(100, 22),
                    CheckState = CheckState.Unchecked,
                    Checked = columnSetting.Visible,
                };
                item.Click += CheckColumn;
                MenuStripHeader.Items.Add(item);

                column.Visible = columnSetting.Visible;
                if (columnSetting.Width > 0)
                    column.Width = columnSetting.Width;
                else
                    columnSetting.Width = column.Width;


                column.ContextMenuStrip = MenuStripHeader;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }
        }
        Grid.ColumnWidthChanged += ColumnWidthChanged;
    }

    private void ShowPopupMenu(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
            return;

        var dgv = (DataGridView)sender;
        ContextMenuStrip cms = null;
        var hit = dgv.HitTest(e.X, e.Y);
        switch (hit.Type)
        {
            case DataGridViewHitTestType.ColumnHeader:
                cms = MenuStripHeader;
                break;
            case DataGridViewHitTestType.Cell:
                cms = MenuStripCells;
                break;
        }
        if (cms != null)
            cms.Show(dgv, e.Location);
    }

    private void CheckColumn(object sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            item.Checked = !item.Checked;
            if (item.Tag is DataGridViewTextBoxColumn column)
            {
                if (!ColumnList.TryGetValue(column.HeaderText, out ColumnSetting columnSetting))
                {
                    columnSetting = new();
                    ColumnList.Add(column.HeaderText, columnSetting);
                }
                column.Visible = item.Checked;

                columnSetting.Width = column.Width;
                columnSetting.Visible = column.Visible;
            }

            GlobalData.SaveUserSettings();
            Grid.Invoke((MethodInvoker)(() => { Grid.Invalidate(); }));
        }
    }

    private void ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
    {
        foreach (DataGridViewColumn column in Grid.Columns)
        {
            if (column.HeaderText != "")
            {
                if (!ColumnList.TryGetValue(column.HeaderText, out ColumnSetting columnSetting))
                {
                    columnSetting = new();
                    ColumnList.Add(column.HeaderText, columnSetting);
                }
                columnSetting.Width = column.Width;
                columnSetting.Visible = column.Visible;
            }
        }

        GlobalData.SaveUserSettings();
    }

    public void ClearSelection(object sender, EventArgs e)
    {
        Grid.ClearSelection();
    }

    public void MenuStripOpening(object sender, EventArgs e)
    {
        var point = Grid.PointToClient(Cursor.Position);
        var info = Grid.HitTest(point.X, point.Y);
        if (info.RowIndex >= 0)
        {
            SelectedObjectIndex = info.RowIndex;
            SelectedObject = List[SelectedObjectIndex];
            //Grid.Rows[SelectedObjectIndex].Selected = true;
            //Grid.ClearSelection();
            Grid.CurrentCell = Grid[0, info.RowIndex];
        }
        else
        {
            SelectedObjectIndex = -1;
            SelectedObject = null;
            //Grid.ClearSelection();
        }
    }

    public void GridDoubleClick(object sender, EventArgs e)
    {
        MenuStripOpening(sender, e);
        MenuStripCells.Items[0].PerformClick();
    }

    //public virtual void AddObject(T signal)
    //{
    //    List.Add(signal);
    //    SortFunction();

    //    Grid.Invoke((MethodInvoker)(() => { Grid.RowCount = List.Count; Grid.Invalidate(); }));
    //}

    public virtual void AddObject(List<T> range)
    {
        List.AddRange(range);
        SortFunction();

        Grid.Invoke((MethodInvoker)(() => { Grid.RowCount = List.Count; Grid.Invalidate(); }));
    }

    private void HeaderClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        int column = e.ColumnIndex;
        //DataGridViewColumn newSortColumn = Grid.Columns[column];

        //ListSortDirection direction;
        if (SortColumn >= 0)
        {
            if (SortColumn == column && SortOrder == SortOrder.Ascending)
            {
                // Sort the same column again, reversing the SortOrder.
                SortOrder = SortOrder.Descending;
                //direction = ListSortDirection.Descending;
            }
            else
            {
                // Sort on new column and remove the old SortGlyph
                //direction = ListSortDirection.Ascending;
                SortOrder = SortOrder.Ascending;
                // Sort indicator niet altijd zichtbaar vanwege breedte kolom
                //Grid.Columns[SortColumn].HeaderCell.SortGlyphDirection = SortOrder.None;
                Grid.Columns[SortColumn].HeaderCell.Style.Font = new(Grid.ColumnHeadersDefaultCellStyle.Font.Name, Grid.ColumnHeadersDefaultCellStyle.Font.Size);
            }
        }
        else
        {
            // Sort on new column and remove the old SortGlyph
            //direction = ListSortDirection.Ascending;
            SortOrder = SortOrder.Ascending;
        }
        SortColumn = column;

        ShowSortIndicator();
        SortFunction();
        Grid.Invalidate();
    }

    public void ShowSortIndicator()
    {
        int column = SortColumn;
        DataGridViewColumn newSortColumn = Grid.Columns[column];
        // Sort indicator niet altijd zichtbaar vanwege breedte kolom
        //newSortColumn.HeaderCell.SortGlyphDirection = SortOrder == SortOrder.Ascending ? SortOrder.Ascending : SortOrder.Descending;
        newSortColumn.HeaderCell.Style.Font = new(Grid.ColumnHeadersDefaultCellStyle.Font.Name, Grid.ColumnHeadersDefaultCellStyle.Font.Size, FontStyle.Bold);
    }

    public void ApplySorting()
    {
        ShowSortIndicator();
        SortFunction();
        Grid.Invalidate();
    }


    public void InitCommandCaptions()
    {
        string text = GlobalData.ExternalUrls.GetTradingAppName(GlobalData.Settings.General.TradingApp, GlobalData.Settings.General.ExchangeName);
        Grid.ContextMenuStrip.Items[0].Text = text;
    }


    internal void AddStandardSymbolCommands(ContextMenuStrip menuStrip, bool isSignal)
    {
        ToolStripMenuItemCommand menuCommand;

        menuCommand = new();
        menuCommand.DataGrid = this;
        menuCommand.Text = "Activate trading app";
        menuCommand.Command = Command.ActivateTradingApp;
        menuCommand.Click += CommandTools.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new();
        menuCommand.DataGrid = this;
        menuCommand.Text = "TradingView browser";
        menuCommand.Command = Command.ActivateTradingviewIntern;
        menuCommand.Click += CommandTools.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new();
        menuCommand.Text = "TradingView extern";
        menuCommand.Command = Command.ActivateTradingviewExtern;
        menuCommand.Click += CommandTools.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);


        menuStrip.Items.Add(new ToolStripSeparator());

        menuCommand = new();
        menuCommand.DataGrid = this;
        menuCommand.Text = "Kopiëer informatie";
        if (isSignal)
            menuCommand.Command = Command.CopySignalInformation;
        else
            menuCommand.Command = Command.CopySymbolInformation;
        menuCommand.Click += CommandTools.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new();
        menuCommand.DataGrid = this;
        menuCommand.Text = "Trend informatie (zie log)";
        menuCommand.Command = Command.ShowTrendInformation;
        menuCommand.Click += CommandTools.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new();
        menuCommand.DataGrid = this;
        menuCommand.Text = "Symbol informatie (Excel)";
        menuCommand.Command = Command.ExcelSymbolInformation;
        menuCommand.Click += CommandTools.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);
    }

}
