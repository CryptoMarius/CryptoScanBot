using CryptoScanBot.Intern;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlInterval : UserControl
{

    // Gewenste trend op interval
    private readonly Dictionary<CheckBox, string> ControlList = new();

    public UserControlInterval()
    {
        InitializeComponent();
    }

    public void InitControls()
    {
        foreach (var interval in GlobalData.IntervalList)
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
