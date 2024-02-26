using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;

using CryptoSbmScanner.Intern;

namespace CryptoSbmScanner;

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

public class CryptoDataGrid<T>
{
    internal DataGridView Grid;
    internal readonly List<T> List;
    internal ContextMenuStrip MenuStrip = new();

    // sorting
    internal int SortColumn = 0;
    internal SortOrder SortOrder = SortOrder.None;
    internal CaseInsensitiveComparer ObjectCompare = new();

    // background color
    internal Color VeryLightGray1 = Color.FromArgb(0xf1, 0xf1, 0xf1);
    internal Color VeryLightGray2 = Color.FromArgb(0xa1, 0xa1, 0xa1);


    public CryptoDataGrid(DataGridView grid, List<T> list)
    {
        Grid = grid;
        List = list;

        Grid.Tag = this; // dirty to acces the list
        Grid.DoubleClick += Commands.ExecuteCommandCommandViaTag;

        // Grid
        InitializeDataGrid();
        InitializeHeaders();

        // Commands
        InitializeCommands(MenuStrip);
        Grid.ContextMenuStrip = MenuStrip;
    }

    internal T GetSelectedObject(out int rowIndex)
    {
        rowIndex = -1;
        if (Grid.SelectedRows.Count > 0)
        {
            rowIndex = Grid.SelectedRows[0].Index;
            if (rowIndex >= 0 && rowIndex < List.Count)
            {
                return List[rowIndex];
            }
        }

        return default;
    }

    internal T GetCellObject(int rowIndex)
    {
        if (rowIndex >= 0 && rowIndex < List.Count)
        {
            return List[rowIndex];
        }

        return default;
    }
    public void AdjustObjectCount()
    {
        Grid.RowCount = List.Count;
    }

    internal void CreateColumn(string headerText, Type type, string format, DataGridViewContentAlignment align, int width = 0) 
        //, { Compare(T A, T B)} compare? add sort? add color? add text? whatever..
    {
        //DataGridViewColumn c;
        DataGridViewTextBoxColumn c;

        c = new();
        //c.Name = name;
        c.HeaderText = headerText;
        if (width > 0)
        {
            c.Width = width;
            c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // NotSet; // AllCellsExceptHeader; // AllCells; //
        }
        else
        {
            c.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells; // NotSet; // AllCellsExceptHeader; // AllCells; //
            c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // NotSet; // AllCellsExceptHeader; // AllCells; //
        }
        //c.Tag = Grid.ColumnCount;
        //c.ValueType = GetType(decimal);
        Grid.Columns.Add(c);
        c.ValueType = type;
        //c.DefaultCellStyle
        //if (type == typeof(decimal))
        c.DefaultCellStyle.Format = format;
        c.DefaultCellStyle.Alignment = align;

        //return c;
    }

    public virtual void InitializeCommands(ContextMenuStrip menuStrip)
    {
        // Context menu
    }

    public virtual void InitializeHeaders()
    {
        // Column headers
    }
    
    public virtual void SortFunction()
    {
        // Sort the list
    }
    
    public virtual void GetTextFunction(object sender, DataGridViewCellValueEventArgs e)
    {
        // Get the cell text
    }

