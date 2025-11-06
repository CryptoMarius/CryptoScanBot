using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

namespace CryptoScanner.SettingsDialog;

public partial class UserControlInterval : UserControl
{
    private readonly Dictionary<CheckBox, string> ControlList = [];

    public UserControlInterval()
    {
        InitializeComponent();
    }

    public void InitControls(CryptoIntervalPeriod minimalInterval = CryptoIntervalPeriod.interval1m)
    {
        foreach (var interval in GlobalData.IntervalList)
        {
            if (interval.IntervalPeriod >= minimalInterval)
            {
                CheckBox checkbox = new()
                {
                    AutoSize = true,
                    UseVisualStyleBackColor = true,
                    Text = interval.Name,
                };
                flowLayoutPanel1.Controls.Add(checkbox);
                ControlList.Add(checkbox, interval.Name);
            }
        }
    }

    public void LoadConfig(List<string> settings)
    {
        foreach (var item in ControlList)
            item.Key.Checked = settings.Contains(item.Value);
    }

    public void SaveConfig(List<string> settings)
    {
        settings.Clear();
        foreach (var item in ControlList)
        {
            if (item.Key.Checked)
                settings.Add(item.Value);
        }
    }
}
