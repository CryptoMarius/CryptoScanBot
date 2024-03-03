namespace CryptoScanBot.SettingsDialog;

partial class UserControlTradeBuy
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
        label83 = new Label();
        EditBuyStepInMethod = new ComboBox();
        label54 = new Label();
        label62 = new Label();
        EditBuyOrderMethod = new ComboBox();
        label46 = new Label();
        EditGlobalBuyRemoveTime = new NumericUpDown();
        groupBoxDca.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyRemoveTime).BeginInit();
        SuspendLayout();
        // 
        // groupBoxDca
        // 
        groupBoxDca.AutoSize = true;
        groupBoxDca.Controls.Add(label83);
        groupBoxDca.Controls.Add(EditBuyStepInMethod);
        groupBoxDca.Controls.Add(label54);
        groupBoxDca.Controls.Add(label62);
        groupBoxDca.Controls.Add(EditBuyOrderMethod);
        groupBoxDca.Controls.Add(label46);
        groupBoxDca.Controls.Add(EditGlobalBuyRemoveTime);
        groupBoxDca.Dock = DockStyle.Fill;
        groupBoxDca.Location = new Point(0, 0);
        groupBoxDca.Name = "groupBoxDca";
        groupBoxDca.Padding = new Padding(5);
        groupBoxDca.Size = new Size(387, 126);
        groupBoxDca.TabIndex = 0;
        groupBoxDca.TabStop = false;
        groupBoxDca.Text = "Aankoop";
        // 
        // label83
        // 
        label83.AutoSize = true;
        label83.Location = new Point(10, 32);
        label83.Margin = new Padding(4, 0, 4, 0);
        label83.Name = "label83";
        label83.Size = new Size(88, 15);
        label83.TabIndex = 324;
        label83.Text = "Instap moment";
        // 
        // EditBuyStepInMethod
        // 
        EditBuyStepInMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        EditBuyStepInMethod.FormattingEnabled = true;
        EditBuyStepInMethod.Location = new Point(178, 24);
        EditBuyStepInMethod.Margin = new Padding(4, 3, 4, 3);
        EditBuyStepInMethod.Name = "EditBuyStepInMethod";
        EditBuyStepInMethod.Size = new Size(200, 23);
        EditBuyStepInMethod.TabIndex = 323;
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
        label62.Location = new Point(10, 54);
        label62.Margin = new Padding(4, 0, 4, 0);
        label62.Name = "label62";
        label62.Size = new Size(86, 15);
        label62.TabIndex = 321;
        label62.Text = "Koop methode";
        // 
        // EditBuyOrderMethod
        // 
        EditBuyOrderMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        EditBuyOrderMethod.FormattingEnabled = true;
        EditBuyOrderMethod.Location = new Point(178, 51);
        EditBuyOrderMethod.Margin = new Padding(4, 3, 4, 3);
        EditBuyOrderMethod.Name = "EditBuyOrderMethod";
        EditBuyOrderMethod.Size = new Size(200, 23);
        EditBuyOrderMethod.TabIndex = 320;
        // 
        // label46
        // 
        label46.AutoSize = true;
        label46.Location = new Point(10, 81);
        label46.Margin = new Padding(4, 0, 4, 0);
        label46.Name = "label46";
        label46.Size = new Size(77, 15);
        label46.TabIndex = 319;
        label46.Text = "Remove time";
        // 
        // EditGlobalBuyRemoveTime
        // 
        EditGlobalBuyRemoveTime.Location = new Point(178, 79);
        EditGlobalBuyRemoveTime.Margin = new Padding(4, 3, 4, 3);
        EditGlobalBuyRemoveTime.Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 });
        EditGlobalBuyRemoveTime.Name = "EditGlobalBuyRemoveTime";
        EditGlobalBuyRemoveTime.Size = new Size(88, 23);
        EditGlobalBuyRemoveTime.TabIndex = 318;
        EditGlobalBuyRemoveTime.Value = new decimal(new int[] { 5, 0, 0, 0 });
        // 
        // UserControlTradeBuy
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(groupBoxDca);
        Name = "UserControlTradeBuy";
        Size = new Size(387, 126);
        groupBoxDca.ResumeLayout(false);
        groupBoxDca.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyRemoveTime).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBoxDca;
    private Label label83;
    private ComboBox EditBuyStepInMethod;
    private Label label54;
    private Label label62;
    private ComboBox EditBuyOrderMethod;
    private Label label46;
    private NumericUpDown EditGlobalBuyRemoveTime;
}
