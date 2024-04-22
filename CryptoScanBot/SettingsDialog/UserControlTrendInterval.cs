using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlTrendInterval : UserControl
{

    // Gewenste trend op interval
    private readonly Dictionary<CheckBox, string> ControlList = new();

    public UserControlTrendInterval()
    {
        InitializeComponent();
    }

    public void InitControls(CryptoTradeSide side)
    {
        string text;
        if (side == CryptoTradeSide.Long)
            text = "bullish";
        else
            text = "bearish";

        foreach (var interval in GlobalData.IntervalList)
        {
            CheckBox checkbox = new()
            {
                AutoSize = true,
                UseVisualStyleBackColor = true,
                Text = interval.Name + " interval=" + text,
            };
            flowLayoutPanel1.Controls.Add(checkbox);
            ControlList.Add(checkbox, interval.Name);
        }
    }

    public void LoadConfig(SettingsTextualIntervalTrend settings)
    {
        foreach (var item in ControlList)
            item.Key.Checked = settings.List.Contains(item.Value);
    }

    public void SaveConfig(SettingsTextualIntervalTrend settings)
    {
        settings.List.Clear();
        foreach (var item in ControlList)
        {
            if (item.Key.Checked)
                settings.List.Add(item.Value);
        }
    }
}
