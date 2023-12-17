namespace CryptoSbmScanner.SettingsDialog;

public partial class UserControlBarometerInterval : UserControl
{
    public UserControlBarometerInterval()
    {
        InitializeComponent();

        EditBarometer.Click += EditBarometerChecked;
    }

    private void EditBarometerChecked(object sender, EventArgs e)
    {
        EditBarometerMin.Enabled = EditBarometer.Checked;
        EditBarometerMax.Enabled = EditBarometer.Checked;
    }

    public void SetChecked(string interval, Dictionary<string, (decimal minValue, decimal maxValue)> barometer)
    {
        EditBarometer.Text = interval;

        EditBarometerMin.Minimum = -999;
        EditBarometerMin.Maximum = +999;

        EditBarometerMax.Minimum = -999;
        EditBarometerMax.Maximum = +999;

        if (barometer.TryGetValue(interval, out var value))
        {
            EditBarometer.Checked = true;
            EditBarometerMin.Value = value.minValue;
            EditBarometerMax.Value = value.maxValue;
        }
        else
        {
            EditBarometer.Checked = false;
            EditBarometerMin.Value = -999;
            EditBarometerMax.Value = 999;
        }
        EditBarometerChecked(null, null);
    }

    public (bool Checked, (decimal minValue, decimal maxValue)) GetChecked()
    {
        return (EditBarometer.Checked, (EditBarometerMin.Value, EditBarometerMax.Value));
    }

}
