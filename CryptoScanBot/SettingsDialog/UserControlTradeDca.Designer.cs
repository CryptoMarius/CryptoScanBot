namespace CryptoScanBot.SettingsDialog;

partial class UserControlTradeDca
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
        PanelDca = new FlowLayoutPanel();
        panelDcaMethod = new Panel();
        label1 = new Label();
        EditOrderType = new ComboBox();
        ButtonDcaDel = new Button();
        ButtonDcaAdd = new Button();
        label82 = new Label();
        EditStrategy = new ComboBox();
        label60 = new Label();
        EditPricing = new ComboBox();
        label54 = new Label();
        groupBoxDca.SuspendLayout();
        PanelDca.SuspendLayout();
        panelDcaMethod.SuspendLayout();
        SuspendLayout();
        // 
        // groupBoxDca
        // 
        groupBoxDca.AutoSize = true;
        groupBoxDca.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupBoxDca.Controls.Add(PanelDca);
        groupBoxDca.Controls.Add(label54);
        groupBoxDca.Dock = DockStyle.Fill;
        groupBoxDca.Location = new Point(0, 0);
        groupBoxDca.Name = "groupBoxDca";
        groupBoxDca.Padding = new Padding(5);
        groupBoxDca.Size = new Size(416, 168);
        groupBoxDca.TabIndex = 0;
        groupBoxDca.TabStop = false;
        groupBoxDca.Text = "DCA/Bijkoop";
        // 
        // PanelDca
        // 
        PanelDca.AutoScroll = true;
        PanelDca.AutoSize = true;
        PanelDca.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        PanelDca.Controls.Add(panelDcaMethod);
        PanelDca.Dock = DockStyle.Fill;
        PanelDca.FlowDirection = FlowDirection.TopDown;
        PanelDca.Location = new Point(5, 21);
        PanelDca.Name = "PanelDca";
        PanelDca.Size = new Size(406, 142);
        PanelDca.TabIndex = 331;
        // 
        // panelDcaMethod
        // 
        panelDcaMethod.AutoSize = true;
        panelDcaMethod.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        panelDcaMethod.Controls.Add(label1);
        panelDcaMethod.Controls.Add(EditOrderType);
        panelDcaMethod.Controls.Add(ButtonDcaDel);
        panelDcaMethod.Controls.Add(ButtonDcaAdd);
        panelDcaMethod.Controls.Add(label82);
        panelDcaMethod.Controls.Add(EditStrategy);
        panelDcaMethod.Controls.Add(label60);
        panelDcaMethod.Controls.Add(EditPricing);
        panelDcaMethod.Dock = DockStyle.Top;
        panelDcaMethod.Location = new Point(3, 3);
        panelDcaMethod.MinimumSize = new Size(400, 90);
        panelDcaMethod.Name = "panelDcaMethod";
        panelDcaMethod.Padding = new Padding(0, 0, 0, 5);
        panelDcaMethod.Size = new Size(400, 136);
        panelDcaMethod.TabIndex = 331;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(9, 14);
        label1.Margin = new Padding(4, 0, 4, 0);
        label1.Name = "label1";
        label1.Size = new Size(64, 15);
        label1.TabIndex = 340;
        label1.Text = "Order Type";
        // 
        // EditOrderType
        // 
        EditOrderType.DropDownStyle = ComboBoxStyle.DropDownList;
        EditOrderType.FormattingEnabled = true;
        EditOrderType.Location = new Point(158, 11);
        EditOrderType.Margin = new Padding(4, 3, 4, 3);
        EditOrderType.Name = "EditOrderType";
        EditOrderType.Size = new Size(226, 23);
        EditOrderType.TabIndex = 339;
        // 
        // ButtonDcaDel
        // 
        ButtonDcaDel.Location = new Point(203, 105);
        ButtonDcaDel.Name = "ButtonDcaDel";
        ButtonDcaDel.Size = new Size(125, 23);
        ButtonDcaDel.TabIndex = 338;
        ButtonDcaDel.Text = "Verwijder DCA";
        ButtonDcaDel.UseVisualStyleBackColor = true;
        ButtonDcaDel.Click += ButtonDcaDelClick;
        // 
        // ButtonDcaAdd
        // 
        ButtonDcaAdd.Location = new Point(72, 105);
        ButtonDcaAdd.Name = "ButtonDcaAdd";
        ButtonDcaAdd.Size = new Size(125, 23);
        ButtonDcaAdd.TabIndex = 337;
        ButtonDcaAdd.Text = "Toevoegen DCA";
        ButtonDcaAdd.UseVisualStyleBackColor = true;
        ButtonDcaAdd.Click += ButtonDcaAddClick;
        // 
        // label82
        // 
        label82.AutoSize = true;
        label82.Location = new Point(9, 43);
        label82.Margin = new Padding(4, 0, 4, 0);
        label82.Name = "label82";
        label82.Size = new Size(50, 15);
        label82.TabIndex = 335;
        label82.Text = "Strategy";
        // 
        // EditStrategy
        // 
        EditStrategy.DropDownStyle = ComboBoxStyle.DropDownList;
        EditStrategy.FormattingEnabled = true;
        EditStrategy.Location = new Point(158, 40);
        EditStrategy.Margin = new Padding(4, 3, 4, 3);
        EditStrategy.Name = "EditStrategy";
        EditStrategy.Size = new Size(226, 23);
        EditStrategy.TabIndex = 334;
        // 
        // label60
        // 
        label60.AutoSize = true;
        label60.Location = new Point(9, 72);
        label60.Margin = new Padding(4, 0, 4, 0);
        label60.Name = "label60";
        label60.Size = new Size(44, 15);
        label60.TabIndex = 333;
        label60.Text = "Pricing";
        // 
        // EditPricing
        // 
        EditPricing.DropDownStyle = ComboBoxStyle.DropDownList;
        EditPricing.FormattingEnabled = true;
        EditPricing.Location = new Point(158, 69);
        EditPricing.Margin = new Padding(4, 3, 4, 3);
        EditPricing.Name = "EditPricing";
        EditPricing.Size = new Size(226, 23);
        EditPricing.TabIndex = 332;
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
        // UserControlTradeDca
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(groupBoxDca);
        Margin = new Padding(0);
        Name = "UserControlTradeDca";
        Size = new Size(416, 168);
        groupBoxDca.ResumeLayout(false);
        groupBoxDca.PerformLayout();
        PanelDca.ResumeLayout(false);
        PanelDca.PerformLayout();
        panelDcaMethod.ResumeLayout(false);
        panelDcaMethod.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBoxDca;
    private Label label54;
    private FlowLayoutPanel PanelDca;
    private Panel panelDcaMethod;
    private Label label82;
    private ComboBox EditStrategy;
    private Label label60;
    private ComboBox EditPricing;
    private Button ButtonDcaDel;
    private Button ButtonDcaAdd;
    private Label label1;
    private ComboBox EditOrderType;
}
