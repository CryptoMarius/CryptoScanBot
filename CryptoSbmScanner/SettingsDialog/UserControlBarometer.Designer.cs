namespace CryptoSbmScanner.SettingsDialog;

partial class UserControlBarometer
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
        UserControlBarometer1d = new UserControlBarometerInterval();
        UserControlBarometer4h = new UserControlBarometerInterval();
        UserControlBarometer1h = new UserControlBarometerInterval();
        UserControlBarometer30m = new UserControlBarometerInterval();
        UserControlBarometer15m = new UserControlBarometerInterval();
        EditBarometerLog = new CheckBox();
        EditGroupBox.SuspendLayout();
        SuspendLayout();
        // 
        // EditGroupBox
        // 
        EditGroupBox.AutoSize = true;
        EditGroupBox.Controls.Add(UserControlBarometer1d);
        EditGroupBox.Controls.Add(UserControlBarometer4h);
        EditGroupBox.Controls.Add(UserControlBarometer1h);
        EditGroupBox.Controls.Add(UserControlBarometer30m);
        EditGroupBox.Controls.Add(UserControlBarometer15m);
        EditGroupBox.Controls.Add(EditBarometerLog);
        EditGroupBox.Dock = DockStyle.Fill;
        EditGroupBox.Location = new Point(0, 0);
        EditGroupBox.Name = "EditGroupBox";
        EditGroupBox.Size = new Size(370, 207);
        EditGroupBox.TabIndex = 286;
        EditGroupBox.TabStop = false;
        EditGroupBox.Text = "Barometer";
        // 
        // UserControlBarometer1d
        // 
        UserControlBarometer1d.AutoSize = true;
        UserControlBarometer1d.Location = new Point(6, 127);
        UserControlBarometer1d.Name = "UserControlBarometer1d";
        UserControlBarometer1d.Size = new Size(358, 30);
        UserControlBarometer1d.TabIndex = 276;
        // 
        // UserControlBarometer4h
        // 
        UserControlBarometer4h.AutoSize = true;
        UserControlBarometer4h.Location = new Point(6, 101);
        UserControlBarometer4h.Name = "UserControlBarometer4h";
        UserControlBarometer4h.Size = new Size(358, 30);
        UserControlBarometer4h.TabIndex = 275;
        // 
        // UserControlBarometer1h
        // 
        UserControlBarometer1h.AutoSize = true;
        UserControlBarometer1h.Location = new Point(6, 74);
        UserControlBarometer1h.Name = "UserControlBarometer1h";
        UserControlBarometer1h.Size = new Size(358, 30);
        UserControlBarometer1h.TabIndex = 274;
        // 
        // UserControlBarometer30m
        // 
        UserControlBarometer30m.AutoSize = true;
        UserControlBarometer30m.Location = new Point(6, 47);
        UserControlBarometer30m.Name = "UserControlBarometer30m";
        UserControlBarometer30m.Size = new Size(358, 30);
        UserControlBarometer30m.TabIndex = 273;
        // 
        // UserControlBarometer15m
        // 
        UserControlBarometer15m.AutoSize = true;
        UserControlBarometer15m.Location = new Point(6, 20);
        UserControlBarometer15m.Name = "UserControlBarometer15m";
        UserControlBarometer15m.Size = new Size(358, 30);
        UserControlBarometer15m.TabIndex = 272;
        // 
        // EditBarometerLog
        // 
        EditBarometerLog.AutoSize = true;
        EditBarometerLog.Location = new Point(17, 163);
        EditBarometerLog.Margin = new Padding(4, 3, 4, 3);
        EditBarometerLog.Name = "EditBarometerLog";
        EditBarometerLog.Size = new Size(180, 19);
        EditBarometerLog.TabIndex = 255;
        EditBarometerLog.Text = "Log indien buiten de grenzen";
        EditBarometerLog.UseVisualStyleBackColor = true;
        // 
        // UserControlBarometer
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        Controls.Add(EditGroupBox);
        Name = "UserControlBarometer";
        Size = new Size(370, 207);
        EditGroupBox.ResumeLayout(false);
        EditGroupBox.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox EditGroupBox;
    private CheckBox EditBarometerLog;
    private UserControlBarometerInterval UserControlBarometer15m;
    private UserControlBarometerInterval UserControlBarometer1d;
    private UserControlBarometerInterval UserControlBarometer4h;
    private UserControlBarometerInterval UserControlBarometer1h;
    private UserControlBarometerInterval UserControlBarometer30m;
}
