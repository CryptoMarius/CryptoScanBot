using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Model;

using Dapper;


namespace CryptoScanner.ViewModels;

public partial class PositionClosedGridViewModel : ObservableObject
{
    //private DispatcherTimer? _timerUpdatePositions = new() { Interval = TimeSpan.FromSeconds(15) };

    [ObservableProperty]
    private ObservableRangeCollection<PositionViewModel> _positions = [];

    public PositionClosedGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("PositionClosedGridViewModel constructor called");
        GlobalData.PositionsHaveChangedEvent += new AddTextEvent(PositionsHaveChangedEvent);

        //_timerUpdatePositions.Tick += TimerUpdatePositionsTick;
        //_timerUpdatePositions.Start();

        LoadClosedPositions();
        GlobalData.PositionsHaveChanged("");
    }


    //public event EventHandler<PositionViewModel>? RequestSortedInsert;
    //public event EventHandler? RequestSort;

    public void TimerUpdatePositionsTick(object? sender, EventArgs? e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        // ? Update Currentprofit and CUrrentBreakEven perhaps?

        //if (WinFormTools.IsControlVisibleToUser(Grid))
        //{
        //    try
        //    {
        //        Grid.SuspendDrawing();
        //        try
        //        {
        //??            SortFunction();
        //            //Grid.InvalidateColumn((int)LiveDataColumnEnum.DlzZoneDistance);
        //            //Grid.InvalidateColumn((int)LiveDataColumnEnum.Volume);
        //        }
        //        finally
        //        {
        //            Grid.ResumeDrawing();
        //        }
        //    }
        //    catch (Exception error)
        //    {
        //        ScannerLog.Logger.Error(error, "");
        //        GlobalData.AddTextToLogTab($"Error TimerUpdatePositionsTick {error}");
        //    }
        //}
    }

    //private void PositionsHaveChangedEvent(string text)
    //{
    //    List<PositionViewModel> list = [];
    //    if (GlobalData.ActiveExchange != null)
    //    {
    //        foreach (var position in GlobalData.ActiveExchange.Data.PositionList.Values)
    //        {
    //            list.Add(new PositionViewModel { Object = position });
    //        }
    //    }
    //    Positions.AddRange(list);
    //}


    private void PositionsHaveChangedEvent(string text)
    {
        if (!GlobalData.ApplicationIsClosing && GlobalData.ActiveExchange != null)
        {
            List<PositionViewModel> list = [];
            if (GlobalData.ActiveExchange != null)
            {
                foreach (var position in GlobalData.PositionsClosed)
                {
                    list.Add(new PositionViewModel { Object = position });
                }
            }
            Positions.Clear();
            Positions.AddRange(list);

            //GlobalData.AddTextToLogTab("PositionsHaveChangedEvent#start");

            // Alle positie gerelateerde zaken verversen
            //Task.Run(() =>
            //{
            //    Invoke(new Action(() =>
            //    {
            //        dataGridViewPositionClosed.SuspendDrawing();
            //        try
            //        {
            //            PositionClosedListView.Clear();
            //            GridPositionClosedView.AddRange(list);
            //            //GridPositionClosedView.AdjustObjectCount();
            //            //GridPositionClosedView.ApplySorting();
            //        }
            //        finally
            //        {
            //            dataGridViewPositionClosed.ResumeDrawing();
            //        }

            //        dataGridViewPositionClosed.SuspendDrawing();
            //        try
            //        {
            //            PositionClosedListView.Clear();
            //            GridPositionClosedView.AddRange(GlobalData.PositionsClosed);
            //            //GridPositionClosedView.AdjustObjectCount();
            //            //GridPositionClosedView.ApplySorting();

            //            dashBoardControl1.TimerUpdatePositionsTick(null, null);
            //            //GlobalData.AddTextToLogTab("PositionsHaveChangedEvent#einde");
            //        }
            //        finally
            //        {
            //            dataGridViewPositionClosed.ResumeDrawing();
            //        }
            //    }));
            //});
        }
    }


    private static void LoadClosedPositions()
    {
        // Alle gesloten posities lezen 
        // TODO - beperken tot de laatste 2 dagen? (en wat handigheden toevoegen wellicht)
        //GlobalData.AddTextToLogTab("Reading closed positions");
        string sql = "select * from position where exchangeid=@exchangeid and not closetime is null order by id desc";
        if (!GlobalData.BackTest)
            sql += " limit 500";
        using var database = new CryptoDatabase();

        GlobalData.PositionsClosed.Clear();
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql, new { exchangeid = GlobalData.ActiveExchange!.Id }))
            PositionTools.AddPositionClosed(position);
    }

}