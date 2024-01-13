using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner.SettingsDialog;

public partial class UserControlTradeDca : UserControl
{
    private readonly SortedList<string, CryptoEntryOrProfitMethod> DcaStepInMethod = new();
    private readonly SortedList<string, CryptoBuyOrderMethod> DcaOrderMethod = new();
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

        // DCA
        //DcaStepInMethod.Add("Direct na het signaal", CryptoBuyStepInMethod.Immediately);
        DcaStepInMethod.Add("Op het opgegeven percentage", CryptoEntryOrProfitMethod.FixedPercentage);
        //DcaStepInMethod.Add("Na een signaal (sbm/stobb/enz)", CryptoEntryOrProfitMethod.AfterNextSignal);
        //DcaStepInMethod.Add("Trace via de Keltner Channel en PSAR", CryptoEntryOrProfitMethod.TrailViaKcPsar);


        // Vanwege de beperkte mogelijkheden (DCA only ook alleen maar signaal prijs)
        // Deze optie is niet overbodig, want dit is altijd een fixed percentage + limit
        //DcaOrderMethod.Add("Market order", CryptoBuyOrderMethod.MarketOrder);
        DcaOrderMethod.Add("Limit order op DCA percentage", CryptoBuyOrderMethod.SignalPrice);
        // Wordt niet door alle exchanges ondersteund
        //BuyOrderMethod.Add("Limit order op bied prijs", CryptoBuyOrderMethod.BidPrice);
        //BuyOrderMethod.Add("Limit order op vraag prijs", CryptoBuyOrderMethod.AskPrice);
    }

    public void LoadConfig(SettingsTrading settings)
    {
        EditDcaStepInMethod.DataSource = new BindingSource(DcaStepInMethod, null);
        EditDcaStepInMethod.DisplayMember = "Key";
        EditDcaStepInMethod.ValueMember = "Value";
        EditDcaStepInMethod.SelectedValue = settings.DcaStepInMethod;

        EditDcaOrderMethod.DataSource = new BindingSource(DcaOrderMethod, null);
        EditDcaOrderMethod.DisplayMember = "Key";
        EditDcaOrderMethod.ValueMember = "Value";
        EditDcaOrderMethod.SelectedValue = settings.DcaOrderMethod;

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
        settings.DcaStepInMethod = (CryptoEntryOrProfitMethod)EditDcaStepInMethod.SelectedValue;
        settings.DcaOrderMethod = (CryptoBuyOrderMethod)EditDcaOrderMethod.SelectedValue;

        settings.DcaList.Clear();
        foreach (UserControlTradeDcaItem control in UserControlDcaList)
        {
            var dca = new CryptoDcaEntry();
            settings.DcaList.Add(dca);
            control.SaveConfig(dca);
        }

        // validatie ontbreekt nog
    }

    private void ButtonDcaAddClick(object sender, EventArgs e)
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

    private void ButtonDcaDelClick(object sender, EventArgs e)
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
