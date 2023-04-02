using CryptoSbmScanner.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoSbmScanner.Settings;


internal class SettingsBaseCoin
{
    public CryptoQuoteData QuoteData;

    // Signal settings
    public CheckBox FetchCandles;
    public NumericUpDown MinVolume;
    public NumericUpDown MinPrice;
    public CheckBox CreateSignals;

    // Display settings
    public Panel PanelColor;
    public Button ButtonColor;

#if tradebot
        // Trade bot settings
        public NumericUpDown BuyAmount;
        public NumericUpDown BuyPercentage;
        public NumericUpDown SlotsMaximal;
#endif


    public SettingsBaseCoin(CryptoQuoteData quoteData, int yPos, Control.ControlCollection controls)
    {
        QuoteData = quoteData;

        FetchCandles = new()
        {
            AutoSize = true,
            Location = new Point(18, yPos + 3),
            Margin = new Padding(4, 3, 4, 3),
            //Name = "EditFetchCandlesBTC",
            Size = new Size(45, 23),
            Text = quoteData.Name,
            UseVisualStyleBackColor = true
        };
        controls.Add(FetchCandles);

        MinVolume = new()
        {
            Location = new Point(118, yPos),
            Margin = new Padding(4, 3, 4, 3),
            Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
            //Name = "EditMinVolumeBTC";
            Size = new Size(140, 23),
            ThousandsSeparator = true
        };
        controls.Add(MinVolume);

        MinPrice = new()
        {
            DecimalPlaces = 8,
            Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
            Location = new Point(265, yPos),
            Margin = new Padding(4, 3, 4, 3),
            Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
            //Name = "EditMinPriceBTC";
            Size = new Size(140, 23),
            ThousandsSeparator = true
        };
        controls.Add(MinPrice);

#if tradebot
            // Het initiele aankoopbedrag
            //public decimal BuyAmount { get; set; }
            BuyAmount = new()
            {
                DecimalPlaces = 8,
                Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
                Location = new Point(265, yPos),
                Margin = new Padding(4, 3, 4, 3),
                Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
                Size = new Size(140, 23),
                ThousandsSeparator = true
            };
            controls.Add(BuyAmount);

            // Het initiele aankooppercentage
            //public decimal BuyPercentage { get; set; }
            BuyPercentage = new()
            {
                DecimalPlaces = 2,
                Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
                Location = new Point(265, yPos),
                Margin = new Padding(4, 3, 4, 3),
                Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
                Size = new Size(140, 23),
                ThousandsSeparator = true
            };
            controls.Add(BuyPercentage);

            // Maximaal aantal slots op de exchange
            //public int SlotsMaximal { get; set; }
            SlotsMaximal = new()
            {
                DecimalPlaces = 0,
                Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
                Location = new Point(265, yPos),
                Margin = new Padding(4, 3, 4, 3),
                Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
                Size = new Size(140, 23),
                ThousandsSeparator = true
            };
            controls.Add(SlotsMaximal);
#endif

        CreateSignals = new()
        {
            AutoSize = true,
            Location = new Point(425, yPos + 3),
            Margin = new Padding(4, 3, 4, 3),
            //Name = "EditCreateSignalsBTC",
            Size = new Size(15, 23),
            UseVisualStyleBackColor = true
        };
        controls.Add(CreateSignals);


        PanelColor = new()
        {
            BackColor = Color.Transparent,
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(465, yPos),
            Margin = new Padding(4, 3, 4, 3),
            //Name = "panelColorBTC",
            Size = new Size(70, 23)
        };
        controls.Add(PanelColor);

        ButtonColor = new()
        {
            Location = new Point(542, yPos),
            Margin = new Padding(4, 3, 4, 3),
            //Name = "buttonColorBTC",
            Size = new Size(88, 23),
            Text = "Achtergrond",
            UseVisualStyleBackColor = true,
            //Tag = PanelColor,
        };
        ButtonColor.Click += PickColor_Click;
        controls.Add(ButtonColor);

        Label labelInfo = new()
        {
            Location = new Point(642, yPos + 3),
            Margin = new Padding(4, 3, 4, 3),
            //Name = "buttonColorBTC",
            Size = new Size(88, 23),
            Text = quoteData.SymbolList.Count.ToString() + " symbols",
        };
        controls.Add(labelInfo);

    }

    private void PickColor_Click(object sender, EventArgs e)
    {
        ColorDialog dlg = new()
        {
            Color = PanelColor.BackColor
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            PanelColor.BackColor = dlg.Color;
        }
    }

    public void SetControlValues()
    {
        FetchCandles.Checked = QuoteData.FetchCandles;
        MinVolume.Value = QuoteData.MinimalVolume;
        MinPrice.Value = QuoteData.MinimalPrice;
        CreateSignals.Checked = QuoteData.CreateSignals;
        PanelColor.BackColor = QuoteData.DisplayColor;
    }

    public void GetControlValues()
    {
        QuoteData.FetchCandles = FetchCandles.Checked;
        QuoteData.MinimalVolume = MinVolume.Value;
        QuoteData.MinimalPrice = MinPrice.Value;
        QuoteData.CreateSignals = CreateSignals.Checked;
        QuoteData.DisplayColor = PanelColor.BackColor;
    }
}


