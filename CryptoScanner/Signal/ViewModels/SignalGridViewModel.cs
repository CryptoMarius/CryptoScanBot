using Avalonia.Interactivity;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Signal.Common;
using CryptoScanner.Signal.Model;


namespace CryptoScanner.Signal.ViewModels;



/// <summary>
/// ViewModel for the Signal Grid
/// Manages trading signals and their display
/// </summary>
public partial class SignalGridViewModel : ObservableObject
{
    private DispatcherTimer? _updateTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    [ObservableProperty]
    private ObservableRangeCollection<SignalInfo> _signals = [];

    // Event voor parent ViewModel
    public event EventHandler<string>? EventOpenInInternalBrowser;

    public SignalGridViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SignalGridViewModel constructor called");

        GlobalData.AnalyzeSignalCreated = AnalyzeSignalCreated;

        _updateTimer.Tick += TimerAddSignalsTick;
        _updateTimer.Start();
    }

    public void Dispose()
    {
        _updateTimer?.Stop();
        _updateTimer = null;
    }

    public event EventHandler<SignalInfo>? RequestSortedInsert;
    public event EventHandler? RequestSort;


    private void TimerAddSignalsTick(object? sender, EventArgs e)
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
                    List<SignalInfo> newSignals = [];
                    while (GlobalData.SignalQueue.Count > 0)
                    {
                        CryptoSignal signal = GlobalData.SignalQueue.Dequeue();
                        if (signal != null)
                        {
                            var s = new SignalInfo
                            {
                                SignalObject = signal,
                            };
                            newSignals.Add(s);
                        }
                    }

                    if (newSignals.Count == 1)
                    {
                        RequestSortedInsert?.Invoke(this, newSignals[0]);
                        System.Diagnostics.Debug.WriteLine($"TimerAddSignalsTick added {newSignals.Count} signal via binsearch");
                    }
                    else
                    {
                        Signals.AddRange(newSignals);
                        RequestSort?.Invoke(this, EventArgs.Empty);
                        System.Diagnostics.Debug.WriteLine($"TimerAddSignalsTick added {newSignals.Count} signals via complete sort");
                    }

                }
                finally
                {
                    Monitor.Exit(GlobalData.SignalQueue);
                }
            }
        }
    }

    private void AnalyzeSignalCreated(CryptoSignal signal)
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

