using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.ViewModels;

using System.Windows.Input;

namespace CryptoScanner.Commands;

public enum CommandEnum
{
    None,
    ActivateTradingApp,
    ActivateActiveExchange,
    ActivateTradingviewIntern,
    ActivateTradingviewExtern,
    ShowTrendInformation,
    ExcelSignalInformation,
    ExcelSignalsInformation,
    ExcelSymbolInformation,
    ExcelExchangeInformation,
    ExcelPositionInformation,
    ExcelPositionsInformation,
    CopyDataGridCells,
    CopySymbolInformation,
    ScannerSessionDebug,
    PositionCalculate,
    TradingViewImportList,
    ShowSymbolGraph,
    About,
    CalculateAllLiquidityZones,
    CalculateSymbolLiquidityZones,
    CommandActivateTradingAppAndTv,
}

public abstract class CommandBase : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public virtual bool CanExecute(object? parameter)
    {
        return true;
    }

    public abstract void Execute(object? parameter);

    // TODO: Fix the casing of the properties when all commands are finished
    internal class parameterObjects
    {
        public DataGrid? datagrid = null;
        public Window? parentWindow = null;

        public SymbolViewModel? SymbolViewModel = null;
        public SignalViewModel? SignalViewModel = null;
        public LiveDataViewModel? LiveDataViewModel = null;
        public PositionViewModel? PositionViewModel = null;

        public Core.Model.CryptoExchange? exchange;
        public CryptoSymbol? symbol = null;
        public CryptoInterval? interval = null;
        public CryptoSignal? signal = null;
        public CryptoPosition? position = null;
    }

    internal static bool GetObjectInformation(object? parameter, out parameterObjects dto)
    {
        dto = new();
        if (parameter is (DataGrid _datagrid1, SignalViewModel signalViewModel, Window parentWindow1))
        {
            dto.datagrid = _datagrid1;
            dto.parentWindow = parentWindow1;
            dto.SignalViewModel = signalViewModel;
            dto.exchange = signalViewModel.Object.Symbol.Exchange;
            dto.symbol = signalViewModel.Object.Symbol;
            dto.interval = signalViewModel.Object.Interval;
            dto.signal = signalViewModel.Object;
            return true;
        }
        
        if (parameter is (DataGrid _datagrid2, SymbolViewModel symbolViewModel, Window parentWindow2))
        {
            dto.datagrid = _datagrid2;
            dto.parentWindow = parentWindow2;
            dto.SymbolViewModel = symbolViewModel;
            dto.exchange = symbolViewModel.Object.Exchange;
            dto.symbol = symbolViewModel.Object;
            dto.interval = null; // reuse selected interval GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m];
            return true;
        }
        
        if (parameter is (DataGrid _datagrid3, LiveDataViewModel liveDataViewModel, Window parentWindow3))
        {
            dto.datagrid = _datagrid3;
            dto.parentWindow = parentWindow3;
            dto.LiveDataViewModel = liveDataViewModel;
            dto.exchange = liveDataViewModel.Object.Symbol.Exchange;
            dto.symbol = liveDataViewModel.Object.Symbol;
            dto.interval = liveDataViewModel.Object.Interval;
            return true;
        }
        
        if (parameter is (DataGrid _datagrid4, PositionViewModel positionViewModel, Window parentWindow4))
        {
            dto.datagrid = _datagrid4;
            dto.parentWindow = parentWindow4;
            dto.PositionViewModel = positionViewModel;
            dto.exchange = positionViewModel.Object.Symbol.Exchange;
            dto.symbol = positionViewModel.Object.Symbol;
            dto.interval = positionViewModel.Object.Interval;
            dto.position = positionViewModel.Object;
            return true;
        }

        return false;
    }     
}
