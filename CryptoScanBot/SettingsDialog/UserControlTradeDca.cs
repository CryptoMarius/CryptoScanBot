using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlTradeDca : UserControl
{
    private readonly SortedList<string, CryptoOrderType> OrderTypeList = [];
    private readonly SortedList<string, CryptoEntryOrDcaStrategy> StrategyList = new();
    private readonly SortedList<string, CryptoEntryOrDcaPricing> PricingList = new();
    private readonly List<UserControlTradeDcaItem> UserControlDcaList = new();

    public UserControlTradeDca()
    {
        InitializeComponent();

        //AutoSize = true;
        //AutoSizeMode = AutoSizeMode.GrowAndShrink;
        //groupBoxDca.AutoSize = true;
        //groupBoxDca.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        panelDcaMethod.AutoSize = true;
        panelDcaMethod.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        PanelDca.AutoSize = true;
        PanelDca.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        // How to buy
        //OrderTypeList.Add("Market order", CryptoOrderType.Market);
        OrderTypeList.Add("Limit order", CryptoOrderType.Limit);
        //OrderTypeList.Add("Stop limit order", CryptoOrderType.StopLimit);
        //OrderTypeList.Add("Order cancels Order (OCO)", CryptoOrderType.Oco); // not relevant (not implemented for all exchanges I assume)

        //StrategyList.Add("Direct na het signaal", CryptoBuyStepInMethod.Immediately);
        StrategyList.Add("Op het opgegeven percentage", CryptoEntryOrDcaStrategy.FixedPercentage);
        //StrategyList.Add("Na een signaal (sbm/stobb/enz)", CryptoEntryOrProfitMethod.AfterNextSignal);
        //StrategyList.Add("Trace via de Keltner Channel en PSAR", CryptoEntryOrProfitMethod.TrailViaKcPsar);


        // Vanwege de beperkte mogelijkheden (DCA only ook alleen maar signaal prijs)
        // Deze optie is niet overbodig, want dit is altijd een fixed percentage + limit
        //PricingList.Add("Market order", CryptoBuyOrderMethod.MarketOrder);
        PricingList.Add("DCA percentage", CryptoEntryOrDcaPricing.SignalPrice);
        //PricingList.Add("Limit order op bied prijs", CryptoBuyOrderMethod.BidPrice);
        //PricingList.Add("Limit order op vraag prijs", CryptoBuyOrderMethod.AskPrice);
    }

    public void LoadConfig(SettingsTrading settings)
    {
        EditOrderType.DataSource = new BindingSource(OrderTypeList, null);
        EditOrderType.DisplayMember = "Key";
        EditOrderType.ValueMember = "Value";
        EditOrderType.SelectedValue = settings.DcaOrderType;

        EditStrategy.DataSource = new BindingSource(StrategyList, null);
        EditStrategy.DisplayMember = "Key";
        EditStrategy.ValueMember = "Value";
        EditStrategy.SelectedValue = settings.DcaStrategy;

        EditPricing.DataSource = new BindingSource(PricingList, null);
        EditPricing.DisplayMember = "Key";
        EditPricing.ValueMember = "Value";
        EditPricing.SelectedValue = settings.DcaOrderPrice;

        //EditDcaPercentage.Value = Math.Abs(settings.Trading.DcaPercentage);
        //EditDcaFactor.Value = settings.Trading.DcaFactor;
        //EditDcaCount.Value = settings.Trading.DcaCount;

        foreach (CryptoDcaEntry dca in settings.DcaList)
        {
            var item = new UserControlTradeDcaItem();
            UserControlDcaList.Add(item);
            PanelDca.Controls.Add(item);
            item.LoadConfig(dca, UserControlDcaList.Count);
        }
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.DcaOrderType = (CryptoOrderType)EditOrderType.SelectedValue;
        settings.DcaStrategy = (CryptoEntryOrDcaStrategy)EditStrategy.SelectedValue;
        settings.DcaOrderPrice = (CryptoEntryOrDcaPricing)EditPricing.SelectedValue;

        settings.DcaList.Clear();
        foreach (UserControlTradeDcaItem control in UserControlDcaList)
        {
            var dca = new CryptoDcaEntry();
            settings.DcaList.Add(dca);
            control.SaveConfig(dca);
        }

        // validatie ontbreekt nog
    }

    private void ButtonDcaAddClick(object? sender, EventArgs? e)
    {
        // Een item toevoegen
        CryptoDcaEntry dca = new();
        var control = new UserControlTradeDcaItem();

        // Wat defaults
        if (UserControlDcaList.Count != 0)
        {
            var dcaLast = new CryptoDcaEntry();
            var controlLast = UserControlDcaList[^1];
            controlLast.SaveConfig(dcaLast);

            dca.Factor = dcaLast.Factor * 2;
            dca.Percentage = dcaLast.Percentage + 6;
        }
        else
        {
            dca.Factor = 2;
            dca.Percentage = 6;
        }

        UserControlDcaList.Add(control);
        PanelDca.Controls.Add(control);

        control.LoadConfig(dca, UserControlDcaList.Count);
    }

    private void ButtonDcaDelClick(object? sender, EventArgs? e)
    {
        // Verwijder het laatste item
        if (UserControlDcaList.Count != 0)
        {
            var control = UserControlDcaList[^1];

            UserControlDcaList.Remove(control);
            PanelDca.Controls.Remove(control);
        }
    }
}
