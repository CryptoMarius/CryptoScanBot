using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Telegram;
using CryptoScanner.Model;


namespace CryptoScanner.ViewModels;

public partial class SignalGridViewModel : ObservableObject
{
    private DispatcherTimer? _timerAddSignalsFromQueue = new() { Interval = TimeSpan.FromMilliseconds(1000) };
    private DispatcherTimer? _timerClearAndUpdateSignals = new () { Interval = TimeSpan.FromMinutes(1) };

    [ObservableProperty]
    private ObservableRangeCollection<SignalViewModel> _signals = [];


    public SignalGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SignalGridViewModel constructor called");

        GlobalData.AnalyzeSignalCreated = ReceivedCreatedSignals;

        _timerAddSignalsFromQueue.Tick += TimerAddSignalsFromQueueTick;
        _timerAddSignalsFromQueue.Start();

        //_timerUpdatePositions.Tick += async (s, e) => await TimerClearAndUpdateSignalsTick();
        _timerClearAndUpdateSignals.Tick += TimerClearAndUpdateSignalsTick;
        _timerClearAndUpdateSignals.Start();

        // Should go via the filter event, but that is obviously not working..
        //Signals.Clear();
        GlobalData.LoadSignals(_currentFilter);
    }

    public void Dispose()
    {
        _timerAddSignalsFromQueue?.Stop();
        _timerAddSignalsFromQueue = null;

        _timerClearAndUpdateSignals?.Stop();
        _timerClearAndUpdateSignals = null;
    }

    public event EventHandler<SignalViewModel>? RequestSortedInsert;
    public event EventHandler? RequestSort;


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
                            }
                        }
                    }

                    if (signalList.Count == 1)
                    {
                        RequestSortedInsert?.Invoke(this, signalList[0]);
                        System.Diagnostics.Debug.WriteLine($"TimerAddSignalsTick added {signalList.Count} signal via binsearch");
                    }
                    else
                    {
                        Signals.AddRange(signalList);
                        RequestSort?.Invoke(this, EventArgs.Empty);
                        System.Diagnostics.Debug.WriteLine($"TimerAddSignalsTick added {signalList.Count} signals via complete sort");
                    }

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
            if (GlobalData.StrategiesSettings.TryGetValue(signal.Strategy, out (SettingsSignalStrategyBase strategySettings, long lastSignalTime) x))
            {
                if (signal.EventTime > x.lastSignalTime)
                {
                    // Stay silent for the next 20 seconds (for his strategy)
                    x.lastSignalTime = signal.EventTime + 20;
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

    static long LastStatisticUpdate = 0;

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
                    long x = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
                    bool updateStats = x != LastStatisticUpdate;
                    LastStatisticUpdate = x;

                    for (int index = Signals.Count - 1; index >= 0; index--)
                    {
                        var signalInfo = Signals[index];
                        var signal = signalInfo.Object;

                        DateTime expirationDate = signal.GetExpirationDate(signal.Interval);
                        if (expirationDate < DateTime.UtcNow)
                        {
                            Signals.RemoveAt(index);
                            updateStats = true;
                        }

                        if (GlobalData.Settings.General.DebugSignalStrength && updateStats)
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
        GlobalData.LoadSignals(_currentFilter);

        // Request sort na filtering
        RequestSort?.Invoke(this, EventArgs.Empty);
    }

}