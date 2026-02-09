using Avalonia.Collections;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;

using Dapper;

using System.Collections.ObjectModel;
using System.Collections.Specialized;


namespace CryptoScanner.ViewModels;

public partial class PositionOpenViewModel : BaseGridViewModel<CryptoPosition, PositionColumnEnum, PositionColumnComparer>
{
    private DispatcherTimer _timerRefreshFields = new() { Interval = TimeSpan.FromSeconds(15) };

    public PositionOpenViewModel()
    {
        System.Diagnostics.Debug.WriteLine("PositionOpenGridViewModel constructor called");
        SortColumn = PositionColumnEnum.UpdateTime;
        _columns = PositionColumns.GetColumns();
        _columnWidths = GetWidths(_columns);
        System.Diagnostics.Debug.WriteLine($"PositionOpenGridViewModel: {_columns.Count} columns, {_columnWidths.Count} widths");

        WeakReferenceMessenger.Default.Register<PositionIsClosedMessage>(this, OnPositionIsClosed);
        WeakReferenceMessenger.Default.Register<PositionIsCreatedMessage>(this, OnPositionIsCreated);
        WeakReferenceMessenger.Default.Register<PositionIsDeletedMessage>(this, OnPositionIsDeleted);

        _timerRefreshFields.Tick += TimerRefreshFieldsTick;
        _timerRefreshFields.Start();

        LoadOpenPositions();
    }

    public void Dispose()
    {
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

    protected override void RefreshVisibleItems()
    {
        System.Diagnostics.Debug.WriteLine("RefreshVisibleItems called");

        if (Dispatcher.UIThread.CheckAccess())
        {
            lock (_lock)
            {
                // Bewaar huidige selectie
                var selectedId = SelectedObject?.Id;

                // Vervang collectie
                VisibleObjects = new AvaloniaList<CryptoPosition>(_allObjects);

                // Herstel selectie
                if (selectedId.HasValue)
                {
                    SelectedObject = VisibleObjects.FirstOrDefault(p => p.Id == selectedId.Value);
                }
            }
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                lock (_lock)
                {
                    var selectedId = SelectedObject?.Id;
                    VisibleObjects = new AvaloniaList<CryptoPosition>(_allObjects);
                    if (selectedId.HasValue)
                    {
                        SelectedObject = VisibleObjects.FirstOrDefault(p => p.Id == selectedId.Value);
                    }
                }
            });
        }
    }

    private void LoadOpenPositions()
    {
        // GlobalData.AddTextToLogTab("Reading open positions");
        List<CryptoPosition> list = [];
        using var database = new CryptoDatabase();
        string sql = "select * from position where exchangeid=@exchangeid and closetime is null and status < 2";
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql, new { exchangeid = GlobalData.ActiveExchange!.Id }))
        {
            PositionTools.AddPosition(position);
            PositionTools.LoadPosition(database, position);
            list.Add(position);
        }

        lock (_lock)
        {
            _allObjects.Clear();
            _allObjects.AddRange(list);
            ApplySort(SortColumn);
        }

        RefreshVisibleItems();
    }

    private void OnPositionIsCreated(object recipient, PositionIsCreatedMessage message)
    {
        lock (_lock)
        {
            _allObjects.Add(message.Position);
            ApplySort(SortColumn);
        }

        RefreshVisibleItems();
    }

    private void OnPositionIsClosed(object recipient, PositionIsClosedMessage message)
    {
        lock (_lock)
        {
            var position = _allObjects.FirstOrDefault(p => p.Id == message.Position.Id);
            if (position != null)
                _allObjects.Remove(position);
        }

        RefreshVisibleItems();
    }

    private void OnPositionIsDeleted(object recipient, PositionIsDeletedMessage message)
    {
        lock (_lock)
        {
            var position = _allObjects.FirstOrDefault(p => p.Id == message.Position.Id);
            if (position != null)
                _allObjects.Remove(position);
        }

        RefreshVisibleItems();
    }

    private void TimerRefreshFieldsTick(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Trigger een reset zonder de collectie te vervangen
            var collection = VisibleObjects as INotifyCollectionChanged;
            // Helaas, dit werkt ook niet zonder ObservableObject...
        });

        //TODO: How to refresh?
        //foreach (var position in VisiblePositions)
        //{

        //    // "Distance" from current price
        //    position.Status = string.Empty;
        //    position.Invested = string.Empty;
        //    position.Returned = string.Empty;
        //    position.Commission = string.Empty;
        //    position.Open = string.Empty;

        //    position.Duration = string.Empty;

        //    //position.CurrentProfit = string.Empty;
        //    //position.BreakEvenPercent = string.Empty;
        //    //position.CurrentProfitPercentage = string.Empty;

        // this wont work, it is not observable anymore... (due to performance, dang..)
        //    position.OnPropertyChanged(nameof(position.CurrentProfitText));
        //    position.OnPropertyChanged(nameof(position.BreakEvenPercentText));
        //    position.OnPropertyChanged(nameof(position.CurrentProfitPercentageText));

        //    // Statistics (not visible at this moment?)
        //    //position.PriceMinPerc = string.Empty;
        //    //position.PriceMaxPerc = string.Empty;
        //}
    }

}