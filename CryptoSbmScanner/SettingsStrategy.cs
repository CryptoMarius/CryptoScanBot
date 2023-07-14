using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Signal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;

namespace CryptoSbmScanner;


internal class SettingsStrategy : IDisposable
{
    public CryptoOrderSide Mode;
    public AlgorithmDefinition Algorithm;

    public Label LabelInfo;
    public CheckBox CheckboxAnalyzeBuy;
    public CheckBox CheckboxAnalyzeSell;

    public CheckBox CheckboxMonitorBuy;
    public CheckBox CheckboxMonitorSell;

    //public NumericUpDown Barometer15m;
    //public NumericUpDown Barometer30m;
    //public NumericUpDown Barometer1h;
    //public NumericUpDown Barometer4h;
    //public NumericUpDown Barometer1d;

    public void Dispose()
    {
        if (LabelInfo != null) { LabelInfo.Dispose(); LabelInfo = null; }
        if (CheckboxAnalyzeBuy != null) { CheckboxAnalyzeBuy.Dispose(); CheckboxAnalyzeBuy = null; }
        if (CheckboxAnalyzeSell != null) { CheckboxAnalyzeSell.Dispose(); CheckboxAnalyzeSell = null; }
        if (CheckboxMonitorBuy != null) { CheckboxMonitorBuy.Dispose(); CheckboxMonitorBuy = null; }
        if (CheckboxMonitorSell != null) { CheckboxMonitorSell.Dispose(); CheckboxMonitorSell = null; }
        //if (Barometer15m != null) { Barometer15m.Dispose(); Barometer15m = null; }
        //if (Barometer30m != null) { Barometer30m.Dispose(); Barometer30m = null; }
        //if (Barometer1h != null) { Barometer1h.Dispose(); Barometer1h = null; }
        //if (Barometer4h != null) { Barometer4h.Dispose(); Barometer4h = null; }
        //if (Barometer1d != null) { Barometer1d.Dispose(); Barometer1d = null; }
    }

