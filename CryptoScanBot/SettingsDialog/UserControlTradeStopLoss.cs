using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlTradeStopLoss : UserControl
{
    public UserControlTradeStopLoss()
    {
        InitializeComponent();
        //EditUseStopLoss.Click += UseStopLossClicked;
    }

    public void LoadConfig(SettingsTrading settings)
    {
        //EditUseStopLoss.Checked = settings.UseStopLoss;
        EditStopPercentage.Value = settings.StopLossPercentage;
        EditStopLimitPercentage.Value = settings.StopLossLimitPercentage;

        //UseStopLossClicked(null, null);
    }

    public void SaveConfig(SettingsTrading settings)
    {
        //settings.UseStopLoss = EditUseStopLoss.Checked;
        settings.StopLossPercentage = EditStopPercentage.Value;
        settings.StopLossLimitPercentage = EditStopLimitPercentage.Value;
    }

    //private void UseStopLossClicked(object? sender, EventArgs? e)
    //{
    //    LabelStopPercentage.Enabled = EditUseStopLoss.Checked;
    //    EditStopPercentage.Enabled = EditUseStopLoss.Checked;
    //    LabelStopLimitPercentage.Enabled = EditUseStopLoss.Checked;
    //    EditStopLimitPercentage.Enabled = EditUseStopLoss.Checked;
    //}

}
