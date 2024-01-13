using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner.SettingsDialog;

public partial class UserControlTradeRule : UserControl
{
    private readonly List<UserControlTradeRuleItem> UserControlTradeRuleList = [];

    public UserControlTradeRule()
    {
        InitializeComponent();

        panelTradeRuleMethod.AutoSize = true;
        panelTradeRuleMethod.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        panelTradeRule.AutoSize = true;
        panelTradeRule.AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    public void LoadConfig(SettingsTrading settings)
    {
        foreach (PauseTradingRule item in settings.PauseTradingRules)
        {
            var control = new UserControlTradeRuleItem();
            UserControlTradeRuleList.Add(control);
            panelTradeRule.Controls.Add(control);
            control.LoadConfig(item, UserControlTradeRuleList.Count);
        }
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.PauseTradingRules.Clear();
        foreach (UserControlTradeRuleItem control in UserControlTradeRuleList)
        {
            var item = new PauseTradingRule();
            settings.PauseTradingRules.Add(item);
            control.SaveConfig(item);
        }
    }

    private void ButtonDcaAddClick(object sender, EventArgs e)
    {
        // Een item toevoegen
        PauseTradingRule item = new();
        var control = new UserControlTradeRuleItem();

        // Wat defaults
        item.Symbol = "BTCUSDT";
        item.Interval = CryptoIntervalPeriod.interval5m;
        item.Candles = 5;
        item.Percentage = 4;
        item.CoolDown = 20;

        UserControlTradeRuleList.Add(control);
        panelTradeRule.Controls.Add(control);

        control.LoadConfig(item, UserControlTradeRuleList.Count);
    }

    private void ButtonDcaDelClick(object sender, EventArgs e)
    {
        // Verwijder het laatste item
        if (UserControlTradeRuleList.Count != 0)
        {
            var control = UserControlTradeRuleList[^1];
            UserControlTradeRuleList.Remove(control);
            panelTradeRule.Controls.Remove(control);
        }
    }
}
