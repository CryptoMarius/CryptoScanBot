namespace CryptoSbmScanner.SettingsDialog;

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
        ButtonDcaDel = new Button();
        ButtonDcaAdd = new Button();
        label82 = new Label();
        EditDcaStepInMethod = new ComboBox();
        label60 = new Label();
        EditDcaOrderMethod = new ComboBox();
        label54 = new Label();
        groupBoxDca.SuspendLayout();
        PanelDca.SuspendLayout();
        panelDcaMethod.SuspendLayout();
        SuspendLayout();
        // 
        // groupBoxDca
        // 
        groupBoxDca.AutoSize = true;
        groupBoxDca.Controls.Add(PanelDca);
        groupBoxDca.Controls.Add(label54);
        groupBoxDca.Dock = DockStyle.Fill;
        groupBoxDca.Location = new Point(0, 0);
        groupBoxDca.Name = "groupBoxDca";
        groupBoxDca.Padding = new Padding(5);
        groupBoxDca.Size = new Size(416, 125);
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
        PanelDca.Size = new Size(406, 99);
        PanelDca.TabIndex = 331;
        // 
        // panelDcaMethod
        // 
        panelDcaMethod.AutoSize = true;
        panelDcaMethod.Controls.Add(ButtonDcaDel);
        panelDcaMethod.Controls.Add(ButtonDcaAdd);
        panelDcaMethod.Controls.Add(label82);
        panelDcaMethod.Controls.Add(EditDcaStepInMethod);
        panelDcaMethod.Controls.Add(label60);
        panelDcaMethod.Controls.Add(EditDcaOrderMethod);
        panelDcaMethod.Dock = DockStyle.Top;
        panelDcaMethod.Location = new Point(3, 3);
        panelDcaMethod.MinimumSize = new Size(400, 90);
        panelDcaMethod.Name = "panelDcaMethod";
        panelDcaMethod.Size = new Size(400, 93);
        panelDcaMethod.TabIndex = 331;
        // 
        // ButtonDcaDel
        // 
        ButtonDcaDel.Location = new Point(198, 67);
        ButtonDcaDel.Name = "ButtonDcaDel";
        ButtonDcaDel.Size = new Size(125, 23);
        ButtonDcaDel.TabIndex = 338;
        ButtonDcaDel.Text = "Verwijder DCA";
        ButtonDcaDel.UseVisualStyleBackColor = true;
        ButtonDcaDel.Click += ButtonDcaDelClick;
        // 
        // ButtonDcaAdd
        // 
        ButtonDcaAdd.Location = new Point(67, 67);
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
        label82.Location = new Point(10, 18);
        label82.Margin = new Padding(4, 0, 4, 0);
        label82.Name = "label82";
        label82.Size = new Size(88, 15);
        label82.TabIndex = 335;
        label82.Text = "Instap moment";
        // 
        // EditDcaStepInMethod
        // 
        EditDcaStepInMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        EditDcaStepInMethod.FormattingEnabled = true;
        EditDcaStepInMethod.Location = new Point(157, 10);
        EditDcaStepInMethod.Margin = new Padding(4, 3, 4, 3);
        EditDcaStepInMethod.Name = "EditDcaStepInMethod";
        EditDcaStepInMethod.Size = new Size(226, 23);
        EditDcaStepInMethod.TabIndex = 334;
        // 
        // label60
        // 
        label60.AutoSize = true;
        label60.Location = new Point(10, 42);
        label60.Margin = new Padding(4, 0, 4, 0);
        label60.Name = "label60";
        label60.Size = new Size(86, 15);
        label60.TabIndex = 333;
        label60.Text = "Koop methode";
        // 
        // EditDcaOrderMethod
        // 
        EditDcaOrderMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        EditDcaOrderMethod.FormattingEnabled = true;
        EditDcaOrderMethod.Location = new Point(157, 38);
        EditDcaOrderMethod.Margin = new Padding(4, 3, 4, 3);
        EditDcaOrderMethod.Name = "EditDcaOrderMethod";
        EditDcaOrderMethod.Size = new Size(226, 23);
        EditDcaOrderMethod.TabIndex = 332;
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
        Size = new Size(416, 125);
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
    private ComboBox EditDcaStepInMethod;
    private Label label60;
    private ComboBox EditDcaOrderMethod;
    private Button ButtonDcaDel;
    private Button ButtonDcaAdd;
}
