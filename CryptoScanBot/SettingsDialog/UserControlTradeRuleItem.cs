using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlTradeRuleItem : UserControl
{
    private readonly SortedList<string, CryptoIntervalPeriod> IntervalList = new();

    public UserControlTradeRuleItem()
    {
        InitializeComponent();
    }

    public void LoadConfig(PauseTradingRule item, int index)
    {
        if (EditInterval.DataSource == null)
        {
            foreach (var interval in GlobalData.IntervalList) 
                IntervalList.Add(interval.Name, interval.IntervalPeriod);

            EditInterval.DataSource = new BindingSource(IntervalList, null);
            EditInterval.DisplayMember = "Key";
            EditInterval.ValueMember = "Value";
        }


        groupBoxDca.Text = $"Trading rule {index}";

        EditSymbol.Text = item.Symbol;
        EditInterval.SelectedValue = item.Interval;
        EditPercent.Value = (decimal)item.Percentage;
        EditCandles.Value = item.Candles;
        EditCoolDown.Value = item.CoolDown;
    }

    public void SaveConfig(PauseTradingRule item)
    {
        item.Symbol = EditSymbol.Text;
        item.Percentage = (double)EditPercent.Value;
        item.Candles = (int)EditCandles.Value;
        item.Interval = (CryptoIntervalPeriod)EditInterval.SelectedValue; 
        item.CoolDown = (int)EditCoolDown.Value;
    }


}
