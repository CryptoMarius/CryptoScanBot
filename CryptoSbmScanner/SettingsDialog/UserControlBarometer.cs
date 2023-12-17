using CryptoSbmScanner.Enums;

namespace CryptoSbmScanner.SettingsDialog;

public partial class UserControlBarometer : UserControl
{
    //private CryptoTradeSide TradeSide;

    public UserControlBarometer()
    {
        InitializeComponent();
    }

    public void InitControls(string caption)
    {
        EditGroupBox.Text = caption;
    }

    public void LoadConfig(Dictionary<string, (decimal minValue, decimal maxValue)> barometer, bool logBarometer)
    {
        UserControlBarometer15m.SetChecked("15m", barometer);
        UserControlBarometer30m.SetChecked("30m", barometer);
        UserControlBarometer1h.SetChecked("1h", barometer);
        UserControlBarometer4h.SetChecked("4h", barometer);
        UserControlBarometer1d.SetChecked("1d", barometer);

        EditBarometerLog.Checked = logBarometer;
    }

    private static void GetChecked(string interval, UserControlBarometerInterval edit, ref Dictionary<string, (decimal minValue, decimal maxValue)> barometer)
    {
        var value = edit.GetChecked();
        if (value.Checked)
            barometer.Add(interval, value.Item2);
    }

    public Dictionary<string, (decimal minValue, decimal maxValue)> SaveConfig(ref bool logBarometer)
    {
        Dictionary<string, (decimal minValue, decimal maxValue)> list = new();
        GetChecked("15m", UserControlBarometer15m, ref list);
        GetChecked("30m", UserControlBarometer30m, ref list);
        GetChecked("1h", UserControlBarometer1h, ref list);
        GetChecked("4h", UserControlBarometer4h, ref list);
        GetChecked("1d", UserControlBarometer1d, ref list);

        logBarometer = EditBarometerLog.Checked;
        return list;
    }
}