    public virtual void RowSetDefaultColor(object sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex % 2 == 0)
        {
            if (GlobalData.Settings.General.BlackTheming)
                Grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = VeryLightGray2; // Color.DarkGray; // VeryLightGray // VeryLightGray2; // 
            else
                Grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = VeryLightGray1;
        }
    }
    public virtual void CellFormattingEvent(object sender, DataGridViewCellFormattingEventArgs e)
    {
        // Cell format (color)
    }

    private void InitializeDataGrid()
    {
        // https://stackoverflow.com/questions/35214250/c-sharp-using-icomparer-to-sort-x-number-of-columns

        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(Grid, true, null);

        Grid.ReadOnly = true;
        Grid.VirtualMode = true;
        Grid.RowHeadersVisible = false; // the first column to select rows
        Grid.AllowUserToAddRows = false;
        Grid.AutoGenerateColumns = false;
        Grid.MultiSelect = false;
        Grid.AllowUserToResizeRows = false;
        Grid.AllowUserToResizeColumns = true;
        Grid.AllowUserToOrderColumns = true;
        Grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        Grid.BackgroundColor = Grid.DefaultCellStyle.BackColor;

        // Hide header cell highlight stuff
        //Grid.EnableHeadersVisualStyles = false;
        //Grid.DefaultCellStyle.SelectionBackColor = Grid.DefaultCellStyle.BackColor;
        //Grid.DefaultCellStyle.SelectionForeColor = Grid.DefaultCellStyle.ForeColor;
        Grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Grid.ColumnHeadersDefaultCellStyle.BackColor;
        Grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Grid.ColumnHeadersDefaultCellStyle.ForeColor;
        Grid.EnableHeadersVisualStyles = false;

        Grid.GridColor = VeryLightGray1;
        Grid.BorderStyle = BorderStyle.None; // Fixed3D, FixedSingle;
        Grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
        Grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single; // Single;
        Grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single; // Raised;

        //Grid.RowHeadersVisible = false;

        // Oh, dat kan dus ook nog
        //foreach (DataGridViewColumn column in Grid.Columns)
        //{
        //    column.SortMode = DataGridViewColumnSortMode.NotSortable;
        //}

        //DataGridViewCellStyle style = Grid.ColumnHeadersDefaultCellStyle;
        //style.BackColor = Color.Navy;
        //style.ForeColor = Color.White;
        //style.Font = new Font(Grid.Font, FontStyle.Italic);

        //Grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.Fill; // DisplayedCells; ?
        Grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; // AllCellsExceptHeader; // AllCells; // DisplayedCells;
        Grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        Grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
        //Grid.GridColor = SystemColors.ActiveBorder;

        //Grid.BackgroundColor = Color.Honeydew;

        //Grid.AutoSizeMode =  column property
        //Grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

        // https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/implementing-virtual-mode-wf-datagridview-control?view=netframeworkdesktop-4.8&redirectedfrom=MSDN
        // Connect the virtual-mode events to event handlers.
        Grid.CellValueNeeded += new DataGridViewCellValueEventHandler(GetTextFunction);
        //Grid.CellValuePushed += new DataGridViewCellValueEventHandler(Grid_CellValuePushed);
        //Grid.NewRowNeeded += new DataGridViewRowEventHandler(Grid_NewRowNeeded);
        //Grid.RowValidated += new DataGridViewCellEventHandler(Grid_RowValidated);
        //Grid.RowDirtyStateNeeded += new QuestionEventHandler(Grid_RowDirtyStateNeeded);
        //Grid.CancelRowEdit += new QuestionEventHandler(Grid_CancelRowEdit);
        //Grid.UserDeletingRow += new DataGridViewRowCancelEventHandler(Grid_UserDeletingRow);
        Grid.ColumnHeaderMouseClick += HeaderClick;
        Grid.CellFormatting += CellFormattingEvent;
        Grid.RowPrePaint += RowSetDefaultColor;

        // Bind to a textbox
        //txtModel.DataBindings.Add("Text", bs, "Model");


        // Defaults for common styles

        //DataGridViewCellStyle style = new DataGridViewCellStyle();
        //style.ForeColor = Grid.Columns["Interval"].DefaultCellStyle.BackColor;
        //style.BackColor = Grid.Columns["Interval"].DefaultCellStyle.ForeColor;

        //styleRed.BackColor = Grid.DefaultCellStyle.BackColor;
        //styleRed.ForeColor = Color.Red; 

        //styleGreen.BackColor = Grid.DefaultCellStyle.BackColor;
        //styleGreen.ForeColor = Color.Green; 
    }


    public virtual void AddObject(T signal)
    {
        List.Add(signal);
        SortFunction();

        Grid.Invoke((MethodInvoker)(() => { Grid.RowCount = List.Count; Grid.Invalidate(); }));
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
        DataGridViewColumn newSortColumn = Grid.Columns[column];

        ListSortDirection direction;
        if (SortColumn >= 0)
        {
            if (SortColumn == column && SortOrder == SortOrder.Ascending)
            {
                // Sort the same column again, reversing the SortOrder.
                SortOrder = SortOrder.Descending;
                direction = ListSortDirection.Descending;
            }
            else
            {
                // Sort on new column and remove the old SortGlyph
                direction = ListSortDirection.Ascending;
                SortOrder = SortOrder.Ascending;
                Grid.Columns[SortColumn].HeaderCell.SortGlyphDirection = SortOrder.None;
            }
        }
        else
        {
            // Sort on new column and remove the old SortGlyph
            direction = ListSortDirection.Ascending;
            SortOrder = SortOrder.Ascending;
        }
        SortColumn = column;
        newSortColumn.HeaderCell.SortGlyphDirection = direction == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending;

        SortFunction();
        Grid.Invalidate();
    }

    public void ApplySorting()
    {
        int column = SortColumn;
        DataGridViewColumn newSortColumn = Grid.Columns[column];
        newSortColumn.HeaderCell.SortGlyphDirection = SortOrder == SortOrder.Ascending ? SortOrder.Ascending : SortOrder.Descending;

        SortFunction();

        Grid.Invalidate();
    }


    public void InitCommandCaptions()
    {
        string text = GlobalData.ExternalUrls.GetTradingAppName(GlobalData.Settings.General.TradingApp, GlobalData.Settings.General.ExchangeName);
        Grid.ContextMenuStrip.Items[0].Text = text;
    }


    internal static void AddStandardSymbolCommands(ContextMenuStrip menuStrip, bool isSignal)
    {
        ToolStripMenuItem menuCommand;

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Activate trading app";
        menuCommand.Tag = Command.ActivateTradingApp;
        menuCommand.Click += Commands.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "TradingView browser";
        menuCommand.Tag = Command.ActivateTradingviewIntern;
        menuCommand.Click += Commands.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "TradingView extern";
        menuCommand.Tag = Command.ActivateTradingviewExtern;
        menuCommand.Click += Commands.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);


        menuStrip.Items.Add(new ToolStripSeparator());

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Kopiëer informatie";
        if (isSignal)
            menuCommand.Tag = Command.CopySignalInformation;
        else
            menuCommand.Tag = Command.CopySymbolInformation;
        menuCommand.Click += Commands.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Trend informatie (zie log)";
        menuCommand.Tag = Command.ShowTrendInformation;
        menuCommand.Click += Commands.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Symbol informatie (Excel)";
        menuCommand.Tag = Command.ExcelSymbolInformation;
        menuCommand.Click += Commands.ExecuteCommandCommandViaTag;
        menuStrip.Items.Add(menuCommand);
    }


    //// https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datagridview.sort?view=windowsdesktop-8.0
    //private class RowComparer : System.Collections.IComparer
    //{
    //    private readonly int sortOrderModifier = 1;

    //    public RowComparer(System.Windows.Forms.SortOrder sortOrder)
    //    {
    //        if (sortOrder == System.Windows.Forms.SortOrder.Descending)
    //            sortOrderModifier = -1;
    //        else if (sortOrder == System.Windows.Forms.SortOrder.Ascending)
    //            sortOrderModifier = 1;
    //    }

    //    public int Compare(object x, object y)
    //    {
    //        DataGridViewRow DataGridViewRow1 = (DataGridViewRow)x;
    //        DataGridViewRow DataGridViewRow2 = (DataGridViewRow)y;

    //        // Try to sort based on the first column.
    //        int CompareResult = String.Compare(DataGridViewRow1.Cells[0].Value.ToString(), DataGridViewRow2.Cells[0].Value.ToString());
    //        // If the Last Names are equal, sort based on the second column
    //        if (CompareResult == 0)
    //            CompareResult = String.Compare(DataGridViewRow1.Cells[1].Value.ToString(), DataGridViewRow2.Cells[1].Value.ToString());

    //        return CompareResult * sortOrderModifier;
    //    }
    //}

    //public static void SortOnMultipleColumns(DataGridView dgv, Dictionary<string, ColumnSortInfo> sortingColumns)
    //{
    //    // Show the glyphs
    //    foreach (DataGridViewColumn col in dgv.Columns)
    //    {
    //        System.Windows.Forms.SortOrder sortOrder = System.Windows.Forms.SortOrder.None;

    //        foreach (var kvp in sortingColumns)
    //        {
    //            if (kvp.Key == col.Name)
    //            {
    //                sortOrder = kvp.Value.SortOrder;
    //                break;
    //            }
    //        }
    //        col.HeaderCell.SortGlyphDirection = sortOrder;
    //    }

    //    // Sort the grid
    //    MultiColumnCompararor multiColumnCompararor = new MultiColumnCompararor(sortingColumns);
    //    dgv.Sort(multiColumnCompararor);
    //}

    //public class ColumnSortInfo
    //{
    //    public enum ValueConversion { ToString, ToNumber, ToDate }
    //    public System.Windows.Forms.SortOrder SortOrder { get; set; }
    //    public int SortOrderMultiplier { get; }
    //    public ValueConversion MyValueConversion { get; set; }


    //    public ColumnSortInfo(System.Windows.Forms.SortOrder sortOrder, ValueConversion valueConversion = ValueConversion.ToString)
    //    {
    //        SortOrder = sortOrder;
    //        MyValueConversion = valueConversion;
    //        SortOrderMultiplier = (SortOrder == System.Windows.Forms.SortOrder.Ascending) ? 1 : -1;
    //    }


    //    public static double StringToDouble(string sVal)
    //    {
    //        if (Double.TryParse(sVal, out double dVal))
    //        {
    //            return dVal;
    //        }
    //        return 0;
    //    }

    //    public static DateTime StringToDateTime(string sVal)
    //    {
    //        if (DateTime.TryParse(sVal, out DateTime dt))
    //        {
    //            return dt;
    //        }
    //        return DateTime.MinValue;
    //    }
    //}


    //private class MultiColumnCompararor : System.Collections.IComparer
    //{

    //    IDictionary<string, ColumnSortInfo> _sortingColumns;

    //    public MultiColumnCompararor(IDictionary<string, ColumnSortInfo> sortingColumns)
    //    {
    //        _sortingColumns = sortingColumns;
    //    }

    //    public int Compare(object x, object y)
    //    {
    //        try
    //        {
    //            DataGridViewRow r1 = (DataGridViewRow)x;
    //            DataGridViewRow r2 = (DataGridViewRow)y;

    //            foreach (var kvp in _sortingColumns)
    //            {
    //                string colName = kvp.Key;
    //                ColumnSortInfo csi = kvp.Value;

    //                string sVal1 = r1.Cells[colName].Value?.ToString().Trim() ?? "";
    //                string sVal2 = r2.Cells[colName].Value?.ToString().Trim() ?? "";

    //                int iCompareResult = 0;

    //                switch (csi.MyValueConversion)
    //                {
    //                    case ColumnSortInfo.ValueConversion.ToString:
    //                        iCompareResult = String.Compare(sVal1, sVal2);
    //                        break;
    //                    case ColumnSortInfo.ValueConversion.ToNumber:
    //                        double d1 = ColumnSortInfo.StringToDouble(sVal1);
    //                        double d2 = ColumnSortInfo.StringToDouble(sVal2);
    //                        iCompareResult = ((d1 == d2) ? 0 : ((d1 > d2) ? 1 : -1));
    //                        break;
    //                    case ColumnSortInfo.ValueConversion.ToDate:
    //                        DateTime dt1 = ColumnSortInfo.StringToDateTime(sVal1);
    //                        DateTime dt2 = ColumnSortInfo.StringToDateTime(sVal2);
    //                        iCompareResult = ((dt1 == dt2) ? 0 : ((dt1 > dt2) ? 1 : -1));
    //                        break;
    //                    default:
    //                        break;
    //                }

    //                iCompareResult = csi.SortOrderMultiplier * iCompareResult;

    //                if (iCompareResult != 0) { return iCompareResult; }
    //            }
    //            return 0;
    //        }
    //        catch (Exception)
    //        {
    //            return 0;
    //        }
    //    }
    //}
    //Grid.SuspendLayout();

    //listBla.Sort(new RowComparer(System.Windows.Forms.SortOrder.Ascending));
    //Dictionary<string, ColumnSortInfo> sortingColumns = new();
    //sortingColumns.Add("Symbol", System.Windows.Forms.SortOrder.Ascending);

    //Dictionary<String, ColumnSortInfo> sortingColumns = new Dictionary<String, ColumnSortInfo>
    //{ {"Symbol", new ColumnSortInfo(System.Windows.Forms.SortOrder.Ascending)},
    //    {"Interval", new ColumnSortInfo(System.Windows.Forms.SortOrder.Descending, ColumnSortInfo.ValueConversion.ToNumber)},
    //    {"CandleDate", new ColumnSortInfo(System.Windows.Forms.SortOrder.Ascending, ColumnSortInfo.ValueConversion.ToDate)}
    //};

    //SortOnMultipleColumns(Grid, sortingColumns);

    //var x = Grid.SelectedRows;
    //listBla.Sort((x, y) => x.Symbol.Name.CompareTo(y.Symbol.Name));
    //Grid.DataSource = null;
    //Grid.DataSource = myBindingSource;

    //bs.ResetBindings(false);

    //bs.ResetBindings(false);
    //Grid.ResumeLayout(true);
}
