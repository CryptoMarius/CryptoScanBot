namespace CryptoSbmScanner.SettingsDialog;

partial class UserControlMarketTrend
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
        groupBox1 = new GroupBox();
        EditLog = new CheckBox();
        UserControlTrendRange = new UserControlMarketTrendRange();
        groupBox1.SuspendLayout();
        SuspendLayout();
        // 
        // groupBox1
        // 
        groupBox1.AutoSize = true;
        groupBox1.Controls.Add(EditLog);
        groupBox1.Controls.Add(UserControlTrendRange);
        groupBox1.Dock = DockStyle.Fill;
        groupBox1.Location = new Point(0, 0);
        groupBox1.Name = "groupBox1";
        groupBox1.Padding = new Padding(10);
        groupBox1.Size = new Size(374, 105);
        groupBox1.TabIndex = 0;
        groupBox1.TabStop = false;
        groupBox1.Text = "Markt trend (geeft problemen)";
        // 
        // EditLog
        // 
        EditLog.AutoSize = true;
        EditLog.Location = new Point(19, 57);
        EditLog.Name = "EditLog";
        EditLog.Size = new Size(46, 19);
        EditLog.TabIndex = 1;
        EditLog.Text = "Log";
        EditLog.UseVisualStyleBackColor = true;
        // 
        // UserControlTrendRange
        // 
        UserControlTrendRange.AutoSize = true;
        UserControlTrendRange.Location = new Point(13, 24);
        UserControlTrendRange.Name = "UserControlTrendRange";
        UserControlTrendRange.Size = new Size(348, 30);
        UserControlTrendRange.TabIndex = 0;
        // 
        // UserControlMarketTrend
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        Controls.Add(groupBox1);
        Name = "UserControlMarketTrend";
        Size = new Size(374, 105);
        groupBox1.ResumeLayout(false);
        groupBox1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBox1;
    private UserControlMarketTrendRange UserControlTrendRange;
    private CheckBox EditLog;
}
