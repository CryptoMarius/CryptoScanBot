using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.EmulatorOld;

public class EmulatorMainCode
{
    // All the code used sofar for the emulator


    //MenuMain.AddSeperator();
    //ApplicationBackTestMode = MenuMain.AddCommand(null, "Backtest mode", Command.None, ApplicationBackTestMode_Click);
    //ApplicationBackTestExec = MenuMain.AddCommand(null, "Backtest exec", Command.None, BacktestToolStripMenuItem_Click);

    //private readonly ToolStripMenuItemCommand ApplicationBackTestMode;
    //private readonly ToolStripMenuItemCommand ApplicationBackTestExec;

    //private void SetApplicationTitle()
    //{
    //    string text = $"{GlobalData.AppName} {GlobalData.AppVersion} {GlobalData.Settings.General.ExchangeName} {GlobalData.Settings.General.ExtraCaption}".Trim();
    //    if (GlobalData.IsEmulatorMode)
    //        text += " (backtest mode)";
    //    // Adjust the application title
    //    Text = text;
    //}

    //private async void BacktestToolStripMenuItem_Click(object? sender, EventArgs? e)
    //{
    /// TODO: Deze code verhuizen naar aparte class of het dialoog zelf?
    /// Probleem: Door recente aanpassingen lopen de meldingen en accounts 
    /// allemaal door elkaar (misschien een extra tabsheet met de resultaten?)
    /// (waarschijnlijk werkt het niets eens meer! was tijdelijk experiment)

    //    try
    //    {
    //        AskSymbolDialog form = new()
    //        {
    //            StartPosition = FormStartPosition.CenterParent
    //        };
    //        if (form.ShowDialog() == DialogResult.OK)
    //        {
    //            GlobalData.SaveSettings();

    //            if (!GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Core.Model.CryptoExchange? exchange))
    //            {
    //                MessageBox.Show("Exchange bestaat niet");
    //                return;
    //            }

    //            // Bestaat de coin? (uiteraard, net geladen)
    //            if (!exchange.SymbolListName.TryGetValue(GlobalData.Settings.BackTest.BackTestSymbol, out CryptoSymbol? symbol))
    //            {
    //                MessageBox.Show("Symbol bestaat niet");
    //                return;
    //            }

    //            if (!GlobalData.IsEmulatorMode)
    //            {
    //                ApplicationBackTestMode_Click(sender, e);
    //                if (GlobalData.ActiveAccount!.AccountType == CryptoAccountType.PaperTrade)
    //                    await PaperTrading.CheckPositionsAfterRestart(GlobalData.ActiveAccount!);
    //            }

    //            BackTestAsync();
    //        }
    //    }
    //    catch (Exception error)
    //    {
    //        ScannerLog.Logger.Error(error, "");
    //        GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
    //    }

    //}


    //private void ApplicationBackTestMode_Click(object? sender, EventArgs? e)
    //{
    //    ApplicationBackTestMode.Checked = !ApplicationBackTestMode.Checked;
    //    if (ApplicationBackTestMode.Checked)
    //    {
    //        GlobalData.IsEmulatorMode = true;
    //        GlobalData.IsEmulatorModeDateTime = GlobalData.Settings.BackTest.BackTestStartTime;
    //        GlobalData.Settings.Trading.ActiveBackup = GlobalData.Settings.Trading.Active;
    //        GlobalData.Settings.Trading.Active = true;
    //    }
    //    else
    //    {
    //        GlobalData.IsEmulatorMode = false;
    //        GlobalData.Settings.Trading.Active = GlobalData.Settings.Trading.ActiveBackup;
    //    }
    //    ApplicationTradingBot.Enabled = !GlobalData.IsEmulatorMode;
    //    ApplicationPlaySounds.Enabled = !GlobalData.IsEmulatorMode;
    //    ApplicationCreateSignals.Enabled = !GlobalData.IsEmulatorMode;
    //    ApplicationBackTestExec.Enabled = GlobalData.IsEmulatorMode;

    //    GlobalData.SaveSettings();
    //    SetApplicationTitle();

    //    GlobalData.SetTradingAccounts();
    //    RefreshDataGrids();

    //    // Resume scanner session, fill missing information
    //    if (!GlobalData.IsEmulatorMode)
    //        ToolStripMenuItemRefresh_Click_1(null, null);
    //}


}
