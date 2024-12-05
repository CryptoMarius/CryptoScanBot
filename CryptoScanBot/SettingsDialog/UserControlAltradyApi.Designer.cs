namespace CryptoScanBot.SettingsDialog;

partial class UserControlAltradyApi
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
        groupBoxTelegram = new GroupBox();
        label24 = new Label();
        EditSecret = new TextBox();
        EditKey = new TextBox();
        label15 = new Label();
        LabelApiKeyDisplay = new Label();
        LabelApiSecretDisplay = new Label();
        groupBoxTelegram.SuspendLayout();
        SuspendLayout();
        // 
        // groupBoxTelegram
        // 
        groupBoxTelegram.AutoSize = true;
        groupBoxTelegram.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupBoxTelegram.Controls.Add(LabelApiSecretDisplay);
        groupBoxTelegram.Controls.Add(LabelApiKeyDisplay);
        groupBoxTelegram.Controls.Add(label24);
        groupBoxTelegram.Controls.Add(EditSecret);
        groupBoxTelegram.Controls.Add(EditKey);
        groupBoxTelegram.Controls.Add(label15);
        groupBoxTelegram.Dock = DockStyle.Fill;
        groupBoxTelegram.Location = new Point(0, 0);
        groupBoxTelegram.Name = "groupBoxTelegram";
        groupBoxTelegram.Size = new Size(407, 96);
        groupBoxTelegram.TabIndex = 0;
        groupBoxTelegram.TabStop = false;
        groupBoxTelegram.Text = "Altrady API key ";
        // 
        // label24
        // 
        label24.AutoSize = true;
        label24.Location = new Point(10, 54);
        label24.Margin = new Padding(4, 0, 4, 0);
        label24.Name = "label24";
        label24.Size = new Size(59, 15);
        label24.TabIndex = 180;
        label24.Text = "API secret";
        // 
        // EditApiSecret
        // 
        EditSecret.Location = new Point(116, 51);
        EditSecret.Margin = new Padding(4, 3, 4, 3);
        EditSecret.Name = "EditSecret";
        EditSecret.PasswordChar = '*';
        EditSecret.Size = new Size(227, 23);
        EditSecret.TabIndex = 179;
        // 
        // EditApiKey
        // 
        EditKey.Location = new Point(116, 22);
        EditKey.Margin = new Padding(4, 3, 4, 3);
        EditKey.Name = "EditKey";
        EditKey.PasswordChar = '*';
        EditKey.Size = new Size(227, 23);
        EditKey.TabIndex = 177;
        // 
        // label15
        // 
        label15.AutoSize = true;
        label15.Location = new Point(10, 25);
        label15.Margin = new Padding(4, 0, 4, 0);
        label15.Name = "label15";
        label15.Size = new Size(46, 15);
        label15.TabIndex = 178;
        label15.Text = "API key";
        // 
        // LabelTelegramToken
        // 
        LabelApiKeyDisplay.AutoSize = true;
        LabelApiKeyDisplay.Location = new Point(354, 25);
        LabelApiKeyDisplay.Margin = new Padding(4, 0, 4, 0);
        LabelApiKeyDisplay.Name = "LabelApiKeyDisplay";
        LabelApiKeyDisplay.Size = new Size(46, 15);
        LabelApiKeyDisplay.TabIndex = 181;
        LabelApiKeyDisplay.Text = "API key";
        // 
        // LabelTelegramChatId
        // 
        LabelApiSecretDisplay.AutoSize = true;
        LabelApiSecretDisplay.Location = new Point(354, 54);
        LabelApiSecretDisplay.Margin = new Padding(4, 0, 4, 0);
        LabelApiSecretDisplay.Name = "LabelApiSecretDisplay";
        LabelApiSecretDisplay.Size = new Size(46, 15);
        LabelApiSecretDisplay.TabIndex = 182;
        LabelApiSecretDisplay.Text = "API key";
        // 
        // UserControlAltradyApi
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(groupBoxTelegram);
        Name = "UserControlAltradyApi";
        Size = new Size(407, 96);
        groupBoxTelegram.ResumeLayout(false);
        groupBoxTelegram.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBoxTelegram;
    private Label label24;
    private TextBox EditSecret;
    private TextBox EditKey;
    private Label label15;
    private Label LabelApiSecretDisplay;
    private Label LabelApiKeyDisplay;
}
