namespace CryptoScanBot.SettingsDialog;

partial class UserControlTradeStopLoss
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
        groupBox = new GroupBox();
        EditStopLimitPercentage = new NumericUpDown();
        LabelStopLimitPercentage = new Label();
        EditStopPercentage = new NumericUpDown();
        LabelStopPercentage = new Label();
        label54 = new Label();
        groupBox.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditStopLimitPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditStopPercentage).BeginInit();
        SuspendLayout();
        // 
        // groupBox
        // 
        groupBox.AutoSize = true;
        groupBox.Controls.Add(EditStopLimitPercentage);
        groupBox.Controls.Add(LabelStopLimitPercentage);
        groupBox.Controls.Add(EditStopPercentage);
        groupBox.Controls.Add(LabelStopPercentage);
        groupBox.Controls.Add(label54);
        groupBox.Dock = DockStyle.Fill;
        groupBox.Location = new Point(0, 0);
        groupBox.Name = "groupBox";
        groupBox.Padding = new Padding(5);
        groupBox.Size = new Size(274, 102);
        groupBox.TabIndex = 0;
        groupBox.TabStop = false;
        groupBox.Text = "Stop loss (not working!!)";
        // 
        // EditStopLimitPercentage
        // 
        EditStopLimitPercentage.DecimalPlaces = 2;
        EditStopLimitPercentage.Font = new Font("Segoe UI", 9F);
        EditStopLimitPercentage.Location = new Point(177, 55);
        EditStopLimitPercentage.Margin = new Padding(4, 3, 4, 3);
        EditStopLimitPercentage.Name = "EditStopLimitPercentage";
        EditStopLimitPercentage.Size = new Size(88, 23);
        EditStopLimitPercentage.TabIndex = 342;
        // 
        // LabelStopLimitPercentage
        // 
        LabelStopLimitPercentage.AutoSize = true;
        LabelStopLimitPercentage.Font = new Font("Segoe UI", 9F);
        LabelStopLimitPercentage.Location = new Point(9, 57);
        LabelStopLimitPercentage.Margin = new Padding(4, 0, 4, 0);
        LabelStopLimitPercentage.Name = "LabelStopLimitPercentage";
        LabelStopLimitPercentage.Size = new Size(79, 15);
        LabelStopLimitPercentage.TabIndex = 343;
        LabelStopLimitPercentage.Text = "Stop limit (%)";
        // 
        // EditStopPercentage
        // 
        EditStopPercentage.DecimalPlaces = 2;
        EditStopPercentage.Font = new Font("Segoe UI", 9F);
        EditStopPercentage.Location = new Point(177, 28);
        EditStopPercentage.Margin = new Padding(4, 3, 4, 3);
        EditStopPercentage.Name = "EditStopPercentage";
        EditStopPercentage.Size = new Size(88, 23);
        EditStopPercentage.TabIndex = 340;
        // 
        // LabelStopPercentage
        // 
        LabelStopPercentage.AutoSize = true;
        LabelStopPercentage.Font = new Font("Segoe UI", 9F);
        LabelStopPercentage.Location = new Point(9, 30);
        LabelStopPercentage.Margin = new Padding(4, 0, 4, 0);
        LabelStopPercentage.Name = "LabelStopPercentage";
        LabelStopPercentage.Size = new Size(81, 15);
        LabelStopPercentage.TabIndex = 341;
        LabelStopPercentage.Text = "Stop price (%)";
        // 
        // label54
        // 
        label54.AutoSize = true;
        label54.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        label54.Location = new Point(18, 54);
        label54.Margin = new Padding(4, 0, 4, 0);
        label54.Name = "label54";
        label54.Size = new Size(0, 15);
        label54.TabIndex = 322;
        // 
        // UserControlTradeStopLoss
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(groupBox);
        Name = "UserControlTradeStopLoss";
        Size = new Size(274, 102);
        groupBox.ResumeLayout(false);
        groupBox.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditStopLimitPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditStopPercentage).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBox;
    private Label label54;
    private NumericUpDown EditStopLimitPercentage;
    private Label LabelStopLimitPercentage;
    private NumericUpDown EditStopPercentage;
    private Label LabelStopPercentage;
}
