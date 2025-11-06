using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.SettingsDialog;

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
