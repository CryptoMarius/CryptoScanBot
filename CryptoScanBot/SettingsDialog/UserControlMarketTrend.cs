using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlMarketTrend : UserControl
{
    public UserControlMarketTrend()
    {
        InitializeComponent();
    }

    public void LoadConfig(SettingsTextualMarketTrend settings)
    {
        EditLog.Checked = settings.Log;

        if (settings.List.Any())
            UserControlTrendRange.SetChecked("Trend", true, (settings.List[0]));
        else
            UserControlTrendRange.SetChecked("Trend", false, (-100, 100));
    }

    public void SaveConfig(SettingsTextualMarketTrend settings)
    {
        settings.List.Clear();
        var (isActive, range) = UserControlTrendRange.GetChecked();
        if (isActive)
            settings.List.Add(range);
        settings.Log = EditLog.Checked;
    }
}
