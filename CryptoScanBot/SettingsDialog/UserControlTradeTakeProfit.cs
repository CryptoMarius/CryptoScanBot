using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlTradeTakeProfit : UserControl
{
    private readonly SortedList<string, CryptoOrderType> OrderTypeList = [];
    private readonly SortedList<string, CryptoTakeProfitStrategy> StrategyList = [];

    public UserControlTradeTakeProfit()
    {
        InitializeComponent();

        //TakeProfitOrderType.Add("Market order", CryptoOrderType.Market); // not relevant (and not implemented)
        OrderTypeList.Add("Limit order", CryptoOrderType.Limit);
        //OrderTypeList.Add("Stop limit order", CryptoOrderType.StopLimit);
        //TakeProfitOrderType.Add("Order cancels Order (OCO)", CryptoOrderType.Oco); // not relevant (not implemented for all exchanges I assume)

        StrategyList.Add("Fixed percentage", CryptoTakeProfitStrategy.FixedPercentage);
        //StrategyList.Add("Trace via LC/PSar", CryptoSellMethod.TrailViaKcPsar);
        //StrategyList.Add("Dynamic percentage of BB", CryptoSellMethod.DynamicPercentage);
    }

    public void LoadConfig(SettingsTrading settings)
    {
        EditOrderType.DataSource = new BindingSource(OrderTypeList, null);
        EditOrderType.DisplayMember = "Key";
        EditOrderType.ValueMember = "Value";
        EditOrderType.SelectedValue = settings.TakeProfitOrderType;

        EditMethod.DataSource = new BindingSource(StrategyList, null);
        EditMethod.DisplayMember = "Key";
        EditMethod.ValueMember = "Value";
        EditMethod.SelectedValue = settings.TakeProfitStrategy;

        EditProfitPercentage.Value = settings.ProfitPercentage;
        EditAddDustToTp.Checked = settings.AddDustToTp;
        //EditLockProfits.Checked = settings.LockProfits;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.TakeProfitOrderType = (CryptoOrderType)EditOrderType.SelectedValue;
        settings.TakeProfitStrategy = (CryptoTakeProfitStrategy)EditMethod.SelectedValue;
        settings.ProfitPercentage = EditProfitPercentage.Value;
        settings.AddDustToTp = EditAddDustToTp.Checked;
        //settings.LockProfits = EditLockProfits.Checked;
    }
}
