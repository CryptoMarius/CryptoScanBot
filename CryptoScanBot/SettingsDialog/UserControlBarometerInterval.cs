namespace CryptoScanBot.SettingsDialog;

public partial class UserControlBarometerInterval : UserControl
{
    public UserControlBarometerInterval()
    {
        InitializeComponent();

        EditIsActive.Click += EditIsActiveChecked;
    }

    private void EditIsActiveChecked(object? sender, EventArgs? e)
    {
        EditMinimal.Enabled = EditIsActive.Checked;
        EditMaximal.Enabled = EditIsActive.Checked;
    }

    public void SetChecked(string interval, Dictionary<string, (decimal minValue, decimal maxValue)> barometer)
    {
        EditIsActive.Text = interval;

        EditMinimal.Minimum = -999;
        EditMinimal.Maximum = +999;

        EditMaximal.Minimum = -999;
        EditMaximal.Maximum = +999;

        if (barometer.TryGetValue(interval, out var value))
        {
            EditIsActive.Checked = true;
            EditMinimal.Value = value.minValue;
            EditMaximal.Value = value.maxValue;
        }
        else
        {
            EditIsActive.Checked = false;
            EditMinimal.Value = -999;
            EditMaximal.Value = 999;
        }
        EditIsActiveChecked(null, null);
    }

    public (bool Checked, (decimal minValue, decimal maxValue)) GetChecked()
    {
        if (EditMinimal.Value > EditMaximal.Value)
            return (EditIsActive.Checked, (EditMaximal.Value, EditMinimal.Value));
        else
            return (EditIsActive.Checked, (EditMinimal.Value, EditMaximal.Value));
    }

}
