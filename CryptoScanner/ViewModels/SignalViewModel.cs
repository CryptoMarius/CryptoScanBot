using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Telegram;

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CryptoScanner.ViewModels;

public partial class SignalViewModel : BaseGridViewModel<CryptoSignal, SignalColumnEnum, SignalColumnComparer>
{
    private DispatcherTimer _timerAddSignalsFromQueue = new() { Interval = TimeSpan.FromMilliseconds(1000) };
    private DispatcherTimer _timerClearAndUpdateSignals = new() { Interval = TimeSpan.FromMinutes(1) };


    private string _currentFilter = string.Empty;


    public SignalViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SignalGridViewModel constructor called");
        SortColumn = SignalColumnEnum.Date;
        _columns = SignalColumns.GetColumns();
        _columnWidths = GetWidths(_columns);
        System.Diagnostics.Debug.WriteLine($"SignalGridViewModel: {_columns.Count} columns, {_columnWidths.Count} widths");

        GlobalData.AnalyzeSignalCreated = ReceivedCreatedSignals;

        _timerAddSignalsFromQueue.Tick += TimerAddSignalsFromQueueTick;
        _timerAddSignalsFromQueue.Start();

        _timerClearAndUpdateSignals.Tick += TimerClearAndUpdateSignalsTick;
        _timerClearAndUpdateSignals.Start();

