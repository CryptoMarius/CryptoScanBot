namespace CryptoSbmScanner.SettingsDialog;

partial class UserControlTradeRule
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
        panelTradeRule = new FlowLayoutPanel();
        panelTradeRuleMethod = new Panel();
        ButtonTradeRuleDel = new Button();
        ButtonTradeRuleAdd = new Button();
        groupBoxDca.SuspendLayout();
        panelTradeRule.SuspendLayout();
        panelTradeRuleMethod.SuspendLayout();
        SuspendLayout();
        // 
        // groupBoxDca
        // 
        groupBoxDca.AutoSize = true;
        groupBoxDca.Controls.Add(panelTradeRule);
        groupBoxDca.Dock = DockStyle.Fill;
        groupBoxDca.Location = new Point(0, 0);
        groupBoxDca.Name = "groupBoxDca";
        groupBoxDca.Padding = new Padding(5);
        groupBoxDca.Size = new Size(416, 102);
        groupBoxDca.TabIndex = 0;
        groupBoxDca.TabStop = false;
        groupBoxDca.Text = "Trading rules";
        // 
        // panelTradeRule
        // 
        panelTradeRule.AutoScroll = true;
        panelTradeRule.AutoSize = true;
        panelTradeRule.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        panelTradeRule.Controls.Add(panelTradeRuleMethod);
        panelTradeRule.Dock = DockStyle.Fill;
        panelTradeRule.FlowDirection = FlowDirection.TopDown;
        panelTradeRule.Location = new Point(5, 21);
        panelTradeRule.Name = "panelTradeRule";
        panelTradeRule.Size = new Size(406, 76);
        panelTradeRule.TabIndex = 331;
        // 
        // panelTradeRuleMethod
        // 
        panelTradeRuleMethod.AutoSize = true;
        panelTradeRuleMethod.Controls.Add(ButtonTradeRuleDel);
        panelTradeRuleMethod.Controls.Add(ButtonTradeRuleAdd);
        panelTradeRuleMethod.Dock = DockStyle.Top;
        panelTradeRuleMethod.Location = new Point(3, 3);
        panelTradeRuleMethod.MinimumSize = new Size(400, 70);
        panelTradeRuleMethod.Name = "panelTradeRuleMethod";
        panelTradeRuleMethod.Size = new Size(400, 70);
        panelTradeRuleMethod.TabIndex = 331;
        // 
        // ButtonTradeRuleDel
        // 
        ButtonTradeRuleDel.Location = new Point(198, 39);
        ButtonTradeRuleDel.Name = "ButtonTradeRuleDel";
        ButtonTradeRuleDel.Size = new Size(125, 23);
        ButtonTradeRuleDel.TabIndex = 338;
        ButtonTradeRuleDel.Text = "Verwijder regel";
        ButtonTradeRuleDel.UseVisualStyleBackColor = true;
        ButtonTradeRuleDel.Click += ButtonDcaDelClick;
        // 
        // ButtonTradeRuleAdd
        // 
        ButtonTradeRuleAdd.Location = new Point(67, 39);
        ButtonTradeRuleAdd.Name = "ButtonTradeRuleAdd";
        ButtonTradeRuleAdd.Size = new Size(125, 23);
        ButtonTradeRuleAdd.TabIndex = 337;
        ButtonTradeRuleAdd.Text = "Toevoegen regel";
        ButtonTradeRuleAdd.UseVisualStyleBackColor = true;
        ButtonTradeRuleAdd.Click += ButtonDcaAddClick;
        // 
        // UserControlTradeRule
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(groupBoxDca);
        Margin = new Padding(0);
        Name = "UserControlTradeRule";
        Size = new Size(416, 102);
        groupBoxDca.ResumeLayout(false);
        groupBoxDca.PerformLayout();
        panelTradeRule.ResumeLayout(false);
        panelTradeRule.PerformLayout();
        panelTradeRuleMethod.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBoxDca;
    private FlowLayoutPanel panelTradeRule;
    private Panel panelTradeRuleMethod;
    private Button ButtonTradeRuleAdd;
    private Button ButtonTradeRuleDel;
}
