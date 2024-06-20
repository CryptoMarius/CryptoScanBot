namespace CryptoScanBot.SettingsDialog;

partial class UserControlTradeTakeProfit
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
        label1 = new Label();
        EditOrderType = new ComboBox();
        EditAddDustToTp = new CheckBox();
        EditLockProfits = new CheckBox();
        label63 = new Label();
        EditMethod = new ComboBox();
        label72 = new Label();
        EditProfitPercentage = new NumericUpDown();
        groupBox.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditProfitPercentage).BeginInit();
        SuspendLayout();
        // 
        // groupBox
        // 
        groupBox.AutoSize = true;
        groupBox.Controls.Add(label1);
        groupBox.Controls.Add(EditOrderType);
        groupBox.Controls.Add(EditAddDustToTp);
        groupBox.Controls.Add(EditLockProfits);
        groupBox.Controls.Add(label63);
        groupBox.Controls.Add(EditMethod);
        groupBox.Controls.Add(label72);
        groupBox.Controls.Add(EditProfitPercentage);
        groupBox.Dock = DockStyle.Fill;
        groupBox.Location = new Point(0, 0);
        groupBox.Name = "groupBox";
        groupBox.Padding = new Padding(5);
        groupBox.Size = new Size(386, 188);
        groupBox.TabIndex = 0;
        groupBox.TabStop = false;
        groupBox.Text = "Take profit";
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(9, 30);
        label1.Margin = new Padding(4, 0, 4, 0);
        label1.Name = "label1";
        label1.Size = new Size(63, 15);
        label1.TabIndex = 330;
        label1.Text = "Order type";
        // 
        // EditOrderType
        // 
        EditOrderType.DropDownStyle = ComboBoxStyle.DropDownList;
        EditOrderType.FormattingEnabled = true;
        EditOrderType.Location = new Point(177, 27);
        EditOrderType.Margin = new Padding(4, 3, 4, 3);
        EditOrderType.Name = "EditOrderType";
        EditOrderType.Size = new Size(200, 23);
        EditOrderType.TabIndex = 1;
        // 
        // EditAddDustToTp
        // 
        EditAddDustToTp.AutoSize = true;
        EditAddDustToTp.Font = new Font("Segoe UI", 9F);
        EditAddDustToTp.Location = new Point(9, 120);
        EditAddDustToTp.Margin = new Padding(4, 3, 4, 3);
        EditAddDustToTp.Name = "EditAddDustToTp";
        EditAddDustToTp.Size = new Size(191, 19);
        EditAddDustToTp.TabIndex = 4;
        EditAddDustToTp.Text = "Allow addition of previous dust";
        EditAddDustToTp.UseVisualStyleBackColor = true;
        // 
        // EditLockProfits
        // 
        EditLockProfits.AutoSize = true;
        EditLockProfits.Enabled = false;
        EditLockProfits.Font = new Font("Segoe UI", 9F, FontStyle.Strikeout);
        EditLockProfits.Location = new Point(9, 145);
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
        label63.Location = new Point(9, 59);
        label63.Margin = new Padding(4, 0, 4, 0);
        label63.Name = "label63";
        label63.Size = new Size(50, 15);
        label63.TabIndex = 326;
        label63.Text = "Strategy";
        // 
        // EditMethod
        // 
        EditMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        EditMethod.FormattingEnabled = true;
        EditMethod.Location = new Point(177, 56);
        EditMethod.Margin = new Padding(4, 3, 4, 3);
        EditMethod.Name = "EditMethod";
        EditMethod.Size = new Size(200, 23);
        EditMethod.TabIndex = 2;
        // 
        // label72
        // 
        label72.AutoSize = true;
        label72.Location = new Point(9, 86);
        label72.Margin = new Padding(4, 0, 4, 0);
        label72.Name = "label72";
        label72.Size = new Size(119, 15);
        label72.TabIndex = 322;
        label72.Text = "Profit percentage (%)";
        // 
        // EditProfitPercentage
        // 
        EditProfitPercentage.DecimalPlaces = 2;
        EditProfitPercentage.Location = new Point(177, 84);
        EditProfitPercentage.Margin = new Padding(4, 3, 4, 3);
        EditProfitPercentage.Name = "EditProfitPercentage";
        EditProfitPercentage.Size = new Size(88, 23);
        EditProfitPercentage.TabIndex = 3;
        EditProfitPercentage.Value = new decimal(new int[] { 75, 0, 0, 131072 });
        // 
        // UserControlTradeTakeProfit
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(groupBox);
        Name = "UserControlTradeTakeProfit";
        Size = new Size(386, 188);
        groupBox.ResumeLayout(false);
        groupBox.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditProfitPercentage).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBox;
    private CheckBox EditLockProfits;
    private Label label63;
    private ComboBox EditMethod;
    private Label label72;
    private NumericUpDown EditProfitPercentage;
    private CheckBox EditAddDustToTp;
    private Label label1;
    private ComboBox EditOrderType;
}
