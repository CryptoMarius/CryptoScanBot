using Avalonia.Collections;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Dapper;

namespace CryptoScanner.ViewModels;

public partial class PositionOpenGridViewModel : ObservableObject
{
    private DispatcherTimer _timerRefreshFields = new() { Interval = TimeSpan.FromSeconds(15) };

    [ObservableProperty]
    private AvaloniaList<PositionViewModel> _positions = [];

    public PositionOpenGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("PositionOpenGridViewModel constructor called");

        WeakReferenceMessenger.Default.Register<PositionDeleteAllMessage>(this, OnPositionDeleteAll);
        WeakReferenceMessenger.Default.Register<PositionIsClosedMessage>(this, OnPositionIsClosed);
        WeakReferenceMessenger.Default.Register<PositionIsCreatedMessage>(this, OnPositionIsCreated);
        WeakReferenceMessenger.Default.Register<PositionIsDeletedMessage>(this, OnPositionIsDeleted);
        WeakReferenceMessenger.Default.Register<ConfigurationChangedMessage>(this, OnConfigurationChanged);
        WeakReferenceMessenger.Default.Register<ExchangeSwitchedMessage>(this, OnExchangeSwitched);

        _timerRefreshFields.Tick += TimerRefreshFieldsTick;
        _timerRefreshFields.Start();

        LoadOpenPositions();
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<PositionDeleteAllMessage>(this);
        WeakReferenceMessenger.Default.Unregister<PositionIsClosedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<PositionIsCreatedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<PositionIsDeletedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ConfigurationChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ExchangeSwitchedMessage>(this);


        _timerRefreshFields.Stop();
        _timerRefreshFields.Tick -= TimerRefreshFieldsTick;
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


    private void OnConfigurationChanged(object recipient, ConfigurationChangedMessage message)
    {
        foreach (var position in Positions)
            position.ResetColors();
    }

    private void OnExchangeSwitched(object recipient, ExchangeSwitchedMessage message)
    {
        // After an exchange switch the old positions reference symbols from the previous exchange
        // which may have been cleared. Reload from the database for the new active exchange.
        Dispatcher.UIThread.Post(LoadOpenPositions);
    }

    private void LoadOpenPositions()
    {
        // GlobalData.AddTextToLogTab("Reading open positions");

        // There is no active exchange while the session failed to start - a settings file naming an
        // exchange this build does not have (Coinbase Futures, for instance) leaves it null. The
        // null-forgiving ! below then turned into an UnhandledException that terminated the whole
        // application, hiding the actual error behind a NullReferenceException. Without an exchange
        // there are simply no positions to show.
        if (GlobalData.ActiveExchange == null)
        {
            Positions.Clear();
            return;
        }

        List<PositionViewModel> viewModels = [];
        using var database = new CryptoDatabase();
        string sql = "select * from position where exchangeid=@exchangeid and closetime is null and status < 2";
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql, new { exchangeid = GlobalData.ActiveExchange!.Id }))
        {
            PositionTools.AddPosition(position);
            PositionTools.LoadPosition(database, position);
            viewModels.Add(new PositionViewModel { Object = position });
        }
        Positions.Clear();
        Positions.AddRange([.. viewModels]);
    }

    private void OnPositionIsCreated(object recipient, PositionIsCreatedMessage message)
    {
        Positions.Add(new PositionViewModel { Object = message.Position });
    }

    private void OnPositionIsClosed(object recipient, PositionIsClosedMessage message)
    {
        var viewModel = Positions.FirstOrDefault(p => p.Object.Id == message.Position.Id);
        if (viewModel != null)
            Positions.Remove(viewModel);
    }

    private void OnPositionIsDeleted(object recipient, PositionIsDeletedMessage message)
    {
        var viewModel = Positions.FirstOrDefault(p => p.Object.Id == message.Position.Id);
        if (viewModel != null)
            Positions.Remove(viewModel);
    }

    private void OnPositionDeleteAll(object recipient, PositionDeleteAllMessage message)
    {
        Positions.Clear();
    }

    private void TimerRefreshFieldsTick(object? sender, EventArgs e)
    {
        foreach (var position in Positions)
        {
            try
            {
                // "Distance" from current price
                position.Status = string.Empty;
                position.Invested = string.Empty;
                position.Returned = string.Empty;
                position.Commission = string.Empty;
                position.Open = string.Empty;

                position.Duration = string.Empty;

                position.Quantity = string.Empty;
                position.CurrentProfit = string.Empty;
                position.BreakEvenPrice = string.Empty;
                position.BreakEvenPercent = string.Empty;
                position.CurrentProfitPercentage = string.Empty;

                // Statistics (not visible at this moment?)
                //position.PriceMinPerc = string.Empty;
                //position.PriceMaxPerc = string.Empty;
            }
            catch (Exception ex)
            {
                ScannerLog.Logger.Error(ex, "");
            }
        }
    }

}