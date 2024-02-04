namespace CryptoSbmScanner.SettingsDialog;

partial class UserControlTradeRuleItem
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
        groupBoxDca = new GroupBox();
        label5 = new Label();
        EditCoolDown = new NumericUpDown();
        label4 = new Label();
        label19 = new Label();
        EditInterval = new ComboBox();
        label3 = new Label();
        EditSymbol = new TextBox();
        EditCandles = new NumericUpDown();
        label2 = new Label();
        EditPercent = new NumericUpDown();
        label1 = new Label();
        groupBoxDca.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditCoolDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditCandles).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditPercent).BeginInit();
        SuspendLayout();
        // 
        // groupBoxDca
        // 
        groupBoxDca.AutoSize = true;
        groupBoxDca.Controls.Add(label5);
        groupBoxDca.Controls.Add(EditCoolDown);
        groupBoxDca.Controls.Add(label4);
        groupBoxDca.Controls.Add(label19);
        groupBoxDca.Controls.Add(EditInterval);
        groupBoxDca.Controls.Add(label3);
        groupBoxDca.Controls.Add(EditSymbol);
        groupBoxDca.Controls.Add(EditCandles);
        groupBoxDca.Controls.Add(label2);
        groupBoxDca.Controls.Add(EditPercent);
        groupBoxDca.Controls.Add(label1);
        groupBoxDca.Dock = DockStyle.Fill;
        groupBoxDca.Location = new Point(0, 0);
        groupBoxDca.Margin = new Padding(0);
        groupBoxDca.Name = "groupBoxDca";
        groupBoxDca.Padding = new Padding(0);
        groupBoxDca.Size = new Size(323, 194);
        groupBoxDca.TabIndex = 0;
        groupBoxDca.TabStop = false;
        groupBoxDca.Text = "Trade rule x";
        // 
        // label5
        // 
        label5.AutoSize = true;
        label5.Location = new Point(262, 156);
        label5.Name = "label5";
        label5.Size = new Size(52, 15);
        label5.TabIndex = 279;
        label5.Text = "Minuten";
        // 
        // EditCoolDown
        // 
        EditCoolDown.Location = new Point(125, 152);
        EditCoolDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditCoolDown.Name = "EditCoolDown";
        EditCoolDown.Size = new Size(120, 23);
        EditCoolDown.TabIndex = 278;
        EditCoolDown.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new Point(7, 154);
        label4.Name = "label4";
        label4.Size = new Size(62, 15);
        label4.TabIndex = 277;
        label4.Text = "Cooldown";
        // 
        // label19
        // 
        label19.AutoSize = true;
        label19.Location = new Point(9, 84);
        label19.Margin = new Padding(4, 0, 4, 0);
        label19.Name = "label19";
        label19.Size = new Size(46, 15);
        label19.TabIndex = 276;
        label19.Text = "Interval";
        // 
        // EditInterval
        // 
        EditInterval.DropDownStyle = ComboBoxStyle.DropDownList;
        EditInterval.FormattingEnabled = true;
        EditInterval.Items.AddRange(new object[] { "Cross", "Isolated" });
        EditInterval.Location = new Point(127, 81);
        EditInterval.Margin = new Padding(4, 3, 4, 3);
        EditInterval.Name = "EditInterval";
        EditInterval.Size = new Size(120, 23);
        EditInterval.TabIndex = 275;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(9, 28);
        label3.Name = "label3";
        label3.Size = new Size(47, 15);
        label3.TabIndex = 13;
        label3.Text = "Symbol";
        // 
        // EditSymbol
        // 
        EditSymbol.Location = new Point(127, 25);
        EditSymbol.Name = "EditSymbol";
        EditSymbol.Size = new Size(120, 23);
        EditSymbol.TabIndex = 12;
        // 
        // EditCandles
        // 
        EditCandles.Location = new Point(127, 54);
        EditCandles.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditCandles.Name = "EditCandles";
        EditCandles.Size = new Size(120, 23);
        EditCandles.TabIndex = 11;
        EditCandles.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(9, 56);
        label2.Name = "label2";
        label2.Size = new Size(49, 15);
        label2.TabIndex = 10;
        label2.Text = "Candles";
        // 
        // EditPercent
        // 
        EditPercent.DecimalPlaces = 2;
        EditPercent.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditPercent.Location = new Point(127, 109);
        EditPercent.Minimum = new decimal(new int[] { 25, 0, 0, 131072 });
        EditPercent.Name = "EditPercent";
        EditPercent.Size = new Size(120, 23);
        EditPercent.TabIndex = 9;
        EditPercent.Value = new decimal(new int[] { 25, 0, 0, 131072 });
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(9, 111);
        label1.Name = "label1";
        label1.Size = new Size(66, 15);
        label1.TabIndex = 8;
        label1.Text = "Percentage";
        // 
        // UserControlTradeRuleItem
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        Controls.Add(groupBoxDca);
        Name = "UserControlTradeRuleItem";
        Size = new Size(323, 194);
        groupBoxDca.ResumeLayout(false);
        groupBoxDca.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditCoolDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditCandles).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditPercent).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBoxDca;
    private NumericUpDown EditCandles;
    private Label label2;
    private NumericUpDown EditPercent;
    private Label label1;
    private Label label3;
    private TextBox EditSymbol;
    private Label label19;
    private ComboBox EditInterval;
    private Label label5;
    private NumericUpDown EditCoolDown;
    private Label label4;
}