#pragma warning disable IDE0059 // Unnecessary assignment of a value
                    string soundFile = signal.Side == CryptoTradeSide.Long ?
                        x.strategySettings.SoundFileLong : x.strategySettings.SoundFileShort;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                               //PlaySound(signal, x.strategySettings.PlaySound, x.strategySettings.PlaySpeech, soundFile);
                               //GlobalData.AddTextToLogTab("Sound " + signal.Symbol.Name + " " + signal.StrategyText + " " + x.lastSignalTime.ToString());
                }
                //else GlobalData.AddTextToLogTab("Sound " + signal.Symbol.Name + " " + signal.StrategyText + " " + x.lastSignalTime.ToString() + " ignored");
            }

            //if (GlobalData.Telegram.SendSignalsToTelegram)
            //    ThreadTelegramBot.SendSignal(signal);
        }
    }

    /// <summary>
    /// Command to open signal in external program
    /// Triggered from context menu
    /// </summary>
    [RelayCommand]
    private static void OpenExternalProgram(object? parameter)
    {
        if (parameter is not SignalInfo signal)
            return;

        // Implement your external program logic here
        System.Diagnostics.Debug.WriteLine($"Opening {signal.Symbol} in external program");
    }

    [RelayCommand]
    private void LaunchTradingApp(object? parameter)
    {
        if (parameter is not SignalInfo signal)
            return;

        // Implement your external program logic here
        System.Diagnostics.Debug.WriteLine($"Opening {signal.Symbol} in external program");

        CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;


        // Voor Altrady en Hypertrader werkt dit kunstje natuurlijk niet
        if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
            tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
        GlobalData.LoadLinkSettings(); // refresh links

        var symbol = signal.SignalObject.Symbol;
        var interval = signal.SignalObject.Interval;
        if (symbol != null && interval != null)
            ActivateTradingApp(GlobalData.Settings.General.TradingApp, symbol, interval, tradingAppInternExtern);
    }

    
    [RelayCommand]
    private void LaunchTradingViewInternal(object? parameter)
    {
        if (parameter is not SignalInfo signal)
            return;

        // Implement your external program logic here
        System.Diagnostics.Debug.WriteLine($"Opening {signal.Symbol} in external program");

        CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;


        // Voor Altrady en Hypertrader werkt dit kunstje natuurlijk niet
        if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
            tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
        GlobalData.LoadLinkSettings(); // refresh links

        var symbol = signal.SignalObject.Symbol;
        var interval = signal.SignalObject.Interval;
        if (symbol != null && interval != null)
            ActivateTradingApp(GlobalData.Settings.General.TradingApp, symbol, interval, tradingAppInternExtern);
    }

    [RelayCommand]
    private void LaunchTradingViewExternal(object? parameter)
    {
        if (parameter is not SignalInfo signal)
            return;

        // Implement your external program logic here
        System.Diagnostics.Debug.WriteLine($"Opening {signal.Symbol} in external program");

        CryptoExternalUrlType tradingAppInternExtern = CryptoExternalUrlType.External;


        // Voor Altrady en Hypertrader werkt dit kunstje natuurlijk niet
        if (GlobalData.Settings.General.TradingApp == CryptoTradingApp.TradingView || GlobalData.Settings.General.TradingApp == CryptoTradingApp.ExchangeUrl)
            tradingAppInternExtern = GlobalData.Settings.General.TradingAppInternExtern;
        GlobalData.LoadLinkSettings(); // refresh links

        var symbol = signal.SignalObject.Symbol;
        var interval = signal.SignalObject.Interval;
        if (symbol != null && interval != null)
            ActivateTradingApp(GlobalData.Settings.General.TradingApp, symbol, interval, tradingAppInternExtern);
    }


    /// <summary>
    /// Command to view signal details
    /// </summary>
    [RelayCommand]
    private static void ViewDetails(object? parameter)
    {
        if (parameter is not SignalInfo signal)
            return;

        System.Diagnostics.Debug.WriteLine($"Viewing details for signal: {signal.Symbol}");
    }

    /// <summary>
    /// Command to copy signal to clipboard
    /// </summary>
    [RelayCommand]
    private static void CopySignal(object? parameter)
    {
        if (parameter is not SignalInfo signal)
            return;

        var text = $"{signal.Symbol} - {signal.Side} @ {signal.SignalPrice:F8}";
        System.Diagnostics.Debug.WriteLine($"Copying signal to clipboard: {text}");
    }

    public void ActivateTradingApp(CryptoTradingApp externalTradingApp, CryptoSymbol symbol, CryptoInterval interval, CryptoExternalUrlType viaTradingBrowser, bool activateTab = true)
    {
        // Activate the trading application (and we use a dummy browser for Altrady)

        (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(externalTradingApp, false, symbol, interval);
        if (Url != "")
        {
            GlobalData.AddTextToLogTab($"Linktools activate {Url}");
            // Open the url via our own hidden browser (to avoid the Altrady jump-step)
            //if (viaTradingBrowser == CryptoExternalUrlType.Internal)
            //{
            //await WebViewTradingView.ActivateUrlAsync(Url);
            //if (activateTab && TabControl != null)
            //    TabControl.SelectedTab = TabPageBrowser;
            //}
            //else
            {
                if (Execute == CryptoExternalUrlType.Internal)
                {
                    // Send url-event via the MainWindowViewModel
                    EventOpenInInternalBrowser?.Invoke(this, Url);
                }
                else
                {
                    // Open via the external (system) browser
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Url) { UseShellExecute = true });
                }
            }
        }
    }




}