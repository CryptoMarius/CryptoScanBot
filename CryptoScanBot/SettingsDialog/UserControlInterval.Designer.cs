namespace CryptoScanBot.SettingsDialog;

partial class UserControlInterval
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
        EditGroupBox = new GroupBox();
        flowLayoutPanel1 = new FlowLayoutPanel();
        EditGroupBox.SuspendLayout();
        SuspendLayout();
        // 
        // EditGroupBox
        // 
        EditGroupBox.AutoSize = true;
        EditGroupBox.Controls.Add(flowLayoutPanel1);
        EditGroupBox.Dock = DockStyle.Fill;
        EditGroupBox.Location = new Point(0, 0);
        EditGroupBox.Name = "EditGroupBox";
        EditGroupBox.Size = new Size(108, 151);
        EditGroupBox.TabIndex = 0;
        EditGroupBox.TabStop = false;
        EditGroupBox.Text = "Interval";
        // 
        // flowLayoutPanel1
        // 
        flowLayoutPanel1.AutoScroll = true;
        flowLayoutPanel1.AutoSize = true;
        flowLayoutPanel1.Dock = DockStyle.Fill;
        flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel1.Location = new Point(3, 19);
        flowLayoutPanel1.Name = "flowLayoutPanel1";
        flowLayoutPanel1.Padding = new Padding(10);
        flowLayoutPanel1.Size = new Size(102, 129);
        flowLayoutPanel1.TabIndex = 1;
        flowLayoutPanel1.WrapContents = false;
        // 
        // UserControlInterval
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        AutoSize = true;
        Controls.Add(EditGroupBox);
        MinimumSize = new Size(100, 150);
        Name = "UserControlInterval";
        Size = new Size(108, 151);
        EditGroupBox.ResumeLayout(false);
        EditGroupBox.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox EditGroupBox;
    private FlowLayoutPanel flowLayoutPanel1;
}
