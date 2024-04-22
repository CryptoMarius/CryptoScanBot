using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlTradeBuy : UserControl
{
    private readonly SortedList<string, CryptoEntryOrProfitMethod> BuyStepInMethod = new();
    private readonly SortedList<string, CryptoBuyOrderMethod> BuyOrderMethod = new();

    public UserControlTradeBuy()
    {
        InitializeComponent();

        // BUY
        // Voorlopig de trailing uit, een stoplimit (zoals Binance) werkt namelijk niet op Bybit
        BuyStepInMethod.Add("Na een signaal", CryptoEntryOrProfitMethod.AfterNextSignal);
        //BuyStepInMethod.Add("Trace via de Keltner Channel en PSAR", CryptoBuyStepInMethod.TrailViaKcPsar);

        // BUY/DCA - Manier van kopen
        // De bid en ask price wordt niet door alle exchanges ondersteund (Kucoin)
        BuyOrderMethod.Add("Market order", CryptoBuyOrderMethod.MarketOrder);
        BuyOrderMethod.Add("Limit order op signaal prijs", CryptoBuyOrderMethod.SignalPrice);
        BuyOrderMethod.Add("Limit order op bied prijs", CryptoBuyOrderMethod.BidPrice);
        BuyOrderMethod.Add("Limit order op vraag prijs", CryptoBuyOrderMethod.AskPrice);
    }

    public void LoadConfig(SettingsTrading settings)
    {
        EditBuyStepInMethod.DataSource = new BindingSource(BuyStepInMethod, null);
        EditBuyStepInMethod.DisplayMember = "Key";
        EditBuyStepInMethod.ValueMember = "Value";
        EditBuyStepInMethod.SelectedValue = settings.BuyStepInMethod;

        EditBuyOrderMethod.DataSource = new BindingSource(BuyOrderMethod, null);
        EditBuyOrderMethod.DisplayMember = "Key";
        EditBuyOrderMethod.ValueMember = "Value";
        EditBuyOrderMethod.SelectedValue = settings.BuyOrderMethod;

        EditGlobalBuyRemoveTime.Value = settings.GlobalBuyRemoveTime;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.BuyStepInMethod = (CryptoEntryOrProfitMethod)EditBuyStepInMethod.SelectedValue;
        settings.BuyOrderMethod = (CryptoBuyOrderMethod)EditBuyOrderMethod.SelectedValue;
        settings.GlobalBuyRemoveTime = (int)EditGlobalBuyRemoveTime.Value;
    }
}
