namespace CryptoScanBot.SettingsDialog;

partial class UserControlTelegram
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
        EditUseEmojiInTrend = new CheckBox();
        buttonTelegramStart = new Button();
        EditSendSignalsToTelegram = new CheckBox();
        ButtonTestTelegram = new Button();
        label24 = new Label();
        EditTelegramChatId = new TextBox();
        EditTelegramToken = new TextBox();
        label15 = new Label();
        groupBoxTelegram.SuspendLayout();
        SuspendLayout();
        // 
        // groupBoxTelegram
        // 
        groupBoxTelegram.AutoSize = true;
        groupBoxTelegram.Controls.Add(EditUseEmojiInTrend);
        groupBoxTelegram.Controls.Add(buttonTelegramStart);
        groupBoxTelegram.Controls.Add(EditSendSignalsToTelegram);
        groupBoxTelegram.Controls.Add(ButtonTestTelegram);
        groupBoxTelegram.Controls.Add(label24);
        groupBoxTelegram.Controls.Add(EditTelegramChatId);
        groupBoxTelegram.Controls.Add(EditTelegramToken);
        groupBoxTelegram.Controls.Add(label15);
        groupBoxTelegram.Dock = DockStyle.Fill;
        groupBoxTelegram.Location = new Point(0, 0);
        groupBoxTelegram.Name = "groupBoxTelegram";
        groupBoxTelegram.Size = new Size(436, 161);
        groupBoxTelegram.TabIndex = 0;
        groupBoxTelegram.TabStop = false;
        groupBoxTelegram.Text = "Telegram ";
        // 
        // EditUseEmojiInTrend
        // 
        EditUseEmojiInTrend.AutoSize = true;
        EditUseEmojiInTrend.Location = new Point(10, 119);
        EditUseEmojiInTrend.Margin = new Padding(4, 3, 4, 3);
        EditUseEmojiInTrend.Name = "EditUseEmojiInTrend";
        EditUseEmojiInTrend.Size = new Size(168, 19);
        EditUseEmojiInTrend.TabIndex = 184;
        EditUseEmojiInTrend.Text = "Gebruik Emoji's in de trend";
        EditUseEmojiInTrend.UseVisualStyleBackColor = true;
        // 
        // buttonTelegramStart
        // 
        buttonTelegramStart.Location = new Point(350, 34);
        buttonTelegramStart.Name = "buttonTelegramStart";
        buttonTelegramStart.Size = new Size(75, 23);
        buttonTelegramStart.TabIndex = 183;
        buttonTelegramStart.Text = "Stop/Start";
        buttonTelegramStart.UseVisualStyleBackColor = true;
        // 
        // EditSendSignalsToTelegram
        // 
        EditSendSignalsToTelegram.AutoSize = true;
        EditSendSignalsToTelegram.Location = new Point(10, 94);
        EditSendSignalsToTelegram.Margin = new Padding(4, 3, 4, 3);
        EditSendSignalsToTelegram.Name = "EditSendSignalsToTelegram";
        EditSendSignalsToTelegram.Size = new Size(190, 19);
        EditSendSignalsToTelegram.TabIndex = 182;
        EditSendSignalsToTelegram.Text = "Stuur meldingen naar telegram";
        EditSendSignalsToTelegram.UseVisualStyleBackColor = true;
        // 
        // ButtonTestTelegram
        // 
        ButtonTestTelegram.Location = new Point(350, 63);
        ButtonTestTelegram.Name = "ButtonTestTelegram";
        ButtonTestTelegram.Size = new Size(75, 23);
        ButtonTestTelegram.TabIndex = 181;
        ButtonTestTelegram.Text = "Test";
        ButtonTestTelegram.UseVisualStyleBackColor = true;
        // 
        // label24
        // 
        label24.AutoSize = true;
        label24.Location = new Point(10, 66);
        label24.Margin = new Padding(4, 0, 4, 0);
        label24.Name = "label24";
        label24.Size = new Size(42, 15);
        label24.TabIndex = 180;
        label24.Text = "ChatId";
        // 
        // EditTelegramChatId
        // 
        EditTelegramChatId.Location = new Point(116, 63);
        EditTelegramChatId.Margin = new Padding(4, 3, 4, 3);
        EditTelegramChatId.Name = "EditTelegramChatId";
        EditTelegramChatId.PasswordChar = '*';
        EditTelegramChatId.Size = new Size(227, 23);
        EditTelegramChatId.TabIndex = 179;
        // 
        // EditTelegramToken
        // 
        EditTelegramToken.Location = new Point(116, 34);
        EditTelegramToken.Margin = new Padding(4, 3, 4, 3);
        EditTelegramToken.Name = "EditTelegramToken";
        EditTelegramToken.PasswordChar = '*';
        EditTelegramToken.Size = new Size(227, 23);
        EditTelegramToken.TabIndex = 177;
        // 
        // label15
        // 
        label15.AutoSize = true;
        label15.Location = new Point(10, 37);
        label15.Margin = new Padding(4, 0, 4, 0);
        label15.Name = "label15";
        label15.Size = new Size(38, 15);
        label15.TabIndex = 178;
        label15.Text = "Token";
        // 
        // UserControlTelegram
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        AutoSize = true;
        Controls.Add(groupBoxTelegram);
        Name = "UserControlTelegram";
        Size = new Size(436, 161);
        groupBoxTelegram.ResumeLayout(false);
        groupBoxTelegram.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBoxTelegram;
    private Button buttonTelegramStart;
    private CheckBox EditSendSignalsToTelegram;
    private Button ButtonTestTelegram;
    private Label label24;
    private TextBox EditTelegramChatId;
    private TextBox EditTelegramToken;
    private Label label15;
    private CheckBox EditUseEmojiInTrend;
}
