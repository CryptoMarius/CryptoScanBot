using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.SettingsDialog;


internal class SettingsQuoteCoin : IDisposable
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

#if TRADEBOT
    // Trade bot settings
    public NumericUpDown BuyAmount;
    //public NumericUpDown BuyPercentage;
    public NumericUpDown SlotsMaximal;
#endif

    public void Dispose()
    {
        if (FetchCandles != null) { FetchCandles.Dispose(); FetchCandles = null; }
        if (MinVolume != null) { MinVolume.Dispose(); MinVolume = null; }
        if (MinPrice != null) { MinPrice.Dispose(); MinPrice = null; }
        if (CreateSignals != null) { CreateSignals.Dispose(); CreateSignals = null; }
        if (PanelColor != null) { PanelColor.Dispose(); PanelColor = null; }
        if (ButtonColor != null) { ButtonColor.Dispose(); ButtonColor = null; }
#if TRADEBOT
        if (BuyAmount != null) { BuyAmount.Dispose(); BuyAmount = null; }
        //if (BuyPercentage != null) { BuyPercentage.Dispose(); BuyPercentage = null; }
        if (SlotsMaximal != null) { SlotsMaximal.Dispose(); SlotsMaximal = null; }
#endif
    }

    public SettingsQuoteCoin(CryptoQuoteData quoteData, int yPos, Control.ControlCollection controls)
    {
        int xPos = 18;
        QuoteData = quoteData;

        // Quote name
        FetchCandles = new()
        {
            AutoSize = true,
            Location = new Point(xPos, yPos + 3),
            Size = new Size(45, 23),
            Text = quoteData.Name,
            UseVisualStyleBackColor = true
        };
        xPos += FetchCandles.Size.Width + 50;
        controls.Add(FetchCandles);

        CreateSignals = new()
        {
            AutoSize = true,
            Location = new Point(xPos, yPos + 3),
            Size = new Size(15, 23),
            UseVisualStyleBackColor = true
        };
        xPos += CreateSignals.Size.Width + 50;
        controls.Add(CreateSignals);

        // Minimum Volume
        MinVolume = new()
        {
            Location = new Point(xPos, yPos),
            Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
            Size = new Size(90, 23),
            ThousandsSeparator = true
        };
        xPos += MinVolume.Size.Width + 10;
        controls.Add(MinVolume);

        // Minimum Price
        MinPrice = new()
        {
            DecimalPlaces = 8,
            Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
            Location = new Point(xPos, yPos),
            Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
            Size = new Size(90, 23),
            ThousandsSeparator = true
        };
        xPos += MinPrice.Size.Width + 10;
        controls.Add(MinPrice);

#if TRADEBOT
        // TODO: uitlijnen ten opzichte van de andere controls!
        // (daarom is de controls.Add even afgesterd)

        // Het initiele aankoopbedrag
        //public decimal BuyAmount { get; set; }
        BuyAmount = new()
        {
            DecimalPlaces = 8,
            Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
            Location = new Point(xPos, yPos),
            Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
            Size = new Size(90, 23),
            ThousandsSeparator = true
        };
        xPos += BuyAmount.Size.Width + 10;
        controls.Add(BuyAmount);

        //// Het initiele aankooppercentage
        ////public decimal BuyPercentage { get; set; }
        //BuyPercentage = new()
        //{
        //    DecimalPlaces = 3,
        //    Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
        //    Location = new Point(xPos, yPos),
        //    Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
        //    Size = new Size(60, 23),
        //    ThousandsSeparator = true
        //};
        //xPos += BuyPercentage.Size.Width + 10;
        //controls.Add(BuyPercentage);

        // Maximaal aantal slots op de exchange
        //public int SlotsMaximal { get; set; }
        SlotsMaximal = new()
        {
            DecimalPlaces = 0,
            //Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
            Location = new Point(xPos, yPos),
            //Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
            Size = new Size(60, 23),
            ThousandsSeparator = true,
            Minimum = 0m,
            //DecimalPlaces = 0,
            Maximum = 1000m,
            Increment = 1m,
        };
        xPos += SlotsMaximal.Size.Width + 10;
        controls.Add(SlotsMaximal);
#endif

        PanelColor = new()
        {
            BackColor = Color.Transparent,
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(xPos, yPos),
            Size = new Size(70, 23)
        };
        xPos += PanelColor.Size.Width + 10;
        controls.Add(PanelColor);


        ButtonColor = new()
        {
            Location = new Point(xPos, yPos),
            Size = new Size(88, 23),
            Text = "Achtergrond",
            UseVisualStyleBackColor = true,
            //Tag = PanelColor,
        };
        xPos += ButtonColor.Size.Width + 10;
        ButtonColor.Click += PickColor_Click;
        controls.Add(ButtonColor);

        // Aantal symbols
        Label labelInfo = new()
        {
            Location = new Point(xPos, yPos + 3),
            Size = new Size(88, 23),
            Text = quoteData.SymbolList.Count.ToString() + " symbols",
        };
        controls.Add(labelInfo);

    }


    public void AddHeaderLabels(int yPos, Control.ControlCollection controls)
    {
        int correct = 3;

        Label label = new()
        {
            AutoSize = true,
            Text = "Candles halen",
            Location = new Point(FetchCandles.Location.X - correct, yPos),
        };
        controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "Min. volume",
            Location = new Point(MinVolume.Location.X - correct, yPos),
        };
        controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "Min. prijs",
            Location = new Point(MinPrice.Location.X - correct, yPos),
        };
        controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "Signalen",
            Location = new Point(CreateSignals.Location.X - correct, yPos),
        };
        controls.Add(label);

#if TRADEBOT
        label = new()
        {
            AutoSize = true,
            Text = "Hoeveelheid",
            Location = new Point(BuyAmount.Location.X - correct, yPos),
        };
        controls.Add(label);

        //label = new()
        //{
        //    AutoSize = true,
        //    Text = "Percentage",
        //    Location = new Point(BuyPercentage.Location.X - correct, yPos),
        //};
        //controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "Slots",
            Location = new Point(SlotsMaximal.Location.X - correct, yPos),
        };
        controls.Add(label);
#endif
        label = new()
        {
            AutoSize = true,
            Text = "Kleur",
            Location = new Point(PanelColor.Location.X - correct, yPos),
        };
        controls.Add(label);
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
        // TODO: nieuwe trading controls!


#if TRADEBOT
        BuyAmount.Value = QuoteData.BuyAmount;
        //BuyPercentage.Value = QuoteData.BuyPercentage;
        SlotsMaximal.Value = QuoteData.SlotsMaximal;
#endif
    }

    public void GetControlValues()
    {
        QuoteData.FetchCandles = FetchCandles.Checked;
        QuoteData.MinimalVolume = MinVolume.Value;
        QuoteData.MinimalPrice = MinPrice.Value;
        QuoteData.CreateSignals = CreateSignals.Checked;
        QuoteData.DisplayColor = PanelColor.BackColor;
#if TRADEBOT
        QuoteData.BuyAmount = BuyAmount.Value;
        //QuoteData.BuyPercentage = BuyPercentage.Value;
        QuoteData.SlotsMaximal = (int)SlotsMaximal.Value;
#endif
    }
}


