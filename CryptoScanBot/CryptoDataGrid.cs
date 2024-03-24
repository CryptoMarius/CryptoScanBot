using System.Collections;
using System.Runtime.InteropServices;
using CryptoScanBot.Commands;
using CryptoScanBot.Enums;
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
        if (!GlobalData.ApplicationIsClosing)
        {
            SendMessage(target.Handle, WM_SETREDRAW, 0, 0);
        }
    }

    public static void ResumeDrawing(this Control target) 
    { 
        ResumeDrawing(target, true); 
    }

    public static void ResumeDrawing(this Control target, bool redraw)
    {
        if (!GlobalData.ApplicationIsClosing)
        {
            SendMessage(target.Handle, WM_SETREDRAW, 1, 0);

            if (redraw)
            {
                target.Refresh();
            }
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
        int count = columnList.Count;

        InitializeGrid();
        InitializeHeaders();
        ShowSortIndicator();
        FillColumnPopup();
        if (columnList.Count != count)
            GlobalData.SaveUserSettings();
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
        DataGridViewTextBoxColumn c = new()
        {
            HeaderText = headerText,
            ValueType = type,
        };
        
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
        c.DefaultCellStyle.Format = format;
        c.DefaultCellStyle.Alignment = align;

        Grid.Columns.Add(c);
        return c;
    }

    private void InitializeGrid()
    {
        // https://stackoverflow.com/questions/35214250/c-sharp-using-icomparer-to-sort-x-number-of-columns

        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(Grid, true, null);

        //DefaultFont = new Font(Grid.DefaultCellStyle.Font.Name, Grid.DefaultCellStyle.Font.Size);
        //SelectedFont = new Font(Grid.DefaultCellStyle.Font.Name, Grid.DefaultCellStyle.Font.Size, FontStyle.Underline); // | FontStyle.Bold | FontStyle.Italic

        // https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/implementing-virtual-mode-wf-datagridview-control?view=netframeworkdesktop-4.8&redirectedfrom=MSDN
        Grid.ReadOnly = true;
        Grid.VirtualMode = true;
        
        Grid.RowHeadersWidth = 20;
        Grid.RowHeadersVisible = false; // the first column to select rows
        //Grid.RowHeadersDefaultCellStyle.BackColor = Grid.DefaultCellStyle.BackColor;

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

        Grid.GridColor = Color.Black; // VeryLightGray1;
        Grid.BackgroundColor = Grid.DefaultCellStyle.BackColor;
        
        Grid.BorderStyle = BorderStyle.None; // Fixed3D, FixedSingle; // Rand rond het grid
        Grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single; // Single;
        Grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single; // Raised;

        Grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        //Grid.DefaultCellStyle.SelectionBackColor = Grid.DefaultCellStyle.BackColor;
        //Grid.DefaultCellStyle.SelectionForeColor = Grid.DefaultCellStyle.ForeColor;

        Grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; // AllCellsExceptHeader; // AllCells; // DisplayedCells;
        Grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single; // Kader rond de header cells

        // Raised  geeft een mooi kadertje om iedere cell, maar dat is wel erg druk (instelbaar maken?)
        Grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single; // Kader rond de header cells
        Grid.CellBorderStyle = DataGridViewCellBorderStyle.Single; // .Single; // Sunken;

        Grid.CellFormatting += CellFormattingEvent;
        Grid.CellValueNeeded += new DataGridViewCellValueEventHandler(GetTextFunction);
        Grid.ColumnHeaderMouseClick += HeaderClick;
        Grid.DoubleClick += GridDoubleClick;
        Grid.MouseUp += ShowPopupMenu;

        // Commands
        Grid.ContextMenuStrip = MenuStripCells;
        MenuStripCells.Opening += MenuStripOpening;
        InitializeCommands(MenuStripCells);

    }

    internal void FillColumnPopup()
    {
        ToolStripMenuItem item = new()
        {
            Text = "Change columns",
            Size = new Size(100, 22),
        };
        item.Click += CheckColumn;
        MenuStripHeader.Items.Add(item);



        int count = 0;
        foreach (DataGridViewColumn column in Grid.Columns)
        {
            if (!ColumnList.TryGetValue(column.HeaderText, out ColumnSetting columnSetting))
            {
                columnSetting = new();
                ColumnList.Add(column.HeaderText, columnSetting);
                columnSetting.Visible = column.Visible;
            }

            column.Visible = columnSetting.Visible;
            if (columnSetting.Width > 0)
                column.Width = columnSetting.Width;
            else
                columnSetting.Width = column.Width;

            column.ContextMenuStrip = MenuStripHeader;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            if (column.Visible)
                count++;
        }

        // Restore a column (otherwise the complete header is gone)
        if (count == 0)
        {
            Grid.Columns[0].Visible = true;
            ColumnList.Values[0].Visible = true;
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
                // adjust the selection
                var info = Grid.HitTest(e.X, e.Y);
                if (info.RowIndex >= 0)
                    Grid.Rows[info.RowIndex].Selected = true;
                else
                    return;
                break;
        }

        if (cms != null)
            cms.Show(dgv, e.Location);
    }

    private void CheckColumn(object sender, EventArgs e)
    {
        // tijdelijk, debug
        CryptoDataGridColumns f = new();
        f.AddColumns(this);
        f.ShowDialog();

        if (f.DialogResult == DialogResult.OK)
        {
            // iets..
            foreach (DataGridViewColumn column in Grid.Columns)
            {
                if (ColumnList.TryGetValue(column.HeaderText, out ColumnSetting columnSetting))
                {
                    column.Visible = columnSetting.Visible;
                }
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

    internal void ClearSelection(object sender, EventArgs e)
    {
        Grid.ClearSelection();
    }

    public void MenuStripOpening(object sender, EventArgs e)
    {
        if (Grid.SelectedRows.Count > 0)
        {
            SelectedObjectIndex = Grid.SelectedRows[0].Index;
            SelectedObject = List[SelectedObjectIndex];
        }
        else
        {
            SelectedObjectIndex = -1;
            SelectedObject = null;
        }
    }

    public void GridDoubleClick(object sender, EventArgs e)
    {
        var point = Grid.PointToClient(Cursor.Position);
        var info = Grid.HitTest(point.X, point.Y);
        if (info.RowIndex >= 0)
        {
            MenuStripOpening(sender, e);
            MenuStripCells.Items[0].PerformClick();
        }
    }


    public virtual void AddObject(List<T> range)
    {
        List.AddRange(range);
        SortFunction();

        Grid.Invoke((MethodInvoker)(() => { Grid.RowCount = List.Count; Grid.Invalidate(); }));
    }

    private void HeaderClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        int column = e.ColumnIndex;
        if (SortColumn >= 0)
        {
            if (SortColumn == column && SortOrder == SortOrder.Ascending)
                SortOrder = SortOrder.Descending; // same column, reverse
            else
            {
                // Sort on new column and remove the old SortGlyph
                SortOrder = SortOrder.Ascending;
                // Sort indicator not always visible because of column width, make it bold instead
                //clumn.HeaderCell.SortGlyphDirection = SortOrder == SortOrder.Ascending ? SortOrder.Ascending : SortOrder.Descending;
                Grid.Columns[SortColumn].HeaderCell.Style.Font = new(Grid.ColumnHeadersDefaultCellStyle.Font.Name, Grid.ColumnHeadersDefaultCellStyle.Font.Size);
            }
        }
        else
            SortOrder = SortOrder.Ascending; // new column, sort
        SortColumn = column;

        ShowSortIndicator();
        SortFunction();
        Grid.Invalidate();
    }

    public void ShowSortIndicator()
    {
        int column = SortColumn;
        DataGridViewColumn newSortColumn = Grid.Columns[column];
        // Sort indicator not always visible because of column width, make it bold instead
        //newSortColumn.HeaderCell.SortGlyphDirection = SortOrder == SortOrder.Ascending ? SortOrder.Ascending : SortOrder.Descending;
        newSortColumn.HeaderCell.Style.Font = new(Grid.ColumnHeadersDefaultCellStyle.Font.Name, Grid.ColumnHeadersDefaultCellStyle.Font.Size, FontStyle.Bold);
    }

    public void AdjustRowHeight()
    {
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

        // https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/how-to-obtain-font-metrics?view=netframeworkdesktop-4.8
        var font = new Font(GlobalData.Settings.General.FontNameNew, GlobalData.Settings.General.FontSizeNew);
        var fontFamily = font.FontFamily;
        float lineSpacing = fontFamily.GetLineSpacing(FontStyle.Regular);
        var lineSpacingPixel = font.Size * lineSpacing / fontFamily.GetEmHeight(FontStyle.Regular);
        Grid.RowTemplate.Height = 8 + (int)Math.Round(lineSpacingPixel, 1, MidpointRounding.AwayFromZero);
    }

    internal static Color GetBackgroudColorForStrategy(CryptoSignalStrategy strategy, CryptoTradeSide side)
    {
        // Dit kan optimaler door het van te voren te indexeren

        switch (strategy)
        {
            case CryptoSignalStrategy.Jump:
                if (side == CryptoTradeSide.Long)
                    return GlobalData.Settings.Signal.Jump.ColorLong;
                else if (side == CryptoTradeSide.Short)
                    return GlobalData.Settings.Signal.Jump.ColorShort;
                break;

            case CryptoSignalStrategy.Stobb:
                if (side == CryptoTradeSide.Long)
                    return GlobalData.Settings.Signal.Stobb.ColorLong;
                else if (side == CryptoTradeSide.Short)
                    return GlobalData.Settings.Signal.Stobb.ColorShort;
                break;

            case CryptoSignalStrategy.Sbm1:
            case CryptoSignalStrategy.Sbm2:
            case CryptoSignalStrategy.Sbm3:
            case CryptoSignalStrategy.Sbm4:
                if (side == CryptoTradeSide.Long)
                    return GlobalData.Settings.Signal.Sbm.ColorLong;
                else if (side == CryptoTradeSide.Short)
                    return GlobalData.Settings.Signal.Sbm.ColorShort;
                break;

            case CryptoSignalStrategy.StoRsi:
                if (side == CryptoTradeSide.Long)
                    return GlobalData.Settings.Signal.StoRsi.ColorLong;
                else if (side == CryptoTradeSide.Short)
                    return GlobalData.Settings.Signal.StoRsi.ColorShort;
                break;
        }

        return Color.White;
    }

}
