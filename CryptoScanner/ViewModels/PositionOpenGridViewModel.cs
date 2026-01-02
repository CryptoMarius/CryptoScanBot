using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Model;

using Dapper;


namespace CryptoScanner.ViewModels;

public partial class PositionOpenGridViewModel : ObservableObject
{
    private DispatcherTimer? _timerUpdatePositions = new() { Interval = TimeSpan.FromSeconds(15) };

    [ObservableProperty]
    private ObservableRangeCollection<PositionViewModel> _positions = [];

    public PositionOpenGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("PositionOpenGridViewModel constructor called");
        GlobalData.PositionsHaveChangedEvent += new AddTextEvent(PositionsHaveChangedEvent);

        _timerUpdatePositions.Tick += TimerUpdatePositionsTick;
        _timerUpdatePositions.Start();

        LoadOpenPositions();
    }


    public event EventHandler<PositionViewModel>? RequestSortedInsert;
    public event EventHandler? RequestSort;

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
                foreach (var position in GlobalData.ActiveExchange.Data.PositionList.Values)
                {
                    list.Add(new PositionViewModel { Object = position });
                }
            }
            Positions.Clear();
            Positions.AddRange(list);

            //GlobalData.AddTextToLogTab("PositionsHaveChangedEvent#start");
        }
    }

    // Move perhaps to the PositionOpenGridViewModel?
    private static void LoadOpenPositions()
    {
        // Alle openstaande posities lezen 
        //GlobalData.AddTextToLogTab("Reading open positions");

        using var database = new CryptoDatabase();
        string sql = "select * from position where exchangeid=@exchangeid and closetime is null and status < 2";
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql, new { exchangeid = GlobalData.ActiveExchange!.Id }))
        {
            PositionTools.AddPosition(position);
            PositionTools.LoadPosition(database, position);
        }
    }
}