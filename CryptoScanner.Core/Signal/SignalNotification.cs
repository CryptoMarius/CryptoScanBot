using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Telegram;

namespace CryptoScanner.Core.Signal;

/// <summary>
/// Host independent handling of a freshly created signal: counter, log line, the queue the grids
/// read from, the per strategy sound and the Telegram notification.
/// <para>
/// This used to live inside the Avalonia SignalGridViewModel, which meant the Photino/Web hosts
/// silently skipped all of it (no signal counter on the dashboard, no log line, no sound, no
/// Telegram message and no filtering of invalid signals). Every host now routes its
/// <see cref="GlobalData.AnalyzeSignalCreated"/> handler through here.
/// </para>
/// </summary>
public static class SignalNotification
{
    /// <summary>
    /// Process a newly created signal. Safe to call from any thread.
    /// </summary>
    public static void HandleCreatedSignal(CryptoSignal signal)
    {
        GlobalData.CreatedSignalCount++;

        string text = "Signal " + signal.Symbol.Name + " " + signal.Interval.Name + " " + signal.SideText
            + " " + signal.StrategyText + " " + signal.EventText;
        GlobalData.AddTextToLogTab(text);

        if (!signal.IsInvalid || (signal.IsInvalid && GlobalData.Settings.General.ShowInvalidSignals))
        {
            // Queue<T> is not thread-safe; enqueue under the same lock the consumer/clear use
            // to avoid a corrupted internal array ("Source array was not long enough" during resize)
            lock (GlobalData.SignalQueue)
            {
                GlobalData.SignalQueue.Enqueue(signal);
            }
        }

        if (!signal.IsInvalid)
        {
            if (GlobalData.StrategiesSettings.TryGetValue(signal.Strategy, out (SettingsSignalStrategyBase strategySettings, DateTime lastSignalTime) x))
            {
                if (x.strategySettings.PlaySound && signal.CloseDate > x.lastSignalTime)
                {
                    // Stay silent for the next 20 seconds (for his strategy)
                    x.lastSignalTime = signal.CloseDate.AddSeconds(20);
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
}
