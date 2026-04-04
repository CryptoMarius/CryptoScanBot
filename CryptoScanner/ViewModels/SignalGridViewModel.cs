using Avalonia.Collections;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Telegram;

namespace CryptoScanner.ViewModels;

public partial class SignalGridViewModel : ObservableObject
{
    private DispatcherTimer _timerAddSignalsFromQueue = new() { Interval = TimeSpan.FromMilliseconds(1000) };
    private DispatcherTimer _timerClearAndUpdateSignals = new() { Interval = TimeSpan.FromMinutes(1) };

    [ObservableProperty]
    private AvaloniaList<SignalViewModel> _signals = [];


    public SignalGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SignalGridViewModel constructor called");

        GlobalData.AnalyzeSignalCreated = ReceivedCreatedSignals;

        WeakReferenceMessenger.Default.Register<ConfigurationChangedMessage>(this, OnConfigurationChanged);

        _timerAddSignalsFromQueue.Tick += TimerAddSignalsFromQueueTick;
        _timerAddSignalsFromQueue.Start();

        //_timerUpdatePositions.Tick += async (s, e) => await TimerClearAndUpdateSignalsTick();
        _timerClearAndUpdateSignals.Tick += TimerClearAndUpdateSignalsTick;
        _timerClearAndUpdateSignals.Start();

        // Load signals
        var viewModels = GlobalData.LoadSignals(_currentFilter).Select(signal => new SignalViewModel { Object = signal });
        Signals = [.. viewModels];
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<ConfigurationChangedMessage>(this);

        _timerAddSignalsFromQueue.Stop();
        _timerAddSignalsFromQueue.Tick -= TimerAddSignalsFromQueueTick;

        _timerClearAndUpdateSignals.Stop();
        _timerClearAndUpdateSignals.Tick -= TimerClearAndUpdateSignalsTick;
    }

    private void OnConfigurationChanged(object recipient, ConfigurationChangedMessage message)
    {
        foreach (var signal in Signals)
            signal.ResetColors();
    }


    private void TimerAddSignalsFromQueueTick(object? sender, EventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        // Speed up adding signals
        if (GlobalData.SignalQueue.Count > 0)
        {
            if (Monitor.TryEnter(GlobalData.SignalQueue))
            {
                try
                {
                    List<SignalViewModel> signalList = [];
                    while (GlobalData.SignalQueue.Count > 0)
                    {
                        CryptoSignal signal = GlobalData.SignalQueue.Dequeue();
                        if (signal != null)
                        {
                            var symbol = signal.Symbol;
                            if (string.IsNullOrWhiteSpace(_currentFilter) || symbol.Name.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase))
                            {
                                var s = new SignalViewModel
                                {
                                    Object = signal,
                                };
                                signalList.Add(s);
                                //Signals.Add(s);
                            }
                        }
                    }
                    Signals.AddRange(signalList);
                }
                finally
                {
                    Monitor.Exit(GlobalData.SignalQueue);
                }
            }
        }
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
            if (GlobalData.StrategiesSettings.TryGetValue(signal.Strategy, out (SettingsSignalStrategyBase strategySettings, DateTime lastSignalTime) x))
            {
                if (signal.CloseDate > x.lastSignalTime)
                {
                    // Stay silent for the next 20 seconds (for his strategy)
                    x.lastSignalTime = signal.CloseDate.AddSeconds(20);
                    GlobalData.StrategiesSettings[signal.Strategy] = x;

                    string soundFile = signal.Side == CryptoTradeSide.Long ?
                        x.strategySettings.SoundFileLong : x.strategySettings.SoundFileShort;
                    GlobalData.PlaySomeMusic(soundFile, false);
                    //GlobalData.AddTextToLogTab("Sound " + signal.Symbol.Name + " " + signal.StrategyText + " " + x.lastSignalTime.ToString());
                }
                //else GlobalData.AddTextToLogTab("Sound " + signal.Symbol.Name + " " + signal.StrategyText + " " + x.lastSignalTime.ToString() + " ignored");
            }

            if (GlobalData.Telegram.SendSignalsToTelegram)
                ThreadTelegramBot.SendSignal(signal);
        }
    }

    static CandleTime LastStatisticUpdate = CandleTime.MinValue;

    private void TimerClearAndUpdateSignalsTick(object? sender, EventArgs e)
    {
        if (GlobalData.BackTest)
            return;

        // Avoid duplicate calls (when the list is serious long)
        if (Monitor.TryEnter(Signals))
        {
            try
            {
                // Circa 1x per minuut de verouderde signalen opruimen
                if (Signals.Count > 0)
                {
                    // Avoid frequent updates
                    CandleTime x = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1);
                    bool updateStats = x != LastStatisticUpdate;
                    LastStatisticUpdate = x;

                    for (int index = Signals.Count - 1; index >= 0; index--)
                    {
                        var signalInfo = Signals[index];
                        var signal = signalInfo.Object;

                        DateTime expirationDate = Helper.GetExpirationDate(signal, signal.Interval);
                        if (expirationDate < DateTime.UtcNow)
                        {
                            Signals.RemoveAt(index);
                            updateStats = true;
                        }

                        if (updateStats)
                        {
                            if (signalInfo.UpdateSignalStatistics())
                            {
                                GlobalData.ThreadSaveObjects!.AddToQueue(signal);
                            }
                        }
                    }
                }
            }
            finally
            {
                Monitor.Exit(Signals);
            }
        }
    }

    string _currentFilter = string.Empty;
    public void OnFilterTextChanged(object? sender, string filterText)
    {
        _currentFilter = filterText;

        Signals.Clear();
        var list = GlobalData.LoadSignals(_currentFilter);
        foreach (var signal in list)
        {
            var s = new SignalViewModel
            {
                Object = signal,
            };
            Signals.Add(s);
        }
    }

}