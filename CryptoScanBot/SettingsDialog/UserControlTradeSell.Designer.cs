namespace CryptoScanBot.SettingsDialog;

partial class UserControlTradeSell
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
        EditLockProfits = new CheckBox();
        label63 = new Label();
        EditSellMethod = new ComboBox();
        label72 = new Label();
        EditProfitPercentage = new NumericUpDown();
        groupBoxDca.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditProfitPercentage).BeginInit();
        SuspendLayout();
        // 
        // groupBoxDca
        // 
        groupBoxDca.AutoSize = true;
        groupBoxDca.Controls.Add(EditLockProfits);
        groupBoxDca.Controls.Add(label63);
        groupBoxDca.Controls.Add(EditSellMethod);
        groupBoxDca.Controls.Add(label72);
        groupBoxDca.Controls.Add(EditProfitPercentage);
        groupBoxDca.Dock = DockStyle.Fill;
        groupBoxDca.Location = new Point(0, 0);
        groupBoxDca.Name = "groupBoxDca";
        groupBoxDca.Padding = new Padding(5);
        groupBoxDca.Size = new Size(386, 129);
        groupBoxDca.TabIndex = 0;
        groupBoxDca.TabStop = false;
        groupBoxDca.Text = "Verkoop";
        // 
        // EditLockProfits
        // 
        EditLockProfits.AutoSize = true;
        EditLockProfits.Enabled = false;
        EditLockProfits.Font = new Font("Segoe UI", 9F, FontStyle.Strikeout);
        EditLockProfits.Location = new Point(9, 86);
        EditLockProfits.Margin = new Padding(4, 3, 4, 3);
        EditLockProfits.Name = "EditLockProfits";
        EditLockProfits.Size = new Size(88, 19);
        EditLockProfits.TabIndex = 327;
        EditLockProfits.Text = "Lock profits";
        EditLockProfits.UseVisualStyleBackColor = true;
        EditLockProfits.Visible = false;
        // 
        // label63
        // 
        label63.AutoSize = true;
        label63.Location = new Point(9, 30);
        label63.Margin = new Padding(4, 0, 4, 0);
        label63.Name = "label63";
        label63.Size = new Size(55, 15);
        label63.TabIndex = 326;
        label63.Text = "Methode";
        // 
        // EditSellMethod
        // 
        EditSellMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        EditSellMethod.FormattingEnabled = true;
        EditSellMethod.Location = new Point(177, 27);
        EditSellMethod.Margin = new Padding(4, 3, 4, 3);
        EditSellMethod.Name = "EditSellMethod";
        EditSellMethod.Size = new Size(200, 23);
        EditSellMethod.TabIndex = 325;
        // 
        // label72
        // 
        label72.AutoSize = true;
        label72.Location = new Point(9, 57);
        label72.Margin = new Padding(4, 0, 4, 0);
        label72.Name = "label72";
        label72.Size = new Size(120, 15);
        label72.TabIndex = 322;
        label72.Text = "Winst percentage (%)";
        // 
        // EditProfitPercentage
        // 
        EditProfitPercentage.DecimalPlaces = 2;
        EditProfitPercentage.Location = new Point(177, 55);
        EditProfitPercentage.Margin = new Padding(4, 3, 4, 3);
        EditProfitPercentage.Name = "EditProfitPercentage";
        EditProfitPercentage.Size = new Size(88, 23);
        EditProfitPercentage.TabIndex = 323;
        EditProfitPercentage.Value = new decimal(new int[] { 75, 0, 0, 131072 });
        // 
        // UserControlTradeSell
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(groupBoxDca);
        Name = "UserControlTradeSell";
        Size = new Size(386, 129);
        groupBoxDca.ResumeLayout(false);
        groupBoxDca.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditProfitPercentage).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBoxDca;
    private CheckBox EditLockProfits;
    private Label label63;
    private ComboBox EditSellMethod;
    private Label label72;
    private NumericUpDown EditProfitPercentage;
}
