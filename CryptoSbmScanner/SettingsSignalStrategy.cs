using CryptoSbmScanner.Model;
using CryptoSbmScanner.Signal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;

namespace CryptoSbmScanner;


internal class SettingsSignalStrategy : IDisposable
{
    public TradeDirection Mode;
    public SignalStrategy Strategy;

    public Label LabelInfo;
    public CheckBox CheckboxLong;
    public CheckBox CheckboxShort;

    public NumericUpDown Barometer15m;
    public NumericUpDown Barometer30m;
    public NumericUpDown Barometer1h;
    public NumericUpDown Barometer4h;
    public NumericUpDown Barometer1d;

    public void Dispose()
    {
        if (LabelInfo != null) { LabelInfo.Dispose(); LabelInfo = null; }
        if (CheckboxLong != null) { CheckboxLong.Dispose(); CheckboxLong = null; }
        if (CheckboxShort != null) { CheckboxShort.Dispose(); CheckboxShort = null; }
        if (Barometer15m != null) { Barometer15m.Dispose(); Barometer15m = null; }
        if (Barometer30m != null) { Barometer30m.Dispose(); Barometer30m = null; }
        if (Barometer1h != null) { Barometer1h.Dispose(); Barometer1h = null; }
        if (Barometer4h != null) { Barometer4h.Dispose(); Barometer4h = null; }
        if (Barometer1d != null) { Barometer1d.Dispose(); Barometer1d = null; }
    }

    public SettingsSignalStrategy(TradeDirection mode, SignalStrategy strategy, int yPos, Control.ControlCollection controls)
    {
        int xPos = 18;
        Mode = mode;
        Strategy = strategy;


        LabelInfo = new()
        {
            AutoSize = true,
            Location = new Point(xPos, yPos + 1),
            Size = new Size(88, 23),
            Text = SignalHelper.GetSignalAlgorithmText(strategy),
        };
        xPos += 125;
        controls.Add(LabelInfo);

        CheckboxLong = new()
        {
            AutoSize = false,
            Location = new Point(xPos, yPos + 1),
            Size = new Size(25, 23),
            Text = "",
            UseVisualStyleBackColor = true
        };
        xPos += CheckboxLong.Size.Width + 10;
        controls.Add(CheckboxLong);

        CheckboxShort = new()
        {
            AutoSize = false,
            Location = new Point(xPos, yPos + 3),
            Size = new Size(25, 23),
            UseVisualStyleBackColor = true
        };
        xPos += CheckboxShort.Size.Width + 10;
        controls.Add(CheckboxShort);


        Barometer15m = new()
        {
            DecimalPlaces = 2,
            Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
            Location = new Point(xPos, yPos),
            Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
            Size = new Size(60, 23),
            ThousandsSeparator = true
        };
        xPos += Barometer15m.Size.Width + 10;
        controls.Add(Barometer15m);

        Barometer30m = new()
        {
            DecimalPlaces = 2,
            Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
            Location = new Point(xPos, yPos),
            Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
            Size = new Size(60, 23),
            ThousandsSeparator = true
        };
        xPos += Barometer30m.Size.Width + 10;
        controls.Add(Barometer30m);

        Barometer1h = new()
        {
            DecimalPlaces = 2,
            Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
            Location = new Point(xPos, yPos),
            Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
            Size = new Size(60, 23),
            ThousandsSeparator = true
        };
        xPos += Barometer1h.Size.Width + 10;
        controls.Add(Barometer1h);

        Barometer4h = new()
        {
            DecimalPlaces = 2,
            Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
            Location = new Point(xPos, yPos),
            Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
            Size = new Size(60, 23),
            ThousandsSeparator = true
        };
        xPos += Barometer4h.Size.Width + 10;
        controls.Add(Barometer4h);

        Barometer1d = new()
        {
            DecimalPlaces = 2,
            Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
            Location = new Point(xPos, yPos),
            Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
            Size = new Size(60, 23),
            ThousandsSeparator = true
        };
        xPos += Barometer1d.Size.Width + 10;
        controls.Add(Barometer1d);
    }


    public void AddHeaderLabels(int yPos, Control.ControlCollection controls)
    {
        int correct = 3;

        Label label = new()
        {
            AutoSize = true,
            Text = "Strategie",
            Location = new Point(LabelInfo.Location.X - correct, yPos),
        };
        controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "Long",
            Location = new Point(CheckboxLong.Location.X - correct, yPos),
        };
        controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "Short",
            Location = new Point(CheckboxShort.Location.X - correct, yPos),
        };
        controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "bm 15m",
            Location = new Point(Barometer15m.Location.X - correct, yPos),
        };
        controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "bm 30m",
            Location = new Point(Barometer30m.Location.X - correct, yPos),
        };
        controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "bm 1h",
            Location = new Point(Barometer1h.Location.X - correct, yPos),
        };
        controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "bm 4h",
            Location = new Point(Barometer4h.Location.X - correct, yPos),
        };
        controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "bm 1d",
            Location = new Point(Barometer1d.Location.X - correct, yPos),
        };
        controls.Add(label);

    }


    public void SetControlValues()
    {
        //CheckboxLong.Checked = QuoteData.FetchCandles;
        //MinVolume.Value = QuoteData.MinimalVolume;
        //MinPrice.Value = QuoteData.MinimalPrice;
        //CheckboxShort.Checked = QuoteData.CreateSignals;
        //PanelColor.BackColor = QuoteData.DisplayColor;
        // TODO: nieuwe trading controls!


    }

    public void GetControlValues()
    {
        //        QuoteData.FetchCandles = CheckboxLong.Checked;
        //        QuoteData.MinimalVolume = MinVolume.Value;
        //        QuoteData.MinimalPrice = MinPrice.Value;
        //        QuoteData.CreateSignals = CheckboxShort.Checked;
        //        QuoteData.DisplayColor = PanelColor.BackColor;
        //#if TRADEBOT
        //        QuoteData.BuyAmount = BuyAmount.Value;
        //        QuoteData.BuyPercentage = Barometer15m.Value;
        //        QuoteData.SlotsMaximal = (int)SlotsMaximal.Value;
        //#endif
    }
}


