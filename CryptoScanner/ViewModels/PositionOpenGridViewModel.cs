using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Model;

using Dapper;

using System.Collections.ObjectModel;


namespace CryptoScanner.ViewModels;

public partial class PositionOpenGridViewModel : ObservableObject
{
    private DispatcherTimer? _timerRefreshFields = new() { Interval = TimeSpan.FromSeconds(15) };

    [ObservableProperty]
    private ObservableCollection<PositionViewModel> _positions = [];

    public PositionOpenGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("PositionOpenGridViewModel constructor called");
        GlobalData.PositionsHaveChangedEvent += new AddTextEvent(PositionsHaveChangedEvent);

        _timerRefreshFields.Tick += TimerRefreshFieldsTick;
        _timerRefreshFields.Start();

        LoadOpenPositions();
        GlobalData.PositionsHaveChanged("");
    }


    //private void StartMinutePlusFiveTimer()
    //{
    //    var now = DateTime.Now;

    //    // volgende minuut
    //    var nextMinute = new DateTime(
    //        now.Year, now.Month, now.Day,
    //        now.Hour, now.Minute, 0
    //    ).AddMinutes(1);

    //    // doelmoment = xx:xx:05
    //    var target = nextMinute.AddSeconds(5);

    //    var initialDelay = target - now;

    //    // eerste timer: eenmalige delay
    //    var firstTimer = new DispatcherTimer
    //    {
    //        Interval = initialDelay
    //    };

    //    firstTimer.Tick += (s, e) =>
    //    {
    //        firstTimer.Stop();
    //        DoWork(); // jouw actie

    //        // daarna elke minuut exact op xx:05
    //        StartRepeatingTimer();
    //    };

    //    firstTimer.Start();
    //}


    private void PositionsHaveChangedEvent(string text)
    {
        if (!GlobalData.ApplicationIsClosing && GlobalData.ActiveExchange != null)
        {
            Positions.Clear();
            //List<PositionViewModel> list = [];
            if (GlobalData.ActiveExchange != null)
            {
                foreach (var position in GlobalData.ActiveExchange.Data.PositionList.Values)
                {
                    //if (position.Status < CryptoPositionStatus.Ready)
                    //list.Add(new PositionViewModel { Object = position });
                    Positions.Add(new PositionViewModel { Object = position });
                }
            }
            //Positions.Clear();
            //Positions.AddRange(list);

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

    private void TimerRefreshFieldsTick(object? sender, EventArgs e)
    {
        foreach (var position in Positions)
        {
            // "Distance" from current price
            position.NotifyColumnChanged("CurrentProfit");
            position.NotifyColumnChanged("BreakEvenPercent");
            position.NotifyColumnChanged("CurrentProfitPercentage");

            // Statistics (not visible at this moment?)
            position.NotifyColumnChanged("PriceMinPerc");
            position.NotifyColumnChanged("PriceMaxPerc");
        }
    }

}