    public SettingsStrategy(CryptoOrderSide mode, AlgorithmDefinition strategy, int xPos, int yPos, Control.ControlCollection controls)
    {
        //int xPos = 18;
        Mode = mode;
        Algorithm = strategy;


        LabelInfo = new()
        {
            AutoSize = true,
            Location = new Point(xPos, yPos + 1),
            Size = new Size(88, 23),
            Text = SignalHelper.GetSignalAlgorithmText(strategy.Strategy),
        };
        controls.Add(LabelInfo);
        xPos += 125;



        CheckboxAnalyzeBuy = new()
        {
            AutoSize = false,
            Location = new Point(xPos, yPos + 1),
            Size = new Size(25, 23),
            Text = "",
            UseVisualStyleBackColor = true
        };
        controls.Add(CheckboxAnalyzeBuy);
        if (strategy.AnalyzeLongType == null)
            CheckboxAnalyzeBuy.Enabled = false;
        xPos += CheckboxAnalyzeBuy.Size.Width + 10;

        CheckboxAnalyzeSell = new()
        {
            AutoSize = false,
            Location = new Point(xPos, yPos + 1),
            Size = new Size(25, 23),
            UseVisualStyleBackColor = true
        };
        controls.Add(CheckboxAnalyzeSell);
        if (strategy.AnalyzeShortType == null)
            CheckboxAnalyzeSell.Enabled = false;
        xPos += CheckboxAnalyzeSell.Size.Width + 10 + 25; // extra space


        CheckboxMonitorBuy = new()
        {
            AutoSize = false,
            Location = new Point(xPos, yPos + 1),
            Size = new Size(25, 23),
            Text = "",
            UseVisualStyleBackColor = true
        };
        controls.Add(CheckboxMonitorBuy);
        if (strategy.AnalyzeLongType == null)
            CheckboxMonitorBuy.Enabled = false;
        xPos += CheckboxMonitorBuy.Size.Width + 10;


        CheckboxMonitorSell = new()
        {
            AutoSize = false,
            Location = new Point(xPos, yPos + 1),
            Size = new Size(25, 23),
            UseVisualStyleBackColor = true
        };
        controls.Add(CheckboxMonitorSell);
        if (strategy.AnalyzeShortType == null)
            CheckboxMonitorSell.Enabled = false;
        //last - xPos += CheckboxMonitorSell.Size.Width + 10;


        //Barometer15m = new()
        //{
        //    DecimalPlaces = 2,
        //    Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
        //    Location = new Point(xPos, yPos),
        //    Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
        //    Size = new Size(60, 23),
        //    ThousandsSeparator = true
        //};
        //xPos += Barometer15m.Size.Width + 10;
        //controls.Add(Barometer15m);

        //Barometer30m = new()
        //{
        //    DecimalPlaces = 2,
        //    Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
        //    Location = new Point(xPos, yPos),
        //    Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
        //    Size = new Size(60, 23),
        //    ThousandsSeparator = true
        //};
        //xPos += Barometer30m.Size.Width + 10;
        //controls.Add(Barometer30m);

        //Barometer1h = new()
        //{
        //    DecimalPlaces = 2,
        //    Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
        //    Location = new Point(xPos, yPos),
        //    Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
        //    Size = new Size(60, 23),
        //    ThousandsSeparator = true
        //};
        //xPos += Barometer1h.Size.Width + 10;
        //controls.Add(Barometer1h);

        //Barometer4h = new()
        //{
        //    DecimalPlaces = 2,
        //    Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
        //    Location = new Point(xPos, yPos),
        //    Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
        //    Size = new Size(60, 23),
        //    ThousandsSeparator = true
        //};
        //xPos += Barometer4h.Size.Width + 10;
        //controls.Add(Barometer4h);

        //Barometer1d = new()
        //{
        //    DecimalPlaces = 2,
        //    Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
        //    Location = new Point(xPos, yPos),
        //    Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
        //    Size = new Size(60, 23),
        //    ThousandsSeparator = true
        //};
        //xPos += Barometer1d.Size.Width + 10;
        //controls.Add(Barometer1d);
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
            Location = new Point(CheckboxAnalyzeBuy.Location.X - correct, yPos),
        };
        controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "Short",
            Location = new Point(CheckboxAnalyzeSell.Location.X - correct, yPos),
        };
        controls.Add(label);


        label = new()
        {
            AutoSize = true,
            Text = "Long",
            Location = new Point(CheckboxMonitorBuy.Location.X - correct, yPos),
        };
        controls.Add(label);

        label = new()
        {
            AutoSize = true,
            Text = "Short",
            Location = new Point(CheckboxMonitorSell.Location.X - correct, yPos),
        };
        controls.Add(label);


        //label = new()
        //{
        //    AutoSize = true,
        //    Text = "bm 15m",
        //    Location = new Point(Barometer15m.Location.X - correct, yPos),
        //};
        //controls.Add(label);

        //label = new()
        //{
        //    AutoSize = true,
        //    Text = "bm 30m",
        //    Location = new Point(Barometer30m.Location.X - correct, yPos),
        //};
        //controls.Add(label);

        //label = new()
        //{
        //    AutoSize = true,
        //    Text = "bm 1h",
        //    Location = new Point(Barometer1h.Location.X - correct, yPos),
        //};
        //controls.Add(label);

        //label = new()
        //{
        //    AutoSize = true,
        //    Text = "bm 4h",
        //    Location = new Point(Barometer4h.Location.X - correct, yPos),
        //};
        //controls.Add(label);

        //label = new()
        //{
        //    AutoSize = true,
        //    Text = "bm 1d",
        //    Location = new Point(Barometer1d.Location.X - correct, yPos),
        //};
        //controls.Add(label);

    }

    public void AddHeaderLabelsMain(int yPos, Control.ControlCollection controls)
    {
        int correct = 3;

        Label label = new()
        {
            AutoSize = true,
            Text = "Signals maken",
            Location = new Point(CheckboxAnalyzeBuy.Location.X - correct, yPos),
        };
        controls.Add(label);


        label = new()
        {
            AutoSize = true,
            Text = "Traden op",
            Location = new Point(CheckboxMonitorBuy.Location.X - correct, yPos),
        };
        controls.Add(label);
    }

    public void SetControlValues()
    {
        CheckboxAnalyzeBuy.Checked = GlobalData.Settings.Signal.Analyze.Strategy[CryptoOrderSide.Buy].Contains(Algorithm.Name);
        CheckboxAnalyzeSell.Checked = GlobalData.Settings.Signal.Analyze.Strategy[CryptoOrderSide.Sell].Contains(Algorithm.Name);

        CheckboxMonitorBuy.Checked = GlobalData.Settings.Trading.Monitor.Strategy[CryptoOrderSide.Buy].Contains(Algorithm.Name);
        CheckboxMonitorSell.Checked = GlobalData.Settings.Trading.Monitor.Strategy[CryptoOrderSide.Sell].Contains(Algorithm.Name);

        bool enableStrategy = true; // Algorithm.Strategy == CryptoSignalStrategy.Sbm4 || Algorithm.Strategy > CryptoSignalStrategy.Stobb;

        // De meeste staan al op andere tabsheets, hier dus disablen (anders dubbel in de interface)
        CheckboxAnalyzeBuy.Enabled = enableStrategy && Algorithm.AnalyzeLongType != null;
        CheckboxAnalyzeSell.Enabled = enableStrategy && Algorithm.AnalyzeShortType != null;
        
        CheckboxMonitorBuy.Enabled = Algorithm.AnalyzeLongType != null;
        CheckboxMonitorSell.Enabled = Algorithm.AnalyzeShortType != null;

        //CheckboxLong.Checked = QuoteData.FetchCandles;
        //MinVolume.Value = QuoteData.MinimalVolume;
        //MinPrice.Value = QuoteData.MinimalPrice;
        //CheckboxShort.Checked = QuoteData.CreateSignals;
        //PanelColor.BackColor = QuoteData.DisplayColor;
        // TODO: nieuwe trading controls!


    }

    private static void GetValueFromCheckBox(Control control, object obj, List<string> text)
    {
        if (control is CheckBox checkBox && checkBox.Enabled && checkBox.Checked)
        {
            if (obj is AlgorithmDefinition definition)
                text.Add(definition.Name);
        }
    }

    public void GetControlValues()
    {
        GetValueFromCheckBox(CheckboxAnalyzeBuy, Algorithm, GlobalData.Settings.Signal.Analyze.Strategy[CryptoOrderSide.Buy]);
        GetValueFromCheckBox(CheckboxAnalyzeSell, Algorithm, GlobalData.Settings.Signal.Analyze.Strategy[CryptoOrderSide.Sell]);

        GetValueFromCheckBox(CheckboxMonitorBuy, Algorithm, GlobalData.Settings.Trading.Monitor.Strategy[CryptoOrderSide.Buy]);
        GetValueFromCheckBox(CheckboxMonitorSell, Algorithm, GlobalData.Settings.Trading.Monitor.Strategy[CryptoOrderSide.Sell]);

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

    private void CheckBox1_CheckedChanged(Object sender, EventArgs e)
    {
        //MessageBox.Show("You are in the CheckBox.CheckedChanged event.");
        if (sender is CheckBox checkBox1 && checkBox1.Tag is CheckBox checkBox2)
        {
            checkBox2.Checked = checkBox1.Checked;
        }
    }

    public void ChainTo(CryptoOrderSide side, CheckBox edit)
    {
        CheckBox otherEdit;

        // Een nieuw idee, maar weet niet of het kan werken
        if (side == CryptoOrderSide.Buy)
            otherEdit = CheckboxAnalyzeBuy;
        else
            otherEdit = CheckboxAnalyzeSell;
        edit.Tag = otherEdit;
        otherEdit.Tag = edit;

        edit.CheckedChanged += CheckBox1_CheckedChanged;
        otherEdit.CheckedChanged += CheckBox1_CheckedChanged;

    }

}


