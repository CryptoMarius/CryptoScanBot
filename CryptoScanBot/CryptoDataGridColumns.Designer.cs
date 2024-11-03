namespace CryptoScanBot;

partial class CryptoDataGridColumns
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

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        flowLayoutPanel = new FlowLayoutPanel();
        panelButtons = new Panel();
        buttonCancel = new Button();
        buttonOk = new Button();
        panelButtons.SuspendLayout();
        SuspendLayout();
        // 
        // flowLayoutPanel
        // 
        flowLayoutPanel.AutoSize = true;
        flowLayoutPanel.Dock = DockStyle.Fill;
        flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel.Location = new Point(0, 0);
        flowLayoutPanel.Name = "flowLayoutPanel";
        flowLayoutPanel.Padding = new Padding(5);
        flowLayoutPanel.Size = new Size(416, 434);
        flowLayoutPanel.TabIndex = 1;
        // 
        // panelButtons
        // 
        panelButtons.Controls.Add(buttonCancel);
        panelButtons.Controls.Add(buttonOk);
        panelButtons.Dock = DockStyle.Bottom;
        panelButtons.Location = new Point(0, 434);
        panelButtons.Margin = new Padding(4, 3, 4, 3);
        panelButtons.Name = "panelButtons";
        panelButtons.Size = new Size(416, 46);
        panelButtons.TabIndex = 2;
        // 
        // buttonCancel
        // 
        buttonCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        buttonCancel.Location = new Point(315, 10);
        buttonCancel.Margin = new Padding(4, 3, 4, 3);
        buttonCancel.Name = "buttonCancel";
        buttonCancel.Size = new Size(88, 27);
        buttonCancel.TabIndex = 1;
        buttonCancel.Text = "&Cancel";
        buttonCancel.UseVisualStyleBackColor = true;
        buttonCancel.Click += ButtonCancelClick;
        // 
        // buttonOk
        // 
        buttonOk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        buttonOk.Location = new Point(219, 10);
        buttonOk.Margin = new Padding(4, 3, 4, 3);
        buttonOk.Name = "buttonOk";
        buttonOk.Size = new Size(88, 27);
        buttonOk.TabIndex = 0;
        buttonOk.Text = "&Ok";
        buttonOk.UseVisualStyleBackColor = true;
        buttonOk.Click += ButtonOkClick;
        // 
        // CryptoDataGridColumns
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ClientSize = new Size(416, 480);
        Controls.Add(flowLayoutPanel);
        Controls.Add(panelButtons);
        Name = "CryptoDataGridColumns";
        Text = "Select your columns";
        panelButtons.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private FlowLayoutPanel flowLayoutPanel;
    private Panel panelButtons;
    private Button buttonCancel;
    private Button buttonOk;
}