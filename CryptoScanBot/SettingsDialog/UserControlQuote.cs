using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.SettingsDialog;
public partial class UserControlQuote : UserControl
{

    public CryptoQuoteData QuoteData;

    public UserControlQuote()
    {
        InitializeComponent();

        EditMinimumVolume.Maximum = decimal.MaxValue;
        EditMinimumPrice.Maximum = decimal.MaxValue;
        EditEntryAmount.Maximum = decimal.MaxValue;
        EditEntryPercentage.Maximum = 100;
    }

    public void LoadConfig(CryptoQuoteData quoteData)
    {
        QuoteData = quoteData;

        EditQuoteName.Text = quoteData.Name;
        EditQuoteName.Checked = quoteData.FetchCandles;
        EditMinimumVolume.Value = quoteData.MinimalVolume;
        EditMinimumPrice.Value = quoteData.MinimalPrice;
        PanelColor.BackColor = quoteData.DisplayColor;
        EditEntryAmount.Value = quoteData.EntryAmount;
        EditEntryPercentage.Value = quoteData.EntryPercentage;
        labelSymbols.Text = $"{quoteData.SymbolList.Count} symbols";
    }

    public void SaveConfig(CryptoQuoteData quoteData)
    {
        quoteData.FetchCandles = EditQuoteName.Checked;
        quoteData.MinimalVolume = EditMinimumVolume.Value;
        quoteData.MinimalPrice = EditMinimumPrice.Value;
        quoteData.DisplayColor = PanelColor.BackColor;
        quoteData.EntryAmount = EditEntryAmount.Value;
        quoteData.EntryPercentage = EditEntryPercentage.Value;
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
