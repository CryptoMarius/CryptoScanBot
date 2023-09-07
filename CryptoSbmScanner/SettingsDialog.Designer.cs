namespace CryptoSbmScanner;

partial class FrmSettings
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
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSettings));
        panel2 = new Panel();
        buttonGotoAppDataFolder = new Button();
        buttonReset = new Button();
        buttonTestSpeech = new Button();
        buttonCancel = new Button();
        buttonOk = new Button();
        panel1 = new Panel();
        tabControl = new TabControl();
        tabAlgemeen = new TabPage();
        EditHideSymbolsOnTheLeft = new CheckBox();
        label58 = new Label();
        EditActivateExchange = new ComboBox();
        EditShowInvalidSignals = new CheckBox();
        label84 = new Label();
        EditExchange = new ComboBox();
        label16 = new Label();
        EditGetCandleInterval = new NumericUpDown();
        label40 = new Label();
        EditTrendCalculationMethod = new ComboBox();
        label6 = new Label();
        EditGlobalDataRemoveSignalAfterxCandles = new NumericUpDown();
        EditBlackTheming = new CheckBox();
        buttonFontDialog = new Button();
        label18 = new Label();
        EditSoundHeartBeatMinutes = new NumericUpDown();
        label2 = new Label();
        EditTradingApp = new ComboBox();
        tabTelegram = new TabPage();
        buttonTelegramStart = new Button();
        EditSendSignalsToTelegram = new CheckBox();
        ButtonTestTelegram = new Button();
        label24 = new Label();
        EditTelegramChatId = new TextBox();
        EditTelegramToken = new TextBox();
        label15 = new Label();
        tabBasismunten = new TabPage();
        tabPageSignals = new TabPage();
        label64 = new Label();
        EditAnalysisMinEffectivePercentage = new NumericUpDown();
        EditAnalysisMaxEffectivePercentage = new NumericUpDown();
        EditLogAnalysisMinMaxEffectivePercentage = new CheckBox();
        label79 = new Label();
        label48 = new Label();
        label38 = new Label();
        label37 = new Label();
        label10 = new Label();
        EditCandlesWithFlatPriceCheck = new CheckBox();
        EditCandlesWithZeroVolumeCheck = new CheckBox();
        EditMinimumAboveBollingerBandsSmaCheck = new CheckBox();
        EditMinimumAboveBollingerBandsUpperCheck = new CheckBox();
        EditCandlesWithZeroVolume = new NumericUpDown();
        EditCandlesWithFlatPrice = new NumericUpDown();
        EditMinimumAboveBollingerBandsUpper = new NumericUpDown();
        EditMinimumAboveBollingerBandsSma = new NumericUpDown();
        label26 = new Label();
        EditLogMinimumTickPercentage = new CheckBox();
        EditMinimumTickPercentage = new NumericUpDown();
        label61 = new Label();
        label53 = new Label();
        EditAnalysisMinChangePercentage = new NumericUpDown();
        EditAnalysisMaxChangePercentage = new NumericUpDown();
        EditLogBarometerToLow = new CheckBox();
        EditLogSymbolMustExistsDays = new CheckBox();
        EditSymbolMustExistsDays = new NumericUpDown();
        label25 = new Label();
        label35 = new Label();
        EditBarometer1hMinimal = new NumericUpDown();
        EditLogAnalysisMinMaxChangePercentage = new CheckBox();
        groupBoxInterval = new GroupBox();
        EditAnalyzeInterval6h = new CheckBox();
        EditAnalyzeInterval8h = new CheckBox();
        EditAnalyzeInterval12h = new CheckBox();
        EditAnalyzeInterval1d = new CheckBox();
        EditAnalyzeInterval5m = new CheckBox();
        EditAnalyzeInterval1m = new CheckBox();
        EditAnalyzeInterval2m = new CheckBox();
        EditAnalyzeInterval3m = new CheckBox();
        EditAnalyzeInterval10m = new CheckBox();
        EditAnalyzeInterval15m = new CheckBox();
        EditAnalyzeInterval30m = new CheckBox();
        EditAnalyzeInterval1h = new CheckBox();
        EditAnalyzeInterval2h = new CheckBox();
        EditAnalyzeInterval4h = new CheckBox();
        tabSignalStobb = new TabPage();
        label66 = new Label();
        EditStobMinimalTrend = new NumericUpDown();
        label77 = new Label();
        label75 = new Label();
        EditStobIncludeSbmPercAndCrossing = new CheckBox();
        label30 = new Label();
        label28 = new Label();
        buttonColorStobb = new Button();
        EditStobIncludeSbmMaLines = new CheckBox();
        EditStobIncludeRsi = new CheckBox();
        buttonPlaySoundStobbOversold = new Button();
        buttonPlaySoundStobbOverbought = new Button();
        buttonSelectSoundStobbOversold = new Button();
        panelColorStobb = new Panel();
        EditSoundStobbOversold = new TextBox();
        EditSoundStobbOverbought = new TextBox();
        buttonSelectSoundStobbOverbought = new Button();
        EditPlaySpeechStobbSignal = new CheckBox();
        EditPlaySoundStobbSignal = new CheckBox();
        EditAnalyzeStobbOversold = new CheckBox();
        EditAnalyzeStobbOverbought = new CheckBox();
        EditStobbUseLowHigh = new CheckBox();
        label1 = new Label();
        EditStobbBBMinPercentage = new NumericUpDown();
        EditStobbBBMaxPercentage = new NumericUpDown();
        tabSignalSbm = new TabPage();
        EditSbm2UseLowHigh = new CheckBox();
        label21 = new Label();
        label20 = new Label();
        label9 = new Label();
        label41 = new Label();
        EditSbm1CandlesLookbackCount = new NumericUpDown();
        label39 = new Label();
        EditSbmCandlesForMacdRecovery = new NumericUpDown();
        label31 = new Label();
        label32 = new Label();
        buttonPlaySoundSbmOversold = new Button();
        buttonPlaySoundSbmOverbought = new Button();
        buttonSelectSoundSbmOversold = new Button();
        EditSoundFileSbmOversold = new TextBox();
        EditSoundFileSbmOverbought = new TextBox();
        buttonSelectSoundSbmOverbought = new Button();
        EditPlaySpeechSbmSignal = new CheckBox();
        EditAnalyzeSbmOversold = new CheckBox();
        EditAnalyzeSbmOverbought = new CheckBox();
        EditPlaySoundSbmSignal = new CheckBox();
        buttonColorSbm = new Button();
        EditSbmUseLowHigh = new CheckBox();
        label17 = new Label();
        EditSbmBBMinPercentage = new NumericUpDown();
        EditSbmBBMaxPercentage = new NumericUpDown();
        label22 = new Label();
        label4 = new Label();
        EditSbmMa200AndMa20Percentage = new NumericUpDown();
        label8 = new Label();
        EditSbmMa50AndMa20Percentage = new NumericUpDown();
        label7 = new Label();
        EditSbmMa200AndMa50Percentage = new NumericUpDown();
        EditSbmMa50AndMa20Lookback = new NumericUpDown();
        EditSbmMa50AndMa20Crossing = new CheckBox();
        EditSbmMa200AndMa50Lookback = new NumericUpDown();
        EditSbmMa200AndMa50Crossing = new CheckBox();
        EditSbmMa200AndMa20Lookback = new NumericUpDown();
        EditSbmMa200AndMa20Crossing = new CheckBox();
        EditAnalyzeSbm3Overbought = new CheckBox();
        EditAnalyzeSbm2Overbought = new CheckBox();
        label12 = new Label();
        EditSbm2BbPercentage = new NumericUpDown();
        panelColorSbm = new Panel();
        label13 = new Label();
        EditSbm3CandlesForBBRecovery = new NumericUpDown();
        label14 = new Label();
        EditSbm3CandlesForBBRecoveryPercentage = new NumericUpDown();
        label11 = new Label();
        EditSbm2CandlesLookbackCount = new NumericUpDown();
        EditAnalyzeSbm3Oversold = new CheckBox();
        EditAnalyzeSbm2Oversold = new CheckBox();
        tabSignalJump = new TabPage();
        label78 = new Label();
        label76 = new Label();
        label33 = new Label();
        label34 = new Label();
        label5 = new Label();
        EditJumpCandlesLookbackCount = new NumericUpDown();
        EditJumpUseLowHighCalculation = new CheckBox();
        buttonColorJump = new Button();
        buttonPlaySoundCandleJumpDown = new Button();
        buttonPlaySoundCandleJumpUp = new Button();
        buttonSelectSoundCandleJumpDown = new Button();
        panelColorJump = new Panel();
        EditSoundFileCandleJumpDown = new TextBox();
        EditSoundFileCandleJumpUp = new TextBox();
        buttonSelectSoundCandleJumpUp = new Button();
        EditPlaySpeechCandleJumpSignal = new CheckBox();
        label3 = new Label();
        EditPlaySoundCandleJumpSignal = new CheckBox();
        EditAnalyzeCandleJumpUp = new CheckBox();
        EditAnalyzeCandleJumpDown = new CheckBox();
        EditAnalysisCandleJumpPercentage = new NumericUpDown();
        tabPageTrading = new TabPage();
        label19 = new Label();
        EditMargin = new ComboBox();
        label23 = new Label();
        EditLeverage = new NumericUpDown();
        EditLogCanceledOrders = new CheckBox();
        EditSoundTradeNotification = new CheckBox();
        EditDisableNewPositions = new CheckBox();
        label83 = new Label();
        EditBuyStepInMethod = new ComboBox();
        label82 = new Label();
        EditDcaStepInMethod = new ComboBox();
        label65 = new Label();
        EditDynamicTpPercentage = new NumericUpDown();
        EditTradeViaBinance = new CheckBox();
        label63 = new Label();
        EditSellMethod = new ComboBox();
        EditTradeViaPaperTrading = new CheckBox();
        label60 = new Label();
        EditDcaOrderMethod = new ComboBox();
        label36 = new Label();
        label81 = new Label();
        label57 = new Label();
        label54 = new Label();
        groupBoxSlots = new GroupBox();
        label50 = new Label();
        EditSlotsMaximalExchange = new NumericUpDown();
        label52 = new Label();
        EditSlotsMaximalSymbol = new NumericUpDown();
        label56 = new Label();
        EditSlotsMaximalBase = new NumericUpDown();
        groupBox2 = new GroupBox();
        EditBarometer15mBotMinimal = new NumericUpDown();
        label27 = new Label();
        EditBarometer24hBotMinimal = new NumericUpDown();
        label42 = new Label();
        EditBarometer04hBotMinimal = new NumericUpDown();
        label43 = new Label();
        EditBarometer01hBotMinimal = new NumericUpDown();
        label44 = new Label();
        label45 = new Label();
        EditBarometer30mBotMinimal = new NumericUpDown();
        groupBox1 = new GroupBox();
        EditMonitorInterval1h = new CheckBox();
        EditMonitorInterval2h = new CheckBox();
        EditMonitorInterval4h = new CheckBox();
        EditMonitorInterval1m = new CheckBox();
        EditMonitorInterval2m = new CheckBox();
        EditMonitorInterval3m = new CheckBox();
        EditMonitorInterval5m = new CheckBox();
        EditMonitorInterval10m = new CheckBox();
        EditMonitorInterval15m = new CheckBox();
        EditMonitorInterval30m = new CheckBox();
        label62 = new Label();
        EditBuyOrderMethod = new ComboBox();
        EditDcaCount = new NumericUpDown();
        label67 = new Label();
        label68 = new Label();
        EditDcaFactor = new NumericUpDown();
        label69 = new Label();
        EditDcaPercentage = new NumericUpDown();
        EditGlobalStopLimitPercentage = new NumericUpDown();
        label70 = new Label();
        EditGlobalStopPercentage = new NumericUpDown();
        label71 = new Label();
        label72 = new Label();
        EditProfitPercentage = new NumericUpDown();
        label73 = new Label();
        EditGlobalBuyCooldownTime = new NumericUpDown();
        EditGlobalBuyVarying = new NumericUpDown();
        label47 = new Label();
        label46 = new Label();
        EditGlobalBuyRemoveTime = new NumericUpDown();
        tabWhiteListOversold = new TabPage();
        textBoxWhiteListOversold = new TextBox();
        panel3 = new Panel();
        label55 = new Label();
        tabBlackListOversold = new TabPage();
        textBoxBlackListOversold = new TextBox();
        panel4 = new Panel();
        label51 = new Label();
        tabWhiteListOverbought = new TabPage();
        textBoxWhiteListOverbought = new TextBox();
        panel5 = new Panel();
        label29 = new Label();
        tabBlacklistOverbought = new TabPage();
        textBoxBlackListOverbought = new TextBox();
        panel6 = new Panel();
        label49 = new Label();
        toolTip1 = new ToolTip(components);
        imageList1 = new ImageList(components);
        colorDialog1 = new ColorDialog();
        panel2.SuspendLayout();
        panel1.SuspendLayout();
        tabControl.SuspendLayout();
        tabAlgemeen.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditGetCandleInterval).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalDataRemoveSignalAfterxCandles).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSoundHeartBeatMinutes).BeginInit();
        tabTelegram.SuspendLayout();
        tabPageSignals.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMinEffectivePercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMaxEffectivePercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditCandlesWithZeroVolume).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditCandlesWithFlatPrice).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumAboveBollingerBandsUpper).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumAboveBollingerBandsSma).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumTickPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMinChangePercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMaxChangePercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSymbolMustExistsDays).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer1hMinimal).BeginInit();
        groupBoxInterval.SuspendLayout();
        tabSignalStobb.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditStobMinimalTrend).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditStobbBBMinPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditStobbBBMaxPercentage).BeginInit();
        tabSignalSbm.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditSbm1CandlesLookbackCount).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmCandlesForMacdRecovery).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmBBMinPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmBBMaxPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa20Percentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa50AndMa20Percentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa50Percentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa50AndMa20Lookback).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa50Lookback).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa20Lookback).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm2BbPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm3CandlesForBBRecovery).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm3CandlesForBBRecoveryPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm2CandlesLookbackCount).BeginInit();
        tabSignalJump.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditJumpCandlesLookbackCount).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisCandleJumpPercentage).BeginInit();
        tabPageTrading.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditLeverage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditDynamicTpPercentage).BeginInit();
        groupBoxSlots.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalExchange).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalSymbol).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalBase).BeginInit();
        groupBox2.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditBarometer15mBotMinimal).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer24hBotMinimal).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer04hBotMinimal).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer01hBotMinimal).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer30mBotMinimal).BeginInit();
        groupBox1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditDcaCount).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditDcaFactor).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditDcaPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalStopLimitPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalStopPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditProfitPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyCooldownTime).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyVarying).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyRemoveTime).BeginInit();
        tabWhiteListOversold.SuspendLayout();
        panel3.SuspendLayout();
        tabBlackListOversold.SuspendLayout();
        panel4.SuspendLayout();
        tabWhiteListOverbought.SuspendLayout();
        panel5.SuspendLayout();
        tabBlacklistOverbought.SuspendLayout();
        panel6.SuspendLayout();
        SuspendLayout();
        // 
        // panel2
        // 
        panel2.Controls.Add(buttonGotoAppDataFolder);
        panel2.Controls.Add(buttonReset);
        panel2.Controls.Add(buttonTestSpeech);
        panel2.Controls.Add(buttonCancel);
        panel2.Controls.Add(buttonOk);
        panel2.Dock = DockStyle.Bottom;
        panel2.Location = new Point(0, 762);
        panel2.Margin = new Padding(4, 3, 4, 3);
        panel2.Name = "panel2";
        panel2.Size = new Size(1240, 46);
        panel2.TabIndex = 1;
        // 
        // buttonGotoAppDataFolder
        // 
        buttonGotoAppDataFolder.Location = new Point(239, 10);
        buttonGotoAppDataFolder.Name = "buttonGotoAppDataFolder";
        buttonGotoAppDataFolder.Size = new Size(117, 27);
        buttonGotoAppDataFolder.TabIndex = 190;
        buttonGotoAppDataFolder.Text = "Naar data folder";
        buttonGotoAppDataFolder.UseVisualStyleBackColor = true;
        // 
        // buttonReset
        // 
        buttonReset.Location = new Point(27, 10);
        buttonReset.Margin = new Padding(4, 3, 4, 3);
        buttonReset.Name = "buttonReset";
        buttonReset.Size = new Size(88, 27);
        buttonReset.TabIndex = 11;
        buttonReset.Text = "Reset";
        buttonReset.UseVisualStyleBackColor = true;
        // 
        // buttonTestSpeech
        // 
        buttonTestSpeech.Location = new Point(132, 10);
        buttonTestSpeech.Margin = new Padding(4, 3, 4, 3);
        buttonTestSpeech.Name = "buttonTestSpeech";
        buttonTestSpeech.Size = new Size(88, 27);
        buttonTestSpeech.TabIndex = 10;
        buttonTestSpeech.Text = "Test speech";
        buttonTestSpeech.UseVisualStyleBackColor = true;
        // 
        // buttonCancel
        // 
        buttonCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        buttonCancel.Location = new Point(1140, 10);
        buttonCancel.Margin = new Padding(4, 3, 4, 3);
        buttonCancel.Name = "buttonCancel";
        buttonCancel.Size = new Size(88, 27);
        buttonCancel.TabIndex = 1;
        buttonCancel.Text = "&Cancel";
        buttonCancel.UseVisualStyleBackColor = true;
        buttonCancel.Click += ButtonCancel_Click;
        // 
        // buttonOk
        // 
        buttonOk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        buttonOk.Location = new Point(1045, 10);
        buttonOk.Margin = new Padding(4, 3, 4, 3);
        buttonOk.Name = "buttonOk";
        buttonOk.Size = new Size(88, 27);
        buttonOk.TabIndex = 0;
        buttonOk.Text = "&Ok";
        buttonOk.UseVisualStyleBackColor = true;
        buttonOk.Click += ButtonOk_Click;
        // 
        // panel1
        // 
        panel1.Controls.Add(tabControl);
        panel1.Dock = DockStyle.Fill;
        panel1.Location = new Point(0, 0);
        panel1.Margin = new Padding(4, 3, 4, 3);
        panel1.Name = "panel1";
        panel1.Size = new Size(1240, 808);
        panel1.TabIndex = 0;
        // 
        // tabControl
        // 
        tabControl.Appearance = TabAppearance.FlatButtons;
        tabControl.Controls.Add(tabAlgemeen);
        tabControl.Controls.Add(tabTelegram);
        tabControl.Controls.Add(tabBasismunten);
        tabControl.Controls.Add(tabPageSignals);
        tabControl.Controls.Add(tabSignalStobb);
        tabControl.Controls.Add(tabSignalSbm);
        tabControl.Controls.Add(tabSignalJump);
        tabControl.Controls.Add(tabPageTrading);
        tabControl.Controls.Add(tabWhiteListOversold);
        tabControl.Controls.Add(tabBlackListOversold);
        tabControl.Controls.Add(tabWhiteListOverbought);
        tabControl.Controls.Add(tabBlacklistOverbought);
        tabControl.Dock = DockStyle.Fill;
        tabControl.Location = new Point(0, 0);
        tabControl.Margin = new Padding(4, 3, 4, 3);
        tabControl.Name = "tabControl";
        tabControl.SelectedIndex = 0;
        tabControl.Size = new Size(1240, 808);
        tabControl.TabIndex = 100;
        // 
        // tabAlgemeen
        // 
        tabAlgemeen.Controls.Add(EditHideSymbolsOnTheLeft);
        tabAlgemeen.Controls.Add(label58);
        tabAlgemeen.Controls.Add(EditActivateExchange);
        tabAlgemeen.Controls.Add(EditShowInvalidSignals);
        tabAlgemeen.Controls.Add(label84);
        tabAlgemeen.Controls.Add(EditExchange);
        tabAlgemeen.Controls.Add(label16);
        tabAlgemeen.Controls.Add(EditGetCandleInterval);
        tabAlgemeen.Controls.Add(label40);
        tabAlgemeen.Controls.Add(EditTrendCalculationMethod);
        tabAlgemeen.Controls.Add(label6);
        tabAlgemeen.Controls.Add(EditGlobalDataRemoveSignalAfterxCandles);
        tabAlgemeen.Controls.Add(EditBlackTheming);
        tabAlgemeen.Controls.Add(buttonFontDialog);
        tabAlgemeen.Controls.Add(label18);
        tabAlgemeen.Controls.Add(EditSoundHeartBeatMinutes);
        tabAlgemeen.Controls.Add(label2);
        tabAlgemeen.Controls.Add(EditTradingApp);
        tabAlgemeen.Location = new Point(4, 27);
        tabAlgemeen.Margin = new Padding(4, 3, 4, 3);
        tabAlgemeen.Name = "tabAlgemeen";
        tabAlgemeen.Padding = new Padding(4, 3, 4, 3);
        tabAlgemeen.Size = new Size(1232, 777);
        tabAlgemeen.TabIndex = 6;
        tabAlgemeen.Text = "Algemeen";
        tabAlgemeen.UseVisualStyleBackColor = true;
        // 
        // EditHideSymbolsOnTheLeft
        // 
        EditHideSymbolsOnTheLeft.AutoSize = true;
        EditHideSymbolsOnTheLeft.Location = new Point(29, 268);
        EditHideSymbolsOnTheLeft.Margin = new Padding(4, 3, 4, 3);
        EditHideSymbolsOnTheLeft.Name = "EditHideSymbolsOnTheLeft";
        EditHideSymbolsOnTheLeft.Size = new Size(182, 19);
        EditHideSymbolsOnTheLeft.TabIndex = 190;
        EditHideSymbolsOnTheLeft.Text = "Verberg de lijst met symbolen";
        EditHideSymbolsOnTheLeft.UseVisualStyleBackColor = true;
        // 
        // label58
        // 
        label58.AutoSize = true;
        label58.Location = new Point(24, 67);
        label58.Margin = new Padding(4, 0, 4, 0);
        label58.Name = "label58";
        label58.Size = new Size(104, 15);
        label58.TabIndex = 189;
        label58.Text = "Activeer exchange";
        // 
        // EditActivateExchange
        // 
        EditActivateExchange.DropDownStyle = ComboBoxStyle.DropDownList;
        EditActivateExchange.FormattingEnabled = true;
        EditActivateExchange.Items.AddRange(new object[] { "De actieve exchange", "Binance", "Bybit Spot", "Bybit Futures", "Kucoin" });
        EditActivateExchange.Location = new Point(189, 63);
        EditActivateExchange.Margin = new Padding(4, 3, 4, 3);
        EditActivateExchange.Name = "EditActivateExchange";
        EditActivateExchange.Size = new Size(190, 23);
        EditActivateExchange.TabIndex = 188;
        // 
        // EditShowInvalidSignals
        // 
        EditShowInvalidSignals.AutoSize = true;
        EditShowInvalidSignals.Location = new Point(29, 241);
        EditShowInvalidSignals.Margin = new Padding(4, 3, 4, 3);
        EditShowInvalidSignals.Name = "EditShowInvalidSignals";
        EditShowInvalidSignals.Size = new Size(175, 19);
        EditShowInvalidSignals.TabIndex = 187;
        EditShowInvalidSignals.Text = "Laat ongeldige signalen zien";
        EditShowInvalidSignals.UseVisualStyleBackColor = true;
        // 
        // label84
        // 
        label84.AutoSize = true;
        label84.Location = new Point(22, 38);
        label84.Margin = new Padding(4, 0, 4, 0);
        label84.Name = "label84";
        label84.Size = new Size(100, 15);
        label84.TabIndex = 165;
        label84.Text = "Actieve exchange";
        // 
        // EditExchange
        // 
        EditExchange.DropDownStyle = ComboBoxStyle.DropDownList;
        EditExchange.FormattingEnabled = true;
        EditExchange.Location = new Point(188, 34);
        EditExchange.Margin = new Padding(4, 3, 4, 3);
        EditExchange.Name = "EditExchange";
        EditExchange.Size = new Size(190, 23);
        EditExchange.TabIndex = 164;
        // 
        // label16
        // 
        label16.AutoSize = true;
        label16.Location = new Point(26, 208);
        label16.Margin = new Padding(4, 0, 4, 0);
        label16.Name = "label16";
        label16.Size = new Size(263, 15);
        label16.TabIndex = 161;
        label16.Text = "Iedere x minuten controleren op nieuwe munten";
        // 
        // EditGetCandleInterval
        // 
        EditGetCandleInterval.Location = new Point(323, 206);
        EditGetCandleInterval.Margin = new Padding(4, 3, 4, 3);
        EditGetCandleInterval.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
        EditGetCandleInterval.Name = "EditGetCandleInterval";
        EditGetCandleInterval.Size = new Size(57, 23);
        EditGetCandleInterval.TabIndex = 162;
        EditGetCandleInterval.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label40
        // 
        label40.AutoSize = true;
        label40.Location = new Point(22, 124);
        label40.Margin = new Padding(4, 0, 4, 0);
        label40.Name = "label40";
        label40.Size = new Size(98, 15);
        label40.TabIndex = 160;
        label40.Text = "Trend berekening";
        // 
        // EditTrendCalculationMethod
        // 
        EditTrendCalculationMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        EditTrendCalculationMethod.FormattingEnabled = true;
        EditTrendCalculationMethod.Items.AddRange(new object[] { "cAlgo#1 zigzag + interpretatie", "cAlgo#2 zigzag + interpretatie", "EMA 8 > EMA 20 " });
        EditTrendCalculationMethod.Location = new Point(188, 121);
        EditTrendCalculationMethod.Margin = new Padding(4, 3, 4, 3);
        EditTrendCalculationMethod.Name = "EditTrendCalculationMethod";
        EditTrendCalculationMethod.Size = new Size(190, 23);
        EditTrendCalculationMethod.TabIndex = 159;
        // 
        // label6
        // 
        label6.AutoSize = true;
        label6.Location = new Point(26, 183);
        label6.Margin = new Padding(4, 0, 4, 0);
        label6.Name = "label6";
        label6.Size = new Size(186, 15);
        label6.TabIndex = 156;
        label6.Text = "Verwijder de signalen na x candles";
        // 
        // EditGlobalDataRemoveSignalAfterxCandles
        // 
        EditGlobalDataRemoveSignalAfterxCandles.Location = new Point(323, 180);
        EditGlobalDataRemoveSignalAfterxCandles.Margin = new Padding(4, 3, 4, 3);
        EditGlobalDataRemoveSignalAfterxCandles.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditGlobalDataRemoveSignalAfterxCandles.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
        EditGlobalDataRemoveSignalAfterxCandles.Name = "EditGlobalDataRemoveSignalAfterxCandles";
        EditGlobalDataRemoveSignalAfterxCandles.Size = new Size(57, 23);
        EditGlobalDataRemoveSignalAfterxCandles.TabIndex = 157;
        toolTip1.SetToolTip(EditGlobalDataRemoveSignalAfterxCandles, "Kunnen filteren op de 24 uur volume percentage.");
        EditGlobalDataRemoveSignalAfterxCandles.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // EditBlackTheming
        // 
        EditBlackTheming.AutoSize = true;
        EditBlackTheming.Location = new Point(29, 326);
        EditBlackTheming.Margin = new Padding(4, 3, 4, 3);
        EditBlackTheming.Name = "EditBlackTheming";
        EditBlackTheming.Size = new Size(84, 19);
        EditBlackTheming.TabIndex = 155;
        EditBlackTheming.Text = "Gray mode";
        EditBlackTheming.UseVisualStyleBackColor = true;
        // 
        // buttonFontDialog
        // 
        buttonFontDialog.Location = new Point(29, 293);
        buttonFontDialog.Margin = new Padding(4, 3, 4, 3);
        buttonFontDialog.Name = "buttonFontDialog";
        buttonFontDialog.Size = new Size(139, 27);
        buttonFontDialog.TabIndex = 154;
        buttonFontDialog.Text = "Lettertype";
        buttonFontDialog.UseVisualStyleBackColor = true;
        // 
        // label18
        // 
        label18.AutoSize = true;
        label18.Location = new Point(26, 156);
        label18.Margin = new Padding(4, 0, 4, 0);
        label18.Name = "label18";
        label18.Size = new Size(257, 15);
        label18.TabIndex = 152;
        label18.Text = "Iedere x minuten een heart beat geluid afspelen";
        // 
        // EditSoundHeartBeatMinutes
        // 
        EditSoundHeartBeatMinutes.Location = new Point(323, 154);
        EditSoundHeartBeatMinutes.Margin = new Padding(4, 3, 4, 3);
        EditSoundHeartBeatMinutes.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSoundHeartBeatMinutes.Name = "EditSoundHeartBeatMinutes";
        EditSoundHeartBeatMinutes.Size = new Size(57, 23);
        EditSoundHeartBeatMinutes.TabIndex = 153;
        EditSoundHeartBeatMinutes.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(23, 96);
        label2.Margin = new Padding(4, 0, 4, 0);
        label2.Name = "label2";
        label2.Size = new Size(69, 15);
        label2.TabIndex = 149;
        label2.Text = "Trading app";
        // 
        // EditTradingApp
        // 
        EditTradingApp.DropDownStyle = ComboBoxStyle.DropDownList;
        EditTradingApp.FormattingEnabled = true;
        EditTradingApp.Items.AddRange(new object[] { "Altrady", "Hypertrader", "TradingView", "Via de exchange" });
        EditTradingApp.Location = new Point(189, 92);
        EditTradingApp.Margin = new Padding(4, 3, 4, 3);
        EditTradingApp.Name = "EditTradingApp";
        EditTradingApp.Size = new Size(190, 23);
        EditTradingApp.TabIndex = 148;
        // 
        // tabTelegram
        // 
        tabTelegram.Controls.Add(buttonTelegramStart);
        tabTelegram.Controls.Add(EditSendSignalsToTelegram);
        tabTelegram.Controls.Add(ButtonTestTelegram);
        tabTelegram.Controls.Add(label24);
        tabTelegram.Controls.Add(EditTelegramChatId);
        tabTelegram.Controls.Add(EditTelegramToken);
        tabTelegram.Controls.Add(label15);
        tabTelegram.Location = new Point(4, 27);
        tabTelegram.Name = "tabTelegram";
        tabTelegram.Padding = new Padding(3);
        tabTelegram.Size = new Size(1232, 777);
        tabTelegram.TabIndex = 12;
        tabTelegram.Text = "Telegram";
        tabTelegram.UseVisualStyleBackColor = true;
        // 
        // buttonTelegramStart
        // 
        buttonTelegramStart.Location = new Point(401, 30);
        buttonTelegramStart.Name = "buttonTelegramStart";
        buttonTelegramStart.Size = new Size(75, 23);
        buttonTelegramStart.TabIndex = 176;
        buttonTelegramStart.Text = "Stop/Start";
        buttonTelegramStart.UseVisualStyleBackColor = true;
        buttonTelegramStart.Click += ButtonTelegramStart_Click;
        // 
        // EditSendSignalsToTelegram
        // 
        EditSendSignalsToTelegram.AutoSize = true;
        EditSendSignalsToTelegram.Location = new Point(19, 86);
        EditSendSignalsToTelegram.Margin = new Padding(4, 3, 4, 3);
        EditSendSignalsToTelegram.Name = "EditSendSignalsToTelegram";
        EditSendSignalsToTelegram.Size = new Size(190, 19);
        EditSendSignalsToTelegram.TabIndex = 175;
        EditSendSignalsToTelegram.Text = "Stuur meldingen naar telegram";
        EditSendSignalsToTelegram.UseVisualStyleBackColor = true;
        // 
        // ButtonTestTelegram
        // 
        ButtonTestTelegram.Location = new Point(401, 59);
        ButtonTestTelegram.Name = "ButtonTestTelegram";
        ButtonTestTelegram.Size = new Size(75, 23);
        ButtonTestTelegram.TabIndex = 174;
        ButtonTestTelegram.Text = "Test";
        ButtonTestTelegram.UseVisualStyleBackColor = true;
        ButtonTestTelegram.Click += ButtonTestTelegram_Click;
        // 
        // label24
        // 
        label24.AutoSize = true;
        label24.Location = new Point(14, 59);
        label24.Margin = new Padding(4, 0, 4, 0);
        label24.Name = "label24";
        label24.Size = new Size(93, 15);
        label24.TabIndex = 173;
        label24.Text = "Telegram ChatId";
        // 
        // EditTelegramChatId
        // 
        EditTelegramChatId.Location = new Point(151, 56);
        EditTelegramChatId.Margin = new Padding(4, 3, 4, 3);
        EditTelegramChatId.Name = "EditTelegramChatId";
        EditTelegramChatId.Size = new Size(227, 23);
        EditTelegramChatId.TabIndex = 172;
        // 
        // EditTelegramToken
        // 
        EditTelegramToken.Location = new Point(151, 27);
        EditTelegramToken.Margin = new Padding(4, 3, 4, 3);
        EditTelegramToken.Name = "EditTelegramToken";
        EditTelegramToken.Size = new Size(227, 23);
        EditTelegramToken.TabIndex = 170;
        // 
        // label15
        // 
        label15.AutoSize = true;
        label15.Location = new Point(14, 30);
        label15.Margin = new Padding(4, 0, 4, 0);
        label15.Name = "label15";
        label15.Size = new Size(89, 15);
        label15.TabIndex = 171;
        label15.Text = "Telegram Token";
        // 
        // tabBasismunten
        // 
        tabBasismunten.Location = new Point(4, 27);
        tabBasismunten.Margin = new Padding(4, 3, 4, 3);
        tabBasismunten.Name = "tabBasismunten";
        tabBasismunten.Padding = new Padding(4, 3, 4, 3);
        tabBasismunten.Size = new Size(1232, 777);
        tabBasismunten.TabIndex = 0;
        tabBasismunten.Text = "Basismunten";
        tabBasismunten.UseVisualStyleBackColor = true;
        // 
        // tabPageSignals
        // 
        tabPageSignals.Controls.Add(label64);
        tabPageSignals.Controls.Add(EditAnalysisMinEffectivePercentage);
        tabPageSignals.Controls.Add(EditAnalysisMaxEffectivePercentage);
        tabPageSignals.Controls.Add(EditLogAnalysisMinMaxEffectivePercentage);
        tabPageSignals.Controls.Add(label79);
        tabPageSignals.Controls.Add(label48);
        tabPageSignals.Controls.Add(label38);
        tabPageSignals.Controls.Add(label37);
        tabPageSignals.Controls.Add(label10);
        tabPageSignals.Controls.Add(EditCandlesWithFlatPriceCheck);
        tabPageSignals.Controls.Add(EditCandlesWithZeroVolumeCheck);
        tabPageSignals.Controls.Add(EditMinimumAboveBollingerBandsSmaCheck);
        tabPageSignals.Controls.Add(EditMinimumAboveBollingerBandsUpperCheck);
        tabPageSignals.Controls.Add(EditCandlesWithZeroVolume);
        tabPageSignals.Controls.Add(EditCandlesWithFlatPrice);
        tabPageSignals.Controls.Add(EditMinimumAboveBollingerBandsUpper);
        tabPageSignals.Controls.Add(EditMinimumAboveBollingerBandsSma);
        tabPageSignals.Controls.Add(label26);
        tabPageSignals.Controls.Add(EditLogMinimumTickPercentage);
        tabPageSignals.Controls.Add(EditMinimumTickPercentage);
        tabPageSignals.Controls.Add(label61);
        tabPageSignals.Controls.Add(label53);
        tabPageSignals.Controls.Add(EditAnalysisMinChangePercentage);
        tabPageSignals.Controls.Add(EditAnalysisMaxChangePercentage);
        tabPageSignals.Controls.Add(EditLogBarometerToLow);
        tabPageSignals.Controls.Add(EditLogSymbolMustExistsDays);
        tabPageSignals.Controls.Add(EditSymbolMustExistsDays);
        tabPageSignals.Controls.Add(label25);
        tabPageSignals.Controls.Add(label35);
        tabPageSignals.Controls.Add(EditBarometer1hMinimal);
        tabPageSignals.Controls.Add(EditLogAnalysisMinMaxChangePercentage);
        tabPageSignals.Controls.Add(groupBoxInterval);
        tabPageSignals.Location = new Point(4, 27);
        tabPageSignals.Margin = new Padding(4, 3, 4, 3);
        tabPageSignals.Name = "tabPageSignals";
        tabPageSignals.Padding = new Padding(4, 3, 4, 3);
        tabPageSignals.Size = new Size(1232, 777);
        tabPageSignals.TabIndex = 10;
        tabPageSignals.Text = "Signalen";
        tabPageSignals.UseVisualStyleBackColor = true;
        // 
        // label64
        // 
        label64.AutoSize = true;
        label64.Location = new Point(287, 86);
        label64.Margin = new Padding(4, 0, 4, 0);
        label64.Name = "label64";
        label64.Size = new Size(86, 15);
        label64.TabIndex = 208;
        label64.Text = "24 uur effectief";
        // 
        // EditAnalysisMinEffectivePercentage
        // 
        EditAnalysisMinEffectivePercentage.Location = new Point(419, 84);
        EditAnalysisMinEffectivePercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMinEffectivePercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisMinEffectivePercentage.Name = "EditAnalysisMinEffectivePercentage";
        EditAnalysisMinEffectivePercentage.Size = new Size(57, 23);
        EditAnalysisMinEffectivePercentage.TabIndex = 209;
        toolTip1.SetToolTip(EditAnalysisMinEffectivePercentage, "Kunnen filteren op de 24 uur volume percentage.");
        // 
        // EditAnalysisMaxEffectivePercentage
        // 
        EditAnalysisMaxEffectivePercentage.Location = new Point(483, 84);
        EditAnalysisMaxEffectivePercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMaxEffectivePercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisMaxEffectivePercentage.Name = "EditAnalysisMaxEffectivePercentage";
        EditAnalysisMaxEffectivePercentage.Size = new Size(57, 23);
        EditAnalysisMaxEffectivePercentage.TabIndex = 210;
        toolTip1.SetToolTip(EditAnalysisMaxEffectivePercentage, "Kunnen filteren op de 24 uur volume percentage.");
        EditAnalysisMaxEffectivePercentage.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // EditLogAnalysisMinMaxEffectivePercentage
        // 
        EditLogAnalysisMinMaxEffectivePercentage.AutoSize = true;
        EditLogAnalysisMinMaxEffectivePercentage.Location = new Point(568, 85);
        EditLogAnalysisMinMaxEffectivePercentage.Margin = new Padding(4, 3, 4, 3);
        EditLogAnalysisMinMaxEffectivePercentage.Name = "EditLogAnalysisMinMaxEffectivePercentage";
        EditLogAnalysisMinMaxEffectivePercentage.Size = new Size(203, 19);
        EditLogAnalysisMinMaxEffectivePercentage.TabIndex = 211;
        EditLogAnalysisMinMaxEffectivePercentage.Text = "Log waarden buiten deze grenzen";
        EditLogAnalysisMinMaxEffectivePercentage.UseVisualStyleBackColor = true;
        // 
        // label79
        // 
        label79.AutoSize = true;
        label79.Location = new Point(723, 344);
        label79.Margin = new Padding(4, 0, 4, 0);
        label79.Name = "label79";
        label79.Size = new Size(176, 15);
        label79.TabIndex = 207;
        label79.Text = "Kleiner dan dit getal is een nogo";
        // 
        // label48
        // 
        label48.AutoSize = true;
        label48.Location = new Point(723, 312);
        label48.Margin = new Padding(4, 0, 4, 0);
        label48.Name = "label48";
        label48.Size = new Size(176, 15);
        label48.TabIndex = 206;
        label48.Text = "Kleiner dan dit getal is een nogo";
        // 
        // label38
        // 
        label38.AutoSize = true;
        label38.Location = new Point(723, 282);
        label38.Margin = new Padding(4, 0, 4, 0);
        label38.Name = "label38";
        label38.Size = new Size(173, 15);
        label38.TabIndex = 205;
        label38.Text = "Groter dan dit getal is een nogo";
        // 
        // label37
        // 
        label37.AutoSize = true;
        label37.Location = new Point(723, 255);
        label37.Margin = new Padding(4, 0, 4, 0);
        label37.Name = "label37";
        label37.Size = new Size(173, 15);
        label37.TabIndex = 204;
        label37.Text = "Groter dan dit getal is een nogo";
        // 
        // label10
        // 
        label10.AutoSize = true;
        label10.Location = new Point(283, 227);
        label10.Margin = new Padding(4, 0, 4, 0);
        label10.Name = "label10";
        label10.Size = new Size(186, 15);
        label10.TabIndex = 203;
        label10.Text = "Controles op de laatste 60 candles";
        // 
        // EditCandlesWithFlatPriceCheck
        // 
        EditCandlesWithFlatPriceCheck.AutoSize = true;
        EditCandlesWithFlatPriceCheck.Location = new Point(283, 257);
        EditCandlesWithFlatPriceCheck.Margin = new Padding(4, 3, 4, 3);
        EditCandlesWithFlatPriceCheck.Name = "EditCandlesWithFlatPriceCheck";
        EditCandlesWithFlatPriceCheck.Size = new Size(213, 19);
        EditCandlesWithFlatPriceCheck.TabIndex = 202;
        EditCandlesWithFlatPriceCheck.Text = "Controleer het aantal platte candles";
        EditCandlesWithFlatPriceCheck.UseVisualStyleBackColor = true;
        // 
        // EditCandlesWithZeroVolumeCheck
        // 
        EditCandlesWithZeroVolumeCheck.AutoSize = true;
        EditCandlesWithZeroVolumeCheck.Location = new Point(284, 286);
        EditCandlesWithZeroVolumeCheck.Margin = new Padding(4, 3, 4, 3);
        EditCandlesWithZeroVolumeCheck.Name = "EditCandlesWithZeroVolumeCheck";
        EditCandlesWithZeroVolumeCheck.Size = new Size(262, 19);
        EditCandlesWithZeroVolumeCheck.TabIndex = 201;
        EditCandlesWithZeroVolumeCheck.Text = "Controleer het aantal candles zonder volume";
        EditCandlesWithZeroVolumeCheck.UseVisualStyleBackColor = true;
        // 
        // EditMinimumAboveBollingerBandsSmaCheck
        // 
        EditMinimumAboveBollingerBandsSmaCheck.AutoSize = true;
        EditMinimumAboveBollingerBandsSmaCheck.Location = new Point(284, 316);
        EditMinimumAboveBollingerBandsSmaCheck.Margin = new Padding(4, 3, 4, 3);
        EditMinimumAboveBollingerBandsSmaCheck.Name = "EditMinimumAboveBollingerBandsSmaCheck";
        EditMinimumAboveBollingerBandsSmaCheck.Size = new Size(211, 19);
        EditMinimumAboveBollingerBandsSmaCheck.TabIndex = 200;
        EditMinimumAboveBollingerBandsSmaCheck.Text = "Controleer aantal boven de bb.sma";
        EditMinimumAboveBollingerBandsSmaCheck.UseVisualStyleBackColor = true;
        // 
        // EditMinimumAboveBollingerBandsUpperCheck
        // 
        EditMinimumAboveBollingerBandsUpperCheck.AutoSize = true;
        EditMinimumAboveBollingerBandsUpperCheck.Location = new Point(284, 346);
        EditMinimumAboveBollingerBandsUpperCheck.Margin = new Padding(4, 3, 4, 3);
        EditMinimumAboveBollingerBandsUpperCheck.Name = "EditMinimumAboveBollingerBandsUpperCheck";
        EditMinimumAboveBollingerBandsUpperCheck.Size = new Size(220, 19);
        EditMinimumAboveBollingerBandsUpperCheck.TabIndex = 199;
        EditMinimumAboveBollingerBandsUpperCheck.Text = "Controleer aantal boven de bb.upper";
        EditMinimumAboveBollingerBandsUpperCheck.UseVisualStyleBackColor = true;
        // 
        // EditCandlesWithZeroVolume
        // 
        EditCandlesWithZeroVolume.Location = new Point(614, 282);
        EditCandlesWithZeroVolume.Margin = new Padding(4, 3, 4, 3);
        EditCandlesWithZeroVolume.Name = "EditCandlesWithZeroVolume";
        EditCandlesWithZeroVolume.Size = new Size(88, 23);
        EditCandlesWithZeroVolume.TabIndex = 197;
        EditCandlesWithZeroVolume.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // EditCandlesWithFlatPrice
        // 
        EditCandlesWithFlatPrice.Location = new Point(614, 253);
        EditCandlesWithFlatPrice.Margin = new Padding(4, 3, 4, 3);
        EditCandlesWithFlatPrice.Name = "EditCandlesWithFlatPrice";
        EditCandlesWithFlatPrice.Size = new Size(88, 23);
        EditCandlesWithFlatPrice.TabIndex = 195;
        EditCandlesWithFlatPrice.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // EditMinimumAboveBollingerBandsUpper
        // 
        EditMinimumAboveBollingerBandsUpper.Location = new Point(613, 342);
        EditMinimumAboveBollingerBandsUpper.Margin = new Padding(4, 3, 4, 3);
        EditMinimumAboveBollingerBandsUpper.Name = "EditMinimumAboveBollingerBandsUpper";
        EditMinimumAboveBollingerBandsUpper.Size = new Size(88, 23);
        EditMinimumAboveBollingerBandsUpper.TabIndex = 192;
        EditMinimumAboveBollingerBandsUpper.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // EditMinimumAboveBollingerBandsSma
        // 
        EditMinimumAboveBollingerBandsSma.Location = new Point(614, 312);
        EditMinimumAboveBollingerBandsSma.Margin = new Padding(4, 3, 4, 3);
        EditMinimumAboveBollingerBandsSma.Name = "EditMinimumAboveBollingerBandsSma";
        EditMinimumAboveBollingerBandsSma.Size = new Size(88, 23);
        EditMinimumAboveBollingerBandsSma.TabIndex = 189;
        EditMinimumAboveBollingerBandsSma.Value = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // label26
        // 
        label26.AutoSize = true;
        label26.Location = new Point(19, 25);
        label26.Margin = new Padding(4, 0, 4, 0);
        label26.Name = "label26";
        label26.Size = new Size(206, 15);
        label26.TabIndex = 185;
        label26.Text = "Create signals for the intervals and .....";
        // 
        // EditLogMinimumTickPercentage
        // 
        EditLogMinimumTickPercentage.AutoSize = true;
        EditLogMinimumTickPercentage.Location = new Point(568, 173);
        EditLogMinimumTickPercentage.Margin = new Padding(4, 3, 4, 3);
        EditLogMinimumTickPercentage.Name = "EditLogMinimumTickPercentage";
        EditLogMinimumTickPercentage.Size = new Size(165, 19);
        EditLogMinimumTickPercentage.TabIndex = 180;
        EditLogMinimumTickPercentage.Text = "Log als dit niet het geval is";
        EditLogMinimumTickPercentage.UseVisualStyleBackColor = true;
        // 
        // EditMinimumTickPercentage
        // 
        EditMinimumTickPercentage.DecimalPlaces = 2;
        EditMinimumTickPercentage.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
        EditMinimumTickPercentage.Location = new Point(455, 172);
        EditMinimumTickPercentage.Margin = new Padding(4, 3, 4, 3);
        EditMinimumTickPercentage.Name = "EditMinimumTickPercentage";
        EditMinimumTickPercentage.Size = new Size(75, 23);
        EditMinimumTickPercentage.TabIndex = 179;
        toolTip1.SetToolTip(EditMinimumTickPercentage, "Soms heb je van die munten die of een barcode streepjes patroon hebben of die per tick een enorme afstand overbruggen. Via deze instelling kun je die markeren in het overzicht");
        EditMinimumTickPercentage.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // label61
        // 
        label61.AutoSize = true;
        label61.Location = new Point(284, 175);
        label61.Margin = new Padding(4, 0, 4, 0);
        label61.Name = "label61";
        label61.Size = new Size(90, 15);
        label61.TabIndex = 178;
        label61.Text = "Tick percentage";
        // 
        // label53
        // 
        label53.AutoSize = true;
        label53.Location = new Point(287, 61);
        label53.Margin = new Padding(4, 0, 4, 0);
        label53.Name = "label53";
        label53.Size = new Size(82, 15);
        label53.TabIndex = 181;
        label53.Text = "24 uur change";
        // 
        // EditAnalysisMinChangePercentage
        // 
        EditAnalysisMinChangePercentage.Location = new Point(419, 59);
        EditAnalysisMinChangePercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMinChangePercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisMinChangePercentage.Name = "EditAnalysisMinChangePercentage";
        EditAnalysisMinChangePercentage.Size = new Size(57, 23);
        EditAnalysisMinChangePercentage.TabIndex = 182;
        toolTip1.SetToolTip(EditAnalysisMinChangePercentage, "Kunnen filteren op de 24 uur volume percentage.");
        // 
        // EditAnalysisMaxChangePercentage
        // 
        EditAnalysisMaxChangePercentage.Location = new Point(483, 59);
        EditAnalysisMaxChangePercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMaxChangePercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisMaxChangePercentage.Name = "EditAnalysisMaxChangePercentage";
        EditAnalysisMaxChangePercentage.Size = new Size(57, 23);
        EditAnalysisMaxChangePercentage.TabIndex = 183;
        toolTip1.SetToolTip(EditAnalysisMaxChangePercentage, "Kunnen filteren op de 24 uur volume percentage.");
        EditAnalysisMaxChangePercentage.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // EditLogBarometerToLow
        // 
        EditLogBarometerToLow.AutoSize = true;
        EditLogBarometerToLow.Location = new Point(568, 118);
        EditLogBarometerToLow.Margin = new Padding(4, 3, 4, 3);
        EditLogBarometerToLow.Name = "EditLogBarometerToLow";
        EditLogBarometerToLow.Size = new Size(142, 19);
        EditLogBarometerToLow.TabIndex = 174;
        EditLogBarometerToLow.Text = "Log te lage barometer";
        EditLogBarometerToLow.UseVisualStyleBackColor = true;
        // 
        // EditLogSymbolMustExistsDays
        // 
        EditLogSymbolMustExistsDays.AutoSize = true;
        EditLogSymbolMustExistsDays.Location = new Point(568, 144);
        EditLogSymbolMustExistsDays.Margin = new Padding(4, 3, 4, 3);
        EditLogSymbolMustExistsDays.Name = "EditLogSymbolMustExistsDays";
        EditLogSymbolMustExistsDays.Size = new Size(208, 19);
        EditLogSymbolMustExistsDays.TabIndex = 177;
        EditLogSymbolMustExistsDays.Text = "Log minimale dagen nieuwe munt";
        EditLogSymbolMustExistsDays.UseVisualStyleBackColor = true;
        // 
        // EditSymbolMustExistsDays
        // 
        EditSymbolMustExistsDays.Location = new Point(454, 144);
        EditSymbolMustExistsDays.Margin = new Padding(4, 3, 4, 3);
        EditSymbolMustExistsDays.Name = "EditSymbolMustExistsDays";
        EditSymbolMustExistsDays.Size = new Size(75, 23);
        EditSymbolMustExistsDays.TabIndex = 176;
        toolTip1.SetToolTip(EditSymbolMustExistsDays, "Negeer munten die korten dan x dagen bestaan");
        EditSymbolMustExistsDays.Value = new decimal(new int[] { 15, 0, 0, 0 });
        // 
        // label25
        // 
        label25.AutoSize = true;
        label25.Location = new Point(284, 148);
        label25.Margin = new Padding(4, 0, 4, 0);
        label25.Name = "label25";
        label25.Size = new Size(115, 15);
        label25.TabIndex = 175;
        label25.Text = "Nieuwe munt dagen";
        // 
        // label35
        // 
        label35.AutoSize = true;
        label35.Location = new Point(284, 120);
        label35.Margin = new Padding(4, 0, 4, 0);
        label35.Name = "label35";
        label35.Size = new Size(115, 15);
        label35.TabIndex = 172;
        label35.Text = "Minimale barometer";
        // 
        // EditBarometer1hMinimal
        // 
        EditBarometer1hMinimal.DecimalPlaces = 2;
        EditBarometer1hMinimal.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditBarometer1hMinimal.Location = new Point(455, 118);
        EditBarometer1hMinimal.Margin = new Padding(4, 3, 4, 3);
        EditBarometer1hMinimal.Name = "EditBarometer1hMinimal";
        EditBarometer1hMinimal.Size = new Size(74, 23);
        EditBarometer1hMinimal.TabIndex = 173;
        toolTip1.SetToolTip(EditBarometer1hMinimal, "Als de barometer laag staat krijg je enorm veel medlingen, negeer meldingen als de barometer onder dit getal staat");
        EditBarometer1hMinimal.Value = new decimal(new int[] { 25, 0, 0, 65536 });
        // 
        // EditLogAnalysisMinMaxChangePercentage
        // 
        EditLogAnalysisMinMaxChangePercentage.AutoSize = true;
        EditLogAnalysisMinMaxChangePercentage.Location = new Point(568, 60);
        EditLogAnalysisMinMaxChangePercentage.Margin = new Padding(4, 3, 4, 3);
        EditLogAnalysisMinMaxChangePercentage.Name = "EditLogAnalysisMinMaxChangePercentage";
        EditLogAnalysisMinMaxChangePercentage.Size = new Size(203, 19);
        EditLogAnalysisMinMaxChangePercentage.TabIndex = 184;
        EditLogAnalysisMinMaxChangePercentage.Text = "Log waarden buiten deze grenzen";
        EditLogAnalysisMinMaxChangePercentage.UseVisualStyleBackColor = true;
        // 
        // groupBoxInterval
        // 
        groupBoxInterval.Controls.Add(EditAnalyzeInterval6h);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval8h);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval12h);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval1d);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval5m);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval1m);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval2m);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval3m);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval10m);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval15m);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval30m);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval1h);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval2h);
        groupBoxInterval.Controls.Add(EditAnalyzeInterval4h);
        groupBoxInterval.Location = new Point(22, 60);
        groupBoxInterval.Margin = new Padding(4, 3, 4, 3);
        groupBoxInterval.Name = "groupBoxInterval";
        groupBoxInterval.Padding = new Padding(4, 3, 4, 3);
        groupBoxInterval.Size = new Size(224, 240);
        groupBoxInterval.TabIndex = 149;
        groupBoxInterval.TabStop = false;
        groupBoxInterval.Text = "Interval";
        // 
        // EditAnalyzeInterval6h
        // 
        EditAnalyzeInterval6h.AutoSize = true;
        EditAnalyzeInterval6h.Location = new Point(134, 112);
        EditAnalyzeInterval6h.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval6h.Name = "EditAnalyzeInterval6h";
        EditAnalyzeInterval6h.Size = new Size(53, 19);
        EditAnalyzeInterval6h.TabIndex = 115;
        EditAnalyzeInterval6h.Text = "6 uur";
        EditAnalyzeInterval6h.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval8h
        // 
        EditAnalyzeInterval8h.AutoSize = true;
        EditAnalyzeInterval8h.Location = new Point(134, 141);
        EditAnalyzeInterval8h.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval8h.Name = "EditAnalyzeInterval8h";
        EditAnalyzeInterval8h.Size = new Size(53, 19);
        EditAnalyzeInterval8h.TabIndex = 116;
        EditAnalyzeInterval8h.Text = "8 uur";
        EditAnalyzeInterval8h.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval12h
        // 
        EditAnalyzeInterval12h.AutoSize = true;
        EditAnalyzeInterval12h.Location = new Point(134, 168);
        EditAnalyzeInterval12h.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval12h.Name = "EditAnalyzeInterval12h";
        EditAnalyzeInterval12h.Size = new Size(59, 19);
        EditAnalyzeInterval12h.TabIndex = 117;
        EditAnalyzeInterval12h.Text = "12 uur";
        EditAnalyzeInterval12h.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval1d
        // 
        EditAnalyzeInterval1d.AutoSize = true;
        EditAnalyzeInterval1d.Location = new Point(134, 195);
        EditAnalyzeInterval1d.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval1d.Name = "EditAnalyzeInterval1d";
        EditAnalyzeInterval1d.Size = new Size(55, 19);
        EditAnalyzeInterval1d.TabIndex = 118;
        EditAnalyzeInterval1d.Text = "1 dag";
        EditAnalyzeInterval1d.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval5m
        // 
        EditAnalyzeInterval5m.AutoSize = true;
        EditAnalyzeInterval5m.Location = new Point(20, 114);
        EditAnalyzeInterval5m.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval5m.Name = "EditAnalyzeInterval5m";
        EditAnalyzeInterval5m.Size = new Size(56, 19);
        EditAnalyzeInterval5m.TabIndex = 108;
        EditAnalyzeInterval5m.Text = "5 min";
        EditAnalyzeInterval5m.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval1m
        // 
        EditAnalyzeInterval1m.AutoSize = true;
        EditAnalyzeInterval1m.Location = new Point(20, 31);
        EditAnalyzeInterval1m.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval1m.Name = "EditAnalyzeInterval1m";
        EditAnalyzeInterval1m.Size = new Size(56, 19);
        EditAnalyzeInterval1m.TabIndex = 105;
        EditAnalyzeInterval1m.Text = "1 min";
        EditAnalyzeInterval1m.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval2m
        // 
        EditAnalyzeInterval2m.AutoSize = true;
        EditAnalyzeInterval2m.Location = new Point(20, 61);
        EditAnalyzeInterval2m.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval2m.Name = "EditAnalyzeInterval2m";
        EditAnalyzeInterval2m.Size = new Size(56, 19);
        EditAnalyzeInterval2m.TabIndex = 106;
        EditAnalyzeInterval2m.Text = "2 min";
        EditAnalyzeInterval2m.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval3m
        // 
        EditAnalyzeInterval3m.AutoSize = true;
        EditAnalyzeInterval3m.Location = new Point(20, 88);
        EditAnalyzeInterval3m.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval3m.Name = "EditAnalyzeInterval3m";
        EditAnalyzeInterval3m.Size = new Size(56, 19);
        EditAnalyzeInterval3m.TabIndex = 107;
        EditAnalyzeInterval3m.Text = "3 min";
        EditAnalyzeInterval3m.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval10m
        // 
        EditAnalyzeInterval10m.AutoSize = true;
        EditAnalyzeInterval10m.Location = new Point(20, 141);
        EditAnalyzeInterval10m.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval10m.Name = "EditAnalyzeInterval10m";
        EditAnalyzeInterval10m.Size = new Size(62, 19);
        EditAnalyzeInterval10m.TabIndex = 109;
        EditAnalyzeInterval10m.Text = "10 min";
        EditAnalyzeInterval10m.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval15m
        // 
        EditAnalyzeInterval15m.AutoSize = true;
        EditAnalyzeInterval15m.Location = new Point(20, 167);
        EditAnalyzeInterval15m.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval15m.Name = "EditAnalyzeInterval15m";
        EditAnalyzeInterval15m.Size = new Size(62, 19);
        EditAnalyzeInterval15m.TabIndex = 110;
        EditAnalyzeInterval15m.Text = "15 min";
        EditAnalyzeInterval15m.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval30m
        // 
        EditAnalyzeInterval30m.AutoSize = true;
        EditAnalyzeInterval30m.Location = new Point(20, 194);
        EditAnalyzeInterval30m.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval30m.Name = "EditAnalyzeInterval30m";
        EditAnalyzeInterval30m.Size = new Size(62, 19);
        EditAnalyzeInterval30m.TabIndex = 111;
        EditAnalyzeInterval30m.Text = "30 min";
        EditAnalyzeInterval30m.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval1h
        // 
        EditAnalyzeInterval1h.AutoSize = true;
        EditAnalyzeInterval1h.Location = new Point(134, 31);
        EditAnalyzeInterval1h.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval1h.Name = "EditAnalyzeInterval1h";
        EditAnalyzeInterval1h.Size = new Size(53, 19);
        EditAnalyzeInterval1h.TabIndex = 112;
        EditAnalyzeInterval1h.Text = "1 uur";
        EditAnalyzeInterval1h.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval2h
        // 
        EditAnalyzeInterval2h.AutoSize = true;
        EditAnalyzeInterval2h.Location = new Point(134, 59);
        EditAnalyzeInterval2h.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval2h.Name = "EditAnalyzeInterval2h";
        EditAnalyzeInterval2h.Size = new Size(53, 19);
        EditAnalyzeInterval2h.TabIndex = 113;
        EditAnalyzeInterval2h.Text = "2 uur";
        EditAnalyzeInterval2h.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeInterval4h
        // 
        EditAnalyzeInterval4h.AutoSize = true;
        EditAnalyzeInterval4h.Location = new Point(134, 85);
        EditAnalyzeInterval4h.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval4h.Name = "EditAnalyzeInterval4h";
        EditAnalyzeInterval4h.Size = new Size(53, 19);
        EditAnalyzeInterval4h.TabIndex = 114;
        EditAnalyzeInterval4h.Text = "4 uur";
        EditAnalyzeInterval4h.UseVisualStyleBackColor = true;
        // 
        // tabSignalStobb
        // 
        tabSignalStobb.Controls.Add(label66);
        tabSignalStobb.Controls.Add(EditStobMinimalTrend);
        tabSignalStobb.Controls.Add(label77);
        tabSignalStobb.Controls.Add(label75);
        tabSignalStobb.Controls.Add(EditStobIncludeSbmPercAndCrossing);
        tabSignalStobb.Controls.Add(label30);
        tabSignalStobb.Controls.Add(label28);
        tabSignalStobb.Controls.Add(buttonColorStobb);
        tabSignalStobb.Controls.Add(EditStobIncludeSbmMaLines);
        tabSignalStobb.Controls.Add(EditStobIncludeRsi);
        tabSignalStobb.Controls.Add(buttonPlaySoundStobbOversold);
        tabSignalStobb.Controls.Add(buttonPlaySoundStobbOverbought);
        tabSignalStobb.Controls.Add(buttonSelectSoundStobbOversold);
        tabSignalStobb.Controls.Add(panelColorStobb);
        tabSignalStobb.Controls.Add(EditSoundStobbOversold);
        tabSignalStobb.Controls.Add(EditSoundStobbOverbought);
        tabSignalStobb.Controls.Add(buttonSelectSoundStobbOverbought);
        tabSignalStobb.Controls.Add(EditPlaySpeechStobbSignal);
        tabSignalStobb.Controls.Add(EditPlaySoundStobbSignal);
        tabSignalStobb.Controls.Add(EditAnalyzeStobbOversold);
        tabSignalStobb.Controls.Add(EditAnalyzeStobbOverbought);
        tabSignalStobb.Controls.Add(EditStobbUseLowHigh);
        tabSignalStobb.Controls.Add(label1);
        tabSignalStobb.Controls.Add(EditStobbBBMinPercentage);
        tabSignalStobb.Controls.Add(EditStobbBBMaxPercentage);
        tabSignalStobb.Location = new Point(4, 27);
        tabSignalStobb.Margin = new Padding(4, 3, 4, 3);
        tabSignalStobb.Name = "tabSignalStobb";
        tabSignalStobb.Padding = new Padding(4, 3, 4, 3);
        tabSignalStobb.Size = new Size(1232, 777);
        tabSignalStobb.TabIndex = 1;
        tabSignalStobb.Text = "STOBB";
        tabSignalStobb.UseVisualStyleBackColor = true;
        // 
        // label66
        // 
        label66.AutoSize = true;
        label66.Location = new Point(19, 475);
        label66.Margin = new Padding(4, 0, 4, 0);
        label66.Name = "label66";
        label66.Size = new Size(88, 15);
        label66.TabIndex = 152;
        label66.Text = "Minimale trend";
        // 
        // EditStobMinimalTrend
        // 
        EditStobMinimalTrend.DecimalPlaces = 2;
        EditStobMinimalTrend.Location = new Point(140, 473);
        EditStobMinimalTrend.Margin = new Padding(4, 3, 4, 3);
        EditStobMinimalTrend.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditStobMinimalTrend.Minimum = new decimal(new int[] { 1000, 0, 0, int.MinValue });
        EditStobMinimalTrend.Name = "EditStobMinimalTrend";
        EditStobMinimalTrend.Size = new Size(65, 23);
        EditStobMinimalTrend.TabIndex = 153;
        EditStobMinimalTrend.Value = new decimal(new int[] { 150, 0, 0, 131072 });
        // 
        // label77
        // 
        label77.AutoSize = true;
        label77.Location = new Point(19, 221);
        label77.Margin = new Padding(4, 0, 4, 0);
        label77.Name = "label77";
        label77.Size = new Size(41, 15);
        label77.TabIndex = 151;
        label77.Text = "STOBB";
        // 
        // label75
        // 
        label75.AutoSize = true;
        label75.Location = new Point(19, 316);
        label75.Margin = new Padding(4, 0, 4, 0);
        label75.Name = "label75";
        label75.Size = new Size(68, 15);
        label75.TabIndex = 121;
        label75.Text = "Instellingen";
        // 
        // EditStobIncludeSbmPercAndCrossing
        // 
        EditStobIncludeSbmPercAndCrossing.AutoSize = true;
        EditStobIncludeSbmPercAndCrossing.Location = new Point(19, 443);
        EditStobIncludeSbmPercAndCrossing.Margin = new Padding(4, 3, 4, 3);
        EditStobIncludeSbmPercAndCrossing.Name = "EditStobIncludeSbmPercAndCrossing";
        EditStobIncludeSbmPercAndCrossing.Size = new Size(252, 19);
        EditStobIncludeSbmPercAndCrossing.TabIndex = 120;
        EditStobIncludeSbmPercAndCrossing.Text = "Met SBM condities percentages/kruisingen";
        EditStobIncludeSbmPercAndCrossing.UseVisualStyleBackColor = true;
        // 
        // label30
        // 
        label30.AutoSize = true;
        label30.Location = new Point(19, 170);
        label30.Margin = new Padding(4, 0, 4, 0);
        label30.Name = "label30";
        label30.Size = new Size(86, 15);
        label30.TabIndex = 119;
        label30.Text = "Long soundfile";
        // 
        // label28
        // 
        label28.AutoSize = true;
        label28.Location = new Point(19, 144);
        label28.Margin = new Padding(4, 0, 4, 0);
        label28.Name = "label28";
        label28.Size = new Size(87, 15);
        label28.TabIndex = 118;
        label28.Text = "Short soundfile";
        // 
        // buttonColorStobb
        // 
        buttonColorStobb.Location = new Point(127, 47);
        buttonColorStobb.Margin = new Padding(4, 3, 4, 3);
        buttonColorStobb.Name = "buttonColorStobb";
        buttonColorStobb.Size = new Size(88, 27);
        buttonColorStobb.TabIndex = 115;
        buttonColorStobb.Text = "Achtergrond";
        buttonColorStobb.UseVisualStyleBackColor = true;
        // 
        // EditStobIncludeSbmMaLines
        // 
        EditStobIncludeSbmMaLines.AutoSize = true;
        EditStobIncludeSbmMaLines.Location = new Point(19, 418);
        EditStobIncludeSbmMaLines.Margin = new Padding(4, 3, 4, 3);
        EditStobIncludeSbmMaLines.Name = "EditStobIncludeSbmMaLines";
        EditStobIncludeSbmMaLines.Size = new Size(181, 19);
        EditStobIncludeSbmMaLines.TabIndex = 114;
        EditStobIncludeSbmMaLines.Text = "Met SBM condities MA-lijnen";
        EditStobIncludeSbmMaLines.UseVisualStyleBackColor = true;
        // 
        // EditStobIncludeRsi
        // 
        EditStobIncludeRsi.AutoSize = true;
        EditStobIncludeRsi.Location = new Point(19, 393);
        EditStobIncludeRsi.Margin = new Padding(4, 3, 4, 3);
        EditStobIncludeRsi.Name = "EditStobIncludeRsi";
        EditStobIncludeRsi.Size = new Size(232, 19);
        EditStobIncludeRsi.TabIndex = 113;
        EditStobIncludeRsi.Text = "Met RSI oversold/overbought condities";
        EditStobIncludeRsi.UseVisualStyleBackColor = true;
        // 
        // buttonPlaySoundStobbOversold
        // 
        buttonPlaySoundStobbOversold.Image = Properties.Resources.volume;
        buttonPlaySoundStobbOversold.Location = new Point(421, 170);
        buttonPlaySoundStobbOversold.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySoundStobbOversold.Name = "buttonPlaySoundStobbOversold";
        buttonPlaySoundStobbOversold.Size = new Size(23, 23);
        buttonPlaySoundStobbOversold.TabIndex = 112;
        buttonPlaySoundStobbOversold.UseVisualStyleBackColor = true;
        // 
        // buttonPlaySoundStobbOverbought
        // 
        buttonPlaySoundStobbOverbought.Image = Properties.Resources.volume;
        buttonPlaySoundStobbOverbought.Location = new Point(421, 142);
        buttonPlaySoundStobbOverbought.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySoundStobbOverbought.Name = "buttonPlaySoundStobbOverbought";
        buttonPlaySoundStobbOverbought.Size = new Size(23, 23);
        buttonPlaySoundStobbOverbought.TabIndex = 111;
        buttonPlaySoundStobbOverbought.UseVisualStyleBackColor = true;
        // 
        // buttonSelectSoundStobbOversold
        // 
        buttonSelectSoundStobbOversold.Location = new Point(391, 170);
        buttonSelectSoundStobbOversold.Margin = new Padding(4, 3, 4, 3);
        buttonSelectSoundStobbOversold.Name = "buttonSelectSoundStobbOversold";
        buttonSelectSoundStobbOversold.Size = new Size(23, 23);
        buttonSelectSoundStobbOversold.TabIndex = 110;
        buttonSelectSoundStobbOversold.UseVisualStyleBackColor = true;
        // 
        // panelColorStobb
        // 
        panelColorStobb.BackColor = Color.Transparent;
        panelColorStobb.BorderStyle = BorderStyle.FixedSingle;
        panelColorStobb.Location = new Point(19, 50);
        panelColorStobb.Margin = new Padding(4, 3, 4, 3);
        panelColorStobb.Name = "panelColorStobb";
        panelColorStobb.Size = new Size(70, 22);
        panelColorStobb.TabIndex = 116;
        // 
        // EditSoundStobbOversold
        // 
        EditSoundStobbOversold.Location = new Point(156, 170);
        EditSoundStobbOversold.Margin = new Padding(4, 3, 4, 3);
        EditSoundStobbOversold.Name = "EditSoundStobbOversold";
        EditSoundStobbOversold.Size = new Size(227, 23);
        EditSoundStobbOversold.TabIndex = 109;
        // 
        // EditSoundStobbOverbought
        // 
        EditSoundStobbOverbought.Location = new Point(156, 141);
        EditSoundStobbOverbought.Margin = new Padding(4, 3, 4, 3);
        EditSoundStobbOverbought.Name = "EditSoundStobbOverbought";
        EditSoundStobbOverbought.Size = new Size(227, 23);
        EditSoundStobbOverbought.TabIndex = 106;
        // 
        // buttonSelectSoundStobbOverbought
        // 
        buttonSelectSoundStobbOverbought.Location = new Point(391, 142);
        buttonSelectSoundStobbOverbought.Margin = new Padding(4, 3, 4, 3);
        buttonSelectSoundStobbOverbought.Name = "buttonSelectSoundStobbOverbought";
        buttonSelectSoundStobbOverbought.Size = new Size(23, 23);
        buttonSelectSoundStobbOverbought.TabIndex = 107;
        buttonSelectSoundStobbOverbought.Text = "...";
        buttonSelectSoundStobbOverbought.UseVisualStyleBackColor = true;
        // 
        // EditPlaySpeechStobbSignal
        // 
        EditPlaySpeechStobbSignal.AutoSize = true;
        EditPlaySpeechStobbSignal.Location = new Point(19, 110);
        EditPlaySpeechStobbSignal.Margin = new Padding(4, 3, 4, 3);
        EditPlaySpeechStobbSignal.Name = "EditPlaySpeechStobbSignal";
        EditPlaySpeechStobbSignal.Size = new Size(88, 19);
        EditPlaySpeechStobbSignal.TabIndex = 104;
        EditPlaySpeechStobbSignal.Text = "Play speech";
        EditPlaySpeechStobbSignal.UseVisualStyleBackColor = true;
        // 
        // EditPlaySoundStobbSignal
        // 
        EditPlaySoundStobbSignal.AutoSize = true;
        EditPlaySoundStobbSignal.Location = new Point(19, 83);
        EditPlaySoundStobbSignal.Margin = new Padding(4, 3, 4, 3);
        EditPlaySoundStobbSignal.Name = "EditPlaySoundStobbSignal";
        EditPlaySoundStobbSignal.Size = new Size(84, 19);
        EditPlaySoundStobbSignal.TabIndex = 103;
        EditPlaySoundStobbSignal.Text = "Play sound";
        EditPlaySoundStobbSignal.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeStobbOversold
        // 
        EditAnalyzeStobbOversold.AutoSize = true;
        EditAnalyzeStobbOversold.Location = new Point(19, 272);
        EditAnalyzeStobbOversold.Margin = new Padding(2);
        EditAnalyzeStobbOversold.Name = "EditAnalyzeStobbOversold";
        EditAnalyzeStobbOversold.Size = new Size(151, 19);
        EditAnalyzeStobbOversold.TabIndex = 108;
        EditAnalyzeStobbOversold.Text = "Maak aankoop signalen";
        EditAnalyzeStobbOversold.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeStobbOverbought
        // 
        EditAnalyzeStobbOverbought.AutoSize = true;
        EditAnalyzeStobbOverbought.Location = new Point(19, 243);
        EditAnalyzeStobbOverbought.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeStobbOverbought.Name = "EditAnalyzeStobbOverbought";
        EditAnalyzeStobbOverbought.Size = new Size(148, 19);
        EditAnalyzeStobbOverbought.TabIndex = 105;
        EditAnalyzeStobbOverbought.Text = "Maak verkoop signalen";
        EditAnalyzeStobbOverbought.UseVisualStyleBackColor = true;
        // 
        // EditStobbUseLowHigh
        // 
        EditStobbUseLowHigh.AutoSize = true;
        EditStobbUseLowHigh.Location = new Point(19, 368);
        EditStobbUseLowHigh.Margin = new Padding(4, 3, 4, 3);
        EditStobbUseLowHigh.Name = "EditStobbUseLowHigh";
        EditStobbUseLowHigh.Size = new Size(398, 19);
        EditStobbUseLowHigh.TabIndex = 98;
        EditStobbUseLowHigh.Text = "Bereken de BB oversold/overbought via de low/high ipv de open/close";
        EditStobbUseLowHigh.UseVisualStyleBackColor = true;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(19, 340);
        label1.Margin = new Padding(4, 0, 4, 0);
        label1.Name = "label1";
        label1.Size = new Size(77, 15);
        label1.TabIndex = 42;
        label1.Text = "Filter on BB%";
        // 
        // EditStobbBBMinPercentage
        // 
        EditStobbBBMinPercentage.DecimalPlaces = 2;
        EditStobbBBMinPercentage.Location = new Point(140, 338);
        EditStobbBBMinPercentage.Margin = new Padding(4, 3, 4, 3);
        EditStobbBBMinPercentage.Name = "EditStobbBBMinPercentage";
        EditStobbBBMinPercentage.Size = new Size(65, 23);
        EditStobbBBMinPercentage.TabIndex = 43;
        toolTip1.SetToolTip(EditStobbBBMinPercentage, "Een BB heeft een bepaalde breedte, je kunt hier filteren waardoor op de minimale en maximale breedte kan worden gefilterd.");
        EditStobbBBMinPercentage.Value = new decimal(new int[] { 150, 0, 0, 131072 });
        // 
        // EditStobbBBMaxPercentage
        // 
        EditStobbBBMaxPercentage.DecimalPlaces = 2;
        EditStobbBBMaxPercentage.Location = new Point(225, 338);
        EditStobbBBMaxPercentage.Margin = new Padding(4, 3, 4, 3);
        EditStobbBBMaxPercentage.Name = "EditStobbBBMaxPercentage";
        EditStobbBBMaxPercentage.Size = new Size(65, 23);
        EditStobbBBMaxPercentage.TabIndex = 44;
        toolTip1.SetToolTip(EditStobbBBMaxPercentage, "Een BB heeft een bepaalde breedte, je kunt hier filteren waardoor op de minimale en maximale breedte kan worden gefilterd.");
        EditStobbBBMaxPercentage.Value = new decimal(new int[] { 6, 0, 0, 0 });
        // 
        // tabSignalSbm
        // 
        tabSignalSbm.Controls.Add(EditSbm2UseLowHigh);
        tabSignalSbm.Controls.Add(label21);
        tabSignalSbm.Controls.Add(label20);
        tabSignalSbm.Controls.Add(label9);
        tabSignalSbm.Controls.Add(label41);
        tabSignalSbm.Controls.Add(EditSbm1CandlesLookbackCount);
        tabSignalSbm.Controls.Add(label39);
        tabSignalSbm.Controls.Add(EditSbmCandlesForMacdRecovery);
        tabSignalSbm.Controls.Add(label31);
        tabSignalSbm.Controls.Add(label32);
        tabSignalSbm.Controls.Add(buttonPlaySoundSbmOversold);
        tabSignalSbm.Controls.Add(buttonPlaySoundSbmOverbought);
        tabSignalSbm.Controls.Add(buttonSelectSoundSbmOversold);
        tabSignalSbm.Controls.Add(EditSoundFileSbmOversold);
        tabSignalSbm.Controls.Add(EditSoundFileSbmOverbought);
        tabSignalSbm.Controls.Add(buttonSelectSoundSbmOverbought);
        tabSignalSbm.Controls.Add(EditPlaySpeechSbmSignal);
        tabSignalSbm.Controls.Add(EditAnalyzeSbmOversold);
        tabSignalSbm.Controls.Add(EditAnalyzeSbmOverbought);
        tabSignalSbm.Controls.Add(EditPlaySoundSbmSignal);
        tabSignalSbm.Controls.Add(buttonColorSbm);
        tabSignalSbm.Controls.Add(EditSbmUseLowHigh);
        tabSignalSbm.Controls.Add(label17);
        tabSignalSbm.Controls.Add(EditSbmBBMinPercentage);
        tabSignalSbm.Controls.Add(EditSbmBBMaxPercentage);
        tabSignalSbm.Controls.Add(label22);
        tabSignalSbm.Controls.Add(label4);
        tabSignalSbm.Controls.Add(EditSbmMa200AndMa20Percentage);
        tabSignalSbm.Controls.Add(label8);
        tabSignalSbm.Controls.Add(EditSbmMa50AndMa20Percentage);
        tabSignalSbm.Controls.Add(label7);
        tabSignalSbm.Controls.Add(EditSbmMa200AndMa50Percentage);
        tabSignalSbm.Controls.Add(EditSbmMa50AndMa20Lookback);
        tabSignalSbm.Controls.Add(EditSbmMa50AndMa20Crossing);
        tabSignalSbm.Controls.Add(EditSbmMa200AndMa50Lookback);
        tabSignalSbm.Controls.Add(EditSbmMa200AndMa50Crossing);
        tabSignalSbm.Controls.Add(EditSbmMa200AndMa20Lookback);
        tabSignalSbm.Controls.Add(EditSbmMa200AndMa20Crossing);
        tabSignalSbm.Controls.Add(EditAnalyzeSbm3Overbought);
        tabSignalSbm.Controls.Add(EditAnalyzeSbm2Overbought);
        tabSignalSbm.Controls.Add(label12);
        tabSignalSbm.Controls.Add(EditSbm2BbPercentage);
        tabSignalSbm.Controls.Add(panelColorSbm);
        tabSignalSbm.Controls.Add(label13);
        tabSignalSbm.Controls.Add(EditSbm3CandlesForBBRecovery);
        tabSignalSbm.Controls.Add(label14);
        tabSignalSbm.Controls.Add(EditSbm3CandlesForBBRecoveryPercentage);
        tabSignalSbm.Controls.Add(label11);
        tabSignalSbm.Controls.Add(EditSbm2CandlesLookbackCount);
        tabSignalSbm.Controls.Add(EditAnalyzeSbm3Oversold);
        tabSignalSbm.Controls.Add(EditAnalyzeSbm2Oversold);
        tabSignalSbm.Location = new Point(4, 27);
        tabSignalSbm.Margin = new Padding(4, 3, 4, 3);
        tabSignalSbm.Name = "tabSignalSbm";
        tabSignalSbm.Padding = new Padding(4, 3, 4, 3);
        tabSignalSbm.Size = new Size(1232, 777);
        tabSignalSbm.TabIndex = 5;
        tabSignalSbm.Text = "SBM";
        tabSignalSbm.UseVisualStyleBackColor = true;
        // 
        // EditSbm2UseLowHigh
        // 
        EditSbm2UseLowHigh.AutoSize = true;
        EditSbm2UseLowHigh.Location = new Point(23, 415);
        EditSbm2UseLowHigh.Margin = new Padding(4, 3, 4, 3);
        EditSbm2UseLowHigh.Name = "EditSbm2UseLowHigh";
        EditSbm2UseLowHigh.Size = new Size(281, 19);
        EditSbm2UseLowHigh.TabIndex = 152;
        EditSbm2UseLowHigh.Text = "Gebruik daarvoor de high/low ipv de open/close";
        EditSbm2UseLowHigh.UseVisualStyleBackColor = true;
        // 
        // label21
        // 
        label21.AutoSize = true;
        label21.Location = new Point(19, 487);
        label21.Margin = new Padding(4, 0, 4, 0);
        label21.Name = "label21";
        label21.Size = new Size(37, 15);
        label21.TabIndex = 151;
        label21.Text = "SBM3";
        // 
        // label20
        // 
        label20.AutoSize = true;
        label20.Location = new Point(19, 316);
        label20.Margin = new Padding(4, 0, 4, 0);
        label20.Name = "label20";
        label20.Size = new Size(37, 15);
        label20.TabIndex = 150;
        label20.Text = "SBM2";
        // 
        // label9
        // 
        label9.AutoSize = true;
        label9.Location = new Point(19, 178);
        label9.Margin = new Padding(4, 0, 4, 0);
        label9.Name = "label9";
        label9.Size = new Size(37, 15);
        label9.TabIndex = 149;
        label9.Text = "SBM1";
        // 
        // label41
        // 
        label41.AutoSize = true;
        label41.Location = new Point(19, 251);
        label41.Margin = new Padding(4, 0, 4, 0);
        label41.Name = "label41";
        label41.Size = new Size(95, 15);
        label41.TabIndex = 146;
        label41.Text = "Candle lookback";
        // 
        // EditSbm1CandlesLookbackCount
        // 
        EditSbm1CandlesLookbackCount.Location = new Point(274, 248);
        EditSbm1CandlesLookbackCount.Margin = new Padding(4, 3, 4, 3);
        EditSbm1CandlesLookbackCount.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbm1CandlesLookbackCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbm1CandlesLookbackCount.Name = "EditSbm1CandlesLookbackCount";
        EditSbm1CandlesLookbackCount.Size = new Size(57, 23);
        EditSbm1CandlesLookbackCount.TabIndex = 147;
        EditSbm1CandlesLookbackCount.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // label39
        // 
        label39.AutoSize = true;
        label39.Location = new Point(612, 244);
        label39.Margin = new Padding(4, 0, 4, 0);
        label39.Name = "label39";
        label39.Size = new Size(180, 15);
        label39.TabIndex = 141;
        label39.Text = "Het aantal MACD herstel candles";
        // 
        // EditSbmCandlesForMacdRecovery
        // 
        EditSbmCandlesForMacdRecovery.Location = new Point(1052, 238);
        EditSbmCandlesForMacdRecovery.Margin = new Padding(4, 3, 4, 3);
        EditSbmCandlesForMacdRecovery.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        EditSbmCandlesForMacdRecovery.Name = "EditSbmCandlesForMacdRecovery";
        EditSbmCandlesForMacdRecovery.Size = new Size(57, 23);
        EditSbmCandlesForMacdRecovery.TabIndex = 142;
        EditSbmCandlesForMacdRecovery.Value = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // label31
        // 
        label31.AutoSize = true;
        label31.Location = new Point(19, 134);
        label31.Margin = new Padding(4, 0, 4, 0);
        label31.Name = "label31";
        label31.Size = new Size(106, 15);
        label31.TabIndex = 135;
        label31.Text = "Oversold soundfile";
        // 
        // label32
        // 
        label32.AutoSize = true;
        label32.Location = new Point(19, 109);
        label32.Margin = new Padding(4, 0, 4, 0);
        label32.Name = "label32";
        label32.Size = new Size(123, 15);
        label32.TabIndex = 134;
        label32.Text = "Overbought soundfile";
        // 
        // buttonPlaySoundSbmOversold
        // 
        buttonPlaySoundSbmOversold.Image = Properties.Resources.volume;
        buttonPlaySoundSbmOversold.Location = new Point(474, 129);
        buttonPlaySoundSbmOversold.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySoundSbmOversold.Name = "buttonPlaySoundSbmOversold";
        buttonPlaySoundSbmOversold.Size = new Size(23, 23);
        buttonPlaySoundSbmOversold.TabIndex = 132;
        buttonPlaySoundSbmOversold.UseVisualStyleBackColor = true;
        // 
        // buttonPlaySoundSbmOverbought
        // 
        buttonPlaySoundSbmOverbought.Image = Properties.Resources.volume;
        buttonPlaySoundSbmOverbought.Location = new Point(474, 106);
        buttonPlaySoundSbmOverbought.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySoundSbmOverbought.Name = "buttonPlaySoundSbmOverbought";
        buttonPlaySoundSbmOverbought.Size = new Size(23, 23);
        buttonPlaySoundSbmOverbought.TabIndex = 133;
        buttonPlaySoundSbmOverbought.UseVisualStyleBackColor = true;
        // 
        // buttonSelectSoundSbmOversold
        // 
        buttonSelectSoundSbmOversold.Location = new Point(443, 129);
        buttonSelectSoundSbmOversold.Margin = new Padding(4, 3, 4, 3);
        buttonSelectSoundSbmOversold.Name = "buttonSelectSoundSbmOversold";
        buttonSelectSoundSbmOversold.Size = new Size(23, 23);
        buttonSelectSoundSbmOversold.TabIndex = 124;
        buttonSelectSoundSbmOversold.UseVisualStyleBackColor = true;
        // 
        // EditSoundFileSbmOversold
        // 
        EditSoundFileSbmOversold.Location = new Point(209, 129);
        EditSoundFileSbmOversold.Margin = new Padding(4, 3, 4, 3);
        EditSoundFileSbmOversold.Name = "EditSoundFileSbmOversold";
        EditSoundFileSbmOversold.Size = new Size(227, 23);
        EditSoundFileSbmOversold.TabIndex = 131;
        // 
        // EditSoundFileSbmOverbought
        // 
        EditSoundFileSbmOverbought.Location = new Point(209, 103);
        EditSoundFileSbmOverbought.Margin = new Padding(4, 3, 4, 3);
        EditSoundFileSbmOverbought.Name = "EditSoundFileSbmOverbought";
        EditSoundFileSbmOverbought.Size = new Size(227, 23);
        EditSoundFileSbmOverbought.TabIndex = 128;
        // 
        // buttonSelectSoundSbmOverbought
        // 
        buttonSelectSoundSbmOverbought.Location = new Point(443, 104);
        buttonSelectSoundSbmOverbought.Margin = new Padding(4, 3, 4, 3);
        buttonSelectSoundSbmOverbought.Name = "buttonSelectSoundSbmOverbought";
        buttonSelectSoundSbmOverbought.Size = new Size(23, 23);
        buttonSelectSoundSbmOverbought.TabIndex = 129;
        buttonSelectSoundSbmOverbought.UseVisualStyleBackColor = true;
        // 
        // EditPlaySpeechSbmSignal
        // 
        EditPlaySpeechSbmSignal.AutoSize = true;
        EditPlaySpeechSbmSignal.Location = new Point(19, 78);
        EditPlaySpeechSbmSignal.Margin = new Padding(4, 3, 4, 3);
        EditPlaySpeechSbmSignal.Name = "EditPlaySpeechSbmSignal";
        EditPlaySpeechSbmSignal.Size = new Size(88, 19);
        EditPlaySpeechSbmSignal.TabIndex = 126;
        EditPlaySpeechSbmSignal.Text = "Play speech";
        EditPlaySpeechSbmSignal.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeSbmOversold
        // 
        EditAnalyzeSbmOversold.AutoSize = true;
        EditAnalyzeSbmOversold.Location = new Point(19, 225);
        EditAnalyzeSbmOversold.Margin = new Padding(2);
        EditAnalyzeSbmOversold.Name = "EditAnalyzeSbmOversold";
        EditAnalyzeSbmOversold.Size = new Size(151, 19);
        EditAnalyzeSbmOversold.TabIndex = 130;
        EditAnalyzeSbmOversold.Text = "Maak aankoop signalen";
        EditAnalyzeSbmOversold.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeSbmOverbought
        // 
        EditAnalyzeSbmOverbought.AutoSize = true;
        EditAnalyzeSbmOverbought.Location = new Point(19, 200);
        EditAnalyzeSbmOverbought.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeSbmOverbought.Name = "EditAnalyzeSbmOverbought";
        EditAnalyzeSbmOverbought.Size = new Size(148, 19);
        EditAnalyzeSbmOverbought.TabIndex = 127;
        EditAnalyzeSbmOverbought.Text = "Maak verkoop signalen";
        EditAnalyzeSbmOverbought.UseVisualStyleBackColor = true;
        // 
        // EditPlaySoundSbmSignal
        // 
        EditPlaySoundSbmSignal.AutoSize = true;
        EditPlaySoundSbmSignal.Location = new Point(19, 51);
        EditPlaySoundSbmSignal.Margin = new Padding(4, 3, 4, 3);
        EditPlaySoundSbmSignal.Name = "EditPlaySoundSbmSignal";
        EditPlaySoundSbmSignal.Size = new Size(84, 19);
        EditPlaySoundSbmSignal.TabIndex = 125;
        EditPlaySoundSbmSignal.Text = "Play sound";
        EditPlaySoundSbmSignal.UseVisualStyleBackColor = true;
        // 
        // buttonColorSbm
        // 
        buttonColorSbm.Location = new Point(127, 13);
        buttonColorSbm.Margin = new Padding(4, 3, 4, 3);
        buttonColorSbm.Name = "buttonColorSbm";
        buttonColorSbm.Size = new Size(88, 27);
        buttonColorSbm.TabIndex = 121;
        buttonColorSbm.Text = "Achtergrond";
        buttonColorSbm.UseVisualStyleBackColor = true;
        // 
        // EditSbmUseLowHigh
        // 
        EditSbmUseLowHigh.AutoSize = true;
        EditSbmUseLowHigh.Location = new Point(19, 274);
        EditSbmUseLowHigh.Margin = new Padding(4, 3, 4, 3);
        EditSbmUseLowHigh.Name = "EditSbmUseLowHigh";
        EditSbmUseLowHigh.Size = new Size(265, 19);
        EditSbmUseLowHigh.TabIndex = 117;
        EditSbmUseLowHigh.Text = "Gebruik daarvoor de low/high ipv open/close";
        EditSbmUseLowHigh.UseVisualStyleBackColor = true;
        // 
        // label17
        // 
        label17.AutoSize = true;
        label17.Location = new Point(611, 208);
        label17.Margin = new Padding(4, 0, 4, 0);
        label17.Name = "label17";
        label17.Size = new Size(77, 15);
        label17.TabIndex = 113;
        label17.Text = "Filter on BB%";
        // 
        // EditSbmBBMinPercentage
        // 
        EditSbmBBMinPercentage.DecimalPlaces = 2;
        EditSbmBBMinPercentage.Location = new Point(725, 205);
        EditSbmBBMinPercentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmBBMinPercentage.Name = "EditSbmBBMinPercentage";
        EditSbmBBMinPercentage.Size = new Size(65, 23);
        EditSbmBBMinPercentage.TabIndex = 114;
        toolTip1.SetToolTip(EditSbmBBMinPercentage, "Een BB heeft een bepaalde breedte, je kunt hier filteren waardoor op de minimale en maximale breedte kan worden gefilterd.");
        EditSbmBBMinPercentage.Value = new decimal(new int[] { 150, 0, 0, 131072 });
        // 
        // EditSbmBBMaxPercentage
        // 
        EditSbmBBMaxPercentage.DecimalPlaces = 2;
        EditSbmBBMaxPercentage.Location = new Point(810, 205);
        EditSbmBBMaxPercentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmBBMaxPercentage.Name = "EditSbmBBMaxPercentage";
        EditSbmBBMaxPercentage.Size = new Size(65, 23);
        EditSbmBBMaxPercentage.TabIndex = 115;
        toolTip1.SetToolTip(EditSbmBBMaxPercentage, "Een BB heeft een bepaalde breedte, je kunt hier filteren waardoor op de minimale en maximale breedte kan worden gefilterd.");
        EditSbmBBMaxPercentage.Value = new decimal(new int[] { 6, 0, 0, 0 });
        // 
        // label22
        // 
        label22.AutoSize = true;
        label22.Location = new Point(612, 178);
        label22.Margin = new Padding(4, 0, 4, 0);
        label22.Name = "label22";
        label22.Size = new Size(228, 15);
        label22.TabIndex = 112;
        label22.Text = "Extra instellingen voor alle SBM methodes";
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new Point(612, 400);
        label4.Margin = new Padding(4, 0, 4, 0);
        label4.Name = "label4";
        label4.Size = new Size(372, 15);
        label4.TabIndex = 110;
        label4.Text = "Het minimaal percentage wat tussen de ma200 en ma20  moet liggen";
        // 
        // EditSbmMa200AndMa20Percentage
        // 
        EditSbmMa200AndMa20Percentage.DecimalPlaces = 2;
        EditSbmMa200AndMa20Percentage.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        EditSbmMa200AndMa20Percentage.Location = new Point(1052, 397);
        EditSbmMa200AndMa20Percentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa20Percentage.Name = "EditSbmMa200AndMa20Percentage";
        EditSbmMa200AndMa20Percentage.Size = new Size(57, 23);
        EditSbmMa200AndMa20Percentage.TabIndex = 111;
        EditSbmMa200AndMa20Percentage.Value = new decimal(new int[] { 3, 0, 0, 65536 });
        // 
        // label8
        // 
        label8.AutoSize = true;
        label8.Location = new Point(612, 425);
        label8.Margin = new Padding(4, 0, 4, 0);
        label8.Name = "label8";
        label8.Size = new Size(363, 15);
        label8.TabIndex = 108;
        label8.Text = "Het minimaal percentage wat tussen de ma50 en ma20 moet liggen";
        // 
        // EditSbmMa50AndMa20Percentage
        // 
        EditSbmMa50AndMa20Percentage.DecimalPlaces = 2;
        EditSbmMa50AndMa20Percentage.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        EditSbmMa50AndMa20Percentage.Location = new Point(1052, 424);
        EditSbmMa50AndMa20Percentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa50AndMa20Percentage.Name = "EditSbmMa50AndMa20Percentage";
        EditSbmMa50AndMa20Percentage.Size = new Size(57, 23);
        EditSbmMa50AndMa20Percentage.TabIndex = 109;
        EditSbmMa50AndMa20Percentage.Value = new decimal(new int[] { 3, 0, 0, 65536 });
        // 
        // label7
        // 
        label7.AutoSize = true;
        label7.Location = new Point(612, 371);
        label7.Margin = new Padding(4, 0, 4, 0);
        label7.Name = "label7";
        label7.Size = new Size(369, 15);
        label7.TabIndex = 106;
        label7.Text = "Het minimaal percentage wat tussen de ma200 en ma50 moet liggen";
        // 
        // EditSbmMa200AndMa50Percentage
        // 
        EditSbmMa200AndMa50Percentage.DecimalPlaces = 2;
        EditSbmMa200AndMa50Percentage.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        EditSbmMa200AndMa50Percentage.Location = new Point(1052, 371);
        EditSbmMa200AndMa50Percentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa50Percentage.Name = "EditSbmMa200AndMa50Percentage";
        EditSbmMa200AndMa50Percentage.Size = new Size(57, 23);
        EditSbmMa200AndMa50Percentage.TabIndex = 107;
        toolTip1.SetToolTip(EditSbmMa200AndMa50Percentage, "Percentage tussen de ma200 en ma50");
        EditSbmMa200AndMa50Percentage.Value = new decimal(new int[] { 3, 0, 0, 65536 });
        // 
        // EditSbmMa50AndMa20Lookback
        // 
        EditSbmMa50AndMa20Lookback.Location = new Point(1052, 335);
        EditSbmMa50AndMa20Lookback.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa50AndMa20Lookback.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbmMa50AndMa20Lookback.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbmMa50AndMa20Lookback.Name = "EditSbmMa50AndMa20Lookback";
        EditSbmMa50AndMa20Lookback.Size = new Size(57, 23);
        EditSbmMa50AndMa20Lookback.TabIndex = 105;
        EditSbmMa50AndMa20Lookback.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // EditSbmMa50AndMa20Crossing
        // 
        EditSbmMa50AndMa20Crossing.AutoSize = true;
        EditSbmMa50AndMa20Crossing.Location = new Point(614, 334);
        EditSbmMa50AndMa20Crossing.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa50AndMa20Crossing.Name = "EditSbmMa50AndMa20Crossing";
        EditSbmMa50AndMa20Crossing.Size = new Size(402, 19);
        EditSbmMa50AndMa20Crossing.TabIndex = 104;
        EditSbmMa50AndMa20Crossing.Text = "Controleer op een kruising van de ma50 en ma20 in de laatste x candles";
        EditSbmMa50AndMa20Crossing.UseVisualStyleBackColor = true;
        // 
        // EditSbmMa200AndMa50Lookback
        // 
        EditSbmMa200AndMa50Lookback.Location = new Point(1052, 283);
        EditSbmMa200AndMa50Lookback.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa50Lookback.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbmMa200AndMa50Lookback.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbmMa200AndMa50Lookback.Name = "EditSbmMa200AndMa50Lookback";
        EditSbmMa200AndMa50Lookback.Size = new Size(57, 23);
        EditSbmMa200AndMa50Lookback.TabIndex = 103;
        EditSbmMa200AndMa50Lookback.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // EditSbmMa200AndMa50Crossing
        // 
        EditSbmMa200AndMa50Crossing.AutoSize = true;
        EditSbmMa200AndMa50Crossing.Location = new Point(614, 283);
        EditSbmMa200AndMa50Crossing.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa50Crossing.Name = "EditSbmMa200AndMa50Crossing";
        EditSbmMa200AndMa50Crossing.Size = new Size(408, 19);
        EditSbmMa200AndMa50Crossing.TabIndex = 102;
        EditSbmMa200AndMa50Crossing.Text = "Controleer op een kruising van de ma200 en ma50 in de laatste x candles";
        EditSbmMa200AndMa50Crossing.UseVisualStyleBackColor = true;
        // 
        // EditSbmMa200AndMa20Lookback
        // 
        EditSbmMa200AndMa20Lookback.Location = new Point(1052, 309);
        EditSbmMa200AndMa20Lookback.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa20Lookback.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbmMa200AndMa20Lookback.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbmMa200AndMa20Lookback.Name = "EditSbmMa200AndMa20Lookback";
        EditSbmMa200AndMa20Lookback.Size = new Size(57, 23);
        EditSbmMa200AndMa20Lookback.TabIndex = 101;
        EditSbmMa200AndMa20Lookback.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // EditSbmMa200AndMa20Crossing
        // 
        EditSbmMa200AndMa20Crossing.AutoSize = true;
        EditSbmMa200AndMa20Crossing.Location = new Point(614, 309);
        EditSbmMa200AndMa20Crossing.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa20Crossing.Name = "EditSbmMa200AndMa20Crossing";
        EditSbmMa200AndMa20Crossing.Size = new Size(408, 19);
        EditSbmMa200AndMa20Crossing.TabIndex = 100;
        EditSbmMa200AndMa20Crossing.Text = "Controleer op een kruising van de ma200 en ma20 in de laatste x candles";
        EditSbmMa200AndMa20Crossing.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeSbm3Overbought
        // 
        EditAnalyzeSbm3Overbought.AutoSize = true;
        EditAnalyzeSbm3Overbought.Location = new Point(19, 514);
        EditAnalyzeSbm3Overbought.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeSbm3Overbought.Name = "EditAnalyzeSbm3Overbought";
        EditAnalyzeSbm3Overbought.Size = new Size(148, 19);
        EditAnalyzeSbm3Overbought.TabIndex = 97;
        EditAnalyzeSbm3Overbought.Text = "Maak verkoop signalen";
        EditAnalyzeSbm3Overbought.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeSbm2Overbought
        // 
        EditAnalyzeSbm2Overbought.AutoSize = true;
        EditAnalyzeSbm2Overbought.Location = new Point(19, 341);
        EditAnalyzeSbm2Overbought.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeSbm2Overbought.Name = "EditAnalyzeSbm2Overbought";
        EditAnalyzeSbm2Overbought.Size = new Size(148, 19);
        EditAnalyzeSbm2Overbought.TabIndex = 96;
        EditAnalyzeSbm2Overbought.Text = "Maak verkoop signalen";
        EditAnalyzeSbm2Overbought.UseVisualStyleBackColor = true;
        // 
        // label12
        // 
        label12.AutoSize = true;
        label12.Location = new Point(19, 392);
        label12.Margin = new Padding(4, 0, 4, 0);
        label12.Name = "label12";
        label12.Size = new Size(224, 15);
        label12.TabIndex = 94;
        label12.Text = "Percentage ten opzichte van de BB bands";
        // 
        // EditSbm2BbPercentage
        // 
        EditSbm2BbPercentage.DecimalPlaces = 2;
        EditSbm2BbPercentage.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        EditSbm2BbPercentage.Location = new Point(274, 390);
        EditSbm2BbPercentage.Margin = new Padding(4, 3, 4, 3);
        EditSbm2BbPercentage.Name = "EditSbm2BbPercentage";
        EditSbm2BbPercentage.Size = new Size(57, 23);
        EditSbm2BbPercentage.TabIndex = 95;
        EditSbm2BbPercentage.Value = new decimal(new int[] { 50, 0, 0, 131072 });
        // 
        // panelColorSbm
        // 
        panelColorSbm.BorderStyle = BorderStyle.FixedSingle;
        panelColorSbm.Location = new Point(19, 16);
        panelColorSbm.Margin = new Padding(4, 3, 4, 3);
        panelColorSbm.Name = "panelColorSbm";
        panelColorSbm.Size = new Size(70, 22);
        panelColorSbm.TabIndex = 122;
        // 
        // label13
        // 
        label13.AutoSize = true;
        label13.Location = new Point(19, 591);
        label13.Margin = new Padding(4, 0, 4, 0);
        label13.Name = "label13";
        label13.Size = new Size(95, 15);
        label13.TabIndex = 85;
        label13.Text = "Candle lookback";
        // 
        // EditSbm3CandlesForBBRecovery
        // 
        EditSbm3CandlesForBBRecovery.Location = new Point(274, 589);
        EditSbm3CandlesForBBRecovery.Margin = new Padding(4, 3, 4, 3);
        EditSbm3CandlesForBBRecovery.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbm3CandlesForBBRecovery.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbm3CandlesForBBRecovery.Name = "EditSbm3CandlesForBBRecovery";
        EditSbm3CandlesForBBRecovery.Size = new Size(57, 23);
        EditSbm3CandlesForBBRecovery.TabIndex = 86;
        EditSbm3CandlesForBBRecovery.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // label14
        // 
        label14.AutoSize = true;
        label14.Location = new Point(19, 567);
        label14.Margin = new Padding(4, 0, 4, 0);
        label14.Name = "label14";
        label14.Size = new Size(139, 15);
        label14.TabIndex = 83;
        label14.Text = "Percentage oprekking BB";
        // 
        // EditSbm3CandlesForBBRecoveryPercentage
        // 
        EditSbm3CandlesForBBRecoveryPercentage.Location = new Point(274, 565);
        EditSbm3CandlesForBBRecoveryPercentage.Margin = new Padding(4, 3, 4, 3);
        EditSbm3CandlesForBBRecoveryPercentage.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
        EditSbm3CandlesForBBRecoveryPercentage.Name = "EditSbm3CandlesForBBRecoveryPercentage";
        EditSbm3CandlesForBBRecoveryPercentage.Size = new Size(57, 23);
        EditSbm3CandlesForBBRecoveryPercentage.TabIndex = 84;
        EditSbm3CandlesForBBRecoveryPercentage.Value = new decimal(new int[] { 225, 0, 0, 0 });
        // 
        // label11
        // 
        label11.AutoSize = true;
        label11.Location = new Point(19, 439);
        label11.Margin = new Padding(4, 0, 4, 0);
        label11.Name = "label11";
        label11.Size = new Size(95, 15);
        label11.TabIndex = 79;
        label11.Text = "Candle lookback";
        // 
        // EditSbm2CandlesLookbackCount
        // 
        EditSbm2CandlesLookbackCount.Location = new Point(274, 437);
        EditSbm2CandlesLookbackCount.Margin = new Padding(4, 3, 4, 3);
        EditSbm2CandlesLookbackCount.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbm2CandlesLookbackCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbm2CandlesLookbackCount.Name = "EditSbm2CandlesLookbackCount";
        EditSbm2CandlesLookbackCount.Size = new Size(57, 23);
        EditSbm2CandlesLookbackCount.TabIndex = 80;
        EditSbm2CandlesLookbackCount.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // EditAnalyzeSbm3Oversold
        // 
        EditAnalyzeSbm3Oversold.AutoSize = true;
        EditAnalyzeSbm3Oversold.Location = new Point(19, 540);
        EditAnalyzeSbm3Oversold.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeSbm3Oversold.Name = "EditAnalyzeSbm3Oversold";
        EditAnalyzeSbm3Oversold.Size = new Size(151, 19);
        EditAnalyzeSbm3Oversold.TabIndex = 69;
        EditAnalyzeSbm3Oversold.Text = "Maak aankoop signalen";
        EditAnalyzeSbm3Oversold.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeSbm2Oversold
        // 
        EditAnalyzeSbm2Oversold.AutoSize = true;
        EditAnalyzeSbm2Oversold.Location = new Point(19, 367);
        EditAnalyzeSbm2Oversold.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeSbm2Oversold.Name = "EditAnalyzeSbm2Oversold";
        EditAnalyzeSbm2Oversold.Size = new Size(151, 19);
        EditAnalyzeSbm2Oversold.TabIndex = 68;
        EditAnalyzeSbm2Oversold.Text = "Maak aankoop signalen";
        EditAnalyzeSbm2Oversold.UseVisualStyleBackColor = true;
        // 
        // tabSignalJump
        // 
        tabSignalJump.Controls.Add(label78);
        tabSignalJump.Controls.Add(label76);
        tabSignalJump.Controls.Add(label33);
        tabSignalJump.Controls.Add(label34);
        tabSignalJump.Controls.Add(label5);
        tabSignalJump.Controls.Add(EditJumpCandlesLookbackCount);
        tabSignalJump.Controls.Add(EditJumpUseLowHighCalculation);
        tabSignalJump.Controls.Add(buttonColorJump);
        tabSignalJump.Controls.Add(buttonPlaySoundCandleJumpDown);
        tabSignalJump.Controls.Add(buttonPlaySoundCandleJumpUp);
        tabSignalJump.Controls.Add(buttonSelectSoundCandleJumpDown);
        tabSignalJump.Controls.Add(panelColorJump);
        tabSignalJump.Controls.Add(EditSoundFileCandleJumpDown);
        tabSignalJump.Controls.Add(EditSoundFileCandleJumpUp);
        tabSignalJump.Controls.Add(buttonSelectSoundCandleJumpUp);
        tabSignalJump.Controls.Add(EditPlaySpeechCandleJumpSignal);
        tabSignalJump.Controls.Add(label3);
        tabSignalJump.Controls.Add(EditPlaySoundCandleJumpSignal);
        tabSignalJump.Controls.Add(EditAnalyzeCandleJumpUp);
        tabSignalJump.Controls.Add(EditAnalyzeCandleJumpDown);
        tabSignalJump.Controls.Add(EditAnalysisCandleJumpPercentage);
        tabSignalJump.Location = new Point(4, 27);
        tabSignalJump.Margin = new Padding(4, 3, 4, 3);
        tabSignalJump.Name = "tabSignalJump";
        tabSignalJump.Padding = new Padding(4, 3, 4, 3);
        tabSignalJump.Size = new Size(1232, 777);
        tabSignalJump.TabIndex = 9;
        tabSignalJump.Text = "JUMP";
        tabSignalJump.UseVisualStyleBackColor = true;
        // 
        // label78
        // 
        label78.AutoSize = true;
        label78.Location = new Point(21, 246);
        label78.Margin = new Padding(4, 0, 4, 0);
        label78.Name = "label78";
        label78.Size = new Size(37, 15);
        label78.TabIndex = 151;
        label78.Text = "JUMP";
        // 
        // label76
        // 
        label76.AutoSize = true;
        label76.Location = new Point(23, 349);
        label76.Margin = new Padding(4, 0, 4, 0);
        label76.Name = "label76";
        label76.Size = new Size(68, 15);
        label76.TabIndex = 138;
        label76.Text = "Instellingen";
        // 
        // label33
        // 
        label33.AutoSize = true;
        label33.Location = new Point(20, 171);
        label33.Margin = new Padding(4, 0, 4, 0);
        label33.Name = "label33";
        label33.Size = new Size(121, 15);
        label33.TabIndex = 137;
        label33.Text = "Jump down soundfile";
        // 
        // label34
        // 
        label34.AutoSize = true;
        label34.Location = new Point(19, 145);
        label34.Margin = new Padding(4, 0, 4, 0);
        label34.Name = "label34";
        label34.Size = new Size(105, 15);
        label34.TabIndex = 136;
        label34.Text = "Jump up soundfile";
        // 
        // label5
        // 
        label5.AutoSize = true;
        label5.Location = new Point(23, 402);
        label5.Margin = new Padding(4, 0, 4, 0);
        label5.Name = "label5";
        label5.Size = new Size(95, 15);
        label5.TabIndex = 123;
        label5.Text = "Candle lookback";
        // 
        // EditJumpCandlesLookbackCount
        // 
        EditJumpCandlesLookbackCount.Location = new Point(170, 401);
        EditJumpCandlesLookbackCount.Margin = new Padding(4, 3, 4, 3);
        EditJumpCandlesLookbackCount.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
        EditJumpCandlesLookbackCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditJumpCandlesLookbackCount.Name = "EditJumpCandlesLookbackCount";
        EditJumpCandlesLookbackCount.Size = new Size(57, 23);
        EditJumpCandlesLookbackCount.TabIndex = 124;
        EditJumpCandlesLookbackCount.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // EditJumpUseLowHighCalculation
        // 
        EditJumpUseLowHighCalculation.AutoSize = true;
        EditJumpUseLowHighCalculation.Location = new Point(23, 430);
        EditJumpUseLowHighCalculation.Margin = new Padding(4, 3, 4, 3);
        EditJumpUseLowHighCalculation.Name = "EditJumpUseLowHighCalculation";
        EditJumpUseLowHighCalculation.Size = new Size(250, 19);
        EditJumpUseLowHighCalculation.TabIndex = 122;
        EditJumpUseLowHighCalculation.Text = "Bereken via de high/low ipv de open/close";
        EditJumpUseLowHighCalculation.UseVisualStyleBackColor = true;
        // 
        // buttonColorJump
        // 
        buttonColorJump.Location = new Point(127, 47);
        buttonColorJump.Margin = new Padding(4, 3, 4, 3);
        buttonColorJump.Name = "buttonColorJump";
        buttonColorJump.Size = new Size(88, 27);
        buttonColorJump.TabIndex = 120;
        buttonColorJump.Text = "Achtergrond";
        buttonColorJump.UseVisualStyleBackColor = true;
        // 
        // buttonPlaySoundCandleJumpDown
        // 
        buttonPlaySoundCandleJumpDown.Image = Properties.Resources.volume;
        buttonPlaySoundCandleJumpDown.Location = new Point(416, 167);
        buttonPlaySoundCandleJumpDown.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySoundCandleJumpDown.Name = "buttonPlaySoundCandleJumpDown";
        buttonPlaySoundCandleJumpDown.Size = new Size(23, 23);
        buttonPlaySoundCandleJumpDown.TabIndex = 119;
        buttonPlaySoundCandleJumpDown.UseVisualStyleBackColor = true;
        // 
        // buttonPlaySoundCandleJumpUp
        // 
        buttonPlaySoundCandleJumpUp.Image = Properties.Resources.volume;
        buttonPlaySoundCandleJumpUp.Location = new Point(416, 142);
        buttonPlaySoundCandleJumpUp.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySoundCandleJumpUp.Name = "buttonPlaySoundCandleJumpUp";
        buttonPlaySoundCandleJumpUp.Size = new Size(23, 23);
        buttonPlaySoundCandleJumpUp.TabIndex = 118;
        buttonPlaySoundCandleJumpUp.UseVisualStyleBackColor = true;
        // 
        // buttonSelectSoundCandleJumpDown
        // 
        buttonSelectSoundCandleJumpDown.Location = new Point(386, 167);
        buttonSelectSoundCandleJumpDown.Margin = new Padding(4, 3, 4, 3);
        buttonSelectSoundCandleJumpDown.Name = "buttonSelectSoundCandleJumpDown";
        buttonSelectSoundCandleJumpDown.Size = new Size(23, 23);
        buttonSelectSoundCandleJumpDown.TabIndex = 115;
        buttonSelectSoundCandleJumpDown.UseVisualStyleBackColor = true;
        // 
        // panelColorJump
        // 
        panelColorJump.BackColor = Color.Transparent;
        panelColorJump.BorderStyle = BorderStyle.FixedSingle;
        panelColorJump.Location = new Point(19, 50);
        panelColorJump.Margin = new Padding(4, 3, 4, 3);
        panelColorJump.Name = "panelColorJump";
        panelColorJump.Size = new Size(70, 22);
        panelColorJump.TabIndex = 121;
        // 
        // EditSoundFileCandleJumpDown
        // 
        EditSoundFileCandleJumpDown.Location = new Point(152, 167);
        EditSoundFileCandleJumpDown.Margin = new Padding(4, 3, 4, 3);
        EditSoundFileCandleJumpDown.Name = "EditSoundFileCandleJumpDown";
        EditSoundFileCandleJumpDown.Size = new Size(227, 23);
        EditSoundFileCandleJumpDown.TabIndex = 114;
        // 
        // EditSoundFileCandleJumpUp
        // 
        EditSoundFileCandleJumpUp.Location = new Point(152, 142);
        EditSoundFileCandleJumpUp.Margin = new Padding(4, 3, 4, 3);
        EditSoundFileCandleJumpUp.Name = "EditSoundFileCandleJumpUp";
        EditSoundFileCandleJumpUp.Size = new Size(227, 23);
        EditSoundFileCandleJumpUp.TabIndex = 112;
        // 
        // buttonSelectSoundCandleJumpUp
        // 
        buttonSelectSoundCandleJumpUp.Location = new Point(386, 142);
        buttonSelectSoundCandleJumpUp.Margin = new Padding(4, 3, 4, 3);
        buttonSelectSoundCandleJumpUp.Name = "buttonSelectSoundCandleJumpUp";
        buttonSelectSoundCandleJumpUp.Size = new Size(23, 23);
        buttonSelectSoundCandleJumpUp.TabIndex = 109;
        buttonSelectSoundCandleJumpUp.UseVisualStyleBackColor = true;
        // 
        // EditPlaySpeechCandleJumpSignal
        // 
        EditPlaySpeechCandleJumpSignal.AutoSize = true;
        EditPlaySpeechCandleJumpSignal.Location = new Point(19, 115);
        EditPlaySpeechCandleJumpSignal.Margin = new Padding(4, 3, 4, 3);
        EditPlaySpeechCandleJumpSignal.Name = "EditPlaySpeechCandleJumpSignal";
        EditPlaySpeechCandleJumpSignal.Size = new Size(88, 19);
        EditPlaySpeechCandleJumpSignal.TabIndex = 110;
        EditPlaySpeechCandleJumpSignal.Text = "Play speech";
        EditPlaySpeechCandleJumpSignal.UseVisualStyleBackColor = true;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(23, 371);
        label3.Margin = new Padding(4, 0, 4, 0);
        label3.Name = "label3";
        label3.Size = new Size(98, 15);
        label3.TabIndex = 116;
        label3.Text = "Jump percentage";
        // 
        // EditPlaySoundCandleJumpSignal
        // 
        EditPlaySoundCandleJumpSignal.AutoSize = true;
        EditPlaySoundCandleJumpSignal.Location = new Point(19, 88);
        EditPlaySoundCandleJumpSignal.Margin = new Padding(4, 3, 4, 3);
        EditPlaySoundCandleJumpSignal.Name = "EditPlaySoundCandleJumpSignal";
        EditPlaySoundCandleJumpSignal.Size = new Size(84, 19);
        EditPlaySoundCandleJumpSignal.TabIndex = 108;
        EditPlaySoundCandleJumpSignal.Text = "Play sound";
        EditPlaySoundCandleJumpSignal.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeCandleJumpUp
        // 
        EditAnalyzeCandleJumpUp.AutoSize = true;
        EditAnalyzeCandleJumpUp.Location = new Point(21, 272);
        EditAnalyzeCandleJumpUp.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeCandleJumpUp.Name = "EditAnalyzeCandleJumpUp";
        EditAnalyzeCandleJumpUp.Size = new Size(160, 19);
        EditAnalyzeCandleJumpUp.TabIndex = 111;
        EditAnalyzeCandleJumpUp.Text = "Maak \"jump up\" signalen";
        EditAnalyzeCandleJumpUp.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeCandleJumpDown
        // 
        EditAnalyzeCandleJumpDown.AutoSize = true;
        EditAnalyzeCandleJumpDown.Location = new Point(21, 298);
        EditAnalyzeCandleJumpDown.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeCandleJumpDown.Name = "EditAnalyzeCandleJumpDown";
        EditAnalyzeCandleJumpDown.Size = new Size(176, 19);
        EditAnalyzeCandleJumpDown.TabIndex = 113;
        EditAnalyzeCandleJumpDown.Text = "Maak \"jump down\" signalen";
        EditAnalyzeCandleJumpDown.UseVisualStyleBackColor = true;
        // 
        // EditAnalysisCandleJumpPercentage
        // 
        EditAnalysisCandleJumpPercentage.DecimalPlaces = 2;
        EditAnalysisCandleJumpPercentage.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditAnalysisCandleJumpPercentage.Location = new Point(170, 371);
        EditAnalysisCandleJumpPercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisCandleJumpPercentage.Name = "EditAnalysisCandleJumpPercentage";
        EditAnalysisCandleJumpPercentage.Size = new Size(56, 23);
        EditAnalysisCandleJumpPercentage.TabIndex = 117;
        // 
        // tabPageTrading
        // 
        tabPageTrading.Controls.Add(label19);
        tabPageTrading.Controls.Add(EditMargin);
        tabPageTrading.Controls.Add(label23);
        tabPageTrading.Controls.Add(EditLeverage);
        tabPageTrading.Controls.Add(EditLogCanceledOrders);
        tabPageTrading.Controls.Add(EditSoundTradeNotification);
        tabPageTrading.Controls.Add(EditDisableNewPositions);
        tabPageTrading.Controls.Add(label83);
        tabPageTrading.Controls.Add(EditBuyStepInMethod);
        tabPageTrading.Controls.Add(label82);
        tabPageTrading.Controls.Add(EditDcaStepInMethod);
        tabPageTrading.Controls.Add(label65);
        tabPageTrading.Controls.Add(EditDynamicTpPercentage);
        tabPageTrading.Controls.Add(EditTradeViaBinance);
        tabPageTrading.Controls.Add(label63);
        tabPageTrading.Controls.Add(EditSellMethod);
        tabPageTrading.Controls.Add(EditTradeViaPaperTrading);
        tabPageTrading.Controls.Add(label60);
        tabPageTrading.Controls.Add(EditDcaOrderMethod);
        tabPageTrading.Controls.Add(label36);
        tabPageTrading.Controls.Add(label81);
        tabPageTrading.Controls.Add(label57);
        tabPageTrading.Controls.Add(label54);
        tabPageTrading.Controls.Add(groupBoxSlots);
        tabPageTrading.Controls.Add(groupBox2);
        tabPageTrading.Controls.Add(groupBox1);
        tabPageTrading.Controls.Add(label62);
        tabPageTrading.Controls.Add(EditBuyOrderMethod);
        tabPageTrading.Controls.Add(EditDcaCount);
        tabPageTrading.Controls.Add(label67);
        tabPageTrading.Controls.Add(label68);
        tabPageTrading.Controls.Add(EditDcaFactor);
        tabPageTrading.Controls.Add(label69);
        tabPageTrading.Controls.Add(EditDcaPercentage);
        tabPageTrading.Controls.Add(EditGlobalStopLimitPercentage);
        tabPageTrading.Controls.Add(label70);
        tabPageTrading.Controls.Add(EditGlobalStopPercentage);
        tabPageTrading.Controls.Add(label71);
        tabPageTrading.Controls.Add(label72);
        tabPageTrading.Controls.Add(EditProfitPercentage);
        tabPageTrading.Controls.Add(label73);
        tabPageTrading.Controls.Add(EditGlobalBuyCooldownTime);
        tabPageTrading.Controls.Add(EditGlobalBuyVarying);
        tabPageTrading.Controls.Add(label47);
        tabPageTrading.Controls.Add(label46);
        tabPageTrading.Controls.Add(EditGlobalBuyRemoveTime);
        tabPageTrading.Location = new Point(4, 27);
        tabPageTrading.Margin = new Padding(4, 3, 4, 3);
        tabPageTrading.Name = "tabPageTrading";
        tabPageTrading.Padding = new Padding(4, 3, 4, 3);
        tabPageTrading.Size = new Size(1232, 777);
        tabPageTrading.TabIndex = 11;
        tabPageTrading.Text = "Trading";
        tabPageTrading.UseVisualStyleBackColor = true;
        // 
        // label19
        // 
        label19.AutoSize = true;
        label19.Location = new Point(400, 637);
        label19.Margin = new Padding(4, 0, 4, 0);
        label19.Name = "label19";
        label19.Size = new Size(45, 15);
        label19.TabIndex = 270;
        label19.Text = "Margin";
        // 
        // EditMargin
        // 
        EditMargin.DropDownStyle = ComboBoxStyle.DropDownList;
        EditMargin.FormattingEnabled = true;
        EditMargin.Items.AddRange(new object[] { "Cross", "Isolated" });
        EditMargin.Location = new Point(525, 633);
        EditMargin.Margin = new Padding(4, 3, 4, 3);
        EditMargin.Name = "EditMargin";
        EditMargin.Size = new Size(131, 23);
        EditMargin.TabIndex = 269;
        // 
        // label23
        // 
        label23.AutoSize = true;
        label23.Location = new Point(400, 665);
        label23.Margin = new Padding(4, 0, 4, 0);
        label23.Name = "label23";
        label23.Size = new Size(54, 15);
        label23.TabIndex = 268;
        label23.Text = "Leverage";
        // 
        // EditLeverage
        // 
        EditLeverage.DecimalPlaces = 2;
        EditLeverage.Location = new Point(524, 662);
        EditLeverage.Margin = new Padding(4, 3, 4, 3);
        EditLeverage.Name = "EditLeverage";
        EditLeverage.Size = new Size(88, 23);
        EditLeverage.TabIndex = 267;
        EditLeverage.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // EditLogCanceledOrders
        // 
        EditLogCanceledOrders.AutoSize = true;
        EditLogCanceledOrders.Location = new Point(402, 600);
        EditLogCanceledOrders.Margin = new Padding(4, 3, 4, 3);
        EditLogCanceledOrders.Name = "EditLogCanceledOrders";
        EditLogCanceledOrders.Size = new Size(157, 19);
        EditLogCanceledOrders.TabIndex = 266;
        EditLogCanceledOrders.Text = "Log geannuleerde orders";
        EditLogCanceledOrders.UseVisualStyleBackColor = true;
        // 
        // EditSoundTradeNotification
        // 
        EditSoundTradeNotification.AutoSize = true;
        EditSoundTradeNotification.Location = new Point(23, 91);
        EditSoundTradeNotification.Margin = new Padding(4, 3, 4, 3);
        EditSoundTradeNotification.Name = "EditSoundTradeNotification";
        EditSoundTradeNotification.Size = new Size(186, 19);
        EditSoundTradeNotification.TabIndex = 265;
        EditSoundTradeNotification.Text = "Geluid voor een trade afspelen";
        EditSoundTradeNotification.UseVisualStyleBackColor = true;
        // 
        // EditDisableNewPositions
        // 
        EditDisableNewPositions.AutoSize = true;
        EditDisableNewPositions.Location = new Point(23, 66);
        EditDisableNewPositions.Margin = new Padding(4, 3, 4, 3);
        EditDisableNewPositions.Name = "EditDisableNewPositions";
        EditDisableNewPositions.Size = new Size(187, 19);
        EditDisableNewPositions.TabIndex = 264;
        EditDisableNewPositions.Text = "Geen nieuwe posities innemen";
        EditDisableNewPositions.UseVisualStyleBackColor = true;
        // 
        // label83
        // 
        label83.AutoSize = true;
        label83.Location = new Point(17, 178);
        label83.Margin = new Padding(4, 0, 4, 0);
        label83.Name = "label83";
        label83.Size = new Size(88, 15);
        label83.TabIndex = 263;
        label83.Text = "Instap moment";
        // 
        // EditBuyStepInMethod
        // 
        EditBuyStepInMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        EditBuyStepInMethod.FormattingEnabled = true;
        EditBuyStepInMethod.Location = new Point(185, 170);
        EditBuyStepInMethod.Margin = new Padding(4, 3, 4, 3);
        EditBuyStepInMethod.Name = "EditBuyStepInMethod";
        EditBuyStepInMethod.Size = new Size(200, 23);
        EditBuyStepInMethod.TabIndex = 262;
        // 
        // label82
        // 
        label82.AutoSize = true;
        label82.Location = new Point(17, 316);
        label82.Margin = new Padding(4, 0, 4, 0);
        label82.Name = "label82";
        label82.Size = new Size(88, 15);
        label82.TabIndex = 261;
        label82.Text = "Instap moment";
        // 
        // EditDcaStepInMethod
        // 
        EditDcaStepInMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        EditDcaStepInMethod.FormattingEnabled = true;
        EditDcaStepInMethod.Location = new Point(185, 308);
        EditDcaStepInMethod.Margin = new Padding(4, 3, 4, 3);
        EditDcaStepInMethod.Name = "EditDcaStepInMethod";
        EditDcaStepInMethod.Size = new Size(200, 23);
        EditDcaStepInMethod.TabIndex = 260;
        // 
        // label65
        // 
        label65.AutoSize = true;
        label65.Font = new Font("Segoe UI", 9F, FontStyle.Strikeout, GraphicsUnit.Point);
        label65.Location = new Point(17, 571);
        label65.Margin = new Padding(4, 0, 4, 0);
        label65.Name = "label65";
        label65.Size = new Size(143, 15);
        label65.TabIndex = 258;
        label65.Text = "Percentage van MA20 (%)";
        // 
        // EditDynamicTpPercentage
        // 
        EditDynamicTpPercentage.DecimalPlaces = 2;
        EditDynamicTpPercentage.Enabled = false;
        EditDynamicTpPercentage.Font = new Font("Segoe UI", 9F, FontStyle.Strikeout, GraphicsUnit.Point);
        EditDynamicTpPercentage.Location = new Point(185, 569);
        EditDynamicTpPercentage.Margin = new Padding(4, 3, 4, 3);
        EditDynamicTpPercentage.Name = "EditDynamicTpPercentage";
        EditDynamicTpPercentage.Size = new Size(88, 23);
        EditDynamicTpPercentage.TabIndex = 259;
        EditDynamicTpPercentage.Value = new decimal(new int[] { 75, 0, 0, 131072 });
        // 
        // EditTradeViaBinance
        // 
        EditTradeViaBinance.AutoSize = true;
        EditTradeViaBinance.Location = new Point(23, 41);
        EditTradeViaBinance.Margin = new Padding(4, 3, 4, 3);
        EditTradeViaBinance.Name = "EditTradeViaBinance";
        EditTradeViaBinance.Size = new Size(148, 19);
        EditTradeViaBinance.TabIndex = 255;
        EditTradeViaBinance.Text = "Traden op de exchange";
        EditTradeViaBinance.UseVisualStyleBackColor = true;
        // 
        // label63
        // 
        label63.AutoSize = true;
        label63.Location = new Point(17, 515);
        label63.Margin = new Padding(4, 0, 4, 0);
        label63.Name = "label63";
        label63.Size = new Size(55, 15);
        label63.TabIndex = 254;
        label63.Text = "Methode";
        // 
        // EditSellMethod
        // 
        EditSellMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        EditSellMethod.FormattingEnabled = true;
        EditSellMethod.Location = new Point(185, 512);
        EditSellMethod.Margin = new Padding(4, 3, 4, 3);
        EditSellMethod.Name = "EditSellMethod";
        EditSellMethod.Size = new Size(200, 23);
        EditSellMethod.TabIndex = 253;
        // 
        // EditTradeViaPaperTrading
        // 
        EditTradeViaPaperTrading.AutoSize = true;
        EditTradeViaPaperTrading.Location = new Point(23, 19);
        EditTradeViaPaperTrading.Margin = new Padding(4, 3, 4, 3);
        EditTradeViaPaperTrading.Name = "EditTradeViaPaperTrading";
        EditTradeViaPaperTrading.Size = new Size(97, 19);
        EditTradeViaPaperTrading.TabIndex = 251;
        EditTradeViaPaperTrading.Text = "Paper trading";
        EditTradeViaPaperTrading.UseVisualStyleBackColor = true;
        // 
        // label60
        // 
        label60.AutoSize = true;
        label60.Location = new Point(17, 340);
        label60.Margin = new Padding(4, 0, 4, 0);
        label60.Name = "label60";
        label60.Size = new Size(86, 15);
        label60.TabIndex = 250;
        label60.Text = "Koop methode";
        // 
        // EditDcaOrderMethod
        // 
        EditDcaOrderMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        EditDcaOrderMethod.FormattingEnabled = true;
        EditDcaOrderMethod.Location = new Point(185, 336);
        EditDcaOrderMethod.Margin = new Padding(4, 3, 4, 3);
        EditDcaOrderMethod.Name = "EditDcaOrderMethod";
        EditDcaOrderMethod.Size = new Size(200, 23);
        EditDcaOrderMethod.TabIndex = 249;
        // 
        // label36
        // 
        label36.AutoSize = true;
        label36.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        label36.Location = new Point(17, 613);
        label36.Margin = new Padding(4, 0, 4, 0);
        label36.Name = "label36";
        label36.Size = new Size(63, 15);
        label36.TabIndex = 248;
        label36.Text = "Stopploss:";
        // 
        // label81
        // 
        label81.AutoSize = true;
        label81.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        label81.Location = new Point(17, 492);
        label81.Margin = new Padding(4, 0, 4, 0);
        label81.Name = "label81";
        label81.Size = new Size(57, 15);
        label81.TabIndex = 247;
        label81.Text = "Verkoop:";
        // 
        // label57
        // 
        label57.AutoSize = true;
        label57.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        label57.Location = new Point(17, 290);
        label57.Margin = new Padding(4, 0, 4, 0);
        label57.Name = "label57";
        label57.Size = new Size(52, 15);
        label57.TabIndex = 246;
        label57.Text = "Bijkoop:";
        // 
        // label54
        // 
        label54.AutoSize = true;
        label54.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        label54.Location = new Point(17, 154);
        label54.Margin = new Padding(4, 0, 4, 0);
        label54.Name = "label54";
        label54.Size = new Size(59, 15);
        label54.TabIndex = 245;
        label54.Text = "Aankoop:";
        // 
        // groupBoxSlots
        // 
        groupBoxSlots.Controls.Add(label50);
        groupBoxSlots.Controls.Add(EditSlotsMaximalExchange);
        groupBoxSlots.Controls.Add(label52);
        groupBoxSlots.Controls.Add(EditSlotsMaximalSymbol);
        groupBoxSlots.Controls.Add(label56);
        groupBoxSlots.Controls.Add(EditSlotsMaximalBase);
        groupBoxSlots.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        groupBoxSlots.Location = new Point(395, 459);
        groupBoxSlots.Name = "groupBoxSlots";
        groupBoxSlots.Size = new Size(234, 116);
        groupBoxSlots.TabIndex = 244;
        groupBoxSlots.TabStop = false;
        groupBoxSlots.Text = "Slot limits";
        // 
        // label50
        // 
        label50.AutoSize = true;
        label50.Location = new Point(5, 26);
        label50.Margin = new Padding(4, 0, 4, 0);
        label50.Name = "label50";
        label50.Size = new Size(58, 15);
        label50.TabIndex = 194;
        label50.Text = "Exchange";
        // 
        // EditSlotsMaximalExchange
        // 
        EditSlotsMaximalExchange.Location = new Point(129, 24);
        EditSlotsMaximalExchange.Margin = new Padding(4, 3, 4, 3);
        EditSlotsMaximalExchange.Name = "EditSlotsMaximalExchange";
        EditSlotsMaximalExchange.Size = new Size(88, 23);
        EditSlotsMaximalExchange.TabIndex = 195;
        EditSlotsMaximalExchange.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label52
        // 
        label52.AutoSize = true;
        label52.Location = new Point(5, 53);
        label52.Margin = new Padding(4, 0, 4, 0);
        label52.Name = "label52";
        label52.Size = new Size(47, 15);
        label52.TabIndex = 196;
        label52.Text = "Symbol";
        // 
        // EditSlotsMaximalSymbol
        // 
        EditSlotsMaximalSymbol.Location = new Point(129, 50);
        EditSlotsMaximalSymbol.Margin = new Padding(4, 3, 4, 3);
        EditSlotsMaximalSymbol.Name = "EditSlotsMaximalSymbol";
        EditSlotsMaximalSymbol.Size = new Size(88, 23);
        EditSlotsMaximalSymbol.TabIndex = 197;
        EditSlotsMaximalSymbol.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label56
        // 
        label56.AutoSize = true;
        label56.Location = new Point(5, 80);
        label56.Margin = new Padding(4, 0, 4, 0);
        label56.Name = "label56";
        label56.Size = new Size(31, 15);
        label56.TabIndex = 198;
        label56.Text = "Base";
        // 
        // EditSlotsMaximalBase
        // 
        EditSlotsMaximalBase.Location = new Point(129, 78);
        EditSlotsMaximalBase.Margin = new Padding(4, 3, 4, 3);
        EditSlotsMaximalBase.Name = "EditSlotsMaximalBase";
        EditSlotsMaximalBase.Size = new Size(88, 23);
        EditSlotsMaximalBase.TabIndex = 199;
        EditSlotsMaximalBase.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // groupBox2
        // 
        groupBox2.Controls.Add(EditBarometer15mBotMinimal);
        groupBox2.Controls.Add(label27);
        groupBox2.Controls.Add(EditBarometer24hBotMinimal);
        groupBox2.Controls.Add(label42);
        groupBox2.Controls.Add(EditBarometer04hBotMinimal);
        groupBox2.Controls.Add(label43);
        groupBox2.Controls.Add(EditBarometer01hBotMinimal);
        groupBox2.Controls.Add(label44);
        groupBox2.Controls.Add(label45);
        groupBox2.Controls.Add(EditBarometer30mBotMinimal);
        groupBox2.Location = new Point(395, 14);
        groupBox2.Name = "groupBox2";
        groupBox2.Size = new Size(234, 186);
        groupBox2.TabIndex = 243;
        groupBox2.TabStop = false;
        groupBox2.Text = "Barometer";
        // 
        // EditBarometer15mBotMinimal
        // 
        EditBarometer15mBotMinimal.DecimalPlaces = 2;
        EditBarometer15mBotMinimal.Location = new Point(130, 30);
        EditBarometer15mBotMinimal.Margin = new Padding(4, 3, 4, 3);
        EditBarometer15mBotMinimal.Name = "EditBarometer15mBotMinimal";
        EditBarometer15mBotMinimal.Size = new Size(88, 23);
        EditBarometer15mBotMinimal.TabIndex = 175;
        EditBarometer15mBotMinimal.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label27
        // 
        label27.AutoSize = true;
        label27.Location = new Point(7, 33);
        label27.Margin = new Padding(4, 0, 4, 0);
        label27.Name = "label27";
        label27.Size = new Size(88, 15);
        label27.TabIndex = 173;
        label27.Text = "Barometer 15m";
        // 
        // EditBarometer24hBotMinimal
        // 
        EditBarometer24hBotMinimal.DecimalPlaces = 2;
        EditBarometer24hBotMinimal.Location = new Point(130, 138);
        EditBarometer24hBotMinimal.Margin = new Padding(4, 3, 4, 3);
        EditBarometer24hBotMinimal.Name = "EditBarometer24hBotMinimal";
        EditBarometer24hBotMinimal.Size = new Size(88, 23);
        EditBarometer24hBotMinimal.TabIndex = 179;
        EditBarometer24hBotMinimal.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label42
        // 
        label42.AutoSize = true;
        label42.Location = new Point(9, 140);
        label42.Margin = new Padding(4, 0, 4, 0);
        label42.Name = "label42";
        label42.Size = new Size(84, 15);
        label42.TabIndex = 182;
        label42.Text = "Barometer 24h";
        // 
        // EditBarometer04hBotMinimal
        // 
        EditBarometer04hBotMinimal.DecimalPlaces = 2;
        EditBarometer04hBotMinimal.Location = new Point(130, 111);
        EditBarometer04hBotMinimal.Margin = new Padding(4, 3, 4, 3);
        EditBarometer04hBotMinimal.Name = "EditBarometer04hBotMinimal";
        EditBarometer04hBotMinimal.Size = new Size(88, 23);
        EditBarometer04hBotMinimal.TabIndex = 178;
        EditBarometer04hBotMinimal.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label43
        // 
        label43.AutoSize = true;
        label43.Location = new Point(9, 112);
        label43.Margin = new Padding(4, 0, 4, 0);
        label43.Name = "label43";
        label43.Size = new Size(78, 15);
        label43.TabIndex = 181;
        label43.Text = "Barometer 4h";
        // 
        // EditBarometer01hBotMinimal
        // 
        EditBarometer01hBotMinimal.DecimalPlaces = 2;
        EditBarometer01hBotMinimal.Location = new Point(130, 83);
        EditBarometer01hBotMinimal.Margin = new Padding(4, 3, 4, 3);
        EditBarometer01hBotMinimal.Name = "EditBarometer01hBotMinimal";
        EditBarometer01hBotMinimal.Size = new Size(88, 23);
        EditBarometer01hBotMinimal.TabIndex = 177;
        EditBarometer01hBotMinimal.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label44
        // 
        label44.AutoSize = true;
        label44.Location = new Point(9, 58);
        label44.Margin = new Padding(4, 0, 4, 0);
        label44.Name = "label44";
        label44.Size = new Size(88, 15);
        label44.TabIndex = 174;
        label44.Text = "Barometer 30m";
        // 
        // label45
        // 
        label45.AutoSize = true;
        label45.Location = new Point(9, 86);
        label45.Margin = new Padding(4, 0, 4, 0);
        label45.Name = "label45";
        label45.Size = new Size(78, 15);
        label45.TabIndex = 180;
        label45.Text = "Barometer 1h";
        // 
        // EditBarometer30mBotMinimal
        // 
        EditBarometer30mBotMinimal.DecimalPlaces = 2;
        EditBarometer30mBotMinimal.Location = new Point(130, 56);
        EditBarometer30mBotMinimal.Margin = new Padding(4, 3, 4, 3);
        EditBarometer30mBotMinimal.Name = "EditBarometer30mBotMinimal";
        EditBarometer30mBotMinimal.Size = new Size(88, 23);
        EditBarometer30mBotMinimal.TabIndex = 176;
        EditBarometer30mBotMinimal.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // groupBox1
        // 
        groupBox1.Controls.Add(EditMonitorInterval1h);
        groupBox1.Controls.Add(EditMonitorInterval2h);
        groupBox1.Controls.Add(EditMonitorInterval4h);
        groupBox1.Controls.Add(EditMonitorInterval1m);
        groupBox1.Controls.Add(EditMonitorInterval2m);
        groupBox1.Controls.Add(EditMonitorInterval3m);
        groupBox1.Controls.Add(EditMonitorInterval5m);
        groupBox1.Controls.Add(EditMonitorInterval10m);
        groupBox1.Controls.Add(EditMonitorInterval15m);
        groupBox1.Controls.Add(EditMonitorInterval30m);
        groupBox1.Location = new Point(395, 218);
        groupBox1.Margin = new Padding(4, 3, 4, 3);
        groupBox1.Name = "groupBox1";
        groupBox1.Padding = new Padding(4, 3, 4, 3);
        groupBox1.Size = new Size(234, 218);
        groupBox1.TabIndex = 242;
        groupBox1.TabStop = false;
        groupBox1.Text = "Trade on interval";
        // 
        // EditMonitorInterval1h
        // 
        EditMonitorInterval1h.AutoSize = true;
        EditMonitorInterval1h.Location = new Point(118, 27);
        EditMonitorInterval1h.Margin = new Padding(4, 3, 4, 3);
        EditMonitorInterval1h.Name = "EditMonitorInterval1h";
        EditMonitorInterval1h.Size = new Size(53, 19);
        EditMonitorInterval1h.TabIndex = 150;
        EditMonitorInterval1h.Text = "1 uur";
        EditMonitorInterval1h.UseVisualStyleBackColor = true;
        // 
        // EditMonitorInterval2h
        // 
        EditMonitorInterval2h.AutoSize = true;
        EditMonitorInterval2h.Location = new Point(118, 55);
        EditMonitorInterval2h.Margin = new Padding(4, 3, 4, 3);
        EditMonitorInterval2h.Name = "EditMonitorInterval2h";
        EditMonitorInterval2h.Size = new Size(53, 19);
        EditMonitorInterval2h.TabIndex = 151;
        EditMonitorInterval2h.Text = "2 uur";
        EditMonitorInterval2h.UseVisualStyleBackColor = true;
        // 
        // EditMonitorInterval4h
        // 
        EditMonitorInterval4h.AutoSize = true;
        EditMonitorInterval4h.Location = new Point(118, 82);
        EditMonitorInterval4h.Margin = new Padding(4, 3, 4, 3);
        EditMonitorInterval4h.Name = "EditMonitorInterval4h";
        EditMonitorInterval4h.Size = new Size(53, 19);
        EditMonitorInterval4h.TabIndex = 152;
        EditMonitorInterval4h.Text = "4 uur";
        EditMonitorInterval4h.UseVisualStyleBackColor = true;
        // 
        // EditMonitorInterval1m
        // 
        EditMonitorInterval1m.AutoSize = true;
        EditMonitorInterval1m.Location = new Point(14, 27);
        EditMonitorInterval1m.Margin = new Padding(4, 3, 4, 3);
        EditMonitorInterval1m.Name = "EditMonitorInterval1m";
        EditMonitorInterval1m.Size = new Size(56, 19);
        EditMonitorInterval1m.TabIndex = 143;
        EditMonitorInterval1m.Text = "1 min";
        EditMonitorInterval1m.UseVisualStyleBackColor = true;
        // 
        // EditMonitorInterval2m
        // 
        EditMonitorInterval2m.AutoSize = true;
        EditMonitorInterval2m.Location = new Point(14, 52);
        EditMonitorInterval2m.Margin = new Padding(4, 3, 4, 3);
        EditMonitorInterval2m.Name = "EditMonitorInterval2m";
        EditMonitorInterval2m.Size = new Size(56, 19);
        EditMonitorInterval2m.TabIndex = 144;
        EditMonitorInterval2m.Text = "2 min";
        EditMonitorInterval2m.UseVisualStyleBackColor = true;
        // 
        // EditMonitorInterval3m
        // 
        EditMonitorInterval3m.AutoSize = true;
        EditMonitorInterval3m.Location = new Point(14, 79);
        EditMonitorInterval3m.Margin = new Padding(4, 3, 4, 3);
        EditMonitorInterval3m.Name = "EditMonitorInterval3m";
        EditMonitorInterval3m.Size = new Size(56, 19);
        EditMonitorInterval3m.TabIndex = 145;
        EditMonitorInterval3m.Text = "3 min";
        EditMonitorInterval3m.UseVisualStyleBackColor = true;
        // 
        // EditMonitorInterval5m
        // 
        EditMonitorInterval5m.AutoSize = true;
        EditMonitorInterval5m.Location = new Point(14, 105);
        EditMonitorInterval5m.Margin = new Padding(4, 3, 4, 3);
        EditMonitorInterval5m.Name = "EditMonitorInterval5m";
        EditMonitorInterval5m.Size = new Size(56, 19);
        EditMonitorInterval5m.TabIndex = 146;
        EditMonitorInterval5m.Text = "5 min";
        EditMonitorInterval5m.UseVisualStyleBackColor = true;
        // 
        // EditMonitorInterval10m
        // 
        EditMonitorInterval10m.AutoSize = true;
        EditMonitorInterval10m.Location = new Point(14, 132);
        EditMonitorInterval10m.Margin = new Padding(4, 3, 4, 3);
        EditMonitorInterval10m.Name = "EditMonitorInterval10m";
        EditMonitorInterval10m.Size = new Size(62, 19);
        EditMonitorInterval10m.TabIndex = 147;
        EditMonitorInterval10m.Text = "10 min";
        EditMonitorInterval10m.UseVisualStyleBackColor = true;
        // 
        // EditMonitorInterval15m
        // 
        EditMonitorInterval15m.AutoSize = true;
        EditMonitorInterval15m.Location = new Point(14, 158);
        EditMonitorInterval15m.Margin = new Padding(4, 3, 4, 3);
        EditMonitorInterval15m.Name = "EditMonitorInterval15m";
        EditMonitorInterval15m.Size = new Size(62, 19);
        EditMonitorInterval15m.TabIndex = 148;
        EditMonitorInterval15m.Text = "15 min";
        EditMonitorInterval15m.UseVisualStyleBackColor = true;
        // 
        // EditMonitorInterval30m
        // 
        EditMonitorInterval30m.AutoSize = true;
        EditMonitorInterval30m.Location = new Point(14, 185);
        EditMonitorInterval30m.Margin = new Padding(4, 3, 4, 3);
        EditMonitorInterval30m.Name = "EditMonitorInterval30m";
        EditMonitorInterval30m.Size = new Size(62, 19);
        EditMonitorInterval30m.TabIndex = 149;
        EditMonitorInterval30m.Text = "30 min";
        EditMonitorInterval30m.UseVisualStyleBackColor = true;
        // 
        // label62
        // 
        label62.AutoSize = true;
        label62.Location = new Point(17, 200);
        label62.Margin = new Padding(4, 0, 4, 0);
        label62.Name = "label62";
        label62.Size = new Size(86, 15);
        label62.TabIndex = 214;
        label62.Text = "Koop methode";
        // 
        // EditBuyOrderMethod
        // 
        EditBuyOrderMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        EditBuyOrderMethod.FormattingEnabled = true;
        EditBuyOrderMethod.Location = new Point(185, 197);
        EditBuyOrderMethod.Margin = new Padding(4, 3, 4, 3);
        EditBuyOrderMethod.Name = "EditBuyOrderMethod";
        EditBuyOrderMethod.Size = new Size(200, 23);
        EditBuyOrderMethod.TabIndex = 213;
        // 
        // EditDcaCount
        // 
        EditDcaCount.Enabled = false;
        EditDcaCount.Font = new Font("Segoe UI", 9F, FontStyle.Strikeout, GraphicsUnit.Point);
        EditDcaCount.Location = new Point(187, 419);
        EditDcaCount.Margin = new Padding(4, 3, 4, 3);
        EditDcaCount.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        EditDcaCount.Name = "EditDcaCount";
        EditDcaCount.Size = new Size(88, 23);
        EditDcaCount.TabIndex = 206;
        EditDcaCount.Value = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // label67
        // 
        label67.AutoSize = true;
        label67.Font = new Font("Segoe UI", 9F, FontStyle.Strikeout, GraphicsUnit.Point);
        label67.Location = new Point(17, 421);
        label67.Margin = new Padding(4, 0, 4, 0);
        label67.Name = "label67";
        label67.Size = new Size(90, 15);
        label67.TabIndex = 207;
        label67.Text = "Aantal bijkopen";
        // 
        // label68
        // 
        label68.AutoSize = true;
        label68.Location = new Point(17, 395);
        label68.Margin = new Padding(4, 0, 4, 0);
        label68.Name = "label68";
        label68.Size = new Size(81, 15);
        label68.TabIndex = 202;
        label68.Text = "Bijkoop factor";
        // 
        // EditDcaFactor
        // 
        EditDcaFactor.DecimalPlaces = 2;
        EditDcaFactor.Location = new Point(185, 394);
        EditDcaFactor.Margin = new Padding(4, 3, 4, 3);
        EditDcaFactor.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        EditDcaFactor.Name = "EditDcaFactor";
        EditDcaFactor.Size = new Size(88, 23);
        EditDcaFactor.TabIndex = 203;
        EditDcaFactor.Value = new decimal(new int[] { 10, 0, 0, 0 });
        // 
        // label69
        // 
        label69.AutoSize = true;
        label69.Location = new Point(17, 368);
        label69.Margin = new Padding(4, 0, 4, 0);
        label69.Name = "label69";
        label69.Size = new Size(91, 15);
        label69.TabIndex = 205;
        label69.Text = "Bijkopen op (%)";
        // 
        // EditDcaPercentage
        // 
        EditDcaPercentage.DecimalPlaces = 2;
        EditDcaPercentage.Location = new Point(185, 365);
        EditDcaPercentage.Margin = new Padding(4, 3, 4, 3);
        EditDcaPercentage.Name = "EditDcaPercentage";
        EditDcaPercentage.Size = new Size(88, 23);
        EditDcaPercentage.TabIndex = 204;
        EditDcaPercentage.Value = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // EditGlobalStopLimitPercentage
        // 
        EditGlobalStopLimitPercentage.DecimalPlaces = 2;
        EditGlobalStopLimitPercentage.Location = new Point(185, 668);
        EditGlobalStopLimitPercentage.Margin = new Padding(4, 3, 4, 3);
        EditGlobalStopLimitPercentage.Name = "EditGlobalStopLimitPercentage";
        EditGlobalStopLimitPercentage.Size = new Size(88, 23);
        EditGlobalStopLimitPercentage.TabIndex = 200;
        // 
        // label70
        // 
        label70.AutoSize = true;
        label70.Location = new Point(17, 670);
        label70.Margin = new Padding(4, 0, 4, 0);
        label70.Name = "label70";
        label70.Size = new Size(107, 15);
        label70.TabIndex = 201;
        label70.Text = "OCO stop limit (%)";
        // 
        // EditGlobalStopPercentage
        // 
        EditGlobalStopPercentage.DecimalPlaces = 2;
        EditGlobalStopPercentage.Location = new Point(185, 639);
        EditGlobalStopPercentage.Margin = new Padding(4, 3, 4, 3);
        EditGlobalStopPercentage.Name = "EditGlobalStopPercentage";
        EditGlobalStopPercentage.Size = new Size(88, 23);
        EditGlobalStopPercentage.TabIndex = 198;
        // 
        // label71
        // 
        label71.AutoSize = true;
        label71.Location = new Point(17, 641);
        label71.Margin = new Padding(4, 0, 4, 0);
        label71.Name = "label71";
        label71.Size = new Size(109, 15);
        label71.TabIndex = 199;
        label71.Text = "OCO stop price (%)";
        // 
        // label72
        // 
        label72.AutoSize = true;
        label72.Location = new Point(17, 542);
        label72.Margin = new Padding(4, 0, 4, 0);
        label72.Name = "label72";
        label72.Size = new Size(120, 15);
        label72.TabIndex = 194;
        label72.Text = "Winst percentage (%)";
        // 
        // EditProfitPercentage
        // 
        EditProfitPercentage.DecimalPlaces = 2;
        EditProfitPercentage.Location = new Point(185, 540);
        EditProfitPercentage.Margin = new Padding(4, 3, 4, 3);
        EditProfitPercentage.Name = "EditProfitPercentage";
        EditProfitPercentage.Size = new Size(88, 23);
        EditProfitPercentage.TabIndex = 195;
        EditProfitPercentage.Value = new decimal(new int[] { 75, 0, 0, 131072 });
        // 
        // label73
        // 
        label73.AutoSize = true;
        label73.Location = new Point(17, 448);
        label73.Margin = new Padding(4, 0, 4, 0);
        label73.Name = "label73";
        label73.Size = new Size(114, 15);
        label73.TabIndex = 197;
        label73.Text = "Cool down time (m)";
        // 
        // EditGlobalBuyCooldownTime
        // 
        EditGlobalBuyCooldownTime.Location = new Point(185, 446);
        EditGlobalBuyCooldownTime.Margin = new Padding(4, 3, 4, 3);
        EditGlobalBuyCooldownTime.Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 });
        EditGlobalBuyCooldownTime.Name = "EditGlobalBuyCooldownTime";
        EditGlobalBuyCooldownTime.Size = new Size(88, 23);
        EditGlobalBuyCooldownTime.TabIndex = 196;
        // 
        // EditGlobalBuyVarying
        // 
        EditGlobalBuyVarying.DecimalPlaces = 2;
        EditGlobalBuyVarying.Location = new Point(185, 225);
        EditGlobalBuyVarying.Margin = new Padding(4, 3, 4, 3);
        EditGlobalBuyVarying.Maximum = new decimal(new int[] { 5, 0, 0, 65536 });
        EditGlobalBuyVarying.Minimum = new decimal(new int[] { 5, 0, 0, -2147418112 });
        EditGlobalBuyVarying.Name = "EditGlobalBuyVarying";
        EditGlobalBuyVarying.Size = new Size(88, 23);
        EditGlobalBuyVarying.TabIndex = 178;
        EditGlobalBuyVarying.Value = new decimal(new int[] { 1, 0, 0, -2147418112 });
        // 
        // label47
        // 
        label47.AutoSize = true;
        label47.Location = new Point(17, 227);
        label47.Margin = new Padding(4, 0, 4, 0);
        label47.Name = "label47";
        label47.Size = new Size(108, 15);
        label47.TabIndex = 177;
        label47.Text = "Instap verlagen (%)";
        // 
        // label46
        // 
        label46.AutoSize = true;
        label46.Location = new Point(17, 251);
        label46.Margin = new Padding(4, 0, 4, 0);
        label46.Name = "label46";
        label46.Size = new Size(77, 15);
        label46.TabIndex = 176;
        label46.Text = "Remove time";
        // 
        // EditGlobalBuyRemoveTime
        // 
        EditGlobalBuyRemoveTime.Location = new Point(185, 249);
        EditGlobalBuyRemoveTime.Margin = new Padding(4, 3, 4, 3);
        EditGlobalBuyRemoveTime.Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 });
        EditGlobalBuyRemoveTime.Name = "EditGlobalBuyRemoveTime";
        EditGlobalBuyRemoveTime.Size = new Size(88, 23);
        EditGlobalBuyRemoveTime.TabIndex = 175;
        EditGlobalBuyRemoveTime.Value = new decimal(new int[] { 5, 0, 0, 0 });
        // 
        // tabWhiteListOversold
        // 
        tabWhiteListOversold.Controls.Add(textBoxWhiteListOversold);
        tabWhiteListOversold.Controls.Add(panel3);
        tabWhiteListOversold.Location = new Point(4, 27);
        tabWhiteListOversold.Margin = new Padding(4, 3, 4, 3);
        tabWhiteListOversold.Name = "tabWhiteListOversold";
        tabWhiteListOversold.Padding = new Padding(4, 3, 4, 3);
        tabWhiteListOversold.Size = new Size(1232, 777);
        tabWhiteListOversold.TabIndex = 3;
        tabWhiteListOversold.Text = "WhiteList long";
        tabWhiteListOversold.UseVisualStyleBackColor = true;
        // 
        // textBoxWhiteListOversold
        // 
        textBoxWhiteListOversold.Dock = DockStyle.Fill;
        textBoxWhiteListOversold.Location = new Point(4, 60);
        textBoxWhiteListOversold.Margin = new Padding(4, 3, 4, 3);
        textBoxWhiteListOversold.Multiline = true;
        textBoxWhiteListOversold.Name = "textBoxWhiteListOversold";
        textBoxWhiteListOversold.Size = new Size(1224, 714);
        textBoxWhiteListOversold.TabIndex = 0;
        // 
        // panel3
        // 
        panel3.Controls.Add(label55);
        panel3.Dock = DockStyle.Top;
        panel3.Location = new Point(4, 3);
        panel3.Margin = new Padding(4, 3, 4, 3);
        panel3.Name = "panel3";
        panel3.Size = new Size(1224, 57);
        panel3.TabIndex = 1;
        // 
        // label55
        // 
        label55.AutoSize = true;
        label55.Location = new Point(20, 36);
        label55.Margin = new Padding(4, 0, 4, 0);
        label55.Name = "label55";
        label55.Size = new Size(308, 15);
        label55.TabIndex = 222;
        label55.Text = "(1 munt per regel met een optionele opmerking erachter)";
        // 
        // tabBlackListOversold
        // 
        tabBlackListOversold.Controls.Add(textBoxBlackListOversold);
        tabBlackListOversold.Controls.Add(panel4);
        tabBlackListOversold.Location = new Point(4, 27);
        tabBlackListOversold.Margin = new Padding(4, 3, 4, 3);
        tabBlackListOversold.Name = "tabBlackListOversold";
        tabBlackListOversold.Padding = new Padding(4, 3, 4, 3);
        tabBlackListOversold.Size = new Size(1232, 777);
        tabBlackListOversold.TabIndex = 4;
        tabBlackListOversold.Text = "Blacklist long";
        tabBlackListOversold.UseVisualStyleBackColor = true;
        // 
        // textBoxBlackListOversold
        // 
        textBoxBlackListOversold.Dock = DockStyle.Fill;
        textBoxBlackListOversold.Location = new Point(4, 60);
        textBoxBlackListOversold.Margin = new Padding(4, 3, 4, 3);
        textBoxBlackListOversold.Multiline = true;
        textBoxBlackListOversold.Name = "textBoxBlackListOversold";
        textBoxBlackListOversold.Size = new Size(1224, 714);
        textBoxBlackListOversold.TabIndex = 1;
        // 
        // panel4
        // 
        panel4.Controls.Add(label51);
        panel4.Dock = DockStyle.Top;
        panel4.Location = new Point(4, 3);
        panel4.Margin = new Padding(4, 3, 4, 3);
        panel4.Name = "panel4";
        panel4.Size = new Size(1224, 57);
        panel4.TabIndex = 2;
        // 
        // label51
        // 
        label51.AutoSize = true;
        label51.Location = new Point(20, 36);
        label51.Margin = new Padding(4, 0, 4, 0);
        label51.Name = "label51";
        label51.Size = new Size(308, 15);
        label51.TabIndex = 222;
        label51.Text = "(1 munt per regel met een optionele opmerking erachter)";
        // 
        // tabWhiteListOverbought
        // 
        tabWhiteListOverbought.Controls.Add(textBoxWhiteListOverbought);
        tabWhiteListOverbought.Controls.Add(panel5);
        tabWhiteListOverbought.Location = new Point(4, 27);
        tabWhiteListOverbought.Margin = new Padding(4, 3, 4, 3);
        tabWhiteListOverbought.Name = "tabWhiteListOverbought";
        tabWhiteListOverbought.Padding = new Padding(4, 3, 4, 3);
        tabWhiteListOverbought.Size = new Size(1232, 777);
        tabWhiteListOverbought.TabIndex = 7;
        tabWhiteListOverbought.Text = "Whitelist short";
        tabWhiteListOverbought.UseVisualStyleBackColor = true;
        // 
        // textBoxWhiteListOverbought
        // 
        textBoxWhiteListOverbought.Dock = DockStyle.Fill;
        textBoxWhiteListOverbought.Location = new Point(4, 60);
        textBoxWhiteListOverbought.Margin = new Padding(4, 3, 4, 3);
        textBoxWhiteListOverbought.Multiline = true;
        textBoxWhiteListOverbought.Name = "textBoxWhiteListOverbought";
        textBoxWhiteListOverbought.Size = new Size(1224, 714);
        textBoxWhiteListOverbought.TabIndex = 2;
        // 
        // panel5
        // 
        panel5.Controls.Add(label29);
        panel5.Dock = DockStyle.Top;
        panel5.Location = new Point(4, 3);
        panel5.Margin = new Padding(4, 3, 4, 3);
        panel5.Name = "panel5";
        panel5.Size = new Size(1224, 57);
        panel5.TabIndex = 3;
        // 
        // label29
        // 
        label29.AutoSize = true;
        label29.Location = new Point(20, 36);
        label29.Margin = new Padding(4, 0, 4, 0);
        label29.Name = "label29";
        label29.Size = new Size(308, 15);
        label29.TabIndex = 221;
        label29.Text = "(1 munt per regel met een optionele opmerking erachter)";
        // 
        // tabBlacklistOverbought
        // 
        tabBlacklistOverbought.Controls.Add(textBoxBlackListOverbought);
        tabBlacklistOverbought.Controls.Add(panel6);
        tabBlacklistOverbought.Location = new Point(4, 27);
        tabBlacklistOverbought.Margin = new Padding(4, 3, 4, 3);
        tabBlacklistOverbought.Name = "tabBlacklistOverbought";
        tabBlacklistOverbought.Padding = new Padding(4, 3, 4, 3);
        tabBlacklistOverbought.Size = new Size(1232, 777);
        tabBlacklistOverbought.TabIndex = 8;
        tabBlacklistOverbought.Text = "Blacklist short";
        tabBlacklistOverbought.UseVisualStyleBackColor = true;
        // 
        // textBoxBlackListOverbought
        // 
        textBoxBlackListOverbought.Dock = DockStyle.Fill;
        textBoxBlackListOverbought.Location = new Point(4, 60);
        textBoxBlackListOverbought.Margin = new Padding(4, 3, 4, 3);
        textBoxBlackListOverbought.Multiline = true;
        textBoxBlackListOverbought.Name = "textBoxBlackListOverbought";
        textBoxBlackListOverbought.Size = new Size(1224, 714);
        textBoxBlackListOverbought.TabIndex = 3;
        // 
        // panel6
        // 
        panel6.Controls.Add(label49);
        panel6.Dock = DockStyle.Top;
        panel6.Location = new Point(4, 3);
        panel6.Margin = new Padding(4, 3, 4, 3);
        panel6.Name = "panel6";
        panel6.Size = new Size(1224, 57);
        panel6.TabIndex = 4;
        // 
        // label49
        // 
        label49.AutoSize = true;
        label49.Location = new Point(20, 36);
        label49.Margin = new Padding(4, 0, 4, 0);
        label49.Name = "label49";
        label49.Size = new Size(308, 15);
        label49.TabIndex = 222;
        label49.Text = "(1 munt per regel met een optionele opmerking erachter)";
        // 
        // imageList1
        // 
        imageList1.ColorDepth = ColorDepth.Depth8Bit;
        imageList1.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
        imageList1.TransparentColor = Color.Transparent;
        imageList1.Images.SetKeyName(0, "volume.png");
        // 
        // FrmSettings
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1240, 808);
        Controls.Add(panel2);
        Controls.Add(panel1);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Margin = new Padding(4, 3, 4, 3);
        Name = "FrmSettings";
        Text = "Instellingen";
        panel2.ResumeLayout(false);
        panel1.ResumeLayout(false);
        tabControl.ResumeLayout(false);
        tabAlgemeen.ResumeLayout(false);
        tabAlgemeen.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditGetCandleInterval).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalDataRemoveSignalAfterxCandles).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSoundHeartBeatMinutes).EndInit();
        tabTelegram.ResumeLayout(false);
        tabTelegram.PerformLayout();
        tabPageSignals.ResumeLayout(false);
        tabPageSignals.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMinEffectivePercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMaxEffectivePercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditCandlesWithZeroVolume).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditCandlesWithFlatPrice).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumAboveBollingerBandsUpper).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumAboveBollingerBandsSma).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumTickPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMinChangePercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMaxChangePercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSymbolMustExistsDays).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer1hMinimal).EndInit();
        groupBoxInterval.ResumeLayout(false);
        groupBoxInterval.PerformLayout();
        tabSignalStobb.ResumeLayout(false);
        tabSignalStobb.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditStobMinimalTrend).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditStobbBBMinPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditStobbBBMaxPercentage).EndInit();
        tabSignalSbm.ResumeLayout(false);
        tabSignalSbm.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditSbm1CandlesLookbackCount).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmCandlesForMacdRecovery).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmBBMinPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmBBMaxPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa20Percentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa50AndMa20Percentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa50Percentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa50AndMa20Lookback).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa50Lookback).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa20Lookback).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm2BbPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm3CandlesForBBRecovery).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm3CandlesForBBRecoveryPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm2CandlesLookbackCount).EndInit();
        tabSignalJump.ResumeLayout(false);
        tabSignalJump.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditJumpCandlesLookbackCount).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisCandleJumpPercentage).EndInit();
        tabPageTrading.ResumeLayout(false);
        tabPageTrading.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditLeverage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditDynamicTpPercentage).EndInit();
        groupBoxSlots.ResumeLayout(false);
        groupBoxSlots.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalExchange).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalSymbol).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalBase).EndInit();
        groupBox2.ResumeLayout(false);
        groupBox2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditBarometer15mBotMinimal).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer24hBotMinimal).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer04hBotMinimal).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer01hBotMinimal).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer30mBotMinimal).EndInit();
        groupBox1.ResumeLayout(false);
        groupBox1.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditDcaCount).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditDcaFactor).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditDcaPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalStopLimitPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalStopPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditProfitPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyCooldownTime).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyVarying).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyRemoveTime).EndInit();
        tabWhiteListOversold.ResumeLayout(false);
        tabWhiteListOversold.PerformLayout();
        panel3.ResumeLayout(false);
        panel3.PerformLayout();
        tabBlackListOversold.ResumeLayout(false);
        tabBlackListOversold.PerformLayout();
        panel4.ResumeLayout(false);
        panel4.PerformLayout();
        tabWhiteListOverbought.ResumeLayout(false);
        tabWhiteListOverbought.PerformLayout();
        panel5.ResumeLayout(false);
        panel5.PerformLayout();
        tabBlacklistOverbought.ResumeLayout(false);
        tabBlacklistOverbought.PerformLayout();
        panel6.ResumeLayout(false);
        panel6.PerformLayout();
        ResumeLayout(false);
    }

    #endregion
    private Panel panel2;
    private Button buttonCancel;
    private Button buttonOk;
    private Panel panel1;
    private ToolTip toolTip1;
    private Button buttonTestSpeech;
    private Button buttonReset;
    private ImageList imageList1;
    private ColorDialog colorDialog1;
    private TabControl tabControl;
    private TabPage tabAlgemeen;
    private Label label16;
    private NumericUpDown EditGetCandleInterval;
    private Label label40;
    private ComboBox EditTrendCalculationMethod;
    private Label label6;
    private NumericUpDown EditGlobalDataRemoveSignalAfterxCandles;
    private CheckBox EditBlackTheming;
    private Button buttonFontDialog;
    private Label label18;
    private NumericUpDown EditSoundHeartBeatMinutes;
    private Label label2;
    private ComboBox EditTradingApp;
    private TabPage tabTelegram;
    private CheckBox EditSendSignalsToTelegram;
    private Button ButtonTestTelegram;
    private Label label24;
    private TextBox EditTelegramChatId;
    private TextBox EditTelegramToken;
    private Label label15;
    private TabPage tabBasismunten;
    private TabPage tabPageSignals;
    private Label label64;
    private NumericUpDown EditAnalysisMinEffectivePercentage;
    private NumericUpDown EditAnalysisMaxEffectivePercentage;
    private CheckBox EditLogAnalysisMinMaxEffectivePercentage;
    private Label label79;
    private Label label48;
    private Label label38;
    private Label label37;
    private Label label10;
    private CheckBox EditCandlesWithFlatPriceCheck;
    private CheckBox EditCandlesWithZeroVolumeCheck;
    private CheckBox EditMinimumAboveBollingerBandsSmaCheck;
    private CheckBox EditMinimumAboveBollingerBandsUpperCheck;
    private NumericUpDown EditCandlesWithZeroVolume;
    private NumericUpDown EditCandlesWithFlatPrice;
    private NumericUpDown EditMinimumAboveBollingerBandsUpper;
    private NumericUpDown EditMinimumAboveBollingerBandsSma;
    private Label label26;
    private CheckBox EditLogMinimumTickPercentage;
    private NumericUpDown EditMinimumTickPercentage;
    private Label label61;
    private Label label53;
    private NumericUpDown EditAnalysisMinChangePercentage;
    private NumericUpDown EditAnalysisMaxChangePercentage;
    private CheckBox EditLogBarometerToLow;
    private CheckBox EditLogSymbolMustExistsDays;
    private NumericUpDown EditSymbolMustExistsDays;
    private Label label25;
    private Label label35;
    private NumericUpDown EditBarometer1hMinimal;
    private CheckBox EditLogAnalysisMinMaxChangePercentage;
    private GroupBox groupBoxInterval;
    private CheckBox EditAnalyzeInterval6h;
    private CheckBox EditAnalyzeInterval8h;
    private CheckBox EditAnalyzeInterval12h;
    private CheckBox EditAnalyzeInterval1d;
    private CheckBox EditAnalyzeInterval5m;
    private CheckBox EditAnalyzeInterval1m;
    private CheckBox EditAnalyzeInterval2m;
    private CheckBox EditAnalyzeInterval3m;
    private CheckBox EditAnalyzeInterval10m;
    private CheckBox EditAnalyzeInterval15m;
    private CheckBox EditAnalyzeInterval30m;
    private CheckBox EditAnalyzeInterval1h;
    private CheckBox EditAnalyzeInterval2h;
    private CheckBox EditAnalyzeInterval4h;
    private TabPage tabSignalStobb;
    private Label label66;
    private NumericUpDown EditStobMinimalTrend;
    private Label label77;
    private Label label75;
    private CheckBox EditStobIncludeSbmPercAndCrossing;
    private Label label30;
    private Label label28;
    private Button buttonColorStobb;
    private CheckBox EditStobIncludeSbmMaLines;
    private CheckBox EditStobIncludeRsi;
    private Button buttonPlaySoundStobbOversold;
    private Button buttonPlaySoundStobbOverbought;
    private Button buttonSelectSoundStobbOversold;
    private Panel panelColorStobb;
    private TextBox EditSoundStobbOversold;
    private TextBox EditSoundStobbOverbought;
    private Button buttonSelectSoundStobbOverbought;
    private CheckBox EditPlaySpeechStobbSignal;
    private CheckBox EditPlaySoundStobbSignal;
    private CheckBox EditAnalyzeStobbOversold;
    private CheckBox EditAnalyzeStobbOverbought;
    private CheckBox EditStobbUseLowHigh;
    private Label label1;
    private NumericUpDown EditStobbBBMinPercentage;
    private NumericUpDown EditStobbBBMaxPercentage;
    private TabPage tabSignalSbm;
    private CheckBox EditSbm2UseLowHigh;
    private Label label21;
    private Label label20;
    private Label label9;
    private Label label41;
    private NumericUpDown EditSbm1CandlesLookbackCount;
    private Label label39;
    private NumericUpDown EditSbmCandlesForMacdRecovery;
    private Label label31;
    private Label label32;
    private Button buttonPlaySoundSbmOversold;
    private Button buttonPlaySoundSbmOverbought;
    private Button buttonSelectSoundSbmOversold;
    private TextBox EditSoundFileSbmOversold;
    private TextBox EditSoundFileSbmOverbought;
    private Button buttonSelectSoundSbmOverbought;
    private CheckBox EditPlaySpeechSbmSignal;
    private CheckBox EditAnalyzeSbmOversold;
    private CheckBox EditAnalyzeSbmOverbought;
    private CheckBox EditPlaySoundSbmSignal;
    private Button buttonColorSbm;
    private CheckBox EditSbmUseLowHigh;
    private Label label17;
    private NumericUpDown EditSbmBBMinPercentage;
    private NumericUpDown EditSbmBBMaxPercentage;
    private Label label22;
    private Label label4;
    private NumericUpDown EditSbmMa200AndMa20Percentage;
    private Label label8;
    private NumericUpDown EditSbmMa50AndMa20Percentage;
    private Label label7;
    private NumericUpDown EditSbmMa200AndMa50Percentage;
    private NumericUpDown EditSbmMa50AndMa20Lookback;
    private CheckBox EditSbmMa50AndMa20Crossing;
    private NumericUpDown EditSbmMa200AndMa50Lookback;
    private CheckBox EditSbmMa200AndMa50Crossing;
    private NumericUpDown EditSbmMa200AndMa20Lookback;
    private CheckBox EditSbmMa200AndMa20Crossing;
    private CheckBox EditAnalyzeSbm3Overbought;
    private CheckBox EditAnalyzeSbm2Overbought;
    private Label label12;
    private NumericUpDown EditSbm2BbPercentage;
    private Panel panelColorSbm;
    private Label label13;
    private NumericUpDown EditSbm3CandlesForBBRecovery;
    private Label label14;
    private NumericUpDown EditSbm3CandlesForBBRecoveryPercentage;
    private Label label11;
    private NumericUpDown EditSbm2CandlesLookbackCount;
    private CheckBox EditAnalyzeSbm3Oversold;
    private CheckBox EditAnalyzeSbm2Oversold;
    private TabPage tabSignalJump;
    private Label label78;
    private Label label76;
    private Label label33;
    private Label label34;
    private Label label5;
    private NumericUpDown EditJumpCandlesLookbackCount;
    private CheckBox EditJumpUseLowHighCalculation;
    private Button buttonColorJump;
    private Button buttonPlaySoundCandleJumpDown;
    private Button buttonPlaySoundCandleJumpUp;
    private Button buttonSelectSoundCandleJumpDown;
    private Panel panelColorJump;
    private TextBox EditSoundFileCandleJumpDown;
    private TextBox EditSoundFileCandleJumpUp;
    private Button buttonSelectSoundCandleJumpUp;
    private CheckBox EditPlaySpeechCandleJumpSignal;
    private Label label3;
    private CheckBox EditPlaySoundCandleJumpSignal;
    private CheckBox EditAnalyzeCandleJumpUp;
    private CheckBox EditAnalyzeCandleJumpDown;
    private NumericUpDown EditAnalysisCandleJumpPercentage;
    private TabPage tabWhiteListOversold;
    private TextBox textBoxWhiteListOversold;
    private Panel panel3;
    private Label label55;
    private TabPage tabBlackListOversold;
    private TextBox textBoxBlackListOversold;
    private Panel panel4;
    private Label label51;
    private TabPage tabWhiteListOverbought;
    private TextBox textBoxWhiteListOverbought;
    private Panel panel5;
    private Label label29;
    private TabPage tabBlacklistOverbought;
    private TextBox textBoxBlackListOverbought;
    private Panel panel6;
    private Label label49;
    private TabPage tabPageTrading;
    private Label label83;
    private ComboBox EditBuyStepInMethod;
    private Label label82;
    private ComboBox EditDcaStepInMethod;
    private Label label65;
    private NumericUpDown EditDynamicTpPercentage;
    private CheckBox EditTradeViaBinance;
    private Label label63;
    private ComboBox EditSellMethod;
    private CheckBox EditTradeViaPaperTrading;
    private Label label60;
    private ComboBox EditDcaOrderMethod;
    private Label label36;
    private Label label81;
    private Label label57;
    private Label label54;
    private GroupBox groupBoxSlots;
    private Label label50;
    private NumericUpDown EditSlotsMaximalExchange;
    private Label label52;
    private NumericUpDown EditSlotsMaximalSymbol;
    private Label label56;
    private NumericUpDown EditSlotsMaximalBase;
    private GroupBox groupBox2;
    private NumericUpDown EditBarometer15mBotMinimal;
    private Label label27;
    private NumericUpDown EditBarometer24hBotMinimal;
    private Label label42;
    private NumericUpDown EditBarometer04hBotMinimal;
    private Label label43;
    private NumericUpDown EditBarometer01hBotMinimal;
    private Label label44;
    private Label label45;
    private NumericUpDown EditBarometer30mBotMinimal;
    private GroupBox groupBox1;
    private CheckBox EditMonitorInterval1h;
    private CheckBox EditMonitorInterval2h;
    private CheckBox EditMonitorInterval4h;
    private CheckBox EditMonitorInterval1m;
    private CheckBox EditMonitorInterval2m;
    private CheckBox EditMonitorInterval3m;
    private CheckBox EditMonitorInterval5m;
    private CheckBox EditMonitorInterval10m;
    private CheckBox EditMonitorInterval15m;
    private CheckBox EditMonitorInterval30m;
    private Label label62;
    private ComboBox EditBuyOrderMethod;
    private NumericUpDown EditDcaCount;
    private Label label67;
    private Label label68;
    private NumericUpDown EditDcaFactor;
    private Label label69;
    private NumericUpDown EditDcaPercentage;
    private NumericUpDown EditGlobalStopLimitPercentage;
    private Label label70;
    private NumericUpDown EditGlobalStopPercentage;
    private Label label71;
    private Label label72;
    private NumericUpDown EditProfitPercentage;
    private Label label73;
    private NumericUpDown EditGlobalBuyCooldownTime;
    private NumericUpDown EditGlobalBuyVarying;
    private Label label47;
    private Label label46;
    private NumericUpDown EditGlobalBuyRemoveTime;
    private Label label84;
    private ComboBox EditExchange;
    private CheckBox EditShowInvalidSignals;
    private CheckBox EditDisableNewPositions;
    private CheckBox EditSoundTradeNotification;
    private Label label58;
    private ComboBox EditActivateExchange;
    private Button buttonTelegramStart;
    private Button buttonGotoAppDataFolder;
    private CheckBox EditHideSymbolsOnTheLeft;
    private CheckBox EditLogCanceledOrders;
    private Label label19;
    private ComboBox EditMargin;
    private Label label23;
    private NumericUpDown EditLeverage;
}
