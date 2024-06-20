namespace CryptoScanBot.SettingsDialog;

partial class UserControlTradeEntry
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
        EditPricing = new ComboBox();
        label83 = new Label();
        EditStrategy = new ComboBox();
        label54 = new Label();
        label62 = new Label();
        EditOrderType = new ComboBox();
        label46 = new Label();
        EditGlobalBuyRemoveTime = new NumericUpDown();
        groupBox.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyRemoveTime).BeginInit();
        SuspendLayout();
        // 
        // groupBox
        // 
        groupBox.AutoSize = true;
        groupBox.Controls.Add(label1);
        groupBox.Controls.Add(EditPricing);
        groupBox.Controls.Add(label83);
        groupBox.Controls.Add(EditStrategy);
        groupBox.Controls.Add(label54);
        groupBox.Controls.Add(label62);
        groupBox.Controls.Add(EditOrderType);
        groupBox.Controls.Add(label46);
        groupBox.Controls.Add(EditGlobalBuyRemoveTime);
        groupBox.Dock = DockStyle.Fill;
        groupBox.Location = new Point(0, 0);
        groupBox.Name = "groupBox";
        groupBox.Padding = new Padding(5);
        groupBox.Size = new Size(387, 156);
        groupBox.TabIndex = 0;
        groupBox.TabStop = false;
        groupBox.Text = "Entry";
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(10, 83);
        label1.Margin = new Padding(4, 0, 4, 0);
        label1.Name = "label1";
        label1.Size = new Size(60, 15);
        label1.TabIndex = 326;
        label1.Text = "Price level";
        // 
        // EditPricing
        // 
        EditPricing.DropDownStyle = ComboBoxStyle.DropDownList;
        EditPricing.FormattingEnabled = true;
        EditPricing.Location = new Point(178, 80);
        EditPricing.Margin = new Padding(4, 3, 4, 3);
        EditPricing.Name = "EditPricing";
        EditPricing.Size = new Size(200, 23);
        EditPricing.TabIndex = 3;
        // 
        // label83
        // 
        label83.AutoSize = true;
        label83.Location = new Point(8, 54);
        label83.Margin = new Padding(4, 0, 4, 0);
        label83.Name = "label83";
        label83.Size = new Size(50, 15);
        label83.TabIndex = 324;
        label83.Text = "Strategy";
        // 
        // EditStrategy
        // 
        EditStrategy.DropDownStyle = ComboBoxStyle.DropDownList;
        EditStrategy.FormattingEnabled = true;
        EditStrategy.Location = new Point(178, 51);
        EditStrategy.Margin = new Padding(4, 3, 4, 3);
        EditStrategy.Name = "EditStrategy";
        EditStrategy.Size = new Size(200, 23);
        EditStrategy.TabIndex = 2;
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
        // label62
        // 
        label62.AutoSize = true;
        label62.Location = new Point(10, 27);
        label62.Margin = new Padding(4, 0, 4, 0);
        label62.Name = "label62";
        label62.Size = new Size(63, 15);
        label62.TabIndex = 321;
        label62.Text = "Order type";
        // 
        // EditOrderType
        // 
        EditOrderType.DropDownStyle = ComboBoxStyle.DropDownList;
        EditOrderType.FormattingEnabled = true;
        EditOrderType.Location = new Point(178, 24);
        EditOrderType.Margin = new Padding(4, 3, 4, 3);
        EditOrderType.Name = "EditOrderType";
        EditOrderType.Size = new Size(200, 23);
        EditOrderType.TabIndex = 1;
        // 
        // label46
        // 
        label46.AutoSize = true;
        label46.Location = new Point(10, 111);
        label46.Margin = new Padding(4, 0, 4, 0);
        label46.Name = "label46";
        label46.Size = new Size(77, 15);
        label46.TabIndex = 319;
        label46.Text = "Remove time";
        // 
        // EditGlobalBuyRemoveTime
        // 
        EditGlobalBuyRemoveTime.Location = new Point(178, 109);
        EditGlobalBuyRemoveTime.Margin = new Padding(4, 3, 4, 3);
        EditGlobalBuyRemoveTime.Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 });
        EditGlobalBuyRemoveTime.Name = "EditGlobalBuyRemoveTime";
        EditGlobalBuyRemoveTime.Size = new Size(88, 23);
        EditGlobalBuyRemoveTime.TabIndex = 4;
        EditGlobalBuyRemoveTime.Value = new decimal(new int[] { 5, 0, 0, 0 });
        // 
        // UserControlTradeEntry
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(groupBox);
        Name = "UserControlTradeEntry";
        Size = new Size(387, 156);
        groupBox.ResumeLayout(false);
        groupBox.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyRemoveTime).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBox;
    private Label label83;
    private ComboBox EditStrategy;
    private Label label54;
    private Label label62;
    private ComboBox EditOrderType;
    private Label label46;
    private NumericUpDown EditGlobalBuyRemoveTime;
    private Label label1;
    private ComboBox EditPricing;
}
