using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Settings;
using CryptoSbmScanner.Signal;
using CryptoSbmScanner.Trader;

namespace CryptoSbmScanner.SettingsDialog;


internal class SettingsStrategy : IDisposable
{
    public SettingsTextual Settings;
    public AlgorithmDefinition Algorithm;

    public Label LabelInfo;
    public CheckBox CheckboxSignal;
    public CheckBox CheckboxTrading;

    public void Dispose()
    {
        if (LabelInfo != null) { LabelInfo.Dispose(); LabelInfo = null; }
        if (CheckboxSignal != null) { CheckboxSignal.Dispose(); CheckboxSignal = null; }
        if (CheckboxTrading != null) { CheckboxTrading.Dispose(); CheckboxTrading = null; }
    }

    public SettingsStrategy(SettingsTextual settings, AlgorithmDefinition strategy, int xPos, int yPos, Control.ControlCollection controls)
    {
        Settings = settings;
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


        CheckboxSignal = new()
        {
            AutoSize = false,
            Location = new Point(xPos, yPos + 1),
            Size = new Size(25, 23),
            Text = "",
            UseVisualStyleBackColor = true
        };
        controls.Add(CheckboxSignal);
        xPos += CheckboxSignal.Size.Width + 10;


        CheckboxTrading = new()
        {
            AutoSize = false,
            Location = new Point(xPos, yPos + 1),
            Size = new Size(25, 23),
            Text = "",
            UseVisualStyleBackColor = true
        };
        controls.Add(CheckboxTrading);
    }


    public void AddHeaderLabels(int yPos, Control.ControlCollection controls)
    {
        int correct = 3;
        int xPos = LabelInfo.Location.X;

        Label label = new()
        {
            AutoSize = true,
            Text = "Strategie",
            Location = new Point(xPos - correct, yPos),
        };
        controls.Add(label);
        xPos += 125;

        label = new()
        {
            AutoSize = true,
            Text = "Signals maken",
            Location = new Point(xPos - correct, yPos),
        };
        controls.Add(label);
        xPos += CheckboxSignal.Size.Width + 10;


        label = new()
        {
            AutoSize = true,
            Text = "Traden op",
            Location = new Point(xPos - correct, yPos),
        };
        controls.Add(label);
    }

    public void AddHeaderLabelsMain(int yPos, Control.ControlCollection controls)
    {
        int correct = 3;

        GroupBox groupboxHorizontal = new()
        {
            AutoSize = false,
            Text = "",
            Location = new Point(LabelInfo.Location.X, yPos + 2 * CheckboxSignal.Size.Height),
            Size = new Size(300, 3),
        };
        controls.Add(groupboxHorizontal);


        GroupBox groupboxVertical = new()
        {
            AutoSize = false,
            Text = "",
            Location = new Point(CheckboxTrading.Location.X * correct - 4, yPos - correct),
            Size = new Size(1, SignalHelper.AlgorithmDefinitionIndex.Count * 30 + 60), // CheckboxAnalyzeBuy.Size.Height
        };
        controls.Add(groupboxVertical);
    }

    public void SetControlValues()
    {
        CheckboxSignal.Checked = Settings.Strategy.Contains(Algorithm.Name);
        CheckboxTrading.Checked = Settings.Strategy.Contains(Algorithm.Name);
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
        GetValueFromCheckBox(CheckboxSignal, Algorithm, Settings.Strategy);
        GetValueFromCheckBox(CheckboxTrading, Algorithm, Settings.Strategy);
    }

    private void CheckBox1_CheckedChanged(object sender, EventArgs e)
    {
        //MessageBox.Show("You are in the CheckBox.CheckedChanged event.");
        if (sender is CheckBox checkBox1 && checkBox1.Tag is CheckBox checkBox2)
        {
            checkBox2.Checked = checkBox1.Checked;
        }
    }

    public void ChainTo(CryptoOrderSide side, CheckBox edit)
    {
        edit.Tag = CheckboxSignal;
        CheckboxSignal.Tag = edit;

        edit.CheckedChanged += CheckBox1_CheckedChanged;
        CheckboxSignal.CheckedChanged += CheckBox1_CheckedChanged;
    }

}


