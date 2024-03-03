namespace CryptoScanBot.SettingsDialog;

partial class UserControlTradeDcaItem
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
        EditFactor = new NumericUpDown();
        label2 = new Label();
        EditPercent = new NumericUpDown();
        label1 = new Label();
        groupBoxDca.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditFactor).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditPercent).BeginInit();
        SuspendLayout();
        // 
        // groupBoxDca
        // 
        groupBoxDca.AutoSize = true;
        groupBoxDca.Controls.Add(EditFactor);
        groupBoxDca.Controls.Add(label2);
        groupBoxDca.Controls.Add(EditPercent);
        groupBoxDca.Controls.Add(label1);
        groupBoxDca.Dock = DockStyle.Fill;
        groupBoxDca.Location = new Point(0, 0);
        groupBoxDca.Margin = new Padding(0);
        groupBoxDca.Name = "groupBoxDca";
        groupBoxDca.Padding = new Padding(0);
        groupBoxDca.Size = new Size(246, 93);
        groupBoxDca.TabIndex = 0;
        groupBoxDca.TabStop = false;
        groupBoxDca.Text = "DCA x";
        // 
        // EditFactor
        // 
        EditFactor.DecimalPlaces = 2;
        EditFactor.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditFactor.Location = new Point(123, 51);
        EditFactor.Name = "EditFactor";
        EditFactor.Size = new Size(120, 23);
        EditFactor.TabIndex = 11;
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(5, 48);
        label2.Name = "label2";
        label2.Size = new Size(40, 15);
        label2.TabIndex = 10;
        label2.Text = "Factor";
        // 
        // EditPercent
        // 
        EditPercent.DecimalPlaces = 2;
        EditPercent.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditPercent.Location = new Point(123, 24);
        EditPercent.Name = "EditPercent";
        EditPercent.Size = new Size(120, 23);
        EditPercent.TabIndex = 9;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(5, 21);
        label1.Name = "label1";
        label1.Size = new Size(66, 15);
        label1.TabIndex = 8;
        label1.Text = "Percentage";
        // 
        // UserControlTradeDcaItem
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(groupBoxDca);
        Name = "UserControlTradeDcaItem";
        Size = new Size(246, 93);
        groupBoxDca.ResumeLayout(false);
        groupBoxDca.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditFactor).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditPercent).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBoxDca;
    private NumericUpDown EditFactor;
    private Label label2;
    private NumericUpDown EditPercent;
    private Label label1;
}
