using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlTradeEntry : UserControl
{
    private readonly SortedList<string, CryptoOrderType> OrderTypeList = [];
    private readonly SortedList<string, CryptoEntryOrDcaPricing> PricingList = [];
    private readonly SortedList<string, CryptoEntryOrDcaStrategy> StrategyList = [];

    public UserControlTradeEntry()
    {
        InitializeComponent();

        // How to buy
        OrderTypeList.Add("Market order", CryptoOrderType.Market);
        OrderTypeList.Add("Limit order", CryptoOrderType.Limit);
        //OrderTypeList.Add("Stop limit order", CryptoOrderType.StopLimit);
        //OrderTypeList.Add("Order cancels Order (OCO)", CryptoOrderType.Oco); // not relevant (not implemented for all exchanges I assume)

        // When to buy
        StrategyList.Add("After a signal", CryptoEntryOrDcaStrategy.AfterNextSignal);
        //StrategyList.Add("Trace via KC/PSar", CryptoBuyStepInMethod.TrailViaKcPsar);

        // At which price
        PricingList.Add("Market price", CryptoEntryOrDcaPricing.MarketPrice);
        PricingList.Add("Signal price", CryptoEntryOrDcaPricing.SignalPrice);
        PricingList.Add("Bid price", CryptoEntryOrDcaPricing.BidPrice);
        PricingList.Add("Ask price", CryptoEntryOrDcaPricing.AskPrice);
    }

    public void LoadConfig(SettingsTrading settings)
    {
        EditOrderType.DataSource = new BindingSource(OrderTypeList, null);
        EditOrderType.DisplayMember = "Key";
        EditOrderType.ValueMember = "Value";
        EditOrderType.SelectedValue = settings.EntryOrderType;

        EditStrategy.DataSource = new BindingSource(StrategyList, null);
        EditStrategy.DisplayMember = "Key";
        EditStrategy.ValueMember = "Value";
        EditStrategy.SelectedValue = settings.EntryStrategy;

        EditPricing.DataSource = new BindingSource(PricingList, null);
        EditPricing.DisplayMember = "Key";
        EditPricing.ValueMember = "Value";
        EditPricing.SelectedValue = settings.EntryOrderPrice;

        EditGlobalBuyRemoveTime.Value = settings.EntryRemoveTime;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.EntryOrderType = (CryptoOrderType)EditOrderType.SelectedValue;
        settings.EntryStrategy = (CryptoEntryOrDcaStrategy)EditStrategy.SelectedValue;
        settings.EntryOrderPrice = (CryptoEntryOrDcaPricing)EditPricing.SelectedValue;
        settings.EntryRemoveTime = (int)EditGlobalBuyRemoveTime.Value;

        // fix: if market order then use marketprice
        if (settings.EntryOrderType == CryptoOrderType.Market)
            settings.EntryOrderPrice = CryptoEntryOrDcaPricing.MarketPrice;

        // fix: if trailing then use stop limit
        if (settings.EntryStrategy == CryptoEntryOrDcaStrategy.TrailViaKcPsar)
            settings.EntryOrderType = CryptoOrderType.StopLimit;
    }
}
