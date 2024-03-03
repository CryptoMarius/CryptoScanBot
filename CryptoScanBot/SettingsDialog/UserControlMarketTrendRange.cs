namespace CryptoScanBot.SettingsDialog;

public partial class UserControlMarketTrendRange : UserControl
{
    public UserControlMarketTrendRange()
    {
        InitializeComponent();

        EditIsActive.Click += EditIsActiveChecked;
    }

    private void EditIsActiveChecked(object sender, EventArgs e)
    {
        EditMinimal.Enabled = EditIsActive.Checked;
        EditMaximal.Enabled = EditIsActive.Checked;
    }

    public void SetChecked(string caption, bool isActive, (decimal minValue, decimal maxValue) range)
    {
        EditIsActive.Text = caption;

        EditMinimal.Minimum = -100;
        EditMinimal.Maximum = +100;

        EditMaximal.Minimum = -100;
        EditMaximal.Maximum = +100;

        if (isActive)
        {
            EditIsActive.Checked = isActive;
            EditMinimal.Value = range.minValue;
            EditMaximal.Value = range.maxValue;
        }
        else
        {
            EditIsActive.Checked = false;
            EditMinimal.Value = -100;
            EditMaximal.Value = 100;
        }
        EditIsActiveChecked(null, null);
    }

    public (bool isActive, (decimal minValue, decimal maxValue) range) GetChecked()
    {
        if (EditMinimal.Value > EditMaximal.Value)
            return (EditIsActive.Checked, (EditMaximal.Value, EditMinimal.Value));
        else
            return (EditIsActive.Checked, (EditMinimal.Value, EditMaximal.Value));
    }

}
