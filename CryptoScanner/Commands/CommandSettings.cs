using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Telegram;
using CryptoScanner.Settings.Views;

using Nito.AsyncEx;

namespace CryptoScanner.Commands;

public class CommandSettings : CommandBase
{

    private static void GetReloadRelatedSettings(out string activeQuoteData)
    {
        activeQuoteData = "";
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                activeQuoteData += "," + quoteData.Name;
        }
    }

    public override async void Execute(object? parameter)
    {
        if (parameter is not Window parentWindow)
            return;

        System.Diagnostics.Debug.WriteLine($"Settings");
        var scannerSession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("ScannerSession service not found");

        // Save some old stuff for reloading stuff
        var previousExchange = GlobalData.ActiveExchange;
        GetReloadRelatedSettings(out string previousActiveQuotes);
        string previousExchangeName = GlobalData.Settings.General.ExchangeName;

        try
        {
            var dialog = new SettingsWindow
            {
                CanResize = true,
                Title = "Chart form",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            // Hacky, needs work...
            var result = await dialog.ShowDialog<bool?>(parentWindow!);
            if (result != true)
                return;

            GlobalData.SaveSettings();
            GlobalData.SaveUserSettings(); // custom colors (not sure?)

            // Don't save exchange immediately, lots of data still in memory etc
            if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Core.Model.CryptoExchange? newActiveExchange))
                return;
            if (newActiveExchange == null)
                return;


            // Did we choose another exchange (reload)
            bool exchangeChanged = previousExchangeName != GlobalData.Settings.General.ExchangeName;
            if (exchangeChanged)
                GlobalData.AddTextToLogTab("Exchange was changed (reload)!");

            // Did we changes quotes (reload)
            GetReloadRelatedSettings(out string currentActiveQuotes);
            bool quoteChanged = previousActiveQuotes != currentActiveQuotes;
            if (quoteChanged)
                GlobalData.AddTextToLogTab("Quotes have changed (reload)!");

            if (exchangeChanged || quoteChanged)
            {
                GlobalData.AddTextToLogTab("");

                AsyncContext.Run(scannerSession.StopAsync);

                // Stop the current exchange
                if (exchangeChanged && previousExchange != null)
                {
                    previousExchange?.Clear();
                    previousExchange?.Data.Clear();
                    // TODO: Delete symbols, assets, orders, trades, positions, parts, steps from database!
                }

                // Standaard timers e.d.
                scannerSession.ApplySettings();


                // Clear candle data
                if (quoteChanged || exchangeChanged)
                {
                    foreach (var symbol in GlobalData.ActiveExchange!.SymbolListId.Values)
                    {
                        if (!symbol.QuoteData.FetchCandles || symbol.Status == 0)
                        {
                            symbol.ClearCandles();
                            //GlobalData.AddTextToLogTab($"Cleared candles for {symbol.Name}");
                        }
                    }
                }

                GlobalData.ActiveExchange!.GetApiInstance().ExchangeDefaults();

                // Schedule een reload of data
                scannerSession.ScheduleRefresh();
            }
            else scannerSession.ApplySettings();



            // Restart Telegram if token changed
            if (GlobalData.Telegram.Token != ThreadTelegramBot.Token)
                await ThreadTelegramBot.Start(GlobalData.Telegram.Token, GlobalData.Telegram.ChatId);
            ThreadTelegramBot.ChatId = GlobalData.Telegram.ChatId;


            // Change theme if needed
            ThemeVariant choosenTheme = ThemeVariant.Default;
            if (GlobalData.Settings.General.Theme == "Light")
                choosenTheme = ThemeVariant.Light;
            else if (GlobalData.Settings.General.Theme == "Dark")
                choosenTheme = ThemeVariant.Dark;

            var currentTheme = Application.Current?.ActualThemeVariant;
            if (currentTheme != choosenTheme)
                Application.Current?.RequestedThemeVariant = choosenTheme;

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
        }

    }
}
