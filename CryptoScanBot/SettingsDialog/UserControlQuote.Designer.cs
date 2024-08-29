namespace CryptoScanBot.SettingsDialog;

partial class UserControlQuote
{
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        EditQuoteName = new CheckBox();
        EditMinimumVolume = new NumericUpDown();
        EditMinimumPrice = new NumericUpDown();
        EditEntryAmount = new NumericUpDown();
        EditEntryPercentage = new NumericUpDown();
        PanelColor = new Panel();
        ButtonColor = new Button();
        labelSymbols = new Label();
        ((System.ComponentModel.ISupportInitialize)EditMinimumVolume).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumPrice).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditEntryAmount).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditEntryPercentage).BeginInit();
        SuspendLayout();
        // 
        // EditQuoteName
        // 
        EditQuoteName.AutoSize = true;
        EditQuoteName.Location = new Point(17, 4);
        EditQuoteName.Margin = new Padding(0);
        EditQuoteName.Name = "EditQuoteName";
        EditQuoteName.Size = new Size(91, 19);
        EditQuoteName.TabIndex = 0;
        EditQuoteName.Text = "QuoteName";
        EditQuoteName.UseVisualStyleBackColor = true;
        // 
        // EditMinimumVolume
        // 
        EditMinimumVolume.Location = new Point(119, 2);
        EditMinimumVolume.Name = "EditMinimumVolume";
        EditMinimumVolume.Size = new Size(120, 23);
        EditMinimumVolume.TabIndex = 1;
        EditMinimumVolume.ThousandsSeparator = true;
        // 
        // EditMinimumPrice
        // 
        EditMinimumPrice.DecimalPlaces = 8;
        EditMinimumPrice.Location = new Point(245, 2);
        EditMinimumPrice.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
        EditMinimumPrice.Name = "EditMinimumPrice";
        EditMinimumPrice.Size = new Size(120, 23);
        EditMinimumPrice.TabIndex = 2;
        EditMinimumPrice.ThousandsSeparator = true;
        // 
        // EditEntryAmount
        // 
        EditEntryAmount.DecimalPlaces = 8;
        EditEntryAmount.Location = new Point(371, 2);
        EditEntryAmount.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
        EditEntryAmount.Name = "EditEntryAmount";
        EditEntryAmount.Size = new Size(120, 23);
        EditEntryAmount.TabIndex = 3;
        EditEntryAmount.ThousandsSeparator = true;
        // 
        // EditEntryPercentage
        // 
        EditEntryPercentage.DecimalPlaces = 3;
        EditEntryPercentage.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditEntryPercentage.Location = new Point(497, 2);
        EditEntryPercentage.Name = "EditEntryPercentage";
        EditEntryPercentage.Size = new Size(78, 23);
        EditEntryPercentage.TabIndex = 4;
        EditEntryPercentage.ThousandsSeparator = true;
        // 
        // PanelColor
        // 
        PanelColor.BackColor = SystemColors.Window;
        PanelColor.BorderStyle = BorderStyle.FixedSingle;
        PanelColor.Location = new Point(581, 2);
        PanelColor.Name = "PanelColor";
        PanelColor.Size = new Size(112, 23);
        PanelColor.TabIndex = 5;
        PanelColor.DoubleClick += PanelColor_DoubleClick;
        // 
        // ButtonColor
        // 
        ButtonColor.Location = new Point(699, 1);
        ButtonColor.Name = "ButtonColor";
        ButtonColor.Size = new Size(97, 23);
        ButtonColor.TabIndex = 6;
        ButtonColor.Text = "Achtergrond";
        ButtonColor.UseVisualStyleBackColor = true;
        ButtonColor.Click += ButtonColor_Click;
        // 
        // labelSymbols
        // 
        labelSymbols.AutoSize = true;
        labelSymbols.Location = new Point(812, 5);
        labelSymbols.Name = "labelSymbols";
        labelSymbols.Size = new Size(69, 15);
        labelSymbols.TabIndex = 7;
        labelSymbols.Text = "x symbols...";
        // 
        // UserControlQuote
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(labelSymbols);
        Controls.Add(ButtonColor);
        Controls.Add(PanelColor);
        Controls.Add(EditEntryPercentage);
        Controls.Add(EditEntryAmount);
        Controls.Add(EditMinimumPrice);
        Controls.Add(EditMinimumVolume);
        Controls.Add(EditQuoteName);
        Margin = new Padding(0);
        Name = "UserControlQuote";
        Size = new Size(884, 28);
        ((System.ComponentModel.ISupportInitialize)EditMinimumVolume).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumPrice).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditEntryAmount).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditEntryPercentage).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private CheckBox EditQuoteName;
    private NumericUpDown EditMinimumVolume;
    private NumericUpDown EditMinimumPrice;
    private NumericUpDown EditEntryAmount;
    private NumericUpDown EditEntryPercentage;
    private Panel PanelColor;
    private Button ButtonColor;
    private Label labelSymbols;
}
