using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner.SettingsDialog;

public partial class UserControlTradeDcaItem : UserControl
{
    public UserControlTradeDcaItem()
    {
        InitializeComponent();
    }

    public void LoadConfig(CryptoDcaEntry dca, int dcaIndex)
    {
        groupBoxDca.Text = $"DCA {dcaIndex}";
        EditPercent.Value = dca.Percentage;
        EditFactor.Value = dca.Factor;
    }

    public void SaveConfig(CryptoDcaEntry dca)
    {
        dca.Percentage = EditPercent.Value;
        dca.Factor = EditFactor.Value;
    }
}
