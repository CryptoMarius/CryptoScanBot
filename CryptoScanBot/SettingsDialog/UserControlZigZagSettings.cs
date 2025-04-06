using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlZigZagSettings : UserControl
{
    public UserControlZigZagSettings()
    {
        InitializeComponent();
    }

    public void LoadConfig(string caption, SettingsZigZag settings)
    {
        groupBox16.Text = caption;
        EditUseHighLow.Checked = settings.UseHighLow;
        EditUsePrimary.Checked = settings.TrendType == TrendType.Primary;
    }

    public void SaveConfig(SettingsZigZag settings)
    {
        settings.UseHighLow = EditUseHighLow.Checked;
        settings.TrendType = EditUsePrimary.Checked ? TrendType.Primary : TrendType.Secondary;
    }
}
