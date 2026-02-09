using Avalonia.Controls;

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
    internal class ParameterObjects
    {
        public ListBox? listBox = null;
        public Window? parentWindow = null;

        public SymbolViewModel? SymbolViewModel = null;
        public SignalViewModel? SignalViewModel = null;
        public LiveDataViewModel? LiveDataViewModel = null;
        public PositionOpenViewModel? PositionOpenViewModel = null;
        public PositionClosedViewModel? PositionClosedViewModel = null;

        public Core.Model.CryptoExchange? exchange;
        public CryptoSymbol? symbol = null;
        public CryptoInterval? interval = null;
        public CryptoSignal? signal = null;
        public CryptoPosition? position = null;
    }

    internal static bool GetObjectInformation(object? parameter, out ParameterObjects dto)
    {
        dto = new();
        if (parameter is (ListBox _listbox1, CryptoSignal signal1, SignalViewModel viewModel1, Window parentWindow1))
        {
            dto.listBox = _listbox1;
            dto.parentWindow = parentWindow1;
            dto.SignalViewModel = viewModel1;

            dto.signal = signal1;
            dto.exchange = signal1?.Exchange;
            dto.symbol = signal1?.Symbol;
            dto.interval = signal1?.Interval;
            return true;
        }


        if (parameter is (ListBox _listbox2, CryptoSymbol symbol1, SymbolViewModel viewModel2, Window parentWindow2))
        {
            dto.listBox = _listbox2;
            dto.parentWindow = parentWindow2;
            dto.SymbolViewModel = viewModel2;

            dto.symbol = symbol1;
            dto.exchange = symbol1.Exchange;
            dto.interval = null; // reuse selected interval GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m];
            return true;
        }

        if (parameter is (ListBox _listbox3, CryptoLiveData livedata3, LiveDataViewModel viewModel3, Window parentWindow3))
        {
            dto.listBox = _listbox3;
            dto.parentWindow = parentWindow3;
            dto.LiveDataViewModel = viewModel3;

            dto.exchange = livedata3.Symbol.Exchange;
            dto.symbol = livedata3.Symbol;
            dto.interval = livedata3.Interval;
            return true;
        }

        if (parameter is (ListBox _listbox4, CryptoPosition position4, PositionOpenViewModel viewModel4, Window parentWindow4))
        {
            dto.listBox = _listbox4;
            dto.parentWindow = parentWindow4;
            dto.PositionOpenViewModel= viewModel4;

            dto.position = position4;
            dto.exchange = position4.Symbol.Exchange;
            dto.symbol = position4.Symbol;
            dto.interval = position4.Interval;
            return true;
        }

        if (parameter is (ListBox _listbox5, CryptoPosition position5, PositionClosedViewModel viewModel5, Window parentWindow5))
        {
            dto.listBox = _listbox5;
            dto.parentWindow = parentWindow5;
            dto.PositionClosedViewModel = viewModel5;

            dto.position = position5;
            dto.exchange = position5.Symbol.Exchange;
            dto.symbol = position5.Symbol;
            dto.interval = position5.Interval;
            return true;
        }

        return false;
    }
}
