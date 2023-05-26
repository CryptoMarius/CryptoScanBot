using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner;

public partial class FrmMain
{
    private ListViewDoubleBuffered listViewPositions;
    
    //public void Dispose()
    //{
    //    if (TimerClearEvents != null) { TimerClearEvents.Dispose(); TimerClearEvents = null; }
    //}

    private void ListViewPositionsConstructor()
    {
        //ListViewColumnSorter listViewColumnSorter = new()
        //{
        //    SortOrder = SortOrder.Descending
        //};

        // ruzie (component of events raken weg), dan maar dynamisch
        listViewPositions = new()
        {
            CheckBoxes = false
        };
        listViewPositions.CheckBoxes = false;
        listViewPositions.AllowColumnReorder = false;
        listViewPositions.Dock = DockStyle.Fill;
        listViewPositions.Location = new Point(4, 3);
        listViewPositions.GridLines = true;
        listViewPositions.View = View.Details;
        listViewPositions.FullRowSelect = true;
        listViewPositions.HideSelection = true;
        listViewPositions.BorderStyle = BorderStyle.None;
        //listViewPositions.ContextMenuStrip = listViewSignalsMenuStrip;
        //listViewPositions.ListViewItemSorter = listViewColumnSorter;
        //listViewPositions.ColumnClick += ListViewSignals_ColumnClick;
        //listViewPositions.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.SortOrder);
        //listViewPositions.DoubleClick += new System.EventHandler(ListViewSignalsMenuItem_DoubleClick);
        tabPageSignals.Controls.Add(listViewPositions);

        //TimerClearEvents = new();
        //InitTimerInterval(ref TimerClearEvents, 1 * 60);
        //TimerClearEvents.Tick += new System.EventHandler(TimerClearOldSignals_Tick);

        ListViewPositionsInitColumns();
    }




    private void ListViewPositionsInitColumns()
    {
        // TODO: Positie kolommen kiezen..

        // Create columns and subitems. Width of -2 indicates auto-size
        listViewPositions.Columns.Add("Datum", -2, HorizontalAlignment.Left);
        listViewPositions.Columns.Add("Symbol", -2, HorizontalAlignment.Left);
        listViewPositions.Columns.Add("Interval", -2, HorizontalAlignment.Left);
        listViewPositions.Columns.Add("Mode", -2, HorizontalAlignment.Left);
        listViewPositions.Columns.Add("Strategie", -2, HorizontalAlignment.Left);
        listViewPositions.Columns.Add("Text", 250, HorizontalAlignment.Left);
        listViewPositions.Columns.Add("Price", -2, HorizontalAlignment.Right);
        listViewPositions.Columns.Add("Stijging", -2, HorizontalAlignment.Right);
        listViewPositions.Columns.Add("Volume", -2, HorizontalAlignment.Right);
        listViewPositions.Columns.Add("Trend", -2, HorizontalAlignment.Right);
        listViewPositions.Columns.Add("Trend%", -2, HorizontalAlignment.Right);
        listViewPositions.Columns.Add("24h Change", -2, HorizontalAlignment.Right);
        listViewPositions.Columns.Add("24h Beweging", -2, HorizontalAlignment.Right);
        listViewPositions.Columns.Add("BB%", -2, HorizontalAlignment.Right);
        listViewPositions.Columns.Add("", -2, HorizontalAlignment.Right); // filler

        for (int i = 0; i <= listViewPositions.Columns.Count - 1; i++)
        {
            if (i != 5)
                listViewPositions.Columns[i].Width = -2;
        }
    }
}