        LoadSignals();
    }

    public void Dispose()
    {
        _timerAddSignalsFromQueue.Stop();
        _timerAddSignalsFromQueue.Tick -= TimerAddSignalsFromQueueTick;

        _timerClearAndUpdateSignals.Stop();
        _timerClearAndUpdateSignals.Tick -= TimerClearAndUpdateSignalsTick;
    }

    public void LoadSignals()
    {
        lock (_lock)
        {
            _allObjects = GlobalData.LoadSignals(_currentFilter);
            ApplySort(SortColumn);
        }

        RefreshVisibleItems();
    }

    public void OnFilterTextChanged(object? sender, string filterText)
    {
        _currentFilter = filterText;
        LoadSignals();
    }

    protected override void RefreshVisibleItems()
    {
        System.Diagnostics.Debug.WriteLine("RefreshVisibleItems called");

        if (Dispatcher.UIThread.CheckAccess())
        {
            lock (_lock)
            {
                var selectedId = SelectedObject?.Id;
                VisibleObjects = new AvaloniaList<CryptoSignal>(_allObjects);
                if (selectedId.HasValue)
                    SelectedObject = VisibleObjects.FirstOrDefault(p => p.Id == selectedId.Value);
            }
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                lock (_lock)
                {
                    var selectedId = SelectedObject?.Id;
                    VisibleObjects = new AvaloniaList<CryptoSignal>(_allObjects);
                    if (selectedId.HasValue)
                        SelectedObject = VisibleObjects.FirstOrDefault(p => p.Id == selectedId.Value);
                }
            });
        }
    }


    private void TimerAddSignalsFromQueueTick(object? sender, EventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        if (GlobalData.SignalQueue.Count == 0)
            return;

        // Background processing
        Task.Run(() =>
        {
            List<CryptoSignal> signalList = [];

            if (Monitor.TryEnter(GlobalData.SignalQueue, TimeSpan.FromMilliseconds(50)))
            {
                try
                {
                    while (GlobalData.SignalQueue.Count > 0)
                    {
                        CryptoSignal signal = GlobalData.SignalQueue.Dequeue();
                        if (signal != null)
                        {
                            var symbol = signal.Symbol;
                            if (string.IsNullOrWhiteSpace(_currentFilter) ||
                                symbol.Name.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase))
                            {
                                signalList.Add(signal);
                            }
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(GlobalData.SignalQueue);
                }
            }

            if (signalList.Count > 0)
            {
                // Modify binnen lock
                lock (_lock)
                {
                    _allObjects.AddRange(signalList);
                    ApplySort(SortColumn);
                }

                // Update UI
                RefreshVisibleItems();
            }
        });
    }

    private void ReceivedCreatedSignals(CryptoSignal signal)
    {
        GlobalData.CreatedSignalCount++;
        string text = "Signal " + signal.Symbol.Name + " " + signal.Interval.Name + " " + signal.SideText + " " + signal.StrategyText + " " + signal.EventText;
        GlobalData.AddTextToLogTab(text);

        if (!signal.IsInvalid || (signal.IsInvalid && GlobalData.Settings.General.ShowInvalidSignals))
            GlobalData.SignalQueue.Enqueue(signal);

        if (signal.BackTest)
            return;

        if (!signal.IsInvalid)
        {
            if (GlobalData.StrategiesSettings.TryGetValue(signal.Strategy, out (SettingsSignalStrategyBase strategySettings, long lastSignalTime) x))
            {
                if (signal.EventTime > x.lastSignalTime)
                {
                    x.lastSignalTime = signal.EventTime + 20;
                    GlobalData.StrategiesSettings[signal.Strategy] = x;

                    string soundFile = signal.Side == CryptoTradeSide.Long ?
                        x.strategySettings.SoundFileLong : x.strategySettings.SoundFileShort;
                    GlobalData.PlaySomeMusic(soundFile, false);
                }
            }

            if (GlobalData.Telegram.SendSignalsToTelegram)
                ThreadTelegramBot.SendSignal(signal);
        }
    }

    private static bool UpdateSignalStatistics(CryptoSignal signal)
    {
        if (!signal.BackTest) //  && signal.Strategy != CryptoSignalStrategy.Jump
        {
            try
            {
                CryptoSymbolInterval symbolInterval = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                CryptoCandle? candle = symbolInterval.CandleList.Values.LastOrDefault(); // todo, not working for emulator & dates!
                if (candle != null)
                {
                    var result = false;

                    if (candle.Low < signal.PriceMin || signal.PriceMin == 0)
                    {
                        signal.PriceMin = candle.Low;
                        signal.PriceMinPerc = (double)(100 * (signal.PriceMin / signal.SignalPrice - 1));
                        result = true;
                    }
                    else if (candle.High > signal.PriceMax || signal.PriceMax == 0)
                    {
                        signal.PriceMax = candle.High;
                        signal.PriceMaxPerc = (double)(100 * (signal.PriceMax / signal.SignalPrice - 1));
                        result = true;
                    }

#if DEBUG
                    if (signal.SignalStatus == CryptoSignalStatus.Run)
                    {
                        decimal stopLossPerc = GlobalData.Settings.Trading.StopLossPercentage / 100;
                        if (stopLossPerc != 0.0m)
                        {
                            if (signal.Side == CryptoTradeSide.Long)
                            {
                                decimal stopLossPrice = signal.SignalPrice - stopLossPerc * signal.SignalPrice;
                                if (signal.PriceMin <= stopLossPrice)
                                {
                                    signal.SignalStatus = CryptoSignalStatus.Lost;
                                    result = true;
                                }
                            }
                            else if (signal.Side == CryptoTradeSide.Short)
                            {
                                decimal stopLossPrice = signal.SignalPrice + stopLossPerc * signal.SignalPrice;
                                if (signal.PriceMax >= stopLossPrice)
                                {
                                    signal.SignalStatus = CryptoSignalStatus.Lost;
                                    result = true;
                                }
                            }
                        }
                        // still running? ;-)
                        if (signal.SignalStatus == CryptoSignalStatus.Run)
                        {
                            decimal takeProfitPercentage = GlobalData.Settings.Trading.ProfitPercentage / 100;
                            if (takeProfitPercentage != 0.0m)
                            {
                                if (signal.Side == CryptoTradeSide.Long)
                                {
                                    decimal takeProfitPrice = signal.SignalPrice + takeProfitPercentage * signal.SignalPrice;
                                    if (signal.PriceMax > takeProfitPrice)
                                    {
                                        signal.SignalStatus = CryptoSignalStatus.Win;
                                        result = true;
                                    }
                                }
                                else if (signal.Side == CryptoTradeSide.Short)
                                {
                                    decimal takeProfitPrice = signal.SignalPrice - takeProfitPercentage * signal.SignalPrice;
                                    if (signal.PriceMin < takeProfitPrice)
                                    {
                                        signal.SignalStatus = CryptoSignalStatus.Win;
                                        result = true;
                                    }
                                }
                            }
                        }
                    }
#endif
                    return result;
                }
            }
            catch
            {
                // ignore errors
            }
        }
        return false;
    }

    static long LastStatisticUpdate = 0;

    private void TimerClearAndUpdateSignalsTick(object? sender, EventArgs e)
    {
        if (GlobalData.BackTest)
            return;

        Task.Run(() =>
        {
            bool needsRefresh = false;

            lock (_lock)
            {
                if (_allObjects.Count > 0)
                {
                    long x = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
                    bool updateStats = x != LastStatisticUpdate;
                    LastStatisticUpdate = x;

                    for (int index = _allObjects.Count - 1; index >= 0; index--)
                    {
                        var signal = _allObjects[index];

                        DateTime expirationDate = signal.GetExpirationDate(signal.Interval);
                        if (expirationDate < DateTime.UtcNow)
                        {
                            _allObjects.RemoveAt(index);
                            needsRefresh = true;
                        }

                        if (GlobalData.Settings.General.DebugSignalStrength && updateStats)
                        {
                            if (UpdateSignalStatistics(signal))
                            {
                                GlobalData.ThreadSaveObjects!.AddToQueue(signal);
                            }
                        }
                    }
                }
            }

            if (needsRefresh)
            {
                RefreshVisibleItems();
            }
        });
    }
}