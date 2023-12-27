namespace CryptoSbmScanner.SettingsDialog;

partial class UserControlEverything
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
        flowLayoutPanel1 = new FlowLayoutPanel();
        UserControlInterval = new UserControlInterval();
        UserControlIntervalTrend = new UserControlTrendInterval();
        UserControlStrategy = new UserControlStrategy();
        UserControlBarometer = new UserControlBarometer();
        UserControlMarketTrend = new UserControlMarketTrend();
        flowLayoutPanel1.SuspendLayout();
        SuspendLayout();
        // 
        // flowLayoutPanel1
        // 
        flowLayoutPanel1.AutoScroll = true;
        flowLayoutPanel1.AutoSize = true;
        flowLayoutPanel1.Controls.Add(UserControlStrategy);
        flowLayoutPanel1.Controls.Add(UserControlInterval);
        flowLayoutPanel1.Controls.Add(UserControlIntervalTrend);
        flowLayoutPanel1.Controls.Add(UserControlBarometer);
        flowLayoutPanel1.Controls.Add(UserControlMarketTrend);
        flowLayoutPanel1.Dock = DockStyle.Fill;
        flowLayoutPanel1.Location = new Point(0, 0);
        flowLayoutPanel1.Name = "flowLayoutPanel1";
        flowLayoutPanel1.Size = new Size(1175, 597);
        flowLayoutPanel1.TabIndex = 291;
        // 
        // UserControlInterval
        // 
        UserControlInterval.AutoScroll = true;
        UserControlInterval.AutoSize = true;
        UserControlInterval.Location = new Point(79, 3);
        UserControlInterval.MinimumSize = new Size(100, 150);
        UserControlInterval.Name = "UserControlInterval";
        UserControlInterval.Padding = new Padding(10);
        UserControlInterval.Size = new Size(100, 150);
        UserControlInterval.TabIndex = 288;
        // 
        // UserControlIntervalTrend
        // 
        UserControlIntervalTrend.AutoScroll = true;
        UserControlIntervalTrend.AutoSize = true;
        UserControlIntervalTrend.Location = new Point(185, 3);
        UserControlIntervalTrend.MinimumSize = new Size(100, 150);
        UserControlIntervalTrend.Name = "UserControlIntervalTrend";
        UserControlIntervalTrend.Padding = new Padding(10);
        UserControlIntervalTrend.Size = new Size(100, 150);
        UserControlIntervalTrend.TabIndex = 298;
        // 
        // UserControlStrategy
        // 
        UserControlStrategy.AutoSize = true;
        UserControlStrategy.Location = new Point(3, 3);
        UserControlStrategy.MinimumSize = new Size(70, 70);
        UserControlStrategy.Name = "UserControlStrategy";
        UserControlStrategy.Padding = new Padding(10);
        UserControlStrategy.Size = new Size(70, 70);
        UserControlStrategy.TabIndex = 290;
        // 
        // UserControlBarometer
        // 
        UserControlBarometer.AutoSize = true;
        UserControlBarometer.Location = new Point(291, 3);
        UserControlBarometer.Name = "UserControlBarometer";
        UserControlBarometer.Padding = new Padding(10);
        UserControlBarometer.Size = new Size(390, 224);
        UserControlBarometer.TabIndex = 291;
        // 
        // UserControlMarketTrend
        // 
        UserControlMarketTrend.AutoSize = true;
        UserControlMarketTrend.Location = new Point(687, 3);
        UserControlMarketTrend.Name = "UserControlMarketTrend";
        UserControlMarketTrend.Padding = new Padding(11);
        UserControlMarketTrend.Size = new Size(396, 127);
        UserControlMarketTrend.TabIndex = 297;
        // 
        // UserControlEverything
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        AutoSize = true;
        Controls.Add(flowLayoutPanel1);
        Name = "UserControlEverything";
        Size = new Size(1175, 597);
        flowLayoutPanel1.ResumeLayout(false);
        flowLayoutPanel1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private FlowLayoutPanel flowLayoutPanel1;
    private UserControlInterval UserControlInterval;
    private UserControlStrategy UserControlStrategy;
    private UserControlBarometer UserControlBarometer;
    private UserControlMarketTrend UserControlMarketTrend;
    private UserControlTrendInterval UserControlIntervalTrend;
}
