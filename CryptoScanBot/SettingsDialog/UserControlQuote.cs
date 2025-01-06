using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlQuote : UserControl
{
    private readonly CryptoQuoteData QuoteData;

    public UserControlQuote(CryptoQuoteData quoteData)
    {
        InitializeComponent();

        QuoteData = quoteData;
        EditMinimumVolume.Maximum = decimal.MaxValue;
        EditMinimumPrice.Maximum = decimal.MaxValue;
        EditEntryAmount.Maximum = decimal.MaxValue;
        EditEntryPercentage.Maximum = 100;
    }

    public void LoadConfig()
    {
        EditQuoteName.Text = QuoteData.Name;
        EditQuoteName.Checked = QuoteData.FetchCandles;
        EditMinimumVolume.Value = QuoteData.MinimalVolume;
        EditMinimumPrice.Value = QuoteData.MinimalPrice;
        PanelColor.BackColor = QuoteData.DisplayColor;
        EditEntryAmount.Value = QuoteData.EntryAmount;
        EditEntryPercentage.Value = QuoteData.EntryPercentage;
        labelSymbols.Text = $"{QuoteData.SymbolList.Count} symbols";
    }

    public void SaveConfig()
    {
        QuoteData.FetchCandles = EditQuoteName.Checked;
        QuoteData.MinimalVolume = EditMinimumVolume.Value;
        QuoteData.MinimalPrice = EditMinimumPrice.Value;
        QuoteData.DisplayColor = PanelColor.BackColor;
        QuoteData.EntryAmount = EditEntryAmount.Value;
        QuoteData.EntryPercentage = EditEntryPercentage.Value;
    }

    private void ButtonColor_Click(object? sender, EventArgs e)
    {
        ColorDialog dlg = new()
        {
            Color = PanelColor.BackColor,
            CustomColors = GlobalData.SettingsUser.CustomColors
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            PanelColor.BackColor = dlg.Color;
            GlobalData.SettingsUser.CustomColors = dlg.CustomColors;
        }
    }

    private void PanelColor_DoubleClick(object? sender, EventArgs e)
    {
        ButtonColor_Click(sender, e);
    }
}
