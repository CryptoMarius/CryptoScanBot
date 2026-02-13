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

namespace CryptoScanner.ViewModels;

public partial class PositionClosedViewModel : BaseGridViewModel<CryptoPosition, PositionColumnEnum, PositionColumnComparer>
{
    // there us nothing to update in a closed position?
    //private DispatcherTimer? _timerUpdatePositions = new() { Interval = TimeSpan.FromSeconds(15) };

    public PositionClosedViewModel()
    {
        System.Diagnostics.Debug.WriteLine("PositionClosedGridViewModel constructor called");
        SortColumn = PositionColumnEnum.CloseTime;
        _columns = PositionColumns.GetColumns();

        _columnWidths = GetWidths(_columns);
        System.Diagnostics.Debug.WriteLine($"PositionClosedGridViewModel: {_columns.Count} columns, {_columnWidths.Count} widths");

        //_timerUpdatePositions.Tick += TimerUpdatePositionsTick;
        //_timerUpdatePositions.Start();
        WeakReferenceMessenger.Default.Register<PositionIsClosedMessage>(this, OnPositionIsClosed);
        WeakReferenceMessenger.Default.Register<PositionIsDeletedMessage>(this, OnPositionIsDeleted);

        LoadClosedPositions();
        InitializeRefreshTimer();
    }


    public override void Dispose()
    {
        base.Dispose(); 
        //_timerRefreshZones.Stop();
        //_timerRefreshZones.Tick -= TimerRefreshZonesTick;
        WeakReferenceMessenger.Default.Unregister<PositionIsClosedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<PositionIsDeletedMessage>(this);
    }


    //protected override void RefreshVisibleItems()
    //{
    //    System.Diagnostics.Debug.WriteLine("RefreshVisibleItems called");

    //    if (Dispatcher.UIThread.CheckAccess())
    //    {
    //        lock (_lock)
    //        {
    //            // Bewaar huidige selectie
    //            var selectedId = SelectedObject?.Id;

    //            // Vervang collectie
    //            VisibleObjects = new AvaloniaList<CryptoPosition>(_allObjects);

    //            // Herstel selectie
    //            if (selectedId.HasValue)
    //            {
    //                SelectedObject = VisibleObjects.FirstOrDefault(p => p.Id == selectedId.Value);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        Dispatcher.UIThread.Post(() =>
    //        {
    //            lock (_lock)
    //            {
    //                var selectedId = SelectedObject?.Id;
    //                VisibleObjects = new AvaloniaList<CryptoPosition>(_allObjects);
    //                if (selectedId.HasValue)
    //                {
    //                    SelectedObject = VisibleObjects.FirstOrDefault(p => p.Id == selectedId.Value);
    //                }
    //            }
    //        });
    //    }
    //}



    //public void TimerUpdatePositionsTick(object? sender, EventArgs? e)
    //{
    //    if (GlobalData.ApplicationIsClosing)
    //        return;

    //    // ? Update Currentprofit and CUrrentBreakEven perhaps?
    //    foreach (var position in Positions)
    //    {
    //        position.CurrentProfit = string.Empty;
    //        position.CurrentProfit = string.Empty;
    //    }
    //    //if (WinFormTools.IsControlVisibleToUser(Grid))
    //    //{
    //    //    try
    //    //    {
    //    //        Grid.SuspendDrawing();
    //    //        try
    //    //        {
    //    //??          SortFunction();
    //    //            //Grid.InvalidateColumn((int)LiveDataColumnEnum.DlzZoneDistance);
    //    //            //Grid.InvalidateColumn((int)LiveDataColumnEnum.Volume);
    //    //        }
    //    //        finally
    //    //        {
    //    //            Grid.ResumeDrawing();
    //    //        }
    //    //    }
    //    //    catch (Exception error)
    //    //    {
    //    //        ScannerLog.Logger.Error(error, "");
    //    //        GlobalData.AddTextToLogTab($"Error TimerUpdatePositionsTick {error}");
    //    //    }
    //    //}
    //}


    private void LoadClosedPositions()
    {
        // TODO - limit to the last 2 days?
        //GlobalData.AddTextToLogTab("Reading closed positions");
        string sql = "select * from position where exchangeid=@exchangeid and not closetime is null order by id desc";
        if (!GlobalData.BackTest)
            sql += " limit 500";
        using var database = new CryptoDatabase();

        List<CryptoPosition> list = [];
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql, new { exchangeid = GlobalData.ActiveExchange!.Id }))
        {
            if (GlobalData.ExchangeListId.TryGetValue(position.ExchangeId, out Core.Model.CryptoExchange? exchange))
            {
                position.Exchange = exchange;
                if (exchange.SymbolListId.TryGetValue(position.SymbolId, out CryptoSymbol? symbol))
                {
                    position.Symbol = symbol;
                    if (GlobalData.IntervalListId.TryGetValue((int)position.IntervalId!, out CryptoInterval? interval))
                        position.Interval = interval!;

                    list.Add(position);
                }
            }
        }

        lock (_lock)
        {
            _allObjects.Clear();
            _allObjects.AddRange(list);
            ApplySort(SortColumn);
        }

        RefreshVisibleItems();
    }

    private void OnPositionIsClosed(object recipient, PositionIsClosedMessage message)
    {
        lock (_lock)
        {
            _allObjects.Add(message.Position);
            ApplySort(SortColumn);
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
}