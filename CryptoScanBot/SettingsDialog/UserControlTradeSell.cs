using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlTradeSell : UserControl
{
    private readonly SortedList<string, CryptoSellMethod> SellMethod = [];

    public UserControlTradeSell()
    {
        InitializeComponent();

        // SELL
        // Voorlopig de trailing uit, maar die zijn zeer zeker interessant..
        SellMethod.Add("Limit order op een vaste winst percentage", CryptoSellMethod.FixedPercentage);
        //SellMethod.Add("Limit order op dynamisch percentage van de BB", CryptoSellMethod.DynamicPercentage);
        //SellMethod.Add("Trace via de Keltner Channel en PSAR", CryptoSellMethod.TrailViaKcPsar);
    }

    public void LoadConfig(SettingsTrading settings)
    {
        // sell
        EditSellMethod.DataSource = new BindingSource(SellMethod, null);
        EditSellMethod.DisplayMember = "Key";
        EditSellMethod.ValueMember = "Value";
        EditSellMethod.SelectedValue = settings.SellMethod;

        EditProfitPercentage.Value = settings.ProfitPercentage;
        //EditLockProfits.Checked = settings.Trading.LockProfits;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        // sell
        settings.SellMethod = (CryptoSellMethod)EditSellMethod.SelectedValue;
        settings.ProfitPercentage = EditProfitPercentage.Value;
        //settings.Trading.LockProfits = EditLockProfits.Checked;
    }
}
