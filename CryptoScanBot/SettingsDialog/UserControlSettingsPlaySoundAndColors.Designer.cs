namespace CryptoScanBot.SettingsDialog;

partial class UserControlSettingsPlaySoundAndColors
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
        UserControlShort = new UserControlColorAndSound();
        UserControlLong = new UserControlColorAndSound();
        EditPlaySpeech = new CheckBox();
        EditPlaySound = new CheckBox();
        groupBox1.SuspendLayout();
        SuspendLayout();
        // 
        // groupBox1
        // 
        groupBox1.AutoSize = true;
        groupBox1.Controls.Add(UserControlShort);
        groupBox1.Controls.Add(UserControlLong);
        groupBox1.Controls.Add(EditPlaySpeech);
        groupBox1.Controls.Add(EditPlaySound);
        groupBox1.Dock = DockStyle.Fill;
        groupBox1.Location = new Point(0, 0);
        groupBox1.Name = "groupBox1";
        groupBox1.Size = new Size(794, 156);
        groupBox1.TabIndex = 0;
        groupBox1.TabStop = false;
        groupBox1.Text = "Sound and colors";
        // 
        // UserControlShort
        // 
        UserControlShort.AutoSize = true;
        UserControlShort.Location = new Point(6, 69);
        UserControlShort.Name = "UserControlShort";
        UserControlShort.Size = new Size(775, 34);
        UserControlShort.TabIndex = 165;
        // 
        // UserControlLong
        // 
        UserControlLong.AutoSize = true;
        UserControlLong.Location = new Point(6, 100);
        UserControlLong.Name = "UserControlLong";
        UserControlLong.Size = new Size(775, 34);
        UserControlLong.TabIndex = 164;
        // 
        // EditPlaySpeech
        // 
        EditPlaySpeech.AutoSize = true;
        EditPlaySpeech.Location = new Point(20, 47);
        EditPlaySpeech.Margin = new Padding(4, 3, 4, 3);
        EditPlaySpeech.Name = "EditPlaySpeech";
        EditPlaySpeech.Size = new Size(88, 19);
        EditPlaySpeech.TabIndex = 163;
        EditPlaySpeech.Text = "Play speech";
        EditPlaySpeech.UseVisualStyleBackColor = true;
        // 
        // EditPlaySound
        // 
        EditPlaySound.AutoSize = true;
        EditPlaySound.Location = new Point(20, 22);
        EditPlaySound.Margin = new Padding(4, 3, 4, 3);
        EditPlaySound.Name = "EditPlaySound";
        EditPlaySound.Size = new Size(84, 19);
        EditPlaySound.TabIndex = 162;
        EditPlaySound.Text = "Play sound";
        EditPlaySound.UseVisualStyleBackColor = true;
        // 
        // UserControlSettingsPlaySoundAndColors
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        AutoSize = true;
        Controls.Add(groupBox1);
        Name = "UserControlSettingsPlaySoundAndColors";
        Size = new Size(794, 156);
        groupBox1.ResumeLayout(false);
        groupBox1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBox1;
    private UserControlColorAndSound UserControlShort;
    private UserControlColorAndSound UserControlLong;
    private CheckBox EditPlaySpeech;
    private CheckBox EditPlaySound;
}
