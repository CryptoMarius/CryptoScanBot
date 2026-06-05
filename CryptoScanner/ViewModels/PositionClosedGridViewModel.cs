using Avalonia.Collections;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;

using Dapper;

namespace CryptoScanner.ViewModels;

public partial class PositionClosedGridViewModel : ObservableObject
{
    //private DispatcherTimer? _timerUpdatePositions = new() { Interval = TimeSpan.FromSeconds(15) };

    [ObservableProperty]
    private AvaloniaList<PositionViewModel> _positions = [];

    public PositionClosedGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("PositionClosedGridViewModel constructor called");

        //_timerUpdatePositions.Tick += TimerUpdatePositionsTick;
        //_timerUpdatePositions.Start();
        WeakReferenceMessenger.Default.Register<PositionDeleteAllMessage>(this, OnPositionDeleteAll);
        WeakReferenceMessenger.Default.Register<PositionIsClosedMessage>(this, OnPositionIsClosed);
        WeakReferenceMessenger.Default.Register<PositionIsDeletedMessage>(this, OnPositionIsDeleted);
        WeakReferenceMessenger.Default.Register<ConfigurationChangedMessage>(this, OnConfigurationChanged);

        LoadClosedPositions();
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<PositionDeleteAllMessage>(this);
        WeakReferenceMessenger.Default.Unregister<PositionIsClosedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<PositionIsDeletedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ConfigurationChangedMessage>(this);
    }

    private void OnConfigurationChanged(object recipient, ConfigurationChangedMessage message)
    {
        foreach (var position in Positions)
            position.ResetColors();
    }

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
        if (!GlobalData.IsEmulatorMode)
            sql += " limit 500";
        using var database = new CryptoDatabase();

        List<PositionViewModel> viewModels = [];
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

                    viewModels.Add(new PositionViewModel { Object = position });
                }
            }
        }
        Positions.Clear();
        Positions.AddRange([.. viewModels]);
    }

    private void OnPositionIsClosed(object recipient, PositionIsClosedMessage message)
    {
        Positions.Add(new PositionViewModel { Object = message.Position });
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
}