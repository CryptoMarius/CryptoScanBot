using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner.SettingsDialog;

public partial class UserControlBarometer : UserControl
{
    public UserControlBarometer()
    {
        InitializeComponent();
    }

    public void InitControls(string caption)
    {
        EditGroupBox.Text = caption;
    }

    public void LoadConfig(SettingsTextualBarometer settings)
    {
        UserControlBarometer15m.SetChecked("15m", settings.List);
        UserControlBarometer30m.SetChecked("30m", settings.List);
        UserControlBarometer1h.SetChecked("1h", settings.List);
        UserControlBarometer4h.SetChecked("4h", settings.List);
        UserControlBarometer1d.SetChecked("1d", settings.List);

        EditBarometerLog.Checked = settings.Log;
    }

    private static void GetChecked(string interval, UserControlBarometerInterval userControl, Dictionary<string, (decimal minValue, decimal maxValue)> barometer)
    {
        var value = userControl.GetChecked();
        if (value.Checked)
            barometer.Add(interval, value.Item2);
    }

    public void SaveConfig(SettingsTextualBarometer settings)
    {
        settings.List.Clear();
        GetChecked("15m", UserControlBarometer15m, settings.List);
        GetChecked("30m", UserControlBarometer30m, settings.List);
        GetChecked("1h", UserControlBarometer1h, settings.List);
        GetChecked("4h", UserControlBarometer4h, settings.List);
        GetChecked("1d", UserControlBarometer1d, settings.List);

        settings.Log = EditBarometerLog.Checked;
    }
}
