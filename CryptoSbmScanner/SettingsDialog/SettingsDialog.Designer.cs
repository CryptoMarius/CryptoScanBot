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
        groupBoxStoch = new GroupBox();
        EditStochValueOversold = new NumericUpDown();
        label88 = new Label();
        label89 = new Label();
        EditStochValueOverbought = new NumericUpDown();
        groupBoxRsi = new GroupBox();
        EditRsiValueOversold = new NumericUpDown();
        label87 = new Label();
        label90 = new Label();
        EditRsiValueOverbought = new NumericUpDown();
        EditExtraCaption = new TextBox();
        label74 = new Label();
        EditHideSymbolsOnTheLeft = new CheckBox();
        label58 = new Label();
        EditActivateExchange = new ComboBox();
        EditShowInvalidSignals = new CheckBox();
        label84 = new Label();
        EditExchange = new ComboBox();
        label16 = new Label();
        EditGetCandleInterval = new NumericUpDown();
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
        label86 = new Label();
        EditAnalysisMinEffective10DaysPercentage = new NumericUpDown();
        EditAnalysisMaxEffective10DaysPercentage = new NumericUpDown();
        EditLogAnalysisMinMaxEffective10DaysPercentage = new CheckBox();
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
        buttonColorStobbLong = new Button();
        panelColorStobbLong = new Panel();
        label85 = new Label();
        EditStobTrendShort = new NumericUpDown();
        label66 = new Label();
        EditStobTrendLong = new NumericUpDown();
        label77 = new Label();
        label75 = new Label();
        EditStobIncludeSbmPercAndCrossing = new CheckBox();
        label30 = new Label();
        label28 = new Label();
        buttonColorStobbShort = new Button();
        EditStobIncludeSbmMaLines = new CheckBox();
        EditStobIncludeRsi = new CheckBox();
        buttonPlaySoundStobbOversold = new Button();
        buttonPlaySoundStobbOverbought = new Button();
        buttonSelectSoundStobbOversold = new Button();
        panelColorStobbShort = new Panel();
        EditSoundStobbOversold = new TextBox();
        EditSoundStobbOverbought = new TextBox();
        buttonSelectSoundStobbOverbought = new Button();
        EditPlaySpeechStobbSignal = new CheckBox();
        EditPlaySoundStobbSignal = new CheckBox();
        EditAnalyzeStobbLong = new CheckBox();
        EditAnalyzeStobbShort = new CheckBox();
        EditStobbUseLowHigh = new CheckBox();
        label1 = new Label();
        EditStobbBBMinPercentage = new NumericUpDown();
        EditStobbBBMaxPercentage = new NumericUpDown();
        tabSignalSbm = new TabPage();
        label97 = new Label();
        label96 = new Label();
        buttonColorSbmLong = new Button();
        panelColorSbmLong = new Panel();
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
        EditAnalyzeSbm1Long = new CheckBox();
        EditAnalyzeSbm1Short = new CheckBox();
        EditPlaySoundSbmSignal = new CheckBox();
        buttonColorSbmShort = new Button();
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
        EditAnalyzeSbm3Short = new CheckBox();
        EditAnalyzeSbm2Short = new CheckBox();
        label12 = new Label();
        EditSbm2BbPercentage = new NumericUpDown();
        panelColorSbmShort = new Panel();
        label13 = new Label();
        EditSbm3CandlesForBBRecovery = new NumericUpDown();
        label14 = new Label();
        EditSbm3CandlesForBBRecoveryPercentage = new NumericUpDown();
        label11 = new Label();
        EditSbm2CandlesLookbackCount = new NumericUpDown();
        EditAnalyzeSbm3Long = new CheckBox();
        EditAnalyzeSbm2Long = new CheckBox();
        tabSignalJump = new TabPage();
        buttonColorJumpLong = new Button();
        panelColorJumpLong = new Panel();
        label78 = new Label();
        label76 = new Label();
        label33 = new Label();
        label34 = new Label();
        label5 = new Label();
        EditJumpCandlesLookbackCount = new NumericUpDown();
        EditJumpUseLowHighCalculation = new CheckBox();
        buttonColorJumpShort = new Button();
        buttonPlaySoundCandleJumpDown = new Button();
        buttonPlaySoundCandleJumpUp = new Button();
        buttonSelectSoundCandleJumpDown = new Button();
        panelColorJumpShort = new Panel();
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
        groupBoxInstap = new GroupBox();
        EditCheckIncreasingMacd = new CheckBox();
        EditCheckIncreasingStoch = new CheckBox();
        EditCheckIncreasingRsi = new CheckBox();
        groupBoxFutures = new GroupBox();
        label19 = new Label();
        EditCrossOrIsolated = new ComboBox();
        label23 = new Label();
        EditLeverage = new NumericUpDown();
        EditApiSecret = new TextBox();
        EditApiKey = new TextBox();
        label80 = new Label();
        label65 = new Label();
        EditLockProfits = new CheckBox();
        label59 = new Label();
        EditLogCanceledOrders = new CheckBox();
        EditSoundTradeNotification = new CheckBox();
        EditDisableNewPositions = new CheckBox();
        label83 = new Label();
        EditBuyStepInMethod = new ComboBox();
        label82 = new Label();
        EditDcaStepInMethod = new ComboBox();
        EditTradeViaExchange = new CheckBox();
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
        tabPageLong = new TabPage();
        tabControlLong = new TabControl();
        tabPageTradingLong = new TabPage();
        groupTrendOnInterval = new GroupBox();
        EditTrendLong6h = new CheckBox();
        EditTrendLong8h = new CheckBox();
        EditTrendLong12h = new CheckBox();
        EditTrendLong1d = new CheckBox();
        EditTrendLong1h = new CheckBox();
        EditTrendLong2h = new CheckBox();
        EditTrendLong4h = new CheckBox();
        EditTrendLong1m = new CheckBox();
        EditTrendLong2m = new CheckBox();
        EditTrendLong3m = new CheckBox();
        EditTrendLong5m = new CheckBox();
        EditTrendLong10m = new CheckBox();
        EditTrendLong15m = new CheckBox();
        EditTrendLong30m = new CheckBox();
        groupBox2 = new GroupBox();
        EditBarometer15mBotLong = new NumericUpDown();
        label27 = new Label();
        EditBarometer24hBotLong = new NumericUpDown();
        label42 = new Label();
        EditBarometer4hBotLong = new NumericUpDown();
        label43 = new Label();
        EditBarometer1hBotLong = new NumericUpDown();
        label44 = new Label();
        label45 = new Label();
        EditBarometer30mBotLong = new NumericUpDown();
        groupTradeOnInterval = new GroupBox();
        EditTradingIntervalLong1h = new CheckBox();
        EditTradingIntervalLong2h = new CheckBox();
        EditTradingIntervalLong4h = new CheckBox();
        EditTradingIntervalLong1m = new CheckBox();
        EditTradingIntervalLong2m = new CheckBox();
        EditTradingIntervalLong3m = new CheckBox();
        EditTradingIntervalLong5m = new CheckBox();
        EditTradingIntervalLong10m = new CheckBox();
        EditTradingIntervalLong15m = new CheckBox();
        EditTradingIntervalLong30m = new CheckBox();
        tabPageLongWhiteList = new TabPage();
        textBoxWhiteListOversold = new TextBox();
        panel3 = new Panel();
        label55 = new Label();
        tabPageLongBlackList = new TabPage();
        textBoxBlackListOversold = new TextBox();
        panel4 = new Panel();
        label51 = new Label();
        tabPageShort = new TabPage();
        tabControlShort = new TabControl();
        tabPageTradingShort = new TabPage();
        groupBox3 = new GroupBox();
        EditTrendShort6h = new CheckBox();
        EditTrendShort8h = new CheckBox();
        EditTrendShort12h = new CheckBox();
        EditTrendShort1d = new CheckBox();
        EditTrendShort1h = new CheckBox();
        EditTrendShort2h = new CheckBox();
        EditTrendShort4h = new CheckBox();
        EditTrendShort1m = new CheckBox();
        EditTrendShort2m = new CheckBox();
        EditTrendShort3m = new CheckBox();
        EditTrendShort5m = new CheckBox();
        EditTrendShort10m = new CheckBox();
        EditTrendShort15m = new CheckBox();
        EditTrendShort30m = new CheckBox();
        groupBox4 = new GroupBox();
        EditBarometer15mBotShort = new NumericUpDown();
        label91 = new Label();
        EditBarometer24hBotShort = new NumericUpDown();
        label92 = new Label();
        EditBarometer4hBotShort = new NumericUpDown();
        label93 = new Label();
        EditBarometer1hBotShort = new NumericUpDown();
        label94 = new Label();
        label95 = new Label();
        EditBarometer30mBotShort = new NumericUpDown();
        groupBox1 = new GroupBox();
        EditTradingIntervalShort1h = new CheckBox();
        EditTradingIntervalShort2h = new CheckBox();
        EditTradingIntervalShort4h = new CheckBox();
        EditTradingIntervalShort1m = new CheckBox();
        EditTradingIntervalShort2m = new CheckBox();
        EditTradingIntervalShort3m = new CheckBox();
        EditTradingIntervalShort5m = new CheckBox();
        EditTradingIntervalShort10m = new CheckBox();
        EditTradingIntervalShort15m = new CheckBox();
        EditTradingIntervalShort30m = new CheckBox();
        tabPageShortWhiteList = new TabPage();
        textBoxWhiteListOverbought = new TextBox();
        panel5 = new Panel();
        label29 = new Label();
        tabPageShortBlackList = new TabPage();
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
        groupBoxStoch.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditStochValueOversold).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditStochValueOverbought).BeginInit();
        groupBoxRsi.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditRsiValueOversold).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditRsiValueOverbought).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGetCandleInterval).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalDataRemoveSignalAfterxCandles).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSoundHeartBeatMinutes).BeginInit();
        tabTelegram.SuspendLayout();
        tabPageSignals.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMinEffective10DaysPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMaxEffective10DaysPercentage).BeginInit();
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
        ((System.ComponentModel.ISupportInitialize)EditStobTrendShort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditStobTrendLong).BeginInit();
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
        groupBoxInstap.SuspendLayout();
        groupBoxFutures.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditLeverage).BeginInit();
        groupBoxSlots.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalExchange).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalSymbol).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalBase).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditDcaCount).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditDcaFactor).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditDcaPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalStopLimitPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalStopPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditProfitPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyCooldownTime).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyVarying).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyRemoveTime).BeginInit();
        tabPageLong.SuspendLayout();
        tabControlLong.SuspendLayout();
        tabPageTradingLong.SuspendLayout();
        groupTrendOnInterval.SuspendLayout();
        groupBox2.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditBarometer15mBotLong).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer24hBotLong).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer4hBotLong).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer1hBotLong).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer30mBotLong).BeginInit();
        groupTradeOnInterval.SuspendLayout();
        tabPageLongWhiteList.SuspendLayout();
        panel3.SuspendLayout();
        tabPageLongBlackList.SuspendLayout();
        panel4.SuspendLayout();
        tabPageShort.SuspendLayout();
        tabControlShort.SuspendLayout();
        tabPageTradingShort.SuspendLayout();
        groupBox3.SuspendLayout();
        groupBox4.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditBarometer15mBotShort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer24hBotShort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer4hBotShort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer1hBotShort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer30mBotShort).BeginInit();
        groupBox1.SuspendLayout();
        tabPageShortWhiteList.SuspendLayout();
        panel5.SuspendLayout();
        tabPageShortBlackList.SuspendLayout();
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
        tabControl.Controls.Add(tabPageLong);
        tabControl.Controls.Add(tabPageShort);
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
        tabAlgemeen.Controls.Add(groupBoxStoch);
        tabAlgemeen.Controls.Add(groupBoxRsi);
        tabAlgemeen.Controls.Add(EditExtraCaption);
        tabAlgemeen.Controls.Add(label74);
        tabAlgemeen.Controls.Add(EditHideSymbolsOnTheLeft);
        tabAlgemeen.Controls.Add(label58);
        tabAlgemeen.Controls.Add(EditActivateExchange);
        tabAlgemeen.Controls.Add(EditShowInvalidSignals);
        tabAlgemeen.Controls.Add(label84);
        tabAlgemeen.Controls.Add(EditExchange);
        tabAlgemeen.Controls.Add(label16);
        tabAlgemeen.Controls.Add(EditGetCandleInterval);
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
        // groupBoxStoch
        // 
        groupBoxStoch.Controls.Add(EditStochValueOversold);
        groupBoxStoch.Controls.Add(label88);
        groupBoxStoch.Controls.Add(label89);
        groupBoxStoch.Controls.Add(EditStochValueOverbought);
        groupBoxStoch.Location = new Point(418, 137);
        groupBoxStoch.Name = "groupBoxStoch";
        groupBoxStoch.Size = new Size(234, 96);
        groupBoxStoch.TabIndex = 245;
        groupBoxStoch.TabStop = false;
        groupBoxStoch.Text = "Stochastic";
        // 
        // EditStochValueOversold
        // 
        EditStochValueOversold.DecimalPlaces = 2;
        EditStochValueOversold.Location = new Point(130, 30);
        EditStochValueOversold.Margin = new Padding(4, 3, 4, 3);
        EditStochValueOversold.Name = "EditStochValueOversold";
        EditStochValueOversold.Size = new Size(88, 23);
        EditStochValueOversold.TabIndex = 175;
        EditStochValueOversold.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label88
        // 
        label88.AutoSize = true;
        label88.Location = new Point(7, 33);
        label88.Margin = new Padding(4, 0, 4, 0);
        label88.Name = "label88";
        label88.Size = new Size(54, 15);
        label88.TabIndex = 173;
        label88.Text = "Oversold";
        // 
        // label89
        // 
        label89.AutoSize = true;
        label89.Location = new Point(9, 58);
        label89.Margin = new Padding(4, 0, 4, 0);
        label89.Name = "label89";
        label89.Size = new Size(71, 15);
        label89.TabIndex = 174;
        label89.Text = "Overbought";
        // 
        // EditStochValueOverbought
        // 
        EditStochValueOverbought.DecimalPlaces = 2;
        EditStochValueOverbought.Location = new Point(130, 56);
        EditStochValueOverbought.Margin = new Padding(4, 3, 4, 3);
        EditStochValueOverbought.Name = "EditStochValueOverbought";
        EditStochValueOverbought.Size = new Size(88, 23);
        EditStochValueOverbought.TabIndex = 176;
        EditStochValueOverbought.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // groupBoxRsi
        // 
        groupBoxRsi.Controls.Add(EditRsiValueOversold);
        groupBoxRsi.Controls.Add(label87);
        groupBoxRsi.Controls.Add(label90);
        groupBoxRsi.Controls.Add(EditRsiValueOverbought);
        groupBoxRsi.Location = new Point(418, 34);
        groupBoxRsi.Name = "groupBoxRsi";
        groupBoxRsi.Size = new Size(234, 96);
        groupBoxRsi.TabIndex = 244;
        groupBoxRsi.TabStop = false;
        groupBoxRsi.Text = "RSI";
        // 
        // EditRsiValueOversold
        // 
        EditRsiValueOversold.DecimalPlaces = 2;
        EditRsiValueOversold.Location = new Point(130, 30);
        EditRsiValueOversold.Margin = new Padding(4, 3, 4, 3);
        EditRsiValueOversold.Name = "EditRsiValueOversold";
        EditRsiValueOversold.Size = new Size(88, 23);
        EditRsiValueOversold.TabIndex = 175;
        EditRsiValueOversold.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label87
        // 
        label87.AutoSize = true;
        label87.Location = new Point(7, 33);
        label87.Margin = new Padding(4, 0, 4, 0);
        label87.Name = "label87";
        label87.Size = new Size(54, 15);
        label87.TabIndex = 173;
        label87.Text = "Oversold";
        // 
        // label90
        // 
        label90.AutoSize = true;
        label90.Location = new Point(9, 58);
        label90.Margin = new Padding(4, 0, 4, 0);
        label90.Name = "label90";
        label90.Size = new Size(71, 15);
        label90.TabIndex = 174;
        label90.Text = "Overbought";
        // 
        // EditRsiValueOverbought
        // 
        EditRsiValueOverbought.DecimalPlaces = 2;
        EditRsiValueOverbought.Location = new Point(130, 56);
        EditRsiValueOverbought.Margin = new Padding(4, 3, 4, 3);
        EditRsiValueOverbought.Name = "EditRsiValueOverbought";
        EditRsiValueOverbought.Size = new Size(88, 23);
        EditRsiValueOverbought.TabIndex = 176;
        EditRsiValueOverbought.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // EditExtraCaption
        // 
        EditExtraCaption.Location = new Point(187, 9);
        EditExtraCaption.Margin = new Padding(4, 3, 4, 3);
        EditExtraCaption.Name = "EditExtraCaption";
        EditExtraCaption.Size = new Size(193, 23);
        EditExtraCaption.TabIndex = 191;
        // 
        // label74
        // 
        label74.AutoSize = true;
        label74.Location = new Point(26, 12);
        label74.Margin = new Padding(4, 0, 4, 0);
        label74.Name = "label74";
        label74.Size = new Size(130, 15);
        label74.TabIndex = 192;
        label74.Text = "Extra applicatie caption";
        // 
        // EditHideSymbolsOnTheLeft
        // 
        EditHideSymbolsOnTheLeft.AutoSize = true;
        EditHideSymbolsOnTheLeft.Location = new Point(29, 249);
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
        label58.Location = new Point(24, 71);
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
        EditActivateExchange.Location = new Point(189, 67);
        EditActivateExchange.Margin = new Padding(4, 3, 4, 3);
        EditActivateExchange.Name = "EditActivateExchange";
        EditActivateExchange.Size = new Size(190, 23);
        EditActivateExchange.TabIndex = 188;
        // 
        // EditShowInvalidSignals
        // 
        EditShowInvalidSignals.AutoSize = true;
        EditShowInvalidSignals.Location = new Point(29, 222);
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
        label84.Location = new Point(22, 42);
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
        EditExchange.Location = new Point(188, 38);
        EditExchange.Margin = new Padding(4, 3, 4, 3);
        EditExchange.Name = "EditExchange";
        EditExchange.Size = new Size(190, 23);
        EditExchange.TabIndex = 164;
        // 
        // label16
        // 
        label16.AutoSize = true;
        label16.Location = new Point(26, 189);
        label16.Margin = new Padding(4, 0, 4, 0);
        label16.Name = "label16";
        label16.Size = new Size(263, 15);
        label16.TabIndex = 161;
        label16.Text = "Iedere x minuten controleren op nieuwe munten";
        // 
        // EditGetCandleInterval
        // 
        EditGetCandleInterval.Location = new Point(323, 187);
        EditGetCandleInterval.Margin = new Padding(4, 3, 4, 3);
        EditGetCandleInterval.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
        EditGetCandleInterval.Name = "EditGetCandleInterval";
        EditGetCandleInterval.Size = new Size(57, 23);
        EditGetCandleInterval.TabIndex = 162;
        EditGetCandleInterval.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label6
        // 
        label6.AutoSize = true;
        label6.Location = new Point(26, 164);
        label6.Margin = new Padding(4, 0, 4, 0);
        label6.Name = "label6";
        label6.Size = new Size(186, 15);
        label6.TabIndex = 156;
        label6.Text = "Verwijder de signalen na x candles";
        // 
        // EditGlobalDataRemoveSignalAfterxCandles
        // 
        EditGlobalDataRemoveSignalAfterxCandles.Location = new Point(323, 161);
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
        EditBlackTheming.Location = new Point(29, 307);
        EditBlackTheming.Margin = new Padding(4, 3, 4, 3);
        EditBlackTheming.Name = "EditBlackTheming";
        EditBlackTheming.Size = new Size(84, 19);
        EditBlackTheming.TabIndex = 155;
        EditBlackTheming.Text = "Gray mode";
        EditBlackTheming.UseVisualStyleBackColor = true;
        // 
        // buttonFontDialog
        // 
        buttonFontDialog.Location = new Point(29, 274);
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
        label18.Location = new Point(26, 137);
        label18.Margin = new Padding(4, 0, 4, 0);
        label18.Name = "label18";
        label18.Size = new Size(257, 15);
        label18.TabIndex = 152;
        label18.Text = "Iedere x minuten een heart beat geluid afspelen";
        // 
        // EditSoundHeartBeatMinutes
        // 
        EditSoundHeartBeatMinutes.Location = new Point(323, 135);
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
        label2.Location = new Point(23, 100);
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
        EditTradingApp.Location = new Point(189, 96);
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
        EditTelegramChatId.PasswordChar = '*';
        EditTelegramChatId.Size = new Size(227, 23);
        EditTelegramChatId.TabIndex = 172;
        // 
        // EditTelegramToken
        // 
        EditTelegramToken.Location = new Point(151, 27);
        EditTelegramToken.Margin = new Padding(4, 3, 4, 3);
        EditTelegramToken.Name = "EditTelegramToken";
        EditTelegramToken.PasswordChar = '*';
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
        tabPageSignals.Controls.Add(label86);
        tabPageSignals.Controls.Add(EditAnalysisMinEffective10DaysPercentage);
        tabPageSignals.Controls.Add(EditAnalysisMaxEffective10DaysPercentage);
        tabPageSignals.Controls.Add(EditLogAnalysisMinMaxEffective10DaysPercentage);
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
        // label86
        // 
        label86.AutoSize = true;
        label86.Location = new Point(291, 113);
        label86.Margin = new Padding(4, 0, 4, 0);
        label86.Name = "label86";
        label86.Size = new Size(101, 15);
        label86.TabIndex = 244;
        label86.Text = "10 dagen effectief";
        // 
        // EditAnalysisMinEffective10DaysPercentage
        // 
        EditAnalysisMinEffective10DaysPercentage.Location = new Point(423, 111);
        EditAnalysisMinEffective10DaysPercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMinEffective10DaysPercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisMinEffective10DaysPercentage.Name = "EditAnalysisMinEffective10DaysPercentage";
        EditAnalysisMinEffective10DaysPercentage.Size = new Size(57, 23);
        EditAnalysisMinEffective10DaysPercentage.TabIndex = 245;
        toolTip1.SetToolTip(EditAnalysisMinEffective10DaysPercentage, "Kunnen filteren op de 24 uur volume percentage.");
        // 
        // EditAnalysisMaxEffective10DaysPercentage
        // 
        EditAnalysisMaxEffective10DaysPercentage.Location = new Point(487, 111);
        EditAnalysisMaxEffective10DaysPercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMaxEffective10DaysPercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisMaxEffective10DaysPercentage.Name = "EditAnalysisMaxEffective10DaysPercentage";
        EditAnalysisMaxEffective10DaysPercentage.Size = new Size(57, 23);
        EditAnalysisMaxEffective10DaysPercentage.TabIndex = 246;
        toolTip1.SetToolTip(EditAnalysisMaxEffective10DaysPercentage, "Kunnen filteren op de 24 uur volume percentage.");
        EditAnalysisMaxEffective10DaysPercentage.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // EditLogAnalysisMinMaxEffective10DaysPercentage
        // 
        EditLogAnalysisMinMaxEffective10DaysPercentage.AutoSize = true;
        EditLogAnalysisMinMaxEffective10DaysPercentage.Location = new Point(572, 112);
        EditLogAnalysisMinMaxEffective10DaysPercentage.Margin = new Padding(4, 3, 4, 3);
        EditLogAnalysisMinMaxEffective10DaysPercentage.Name = "EditLogAnalysisMinMaxEffective10DaysPercentage";
        EditLogAnalysisMinMaxEffective10DaysPercentage.Size = new Size(203, 19);
        EditLogAnalysisMinMaxEffective10DaysPercentage.TabIndex = 247;
        EditLogAnalysisMinMaxEffective10DaysPercentage.Text = "Log waarden buiten deze grenzen";
        EditLogAnalysisMinMaxEffective10DaysPercentage.UseVisualStyleBackColor = true;
        // 
        // label64
        // 
        label64.AutoSize = true;
        label64.Location = new Point(291, 86);
        label64.Margin = new Padding(4, 0, 4, 0);
        label64.Name = "label64";
        label64.Size = new Size(86, 15);
        label64.TabIndex = 240;
        label64.Text = "24 uur effectief";
        // 
        // EditAnalysisMinEffectivePercentage
        // 
        EditAnalysisMinEffectivePercentage.Location = new Point(423, 84);
        EditAnalysisMinEffectivePercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMinEffectivePercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisMinEffectivePercentage.Name = "EditAnalysisMinEffectivePercentage";
        EditAnalysisMinEffectivePercentage.Size = new Size(57, 23);
        EditAnalysisMinEffectivePercentage.TabIndex = 241;
        toolTip1.SetToolTip(EditAnalysisMinEffectivePercentage, "Kunnen filteren op de 24 uur volume percentage.");
        // 
        // EditAnalysisMaxEffectivePercentage
        // 
        EditAnalysisMaxEffectivePercentage.Location = new Point(487, 84);
        EditAnalysisMaxEffectivePercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMaxEffectivePercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisMaxEffectivePercentage.Name = "EditAnalysisMaxEffectivePercentage";
        EditAnalysisMaxEffectivePercentage.Size = new Size(57, 23);
        EditAnalysisMaxEffectivePercentage.TabIndex = 242;
        toolTip1.SetToolTip(EditAnalysisMaxEffectivePercentage, "Kunnen filteren op de 24 uur volume percentage.");
        EditAnalysisMaxEffectivePercentage.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // EditLogAnalysisMinMaxEffectivePercentage
        // 
        EditLogAnalysisMinMaxEffectivePercentage.AutoSize = true;
        EditLogAnalysisMinMaxEffectivePercentage.Location = new Point(572, 85);
        EditLogAnalysisMinMaxEffectivePercentage.Margin = new Padding(4, 3, 4, 3);
        EditLogAnalysisMinMaxEffectivePercentage.Name = "EditLogAnalysisMinMaxEffectivePercentage";
        EditLogAnalysisMinMaxEffectivePercentage.Size = new Size(203, 19);
        EditLogAnalysisMinMaxEffectivePercentage.TabIndex = 243;
        EditLogAnalysisMinMaxEffectivePercentage.Text = "Log waarden buiten deze grenzen";
        EditLogAnalysisMinMaxEffectivePercentage.UseVisualStyleBackColor = true;
        // 
        // label79
        // 
        label79.AutoSize = true;
        label79.Location = new Point(727, 378);
        label79.Margin = new Padding(4, 0, 4, 0);
        label79.Name = "label79";
        label79.Size = new Size(176, 15);
        label79.TabIndex = 239;
        label79.Text = "Kleiner dan dit getal is een nogo";
        // 
        // label48
        // 
        label48.AutoSize = true;
        label48.Location = new Point(727, 346);
        label48.Margin = new Padding(4, 0, 4, 0);
        label48.Name = "label48";
        label48.Size = new Size(176, 15);
        label48.TabIndex = 238;
        label48.Text = "Kleiner dan dit getal is een nogo";
        // 
        // label38
        // 
        label38.AutoSize = true;
        label38.Location = new Point(727, 316);
        label38.Margin = new Padding(4, 0, 4, 0);
        label38.Name = "label38";
        label38.Size = new Size(173, 15);
        label38.TabIndex = 237;
        label38.Text = "Groter dan dit getal is een nogo";
        // 
        // label37
        // 
        label37.AutoSize = true;
        label37.Location = new Point(727, 289);
        label37.Margin = new Padding(4, 0, 4, 0);
        label37.Name = "label37";
        label37.Size = new Size(173, 15);
        label37.TabIndex = 236;
        label37.Text = "Groter dan dit getal is een nogo";
        // 
        // label10
        // 
        label10.AutoSize = true;
        label10.Location = new Point(287, 261);
        label10.Margin = new Padding(4, 0, 4, 0);
        label10.Name = "label10";
        label10.Size = new Size(186, 15);
        label10.TabIndex = 235;
        label10.Text = "Controles op de laatste 60 candles";
        // 
        // EditCandlesWithFlatPriceCheck
        // 
        EditCandlesWithFlatPriceCheck.AutoSize = true;
        EditCandlesWithFlatPriceCheck.Location = new Point(287, 291);
        EditCandlesWithFlatPriceCheck.Margin = new Padding(4, 3, 4, 3);
        EditCandlesWithFlatPriceCheck.Name = "EditCandlesWithFlatPriceCheck";
        EditCandlesWithFlatPriceCheck.Size = new Size(213, 19);
        EditCandlesWithFlatPriceCheck.TabIndex = 234;
        EditCandlesWithFlatPriceCheck.Text = "Controleer het aantal platte candles";
        EditCandlesWithFlatPriceCheck.UseVisualStyleBackColor = true;
        // 
        // EditCandlesWithZeroVolumeCheck
        // 
        EditCandlesWithZeroVolumeCheck.AutoSize = true;
        EditCandlesWithZeroVolumeCheck.Location = new Point(288, 320);
        EditCandlesWithZeroVolumeCheck.Margin = new Padding(4, 3, 4, 3);
        EditCandlesWithZeroVolumeCheck.Name = "EditCandlesWithZeroVolumeCheck";
        EditCandlesWithZeroVolumeCheck.Size = new Size(262, 19);
        EditCandlesWithZeroVolumeCheck.TabIndex = 233;
        EditCandlesWithZeroVolumeCheck.Text = "Controleer het aantal candles zonder volume";
        EditCandlesWithZeroVolumeCheck.UseVisualStyleBackColor = true;
        // 
        // EditMinimumAboveBollingerBandsSmaCheck
        // 
        EditMinimumAboveBollingerBandsSmaCheck.AutoSize = true;
        EditMinimumAboveBollingerBandsSmaCheck.Location = new Point(288, 350);
        EditMinimumAboveBollingerBandsSmaCheck.Margin = new Padding(4, 3, 4, 3);
        EditMinimumAboveBollingerBandsSmaCheck.Name = "EditMinimumAboveBollingerBandsSmaCheck";
        EditMinimumAboveBollingerBandsSmaCheck.Size = new Size(211, 19);
        EditMinimumAboveBollingerBandsSmaCheck.TabIndex = 232;
        EditMinimumAboveBollingerBandsSmaCheck.Text = "Controleer aantal boven de bb.sma";
        EditMinimumAboveBollingerBandsSmaCheck.UseVisualStyleBackColor = true;
        // 
        // EditMinimumAboveBollingerBandsUpperCheck
        // 
        EditMinimumAboveBollingerBandsUpperCheck.AutoSize = true;
        EditMinimumAboveBollingerBandsUpperCheck.Location = new Point(288, 380);
        EditMinimumAboveBollingerBandsUpperCheck.Margin = new Padding(4, 3, 4, 3);
        EditMinimumAboveBollingerBandsUpperCheck.Name = "EditMinimumAboveBollingerBandsUpperCheck";
        EditMinimumAboveBollingerBandsUpperCheck.Size = new Size(220, 19);
        EditMinimumAboveBollingerBandsUpperCheck.TabIndex = 231;
        EditMinimumAboveBollingerBandsUpperCheck.Text = "Controleer aantal boven de bb.upper";
        EditMinimumAboveBollingerBandsUpperCheck.UseVisualStyleBackColor = true;
        // 
        // EditCandlesWithZeroVolume
        // 
        EditCandlesWithZeroVolume.Location = new Point(618, 316);
        EditCandlesWithZeroVolume.Margin = new Padding(4, 3, 4, 3);
        EditCandlesWithZeroVolume.Name = "EditCandlesWithZeroVolume";
        EditCandlesWithZeroVolume.Size = new Size(88, 23);
        EditCandlesWithZeroVolume.TabIndex = 230;
        EditCandlesWithZeroVolume.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // EditCandlesWithFlatPrice
        // 
        EditCandlesWithFlatPrice.Location = new Point(618, 287);
        EditCandlesWithFlatPrice.Margin = new Padding(4, 3, 4, 3);
        EditCandlesWithFlatPrice.Name = "EditCandlesWithFlatPrice";
        EditCandlesWithFlatPrice.Size = new Size(88, 23);
        EditCandlesWithFlatPrice.TabIndex = 229;
        EditCandlesWithFlatPrice.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // EditMinimumAboveBollingerBandsUpper
        // 
        EditMinimumAboveBollingerBandsUpper.Location = new Point(617, 376);
        EditMinimumAboveBollingerBandsUpper.Margin = new Padding(4, 3, 4, 3);
        EditMinimumAboveBollingerBandsUpper.Name = "EditMinimumAboveBollingerBandsUpper";
        EditMinimumAboveBollingerBandsUpper.Size = new Size(88, 23);
        EditMinimumAboveBollingerBandsUpper.TabIndex = 228;
        EditMinimumAboveBollingerBandsUpper.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // EditMinimumAboveBollingerBandsSma
        // 
        EditMinimumAboveBollingerBandsSma.Location = new Point(618, 346);
        EditMinimumAboveBollingerBandsSma.Margin = new Padding(4, 3, 4, 3);
        EditMinimumAboveBollingerBandsSma.Name = "EditMinimumAboveBollingerBandsSma";
        EditMinimumAboveBollingerBandsSma.Size = new Size(88, 23);
        EditMinimumAboveBollingerBandsSma.TabIndex = 227;
        EditMinimumAboveBollingerBandsSma.Value = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // label26
        // 
        label26.AutoSize = true;
        label26.Location = new Point(23, 25);
        label26.Margin = new Padding(4, 0, 4, 0);
        label26.Name = "label26";
        label26.Size = new Size(206, 15);
        label26.TabIndex = 226;
        label26.Text = "Create signals for the intervals and .....";
        // 
        // EditLogMinimumTickPercentage
        // 
        EditLogMinimumTickPercentage.AutoSize = true;
        EditLogMinimumTickPercentage.Location = new Point(572, 207);
        EditLogMinimumTickPercentage.Margin = new Padding(4, 3, 4, 3);
        EditLogMinimumTickPercentage.Name = "EditLogMinimumTickPercentage";
        EditLogMinimumTickPercentage.Size = new Size(165, 19);
        EditLogMinimumTickPercentage.TabIndex = 221;
        EditLogMinimumTickPercentage.Text = "Log als dit niet het geval is";
        EditLogMinimumTickPercentage.UseVisualStyleBackColor = true;
        // 
        // EditMinimumTickPercentage
        // 
        EditMinimumTickPercentage.DecimalPlaces = 2;
        EditMinimumTickPercentage.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
        EditMinimumTickPercentage.Location = new Point(459, 206);
        EditMinimumTickPercentage.Margin = new Padding(4, 3, 4, 3);
        EditMinimumTickPercentage.Name = "EditMinimumTickPercentage";
        EditMinimumTickPercentage.Size = new Size(75, 23);
        EditMinimumTickPercentage.TabIndex = 220;
        toolTip1.SetToolTip(EditMinimumTickPercentage, "Soms heb je van die munten die of een barcode streepjes patroon hebben of die per tick een enorme afstand overbruggen. Via deze instelling kun je die markeren in het overzicht");
        EditMinimumTickPercentage.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // label61
        // 
        label61.AutoSize = true;
        label61.Location = new Point(288, 209);
        label61.Margin = new Padding(4, 0, 4, 0);
        label61.Name = "label61";
        label61.Size = new Size(90, 15);
        label61.TabIndex = 219;
        label61.Text = "Tick percentage";
        // 
        // label53
        // 
        label53.AutoSize = true;
        label53.Location = new Point(291, 61);
        label53.Margin = new Padding(4, 0, 4, 0);
        label53.Name = "label53";
        label53.Size = new Size(82, 15);
        label53.TabIndex = 222;
        label53.Text = "24 uur change";
        // 
        // EditAnalysisMinChangePercentage
        // 
        EditAnalysisMinChangePercentage.Location = new Point(423, 59);
        EditAnalysisMinChangePercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMinChangePercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisMinChangePercentage.Name = "EditAnalysisMinChangePercentage";
        EditAnalysisMinChangePercentage.Size = new Size(57, 23);
        EditAnalysisMinChangePercentage.TabIndex = 223;
        toolTip1.SetToolTip(EditAnalysisMinChangePercentage, "Kunnen filteren op de 24 uur volume percentage.");
        // 
        // EditAnalysisMaxChangePercentage
        // 
        EditAnalysisMaxChangePercentage.Location = new Point(487, 59);
        EditAnalysisMaxChangePercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMaxChangePercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisMaxChangePercentage.Name = "EditAnalysisMaxChangePercentage";
        EditAnalysisMaxChangePercentage.Size = new Size(57, 23);
        EditAnalysisMaxChangePercentage.TabIndex = 224;
        toolTip1.SetToolTip(EditAnalysisMaxChangePercentage, "Kunnen filteren op de 24 uur volume percentage.");
        EditAnalysisMaxChangePercentage.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // EditLogBarometerToLow
        // 
        EditLogBarometerToLow.AutoSize = true;
        EditLogBarometerToLow.Location = new Point(572, 152);
        EditLogBarometerToLow.Margin = new Padding(4, 3, 4, 3);
        EditLogBarometerToLow.Name = "EditLogBarometerToLow";
        EditLogBarometerToLow.Size = new Size(142, 19);
        EditLogBarometerToLow.TabIndex = 215;
        EditLogBarometerToLow.Text = "Log te lage barometer";
        EditLogBarometerToLow.UseVisualStyleBackColor = true;
        // 
        // EditLogSymbolMustExistsDays
        // 
        EditLogSymbolMustExistsDays.AutoSize = true;
        EditLogSymbolMustExistsDays.Location = new Point(572, 178);
        EditLogSymbolMustExistsDays.Margin = new Padding(4, 3, 4, 3);
        EditLogSymbolMustExistsDays.Name = "EditLogSymbolMustExistsDays";
        EditLogSymbolMustExistsDays.Size = new Size(208, 19);
        EditLogSymbolMustExistsDays.TabIndex = 218;
        EditLogSymbolMustExistsDays.Text = "Log minimale dagen nieuwe munt";
        EditLogSymbolMustExistsDays.UseVisualStyleBackColor = true;
        // 
        // EditSymbolMustExistsDays
        // 
        EditSymbolMustExistsDays.Location = new Point(458, 178);
        EditSymbolMustExistsDays.Margin = new Padding(4, 3, 4, 3);
        EditSymbolMustExistsDays.Name = "EditSymbolMustExistsDays";
        EditSymbolMustExistsDays.Size = new Size(75, 23);
        EditSymbolMustExistsDays.TabIndex = 217;
        toolTip1.SetToolTip(EditSymbolMustExistsDays, "Negeer munten die korten dan x dagen bestaan");
        EditSymbolMustExistsDays.Value = new decimal(new int[] { 15, 0, 0, 0 });
        // 
        // label25
        // 
        label25.AutoSize = true;
        label25.Location = new Point(288, 182);
        label25.Margin = new Padding(4, 0, 4, 0);
        label25.Name = "label25";
        label25.Size = new Size(115, 15);
        label25.TabIndex = 216;
        label25.Text = "Nieuwe munt dagen";
        // 
        // label35
        // 
        label35.AutoSize = true;
        label35.Location = new Point(288, 154);
        label35.Margin = new Padding(4, 0, 4, 0);
        label35.Name = "label35";
        label35.Size = new Size(115, 15);
        label35.TabIndex = 213;
        label35.Text = "Minimale barometer";
        // 
        // EditBarometer1hMinimal
        // 
        EditBarometer1hMinimal.DecimalPlaces = 2;
        EditBarometer1hMinimal.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditBarometer1hMinimal.Location = new Point(459, 152);
        EditBarometer1hMinimal.Margin = new Padding(4, 3, 4, 3);
        EditBarometer1hMinimal.Name = "EditBarometer1hMinimal";
        EditBarometer1hMinimal.Size = new Size(74, 23);
        EditBarometer1hMinimal.TabIndex = 214;
        toolTip1.SetToolTip(EditBarometer1hMinimal, "Als de barometer laag staat krijg je enorm veel medlingen, negeer meldingen als de barometer onder dit getal staat");
        EditBarometer1hMinimal.Value = new decimal(new int[] { 25, 0, 0, 65536 });
        // 
        // EditLogAnalysisMinMaxChangePercentage
        // 
        EditLogAnalysisMinMaxChangePercentage.AutoSize = true;
        EditLogAnalysisMinMaxChangePercentage.Location = new Point(572, 60);
        EditLogAnalysisMinMaxChangePercentage.Margin = new Padding(4, 3, 4, 3);
        EditLogAnalysisMinMaxChangePercentage.Name = "EditLogAnalysisMinMaxChangePercentage";
        EditLogAnalysisMinMaxChangePercentage.Size = new Size(203, 19);
        EditLogAnalysisMinMaxChangePercentage.TabIndex = 225;
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
        groupBoxInterval.Location = new Point(26, 60);
        groupBoxInterval.Margin = new Padding(4, 3, 4, 3);
        groupBoxInterval.Name = "groupBoxInterval";
        groupBoxInterval.Padding = new Padding(4, 3, 4, 3);
        groupBoxInterval.Size = new Size(224, 240);
        groupBoxInterval.TabIndex = 212;
        groupBoxInterval.TabStop = false;
        groupBoxInterval.Text = "Interval";
        // 
        // EditAnalyzeInterval6h
        // 
        EditAnalyzeInterval6h.AutoSize = true;
        EditAnalyzeInterval6h.Location = new Point(135, 112);
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
        EditAnalyzeInterval8h.Location = new Point(135, 141);
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
        EditAnalyzeInterval12h.Location = new Point(135, 168);
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
        EditAnalyzeInterval1d.Location = new Point(135, 195);
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
        EditAnalyzeInterval5m.Location = new Point(21, 114);
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
        EditAnalyzeInterval1m.Location = new Point(21, 31);
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
        EditAnalyzeInterval2m.Location = new Point(21, 61);
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
        EditAnalyzeInterval3m.Location = new Point(21, 88);
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
        EditAnalyzeInterval10m.Location = new Point(21, 141);
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
        EditAnalyzeInterval15m.Location = new Point(21, 167);
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
        EditAnalyzeInterval30m.Location = new Point(21, 194);
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
        EditAnalyzeInterval1h.Location = new Point(135, 31);
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
        EditAnalyzeInterval2h.Location = new Point(135, 59);
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
        EditAnalyzeInterval4h.Location = new Point(135, 85);
        EditAnalyzeInterval4h.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeInterval4h.Name = "EditAnalyzeInterval4h";
        EditAnalyzeInterval4h.Size = new Size(53, 19);
        EditAnalyzeInterval4h.TabIndex = 114;
        EditAnalyzeInterval4h.Text = "4 uur";
        EditAnalyzeInterval4h.UseVisualStyleBackColor = true;
        // 
        // tabSignalStobb
        // 
        tabSignalStobb.Controls.Add(buttonColorStobbLong);
        tabSignalStobb.Controls.Add(panelColorStobbLong);
        tabSignalStobb.Controls.Add(label85);
        tabSignalStobb.Controls.Add(EditStobTrendShort);
        tabSignalStobb.Controls.Add(label66);
        tabSignalStobb.Controls.Add(EditStobTrendLong);
        tabSignalStobb.Controls.Add(label77);
        tabSignalStobb.Controls.Add(label75);
        tabSignalStobb.Controls.Add(EditStobIncludeSbmPercAndCrossing);
        tabSignalStobb.Controls.Add(label30);
        tabSignalStobb.Controls.Add(label28);
        tabSignalStobb.Controls.Add(buttonColorStobbShort);
        tabSignalStobb.Controls.Add(EditStobIncludeSbmMaLines);
        tabSignalStobb.Controls.Add(EditStobIncludeRsi);
        tabSignalStobb.Controls.Add(buttonPlaySoundStobbOversold);
        tabSignalStobb.Controls.Add(buttonPlaySoundStobbOverbought);
        tabSignalStobb.Controls.Add(buttonSelectSoundStobbOversold);
        tabSignalStobb.Controls.Add(panelColorStobbShort);
        tabSignalStobb.Controls.Add(EditSoundStobbOversold);
        tabSignalStobb.Controls.Add(EditSoundStobbOverbought);
        tabSignalStobb.Controls.Add(buttonSelectSoundStobbOverbought);
        tabSignalStobb.Controls.Add(EditPlaySpeechStobbSignal);
        tabSignalStobb.Controls.Add(EditPlaySoundStobbSignal);
        tabSignalStobb.Controls.Add(EditAnalyzeStobbLong);
        tabSignalStobb.Controls.Add(EditAnalyzeStobbShort);
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
        // buttonColorStobbLong
        // 
        buttonColorStobbLong.Location = new Point(360, 132);
        buttonColorStobbLong.Margin = new Padding(4, 3, 4, 3);
        buttonColorStobbLong.Name = "buttonColorStobbLong";
        buttonColorStobbLong.Size = new Size(88, 27);
        buttonColorStobbLong.TabIndex = 156;
        buttonColorStobbLong.Text = "Achtergrond";
        buttonColorStobbLong.UseVisualStyleBackColor = true;
        // 
        // panelColorStobbLong
        // 
        panelColorStobbLong.BackColor = Color.Transparent;
        panelColorStobbLong.BorderStyle = BorderStyle.FixedSingle;
        panelColorStobbLong.Location = new Point(252, 135);
        panelColorStobbLong.Margin = new Padding(4, 3, 4, 3);
        panelColorStobbLong.Name = "panelColorStobbLong";
        panelColorStobbLong.Size = new Size(70, 22);
        panelColorStobbLong.TabIndex = 157;
        // 
        // label85
        // 
        label85.AutoSize = true;
        label85.Location = new Point(23, 370);
        label85.Margin = new Padding(4, 0, 4, 0);
        label85.Name = "label85";
        label85.Size = new Size(118, 15);
        label85.TabIndex = 154;
        label85.Text = "Minimale trend short";
        // 
        // EditStobTrendShort
        // 
        EditStobTrendShort.DecimalPlaces = 2;
        EditStobTrendShort.Location = new Point(190, 368);
        EditStobTrendShort.Margin = new Padding(4, 3, 4, 3);
        EditStobTrendShort.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditStobTrendShort.Minimum = new decimal(new int[] { 1000, 0, 0, int.MinValue });
        EditStobTrendShort.Name = "EditStobTrendShort";
        EditStobTrendShort.Size = new Size(65, 23);
        EditStobTrendShort.TabIndex = 155;
        EditStobTrendShort.Value = new decimal(new int[] { 150, 0, 0, 131072 });
        // 
        // label66
        // 
        label66.AutoSize = true;
        label66.Location = new Point(23, 340);
        label66.Margin = new Padding(4, 0, 4, 0);
        label66.Name = "label66";
        label66.Size = new Size(115, 15);
        label66.TabIndex = 152;
        label66.Text = "Minimale trend long";
        // 
        // EditStobTrendLong
        // 
        EditStobTrendLong.DecimalPlaces = 2;
        EditStobTrendLong.Location = new Point(190, 338);
        EditStobTrendLong.Margin = new Padding(4, 3, 4, 3);
        EditStobTrendLong.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditStobTrendLong.Minimum = new decimal(new int[] { 1000, 0, 0, int.MinValue });
        EditStobTrendLong.Name = "EditStobTrendLong";
        EditStobTrendLong.Size = new Size(65, 23);
        EditStobTrendLong.TabIndex = 153;
        EditStobTrendLong.Value = new decimal(new int[] { 150, 0, 0, 131072 });
        // 
        // label77
        // 
        label77.AutoSize = true;
        label77.Location = new Point(23, 86);
        label77.Margin = new Padding(4, 0, 4, 0);
        label77.Name = "label77";
        label77.Size = new Size(41, 15);
        label77.TabIndex = 151;
        label77.Text = "STOBB";
        // 
        // label75
        // 
        label75.AutoSize = true;
        label75.Location = new Point(23, 181);
        label75.Margin = new Padding(4, 0, 4, 0);
        label75.Name = "label75";
        label75.Size = new Size(68, 15);
        label75.TabIndex = 121;
        label75.Text = "Instellingen";
        // 
        // EditStobIncludeSbmPercAndCrossing
        // 
        EditStobIncludeSbmPercAndCrossing.AutoSize = true;
        EditStobIncludeSbmPercAndCrossing.Location = new Point(23, 308);
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
        label30.Location = new Point(465, 137);
        label30.Margin = new Padding(4, 0, 4, 0);
        label30.Name = "label30";
        label30.Size = new Size(86, 15);
        label30.TabIndex = 119;
        label30.Text = "Long soundfile";
        // 
        // label28
        // 
        label28.AutoSize = true;
        label28.Location = new Point(465, 106);
        label28.Margin = new Padding(4, 0, 4, 0);
        label28.Name = "label28";
        label28.Size = new Size(87, 15);
        label28.TabIndex = 118;
        label28.Text = "Short soundfile";
        // 
        // buttonColorStobbShort
        // 
        buttonColorStobbShort.Location = new Point(360, 100);
        buttonColorStobbShort.Margin = new Padding(4, 3, 4, 3);
        buttonColorStobbShort.Name = "buttonColorStobbShort";
        buttonColorStobbShort.Size = new Size(88, 27);
        buttonColorStobbShort.TabIndex = 115;
        buttonColorStobbShort.Text = "Achtergrond";
        buttonColorStobbShort.UseVisualStyleBackColor = true;
        // 
        // EditStobIncludeSbmMaLines
        // 
        EditStobIncludeSbmMaLines.AutoSize = true;
        EditStobIncludeSbmMaLines.Location = new Point(23, 283);
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
        EditStobIncludeRsi.Location = new Point(23, 258);
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
        buttonPlaySoundStobbOversold.Location = new Point(867, 137);
        buttonPlaySoundStobbOversold.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySoundStobbOversold.Name = "buttonPlaySoundStobbOversold";
        buttonPlaySoundStobbOversold.Size = new Size(23, 23);
        buttonPlaySoundStobbOversold.TabIndex = 112;
        buttonPlaySoundStobbOversold.UseVisualStyleBackColor = true;
        // 
        // buttonPlaySoundStobbOverbought
        // 
        buttonPlaySoundStobbOverbought.Image = Properties.Resources.volume;
        buttonPlaySoundStobbOverbought.Location = new Point(867, 104);
        buttonPlaySoundStobbOverbought.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySoundStobbOverbought.Name = "buttonPlaySoundStobbOverbought";
        buttonPlaySoundStobbOverbought.Size = new Size(23, 23);
        buttonPlaySoundStobbOverbought.TabIndex = 111;
        buttonPlaySoundStobbOverbought.UseVisualStyleBackColor = true;
        // 
        // buttonSelectSoundStobbOversold
        // 
        buttonSelectSoundStobbOversold.Location = new Point(837, 137);
        buttonSelectSoundStobbOversold.Margin = new Padding(4, 3, 4, 3);
        buttonSelectSoundStobbOversold.Name = "buttonSelectSoundStobbOversold";
        buttonSelectSoundStobbOversold.Size = new Size(23, 23);
        buttonSelectSoundStobbOversold.TabIndex = 110;
        buttonSelectSoundStobbOversold.UseVisualStyleBackColor = true;
        // 
        // panelColorStobbShort
        // 
        panelColorStobbShort.BackColor = Color.Transparent;
        panelColorStobbShort.BorderStyle = BorderStyle.FixedSingle;
        panelColorStobbShort.Location = new Point(252, 103);
        panelColorStobbShort.Margin = new Padding(4, 3, 4, 3);
        panelColorStobbShort.Name = "panelColorStobbShort";
        panelColorStobbShort.Size = new Size(70, 22);
        panelColorStobbShort.TabIndex = 116;
        // 
        // EditSoundStobbOversold
        // 
        EditSoundStobbOversold.Location = new Point(602, 137);
        EditSoundStobbOversold.Margin = new Padding(4, 3, 4, 3);
        EditSoundStobbOversold.Name = "EditSoundStobbOversold";
        EditSoundStobbOversold.Size = new Size(227, 23);
        EditSoundStobbOversold.TabIndex = 109;
        // 
        // EditSoundStobbOverbought
        // 
        EditSoundStobbOverbought.Location = new Point(602, 103);
        EditSoundStobbOverbought.Margin = new Padding(4, 3, 4, 3);
        EditSoundStobbOverbought.Name = "EditSoundStobbOverbought";
        EditSoundStobbOverbought.Size = new Size(227, 23);
        EditSoundStobbOverbought.TabIndex = 106;
        // 
        // buttonSelectSoundStobbOverbought
        // 
        buttonSelectSoundStobbOverbought.Location = new Point(837, 104);
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
        EditPlaySpeechStobbSignal.Location = new Point(23, 49);
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
        EditPlaySoundStobbSignal.Location = new Point(23, 22);
        EditPlaySoundStobbSignal.Margin = new Padding(4, 3, 4, 3);
        EditPlaySoundStobbSignal.Name = "EditPlaySoundStobbSignal";
        EditPlaySoundStobbSignal.Size = new Size(84, 19);
        EditPlaySoundStobbSignal.TabIndex = 103;
        EditPlaySoundStobbSignal.Text = "Play sound";
        EditPlaySoundStobbSignal.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeStobbLong
        // 
        EditAnalyzeStobbLong.AutoSize = true;
        EditAnalyzeStobbLong.Location = new Point(23, 137);
        EditAnalyzeStobbLong.Margin = new Padding(2);
        EditAnalyzeStobbLong.Name = "EditAnalyzeStobbLong";
        EditAnalyzeStobbLong.Size = new Size(151, 19);
        EditAnalyzeStobbLong.TabIndex = 108;
        EditAnalyzeStobbLong.Text = "Maak aankoop signalen";
        EditAnalyzeStobbLong.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeStobbShort
        // 
        EditAnalyzeStobbShort.AutoSize = true;
        EditAnalyzeStobbShort.Location = new Point(23, 108);
        EditAnalyzeStobbShort.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeStobbShort.Name = "EditAnalyzeStobbShort";
        EditAnalyzeStobbShort.Size = new Size(148, 19);
        EditAnalyzeStobbShort.TabIndex = 105;
        EditAnalyzeStobbShort.Text = "Maak verkoop signalen";
        EditAnalyzeStobbShort.UseVisualStyleBackColor = true;
        // 
        // EditStobbUseLowHigh
        // 
        EditStobbUseLowHigh.AutoSize = true;
        EditStobbUseLowHigh.Location = new Point(23, 233);
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
        label1.Location = new Point(23, 205);
        label1.Margin = new Padding(4, 0, 4, 0);
        label1.Name = "label1";
        label1.Size = new Size(77, 15);
        label1.TabIndex = 42;
        label1.Text = "Filter on BB%";
        // 
        // EditStobbBBMinPercentage
        // 
        EditStobbBBMinPercentage.DecimalPlaces = 2;
        EditStobbBBMinPercentage.Location = new Point(144, 203);
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
        EditStobbBBMaxPercentage.Location = new Point(229, 203);
        EditStobbBBMaxPercentage.Margin = new Padding(4, 3, 4, 3);
        EditStobbBBMaxPercentage.Name = "EditStobbBBMaxPercentage";
        EditStobbBBMaxPercentage.Size = new Size(65, 23);
        EditStobbBBMaxPercentage.TabIndex = 44;
        toolTip1.SetToolTip(EditStobbBBMaxPercentage, "Een BB heeft een bepaalde breedte, je kunt hier filteren waardoor op de minimale en maximale breedte kan worden gefilterd.");
        EditStobbBBMaxPercentage.Value = new decimal(new int[] { 6, 0, 0, 0 });
        // 
        // tabSignalSbm
        // 
        tabSignalSbm.Controls.Add(label97);
        tabSignalSbm.Controls.Add(label96);
        tabSignalSbm.Controls.Add(buttonColorSbmLong);
        tabSignalSbm.Controls.Add(panelColorSbmLong);
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
        tabSignalSbm.Controls.Add(EditAnalyzeSbm1Long);
        tabSignalSbm.Controls.Add(EditAnalyzeSbm1Short);
        tabSignalSbm.Controls.Add(EditPlaySoundSbmSignal);
        tabSignalSbm.Controls.Add(buttonColorSbmShort);
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
        tabSignalSbm.Controls.Add(EditAnalyzeSbm3Short);
        tabSignalSbm.Controls.Add(EditAnalyzeSbm2Short);
        tabSignalSbm.Controls.Add(label12);
        tabSignalSbm.Controls.Add(EditSbm2BbPercentage);
        tabSignalSbm.Controls.Add(panelColorSbmShort);
        tabSignalSbm.Controls.Add(label13);
        tabSignalSbm.Controls.Add(EditSbm3CandlesForBBRecovery);
        tabSignalSbm.Controls.Add(label14);
        tabSignalSbm.Controls.Add(EditSbm3CandlesForBBRecoveryPercentage);
        tabSignalSbm.Controls.Add(label11);
        tabSignalSbm.Controls.Add(EditSbm2CandlesLookbackCount);
        tabSignalSbm.Controls.Add(EditAnalyzeSbm3Long);
        tabSignalSbm.Controls.Add(EditAnalyzeSbm2Long);
        tabSignalSbm.Location = new Point(4, 27);
        tabSignalSbm.Margin = new Padding(4, 3, 4, 3);
        tabSignalSbm.Name = "tabSignalSbm";
        tabSignalSbm.Padding = new Padding(4, 3, 4, 3);
        tabSignalSbm.Size = new Size(1232, 777);
        tabSignalSbm.TabIndex = 5;
        tabSignalSbm.Text = "SBM";
        tabSignalSbm.UseVisualStyleBackColor = true;
        // 
        // label97
        // 
        label97.AutoSize = true;
        label97.Location = new Point(23, 110);
        label97.Margin = new Padding(4, 0, 4, 0);
        label97.Name = "label97";
        label97.Size = new Size(34, 15);
        label97.TabIndex = 156;
        label97.Text = "Long";
        // 
        // label96
        // 
        label96.AutoSize = true;
        label96.Location = new Point(23, 86);
        label96.Margin = new Padding(4, 0, 4, 0);
        label96.Name = "label96";
        label96.Size = new Size(35, 15);
        label96.TabIndex = 155;
        label96.Text = "Short";
        // 
        // buttonColorSbmLong
        // 
        buttonColorSbmLong.Location = new Point(209, 107);
        buttonColorSbmLong.Margin = new Padding(4, 3, 4, 3);
        buttonColorSbmLong.Name = "buttonColorSbmLong";
        buttonColorSbmLong.Size = new Size(88, 27);
        buttonColorSbmLong.TabIndex = 153;
        buttonColorSbmLong.Text = "Achtergrond";
        buttonColorSbmLong.UseVisualStyleBackColor = true;
        // 
        // panelColorSbmLong
        // 
        panelColorSbmLong.BorderStyle = BorderStyle.FixedSingle;
        panelColorSbmLong.Location = new Point(101, 110);
        panelColorSbmLong.Margin = new Padding(4, 3, 4, 3);
        panelColorSbmLong.Name = "panelColorSbmLong";
        panelColorSbmLong.Size = new Size(70, 22);
        panelColorSbmLong.TabIndex = 154;
        // 
        // EditSbm2UseLowHigh
        // 
        EditSbm2UseLowHigh.AutoSize = true;
        EditSbm2UseLowHigh.Location = new Point(27, 392);
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
        label21.Location = new Point(23, 464);
        label21.Margin = new Padding(4, 0, 4, 0);
        label21.Name = "label21";
        label21.Size = new Size(37, 15);
        label21.TabIndex = 151;
        label21.Text = "SBM3";
        // 
        // label20
        // 
        label20.AutoSize = true;
        label20.Location = new Point(23, 293);
        label20.Margin = new Padding(4, 0, 4, 0);
        label20.Name = "label20";
        label20.Size = new Size(37, 15);
        label20.TabIndex = 150;
        label20.Text = "SBM2";
        // 
        // label9
        // 
        label9.AutoSize = true;
        label9.Location = new Point(23, 155);
        label9.Margin = new Padding(4, 0, 4, 0);
        label9.Name = "label9";
        label9.Size = new Size(37, 15);
        label9.TabIndex = 149;
        label9.Text = "SBM1";
        // 
        // label41
        // 
        label41.AutoSize = true;
        label41.Location = new Point(23, 228);
        label41.Margin = new Padding(4, 0, 4, 0);
        label41.Name = "label41";
        label41.Size = new Size(95, 15);
        label41.TabIndex = 146;
        label41.Text = "Candle lookback";
        // 
        // EditSbm1CandlesLookbackCount
        // 
        EditSbm1CandlesLookbackCount.Location = new Point(278, 225);
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
        label39.Location = new Point(616, 221);
        label39.Margin = new Padding(4, 0, 4, 0);
        label39.Name = "label39";
        label39.Size = new Size(180, 15);
        label39.TabIndex = 141;
        label39.Text = "Het aantal MACD herstel candles";
        // 
        // EditSbmCandlesForMacdRecovery
        // 
        EditSbmCandlesForMacdRecovery.Location = new Point(1056, 215);
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
        label31.Location = new Point(315, 110);
        label31.Margin = new Padding(4, 0, 4, 0);
        label31.Name = "label31";
        label31.Size = new Size(106, 15);
        label31.TabIndex = 135;
        label31.Text = "Oversold soundfile";
        // 
        // label32
        // 
        label32.AutoSize = true;
        label32.Location = new Point(315, 85);
        label32.Margin = new Padding(4, 0, 4, 0);
        label32.Name = "label32";
        label32.Size = new Size(123, 15);
        label32.TabIndex = 134;
        label32.Text = "Overbought soundfile";
        // 
        // buttonPlaySoundSbmOversold
        // 
        buttonPlaySoundSbmOversold.Image = Properties.Resources.volume;
        buttonPlaySoundSbmOversold.Location = new Point(770, 105);
        buttonPlaySoundSbmOversold.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySoundSbmOversold.Name = "buttonPlaySoundSbmOversold";
        buttonPlaySoundSbmOversold.Size = new Size(23, 23);
        buttonPlaySoundSbmOversold.TabIndex = 132;
        buttonPlaySoundSbmOversold.UseVisualStyleBackColor = true;
        // 
        // buttonPlaySoundSbmOverbought
        // 
        buttonPlaySoundSbmOverbought.Image = Properties.Resources.volume;
        buttonPlaySoundSbmOverbought.Location = new Point(770, 82);
        buttonPlaySoundSbmOverbought.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySoundSbmOverbought.Name = "buttonPlaySoundSbmOverbought";
        buttonPlaySoundSbmOverbought.Size = new Size(23, 23);
        buttonPlaySoundSbmOverbought.TabIndex = 133;
        buttonPlaySoundSbmOverbought.UseVisualStyleBackColor = true;
        // 
        // buttonSelectSoundSbmOversold
        // 
        buttonSelectSoundSbmOversold.Location = new Point(739, 105);
        buttonSelectSoundSbmOversold.Margin = new Padding(4, 3, 4, 3);
        buttonSelectSoundSbmOversold.Name = "buttonSelectSoundSbmOversold";
        buttonSelectSoundSbmOversold.Size = new Size(23, 23);
        buttonSelectSoundSbmOversold.TabIndex = 124;
        buttonSelectSoundSbmOversold.UseVisualStyleBackColor = true;
        // 
        // EditSoundFileSbmOversold
        // 
        EditSoundFileSbmOversold.Location = new Point(505, 105);
        EditSoundFileSbmOversold.Margin = new Padding(4, 3, 4, 3);
        EditSoundFileSbmOversold.Name = "EditSoundFileSbmOversold";
        EditSoundFileSbmOversold.Size = new Size(227, 23);
        EditSoundFileSbmOversold.TabIndex = 131;
        // 
        // EditSoundFileSbmOverbought
        // 
        EditSoundFileSbmOverbought.Location = new Point(505, 79);
        EditSoundFileSbmOverbought.Margin = new Padding(4, 3, 4, 3);
        EditSoundFileSbmOverbought.Name = "EditSoundFileSbmOverbought";
        EditSoundFileSbmOverbought.Size = new Size(227, 23);
        EditSoundFileSbmOverbought.TabIndex = 128;
        // 
        // buttonSelectSoundSbmOverbought
        // 
        buttonSelectSoundSbmOverbought.Location = new Point(739, 80);
        buttonSelectSoundSbmOverbought.Margin = new Padding(4, 3, 4, 3);
        buttonSelectSoundSbmOverbought.Name = "buttonSelectSoundSbmOverbought";
        buttonSelectSoundSbmOverbought.Size = new Size(23, 23);
        buttonSelectSoundSbmOverbought.TabIndex = 129;
        buttonSelectSoundSbmOverbought.UseVisualStyleBackColor = true;
        // 
        // EditPlaySpeechSbmSignal
        // 
        EditPlaySpeechSbmSignal.AutoSize = true;
        EditPlaySpeechSbmSignal.Location = new Point(23, 55);
        EditPlaySpeechSbmSignal.Margin = new Padding(4, 3, 4, 3);
        EditPlaySpeechSbmSignal.Name = "EditPlaySpeechSbmSignal";
        EditPlaySpeechSbmSignal.Size = new Size(88, 19);
        EditPlaySpeechSbmSignal.TabIndex = 126;
        EditPlaySpeechSbmSignal.Text = "Play speech";
        EditPlaySpeechSbmSignal.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeSbm1Long
        // 
        EditAnalyzeSbm1Long.AutoSize = true;
        EditAnalyzeSbm1Long.Location = new Point(23, 202);
        EditAnalyzeSbm1Long.Margin = new Padding(2);
        EditAnalyzeSbm1Long.Name = "EditAnalyzeSbm1Long";
        EditAnalyzeSbm1Long.Size = new Size(151, 19);
        EditAnalyzeSbm1Long.TabIndex = 130;
        EditAnalyzeSbm1Long.Text = "Maak aankoop signalen";
        EditAnalyzeSbm1Long.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeSbm1Short
        // 
        EditAnalyzeSbm1Short.AutoSize = true;
        EditAnalyzeSbm1Short.Location = new Point(23, 177);
        EditAnalyzeSbm1Short.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeSbm1Short.Name = "EditAnalyzeSbm1Short";
        EditAnalyzeSbm1Short.Size = new Size(148, 19);
        EditAnalyzeSbm1Short.TabIndex = 127;
        EditAnalyzeSbm1Short.Text = "Maak verkoop signalen";
        EditAnalyzeSbm1Short.UseVisualStyleBackColor = true;
        // 
        // EditPlaySoundSbmSignal
        // 
        EditPlaySoundSbmSignal.AutoSize = true;
        EditPlaySoundSbmSignal.Location = new Point(23, 28);
        EditPlaySoundSbmSignal.Margin = new Padding(4, 3, 4, 3);
        EditPlaySoundSbmSignal.Name = "EditPlaySoundSbmSignal";
        EditPlaySoundSbmSignal.Size = new Size(84, 19);
        EditPlaySoundSbmSignal.TabIndex = 125;
        EditPlaySoundSbmSignal.Text = "Play sound";
        EditPlaySoundSbmSignal.UseVisualStyleBackColor = true;
        // 
        // buttonColorSbmShort
        // 
        buttonColorSbmShort.Location = new Point(209, 80);
        buttonColorSbmShort.Margin = new Padding(4, 3, 4, 3);
        buttonColorSbmShort.Name = "buttonColorSbmShort";
        buttonColorSbmShort.Size = new Size(88, 27);
        buttonColorSbmShort.TabIndex = 121;
        buttonColorSbmShort.Text = "Achtergrond";
        buttonColorSbmShort.UseVisualStyleBackColor = true;
        // 
        // EditSbmUseLowHigh
        // 
        EditSbmUseLowHigh.AutoSize = true;
        EditSbmUseLowHigh.Location = new Point(23, 251);
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
        label17.Location = new Point(615, 185);
        label17.Margin = new Padding(4, 0, 4, 0);
        label17.Name = "label17";
        label17.Size = new Size(77, 15);
        label17.TabIndex = 113;
        label17.Text = "Filter on BB%";
        // 
        // EditSbmBBMinPercentage
        // 
        EditSbmBBMinPercentage.DecimalPlaces = 2;
        EditSbmBBMinPercentage.Location = new Point(729, 182);
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
        EditSbmBBMaxPercentage.Location = new Point(814, 182);
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
        label22.Location = new Point(616, 155);
        label22.Margin = new Padding(4, 0, 4, 0);
        label22.Name = "label22";
        label22.Size = new Size(228, 15);
        label22.TabIndex = 112;
        label22.Text = "Extra instellingen voor alle SBM methodes";
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new Point(616, 377);
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
        EditSbmMa200AndMa20Percentage.Location = new Point(1056, 374);
        EditSbmMa200AndMa20Percentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa20Percentage.Name = "EditSbmMa200AndMa20Percentage";
        EditSbmMa200AndMa20Percentage.Size = new Size(57, 23);
        EditSbmMa200AndMa20Percentage.TabIndex = 111;
        EditSbmMa200AndMa20Percentage.Value = new decimal(new int[] { 3, 0, 0, 65536 });
        // 
        // label8
        // 
        label8.AutoSize = true;
        label8.Location = new Point(616, 402);
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
        EditSbmMa50AndMa20Percentage.Location = new Point(1056, 401);
        EditSbmMa50AndMa20Percentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa50AndMa20Percentage.Name = "EditSbmMa50AndMa20Percentage";
        EditSbmMa50AndMa20Percentage.Size = new Size(57, 23);
        EditSbmMa50AndMa20Percentage.TabIndex = 109;
        EditSbmMa50AndMa20Percentage.Value = new decimal(new int[] { 3, 0, 0, 65536 });
        // 
        // label7
        // 
        label7.AutoSize = true;
        label7.Location = new Point(616, 348);
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
        EditSbmMa200AndMa50Percentage.Location = new Point(1056, 348);
        EditSbmMa200AndMa50Percentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa50Percentage.Name = "EditSbmMa200AndMa50Percentage";
        EditSbmMa200AndMa50Percentage.Size = new Size(57, 23);
        EditSbmMa200AndMa50Percentage.TabIndex = 107;
        toolTip1.SetToolTip(EditSbmMa200AndMa50Percentage, "Percentage tussen de ma200 en ma50");
        EditSbmMa200AndMa50Percentage.Value = new decimal(new int[] { 3, 0, 0, 65536 });
        // 
        // EditSbmMa50AndMa20Lookback
        // 
        EditSbmMa50AndMa20Lookback.Location = new Point(1056, 312);
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
        EditSbmMa50AndMa20Crossing.Location = new Point(618, 311);
        EditSbmMa50AndMa20Crossing.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa50AndMa20Crossing.Name = "EditSbmMa50AndMa20Crossing";
        EditSbmMa50AndMa20Crossing.Size = new Size(402, 19);
        EditSbmMa50AndMa20Crossing.TabIndex = 104;
        EditSbmMa50AndMa20Crossing.Text = "Controleer op een kruising van de ma50 en ma20 in de laatste x candles";
        EditSbmMa50AndMa20Crossing.UseVisualStyleBackColor = true;
        // 
        // EditSbmMa200AndMa50Lookback
        // 
        EditSbmMa200AndMa50Lookback.Location = new Point(1056, 260);
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
        EditSbmMa200AndMa50Crossing.Location = new Point(618, 260);
        EditSbmMa200AndMa50Crossing.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa50Crossing.Name = "EditSbmMa200AndMa50Crossing";
        EditSbmMa200AndMa50Crossing.Size = new Size(408, 19);
        EditSbmMa200AndMa50Crossing.TabIndex = 102;
        EditSbmMa200AndMa50Crossing.Text = "Controleer op een kruising van de ma200 en ma50 in de laatste x candles";
        EditSbmMa200AndMa50Crossing.UseVisualStyleBackColor = true;
        // 
        // EditSbmMa200AndMa20Lookback
        // 
        EditSbmMa200AndMa20Lookback.Location = new Point(1056, 286);
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
        EditSbmMa200AndMa20Crossing.Location = new Point(618, 286);
        EditSbmMa200AndMa20Crossing.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa20Crossing.Name = "EditSbmMa200AndMa20Crossing";
        EditSbmMa200AndMa20Crossing.Size = new Size(408, 19);
        EditSbmMa200AndMa20Crossing.TabIndex = 100;
        EditSbmMa200AndMa20Crossing.Text = "Controleer op een kruising van de ma200 en ma20 in de laatste x candles";
        EditSbmMa200AndMa20Crossing.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeSbm3Short
        // 
        EditAnalyzeSbm3Short.AutoSize = true;
        EditAnalyzeSbm3Short.Location = new Point(23, 491);
        EditAnalyzeSbm3Short.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeSbm3Short.Name = "EditAnalyzeSbm3Short";
        EditAnalyzeSbm3Short.Size = new Size(148, 19);
        EditAnalyzeSbm3Short.TabIndex = 97;
        EditAnalyzeSbm3Short.Text = "Maak verkoop signalen";
        EditAnalyzeSbm3Short.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeSbm2Short
        // 
        EditAnalyzeSbm2Short.AutoSize = true;
        EditAnalyzeSbm2Short.Location = new Point(23, 318);
        EditAnalyzeSbm2Short.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeSbm2Short.Name = "EditAnalyzeSbm2Short";
        EditAnalyzeSbm2Short.Size = new Size(148, 19);
        EditAnalyzeSbm2Short.TabIndex = 96;
        EditAnalyzeSbm2Short.Text = "Maak verkoop signalen";
        EditAnalyzeSbm2Short.UseVisualStyleBackColor = true;
        // 
        // label12
        // 
        label12.AutoSize = true;
        label12.Location = new Point(23, 369);
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
        EditSbm2BbPercentage.Location = new Point(278, 367);
        EditSbm2BbPercentage.Margin = new Padding(4, 3, 4, 3);
        EditSbm2BbPercentage.Name = "EditSbm2BbPercentage";
        EditSbm2BbPercentage.Size = new Size(57, 23);
        EditSbm2BbPercentage.TabIndex = 95;
        EditSbm2BbPercentage.Value = new decimal(new int[] { 50, 0, 0, 131072 });
        // 
        // panelColorSbmShort
        // 
        panelColorSbmShort.BorderStyle = BorderStyle.FixedSingle;
        panelColorSbmShort.Location = new Point(101, 83);
        panelColorSbmShort.Margin = new Padding(4, 3, 4, 3);
        panelColorSbmShort.Name = "panelColorSbmShort";
        panelColorSbmShort.Size = new Size(70, 22);
        panelColorSbmShort.TabIndex = 122;
        // 
        // label13
        // 
        label13.AutoSize = true;
        label13.Location = new Point(23, 568);
        label13.Margin = new Padding(4, 0, 4, 0);
        label13.Name = "label13";
        label13.Size = new Size(95, 15);
        label13.TabIndex = 85;
        label13.Text = "Candle lookback";
        // 
        // EditSbm3CandlesForBBRecovery
        // 
        EditSbm3CandlesForBBRecovery.Location = new Point(278, 566);
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
        label14.Location = new Point(23, 544);
        label14.Margin = new Padding(4, 0, 4, 0);
        label14.Name = "label14";
        label14.Size = new Size(139, 15);
        label14.TabIndex = 83;
        label14.Text = "Percentage oprekking BB";
        // 
        // EditSbm3CandlesForBBRecoveryPercentage
        // 
        EditSbm3CandlesForBBRecoveryPercentage.Location = new Point(278, 542);
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
        label11.Location = new Point(23, 416);
        label11.Margin = new Padding(4, 0, 4, 0);
        label11.Name = "label11";
        label11.Size = new Size(95, 15);
        label11.TabIndex = 79;
        label11.Text = "Candle lookback";
        // 
        // EditSbm2CandlesLookbackCount
        // 
        EditSbm2CandlesLookbackCount.Location = new Point(278, 414);
        EditSbm2CandlesLookbackCount.Margin = new Padding(4, 3, 4, 3);
        EditSbm2CandlesLookbackCount.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbm2CandlesLookbackCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbm2CandlesLookbackCount.Name = "EditSbm2CandlesLookbackCount";
        EditSbm2CandlesLookbackCount.Size = new Size(57, 23);
        EditSbm2CandlesLookbackCount.TabIndex = 80;
        EditSbm2CandlesLookbackCount.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // EditAnalyzeSbm3Long
        // 
        EditAnalyzeSbm3Long.AutoSize = true;
        EditAnalyzeSbm3Long.Location = new Point(23, 517);
        EditAnalyzeSbm3Long.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeSbm3Long.Name = "EditAnalyzeSbm3Long";
        EditAnalyzeSbm3Long.Size = new Size(151, 19);
        EditAnalyzeSbm3Long.TabIndex = 69;
        EditAnalyzeSbm3Long.Text = "Maak aankoop signalen";
        EditAnalyzeSbm3Long.UseVisualStyleBackColor = true;
        // 
        // EditAnalyzeSbm2Long
        // 
        EditAnalyzeSbm2Long.AutoSize = true;
        EditAnalyzeSbm2Long.Location = new Point(23, 344);
        EditAnalyzeSbm2Long.Margin = new Padding(4, 3, 4, 3);
        EditAnalyzeSbm2Long.Name = "EditAnalyzeSbm2Long";
        EditAnalyzeSbm2Long.Size = new Size(151, 19);
        EditAnalyzeSbm2Long.TabIndex = 68;
        EditAnalyzeSbm2Long.Text = "Maak aankoop signalen";
        EditAnalyzeSbm2Long.UseVisualStyleBackColor = true;
        // 
        // tabSignalJump
        // 
        tabSignalJump.Controls.Add(buttonColorJumpLong);
        tabSignalJump.Controls.Add(panelColorJumpLong);
        tabSignalJump.Controls.Add(label78);
        tabSignalJump.Controls.Add(label76);
        tabSignalJump.Controls.Add(label33);
        tabSignalJump.Controls.Add(label34);
        tabSignalJump.Controls.Add(label5);
        tabSignalJump.Controls.Add(EditJumpCandlesLookbackCount);
        tabSignalJump.Controls.Add(EditJumpUseLowHighCalculation);
        tabSignalJump.Controls.Add(buttonColorJumpShort);
        tabSignalJump.Controls.Add(buttonPlaySoundCandleJumpDown);
        tabSignalJump.Controls.Add(buttonPlaySoundCandleJumpUp);
        tabSignalJump.Controls.Add(buttonSelectSoundCandleJumpDown);
        tabSignalJump.Controls.Add(panelColorJumpShort);
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
        // buttonColorJumpLong
        // 
        buttonColorJumpLong.Location = new Point(353, 130);
        buttonColorJumpLong.Margin = new Padding(4, 3, 4, 3);
        buttonColorJumpLong.Name = "buttonColorJumpLong";
        buttonColorJumpLong.Size = new Size(88, 27);
        buttonColorJumpLong.TabIndex = 152;
        buttonColorJumpLong.Text = "Achtergrond";
        buttonColorJumpLong.UseVisualStyleBackColor = true;
        // 
        // panelColorJumpLong
        // 
        panelColorJumpLong.BackColor = Color.Transparent;
        panelColorJumpLong.BorderStyle = BorderStyle.FixedSingle;
        panelColorJumpLong.Location = new Point(245, 133);
        panelColorJumpLong.Margin = new Padding(4, 3, 4, 3);
        panelColorJumpLong.Name = "panelColorJumpLong";
        panelColorJumpLong.Size = new Size(70, 22);
        panelColorJumpLong.TabIndex = 153;
        // 
        // label78
        // 
        label78.AutoSize = true;
        label78.Location = new Point(23, 83);
        label78.Margin = new Padding(4, 0, 4, 0);
        label78.Name = "label78";
        label78.Size = new Size(37, 15);
        label78.TabIndex = 151;
        label78.Text = "JUMP";
        // 
        // label76
        // 
        label76.AutoSize = true;
        label76.Location = new Point(25, 186);
        label76.Margin = new Padding(4, 0, 4, 0);
        label76.Name = "label76";
        label76.Size = new Size(68, 15);
        label76.TabIndex = 138;
        label76.Text = "Instellingen";
        // 
        // label33
        // 
        label33.AutoSize = true;
        label33.Location = new Point(462, 133);
        label33.Margin = new Padding(4, 0, 4, 0);
        label33.Name = "label33";
        label33.Size = new Size(121, 15);
        label33.TabIndex = 137;
        label33.Text = "Jump down soundfile";
        // 
        // label34
        // 
        label34.AutoSize = true;
        label34.Location = new Point(461, 107);
        label34.Margin = new Padding(4, 0, 4, 0);
        label34.Name = "label34";
        label34.Size = new Size(105, 15);
        label34.TabIndex = 136;
        label34.Text = "Jump up soundfile";
        // 
        // label5
        // 
        label5.AutoSize = true;
        label5.Location = new Point(25, 239);
        label5.Margin = new Padding(4, 0, 4, 0);
        label5.Name = "label5";
        label5.Size = new Size(95, 15);
        label5.TabIndex = 123;
        label5.Text = "Candle lookback";
        // 
        // EditJumpCandlesLookbackCount
        // 
        EditJumpCandlesLookbackCount.Location = new Point(172, 238);
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
        EditJumpUseLowHighCalculation.Location = new Point(25, 267);
        EditJumpUseLowHighCalculation.Margin = new Padding(4, 3, 4, 3);
        EditJumpUseLowHighCalculation.Name = "EditJumpUseLowHighCalculation";
        EditJumpUseLowHighCalculation.Size = new Size(250, 19);
        EditJumpUseLowHighCalculation.TabIndex = 122;
        EditJumpUseLowHighCalculation.Text = "Bereken via de high/low ipv de open/close";
        EditJumpUseLowHighCalculation.UseVisualStyleBackColor = true;
        // 
        // buttonColorJumpShort
        // 
        buttonColorJumpShort.Location = new Point(353, 101);
        buttonColorJumpShort.Margin = new Padding(4, 3, 4, 3);
        buttonColorJumpShort.Name = "buttonColorJumpShort";
        buttonColorJumpShort.Size = new Size(88, 27);
        buttonColorJumpShort.TabIndex = 120;
        buttonColorJumpShort.Text = "Achtergrond";
        buttonColorJumpShort.UseVisualStyleBackColor = true;
        // 
        // buttonPlaySoundCandleJumpDown
        // 
        buttonPlaySoundCandleJumpDown.Image = Properties.Resources.volume;
        buttonPlaySoundCandleJumpDown.Location = new Point(858, 129);
        buttonPlaySoundCandleJumpDown.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySoundCandleJumpDown.Name = "buttonPlaySoundCandleJumpDown";
        buttonPlaySoundCandleJumpDown.Size = new Size(23, 23);
        buttonPlaySoundCandleJumpDown.TabIndex = 119;
        buttonPlaySoundCandleJumpDown.UseVisualStyleBackColor = true;
        // 
        // buttonPlaySoundCandleJumpUp
        // 
        buttonPlaySoundCandleJumpUp.Image = Properties.Resources.volume;
        buttonPlaySoundCandleJumpUp.Location = new Point(858, 104);
        buttonPlaySoundCandleJumpUp.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySoundCandleJumpUp.Name = "buttonPlaySoundCandleJumpUp";
        buttonPlaySoundCandleJumpUp.Size = new Size(23, 23);
        buttonPlaySoundCandleJumpUp.TabIndex = 118;
        buttonPlaySoundCandleJumpUp.UseVisualStyleBackColor = true;
        // 
        // buttonSelectSoundCandleJumpDown
        // 
        buttonSelectSoundCandleJumpDown.Location = new Point(828, 129);
        buttonSelectSoundCandleJumpDown.Margin = new Padding(4, 3, 4, 3);
        buttonSelectSoundCandleJumpDown.Name = "buttonSelectSoundCandleJumpDown";
        buttonSelectSoundCandleJumpDown.Size = new Size(23, 23);
        buttonSelectSoundCandleJumpDown.TabIndex = 115;
        buttonSelectSoundCandleJumpDown.UseVisualStyleBackColor = true;
        // 
        // panelColorJumpShort
        // 
        panelColorJumpShort.BackColor = Color.Transparent;
        panelColorJumpShort.BorderStyle = BorderStyle.FixedSingle;
        panelColorJumpShort.Location = new Point(245, 104);
        panelColorJumpShort.Margin = new Padding(4, 3, 4, 3);
        panelColorJumpShort.Name = "panelColorJumpShort";
        panelColorJumpShort.Size = new Size(70, 22);
        panelColorJumpShort.TabIndex = 121;
        // 
        // EditSoundFileCandleJumpDown
        // 
        EditSoundFileCandleJumpDown.Location = new Point(594, 129);
        EditSoundFileCandleJumpDown.Margin = new Padding(4, 3, 4, 3);
        EditSoundFileCandleJumpDown.Name = "EditSoundFileCandleJumpDown";
        EditSoundFileCandleJumpDown.Size = new Size(227, 23);
        EditSoundFileCandleJumpDown.TabIndex = 114;
        // 
        // EditSoundFileCandleJumpUp
        // 
        EditSoundFileCandleJumpUp.Location = new Point(594, 104);
        EditSoundFileCandleJumpUp.Margin = new Padding(4, 3, 4, 3);
        EditSoundFileCandleJumpUp.Name = "EditSoundFileCandleJumpUp";
        EditSoundFileCandleJumpUp.Size = new Size(227, 23);
        EditSoundFileCandleJumpUp.TabIndex = 112;
        // 
        // buttonSelectSoundCandleJumpUp
        // 
        buttonSelectSoundCandleJumpUp.Location = new Point(828, 104);
        buttonSelectSoundCandleJumpUp.Margin = new Padding(4, 3, 4, 3);
        buttonSelectSoundCandleJumpUp.Name = "buttonSelectSoundCandleJumpUp";
        buttonSelectSoundCandleJumpUp.Size = new Size(23, 23);
        buttonSelectSoundCandleJumpUp.TabIndex = 109;
        buttonSelectSoundCandleJumpUp.UseVisualStyleBackColor = true;
        // 
        // EditPlaySpeechCandleJumpSignal
        // 
        EditPlaySpeechCandleJumpSignal.AutoSize = true;
        EditPlaySpeechCandleJumpSignal.Location = new Point(23, 51);
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
        label3.Location = new Point(25, 208);
        label3.Margin = new Padding(4, 0, 4, 0);
        label3.Name = "label3";
        label3.Size = new Size(98, 15);
        label3.TabIndex = 116;
        label3.Text = "Jump percentage";
        // 
        // EditPlaySoundCandleJumpSignal
        // 
        EditPlaySoundCandleJumpSignal.AutoSize = true;
        EditPlaySoundCandleJumpSignal.Location = new Point(23, 24);
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
        EditAnalyzeCandleJumpUp.Location = new Point(23, 109);
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
        EditAnalyzeCandleJumpDown.Location = new Point(23, 135);
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
        EditAnalysisCandleJumpPercentage.Location = new Point(172, 208);
        EditAnalysisCandleJumpPercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisCandleJumpPercentage.Name = "EditAnalysisCandleJumpPercentage";
        EditAnalysisCandleJumpPercentage.Size = new Size(56, 23);
        EditAnalysisCandleJumpPercentage.TabIndex = 117;
        // 
        // tabPageTrading
        // 
        tabPageTrading.Controls.Add(groupBoxInstap);
        tabPageTrading.Controls.Add(groupBoxFutures);
        tabPageTrading.Controls.Add(EditApiSecret);
        tabPageTrading.Controls.Add(EditApiKey);
        tabPageTrading.Controls.Add(label80);
        tabPageTrading.Controls.Add(label65);
        tabPageTrading.Controls.Add(EditLockProfits);
        tabPageTrading.Controls.Add(label59);
        tabPageTrading.Controls.Add(EditLogCanceledOrders);
        tabPageTrading.Controls.Add(EditSoundTradeNotification);
        tabPageTrading.Controls.Add(EditDisableNewPositions);
        tabPageTrading.Controls.Add(label83);
        tabPageTrading.Controls.Add(EditBuyStepInMethod);
        tabPageTrading.Controls.Add(label82);
        tabPageTrading.Controls.Add(EditDcaStepInMethod);
        tabPageTrading.Controls.Add(EditTradeViaExchange);
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
        // groupBoxInstap
        // 
        groupBoxInstap.Controls.Add(EditCheckIncreasingMacd);
        groupBoxInstap.Controls.Add(EditCheckIncreasingStoch);
        groupBoxInstap.Controls.Add(EditCheckIncreasingRsi);
        groupBoxInstap.Location = new Point(17, 461);
        groupBoxInstap.Name = "groupBoxInstap";
        groupBoxInstap.Size = new Size(234, 101);
        groupBoxInstap.TabIndex = 282;
        groupBoxInstap.TabStop = false;
        groupBoxInstap.Text = "Instap condities";
        // 
        // EditCheckIncreasingMacd
        // 
        EditCheckIncreasingMacd.AutoSize = true;
        EditCheckIncreasingMacd.Location = new Point(20, 47);
        EditCheckIncreasingMacd.Margin = new Padding(4, 3, 4, 3);
        EditCheckIncreasingMacd.Name = "EditCheckIncreasingMacd";
        EditCheckIncreasingMacd.Size = new Size(184, 19);
        EditCheckIncreasingMacd.TabIndex = 277;
        EditCheckIncreasingMacd.Text = "MACD moet oplopen/aflopen";
        EditCheckIncreasingMacd.UseVisualStyleBackColor = true;
        // 
        // EditCheckIncreasingStoch
        // 
        EditCheckIncreasingStoch.AutoSize = true;
        EditCheckIncreasingStoch.Location = new Point(20, 68);
        EditCheckIncreasingStoch.Margin = new Padding(4, 3, 4, 3);
        EditCheckIncreasingStoch.Name = "EditCheckIncreasingStoch";
        EditCheckIncreasingStoch.Size = new Size(199, 19);
        EditCheckIncreasingStoch.TabIndex = 276;
        EditCheckIncreasingStoch.Text = "Stoch moet in niemandsland zijn";
        EditCheckIncreasingStoch.UseVisualStyleBackColor = true;
        // 
        // EditCheckIncreasingRsi
        // 
        EditCheckIncreasingRsi.AutoSize = true;
        EditCheckIncreasingRsi.Location = new Point(20, 26);
        EditCheckIncreasingRsi.Margin = new Padding(4, 3, 4, 3);
        EditCheckIncreasingRsi.Name = "EditCheckIncreasingRsi";
        EditCheckIncreasingRsi.Size = new Size(165, 19);
        EditCheckIncreasingRsi.TabIndex = 275;
        EditCheckIncreasingRsi.Text = "RSI moet oplopen/aflopen";
        EditCheckIncreasingRsi.UseVisualStyleBackColor = true;
        // 
        // groupBoxFutures
        // 
        groupBoxFutures.Controls.Add(label19);
        groupBoxFutures.Controls.Add(EditCrossOrIsolated);
        groupBoxFutures.Controls.Add(label23);
        groupBoxFutures.Controls.Add(EditLeverage);
        groupBoxFutures.Location = new Point(17, 353);
        groupBoxFutures.Name = "groupBoxFutures";
        groupBoxFutures.Size = new Size(234, 86);
        groupBoxFutures.TabIndex = 281;
        groupBoxFutures.TabStop = false;
        groupBoxFutures.Text = "Futures";
        // 
        // label19
        // 
        label19.AutoSize = true;
        label19.Location = new Point(5, 26);
        label19.Margin = new Padding(4, 0, 4, 0);
        label19.Name = "label19";
        label19.Size = new Size(45, 15);
        label19.TabIndex = 274;
        label19.Text = "Margin";
        // 
        // EditCrossOrIsolated
        // 
        EditCrossOrIsolated.DropDownStyle = ComboBoxStyle.DropDownList;
        EditCrossOrIsolated.FormattingEnabled = true;
        EditCrossOrIsolated.Items.AddRange(new object[] { "Cross", "Isolated" });
        EditCrossOrIsolated.Location = new Point(130, 22);
        EditCrossOrIsolated.Margin = new Padding(4, 3, 4, 3);
        EditCrossOrIsolated.Name = "EditCrossOrIsolated";
        EditCrossOrIsolated.Size = new Size(87, 23);
        EditCrossOrIsolated.TabIndex = 273;
        // 
        // label23
        // 
        label23.AutoSize = true;
        label23.Location = new Point(5, 54);
        label23.Margin = new Padding(4, 0, 4, 0);
        label23.Name = "label23";
        label23.Size = new Size(54, 15);
        label23.TabIndex = 272;
        label23.Text = "Leverage";
        // 
        // EditLeverage
        // 
        EditLeverage.DecimalPlaces = 2;
        EditLeverage.Location = new Point(129, 51);
        EditLeverage.Margin = new Padding(4, 3, 4, 3);
        EditLeverage.Name = "EditLeverage";
        EditLeverage.Size = new Size(88, 23);
        EditLeverage.TabIndex = 271;
        EditLeverage.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // EditApiSecret
        // 
        EditApiSecret.Location = new Point(148, 164);
        EditApiSecret.Margin = new Padding(4, 3, 4, 3);
        EditApiSecret.Name = "EditApiSecret";
        EditApiSecret.PasswordChar = '*';
        EditApiSecret.Size = new Size(88, 23);
        EditApiSecret.TabIndex = 278;
        // 
        // EditApiKey
        // 
        EditApiKey.Location = new Point(148, 138);
        EditApiKey.Margin = new Padding(4, 3, 4, 3);
        EditApiKey.Name = "EditApiKey";
        EditApiKey.PasswordChar = '*';
        EditApiKey.Size = new Size(88, 23);
        EditApiKey.TabIndex = 276;
        // 
        // label80
        // 
        label80.AutoSize = true;
        label80.Location = new Point(17, 167);
        label80.Margin = new Padding(4, 0, 4, 0);
        label80.Name = "label80";
        label80.Size = new Size(59, 15);
        label80.TabIndex = 279;
        label80.Text = "API secret";
        // 
        // label65
        // 
        label65.AutoSize = true;
        label65.Location = new Point(17, 141);
        label65.Margin = new Padding(4, 0, 4, 0);
        label65.Name = "label65";
        label65.Size = new Size(46, 15);
        label65.TabIndex = 277;
        label65.Text = "API key";
        // 
        // EditLockProfits
        // 
        EditLockProfits.AutoSize = true;
        EditLockProfits.Enabled = false;
        EditLockProfits.Font = new Font("Segoe UI", 9F, FontStyle.Strikeout);
        EditLockProfits.Location = new Point(324, 403);
        EditLockProfits.Margin = new Padding(4, 3, 4, 3);
        EditLockProfits.Name = "EditLockProfits";
        EditLockProfits.Size = new Size(88, 19);
        EditLockProfits.TabIndex = 275;
        EditLockProfits.Text = "Lock profits";
        EditLockProfits.UseVisualStyleBackColor = true;
        // 
        // label59
        // 
        label59.AutoSize = true;
        label59.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        label59.Location = new Point(-170, 282);
        label59.Margin = new Padding(4, 0, 4, 0);
        label59.Name = "label59";
        label59.Size = new Size(97, 15);
        label59.TabIndex = 271;
        label59.Text = "Instap condities:";
        // 
        // EditLogCanceledOrders
        // 
        EditLogCanceledOrders.AutoSize = true;
        EditLogCanceledOrders.Location = new Point(17, 111);
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
        EditSoundTradeNotification.Location = new Point(17, 86);
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
        EditDisableNewPositions.Location = new Point(17, 61);
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
        label83.Location = new Point(324, 60);
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
        EditBuyStepInMethod.Location = new Point(492, 52);
        EditBuyStepInMethod.Margin = new Padding(4, 3, 4, 3);
        EditBuyStepInMethod.Name = "EditBuyStepInMethod";
        EditBuyStepInMethod.Size = new Size(200, 23);
        EditBuyStepInMethod.TabIndex = 262;
        // 
        // label82
        // 
        label82.AutoSize = true;
        label82.Location = new Point(324, 167);
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
        EditDcaStepInMethod.Location = new Point(492, 159);
        EditDcaStepInMethod.Margin = new Padding(4, 3, 4, 3);
        EditDcaStepInMethod.Name = "EditDcaStepInMethod";
        EditDcaStepInMethod.Size = new Size(200, 23);
        EditDcaStepInMethod.TabIndex = 260;
        // 
        // EditTradeViaExchange
        // 
        EditTradeViaExchange.AutoSize = true;
        EditTradeViaExchange.Location = new Point(17, 36);
        EditTradeViaExchange.Margin = new Padding(4, 3, 4, 3);
        EditTradeViaExchange.Name = "EditTradeViaExchange";
        EditTradeViaExchange.Size = new Size(148, 19);
        EditTradeViaExchange.TabIndex = 255;
        EditTradeViaExchange.Text = "Traden op de exchange";
        EditTradeViaExchange.UseVisualStyleBackColor = true;
        // 
        // label63
        // 
        label63.AutoSize = true;
        label63.Location = new Point(324, 347);
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
        EditSellMethod.Location = new Point(492, 344);
        EditSellMethod.Margin = new Padding(4, 3, 4, 3);
        EditSellMethod.Name = "EditSellMethod";
        EditSellMethod.Size = new Size(200, 23);
        EditSellMethod.TabIndex = 253;
        // 
        // EditTradeViaPaperTrading
        // 
        EditTradeViaPaperTrading.AutoSize = true;
        EditTradeViaPaperTrading.Location = new Point(17, 14);
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
        label60.Location = new Point(324, 191);
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
        EditDcaOrderMethod.Location = new Point(492, 187);
        EditDcaOrderMethod.Margin = new Padding(4, 3, 4, 3);
        EditDcaOrderMethod.Name = "EditDcaOrderMethod";
        EditDcaOrderMethod.Size = new Size(200, 23);
        EditDcaOrderMethod.TabIndex = 249;
        // 
        // label36
        // 
        label36.AutoSize = true;
        label36.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        label36.Location = new Point(324, 435);
        label36.Margin = new Padding(4, 0, 4, 0);
        label36.Name = "label36";
        label36.Size = new Size(63, 15);
        label36.TabIndex = 248;
        label36.Text = "Stopploss:";
        // 
        // label81
        // 
        label81.AutoSize = true;
        label81.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        label81.Location = new Point(324, 324);
        label81.Margin = new Padding(4, 0, 4, 0);
        label81.Name = "label81";
        label81.Size = new Size(57, 15);
        label81.TabIndex = 247;
        label81.Text = "Verkoop:";
        // 
        // label57
        // 
        label57.AutoSize = true;
        label57.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        label57.Location = new Point(324, 141);
        label57.Margin = new Padding(4, 0, 4, 0);
        label57.Name = "label57";
        label57.Size = new Size(52, 15);
        label57.TabIndex = 246;
        label57.Text = "Bijkoop:";
        // 
        // label54
        // 
        label54.AutoSize = true;
        label54.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        label54.Location = new Point(324, 36);
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
        groupBoxSlots.Font = new Font("Segoe UI", 9F);
        groupBoxSlots.Location = new Point(17, 223);
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
        label56.Location = new Point(5, 79);
        label56.Margin = new Padding(4, 0, 4, 0);
        label56.Name = "label56";
        label56.Size = new Size(31, 15);
        label56.TabIndex = 198;
        label56.Text = "Base";
        // 
        // EditSlotsMaximalBase
        // 
        EditSlotsMaximalBase.Location = new Point(129, 77);
        EditSlotsMaximalBase.Margin = new Padding(4, 3, 4, 3);
        EditSlotsMaximalBase.Name = "EditSlotsMaximalBase";
        EditSlotsMaximalBase.Size = new Size(88, 23);
        EditSlotsMaximalBase.TabIndex = 199;
        EditSlotsMaximalBase.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label62
        // 
        label62.AutoSize = true;
        label62.Location = new Point(324, 82);
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
        EditBuyOrderMethod.Location = new Point(492, 79);
        EditBuyOrderMethod.Margin = new Padding(4, 3, 4, 3);
        EditBuyOrderMethod.Name = "EditBuyOrderMethod";
        EditBuyOrderMethod.Size = new Size(200, 23);
        EditBuyOrderMethod.TabIndex = 213;
        // 
        // EditDcaCount
        // 
        EditDcaCount.Font = new Font("Segoe UI", 9F);
        EditDcaCount.Location = new Point(494, 267);
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
        label67.Font = new Font("Segoe UI", 9F);
        label67.Location = new Point(324, 269);
        label67.Margin = new Padding(4, 0, 4, 0);
        label67.Name = "label67";
        label67.Size = new Size(90, 15);
        label67.TabIndex = 207;
        label67.Text = "Aantal bijkopen";
        // 
        // label68
        // 
        label68.AutoSize = true;
        label68.Location = new Point(324, 243);
        label68.Margin = new Padding(4, 0, 4, 0);
        label68.Name = "label68";
        label68.Size = new Size(81, 15);
        label68.TabIndex = 202;
        label68.Text = "Bijkoop factor";
        // 
        // EditDcaFactor
        // 
        EditDcaFactor.DecimalPlaces = 2;
        EditDcaFactor.Location = new Point(492, 242);
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
        label69.Location = new Point(324, 218);
        label69.Margin = new Padding(4, 0, 4, 0);
        label69.Name = "label69";
        label69.Size = new Size(91, 15);
        label69.TabIndex = 205;
        label69.Text = "Bijkopen op (%)";
        // 
        // EditDcaPercentage
        // 
        EditDcaPercentage.DecimalPlaces = 2;
        EditDcaPercentage.Location = new Point(492, 215);
        EditDcaPercentage.Margin = new Padding(4, 3, 4, 3);
        EditDcaPercentage.Name = "EditDcaPercentage";
        EditDcaPercentage.Size = new Size(88, 23);
        EditDcaPercentage.TabIndex = 204;
        EditDcaPercentage.Value = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // EditGlobalStopLimitPercentage
        // 
        EditGlobalStopLimitPercentage.DecimalPlaces = 2;
        EditGlobalStopLimitPercentage.Font = new Font("Segoe UI", 9F, FontStyle.Strikeout);
        EditGlobalStopLimitPercentage.Location = new Point(492, 488);
        EditGlobalStopLimitPercentage.Margin = new Padding(4, 3, 4, 3);
        EditGlobalStopLimitPercentage.Name = "EditGlobalStopLimitPercentage";
        EditGlobalStopLimitPercentage.Size = new Size(88, 23);
        EditGlobalStopLimitPercentage.TabIndex = 200;
        // 
        // label70
        // 
        label70.AutoSize = true;
        label70.Font = new Font("Segoe UI", 9F, FontStyle.Strikeout);
        label70.Location = new Point(324, 490);
        label70.Margin = new Padding(4, 0, 4, 0);
        label70.Name = "label70";
        label70.Size = new Size(107, 15);
        label70.TabIndex = 201;
        label70.Text = "OCO stop limit (%)";
        // 
        // EditGlobalStopPercentage
        // 
        EditGlobalStopPercentage.DecimalPlaces = 2;
        EditGlobalStopPercentage.Font = new Font("Segoe UI", 9F, FontStyle.Strikeout);
        EditGlobalStopPercentage.Location = new Point(492, 461);
        EditGlobalStopPercentage.Margin = new Padding(4, 3, 4, 3);
        EditGlobalStopPercentage.Name = "EditGlobalStopPercentage";
        EditGlobalStopPercentage.Size = new Size(88, 23);
        EditGlobalStopPercentage.TabIndex = 198;
        // 
        // label71
        // 
        label71.AutoSize = true;
        label71.Font = new Font("Segoe UI", 9F, FontStyle.Strikeout);
        label71.Location = new Point(324, 463);
        label71.Margin = new Padding(4, 0, 4, 0);
        label71.Name = "label71";
        label71.Size = new Size(109, 15);
        label71.TabIndex = 199;
        label71.Text = "OCO stop price (%)";
        // 
        // label72
        // 
        label72.AutoSize = true;
        label72.Location = new Point(324, 374);
        label72.Margin = new Padding(4, 0, 4, 0);
        label72.Name = "label72";
        label72.Size = new Size(120, 15);
        label72.TabIndex = 194;
        label72.Text = "Winst percentage (%)";
        // 
        // EditProfitPercentage
        // 
        EditProfitPercentage.DecimalPlaces = 2;
        EditProfitPercentage.Location = new Point(492, 372);
        EditProfitPercentage.Margin = new Padding(4, 3, 4, 3);
        EditProfitPercentage.Name = "EditProfitPercentage";
        EditProfitPercentage.Size = new Size(88, 23);
        EditProfitPercentage.TabIndex = 195;
        EditProfitPercentage.Value = new decimal(new int[] { 75, 0, 0, 131072 });
        // 
        // label73
        // 
        label73.AutoSize = true;
        label73.Location = new Point(324, 296);
        label73.Margin = new Padding(4, 0, 4, 0);
        label73.Name = "label73";
        label73.Size = new Size(114, 15);
        label73.TabIndex = 197;
        label73.Text = "Cool down time (m)";
        // 
        // EditGlobalBuyCooldownTime
        // 
        EditGlobalBuyCooldownTime.Location = new Point(494, 294);
        EditGlobalBuyCooldownTime.Margin = new Padding(4, 3, 4, 3);
        EditGlobalBuyCooldownTime.Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 });
        EditGlobalBuyCooldownTime.Name = "EditGlobalBuyCooldownTime";
        EditGlobalBuyCooldownTime.Size = new Size(88, 23);
        EditGlobalBuyCooldownTime.TabIndex = 196;
        // 
        // EditGlobalBuyVarying
        // 
        EditGlobalBuyVarying.DecimalPlaces = 2;
        EditGlobalBuyVarying.Location = new Point(148, 191);
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
        label47.Location = new Point(17, 193);
        label47.Margin = new Padding(4, 0, 4, 0);
        label47.Name = "label47";
        label47.Size = new Size(108, 15);
        label47.TabIndex = 177;
        label47.Text = "Instap verlagen (%)";
        // 
        // label46
        // 
        label46.AutoSize = true;
        label46.Location = new Point(324, 109);
        label46.Margin = new Padding(4, 0, 4, 0);
        label46.Name = "label46";
        label46.Size = new Size(77, 15);
        label46.TabIndex = 176;
        label46.Text = "Remove time";
        // 
        // EditGlobalBuyRemoveTime
        // 
        EditGlobalBuyRemoveTime.Location = new Point(492, 107);
        EditGlobalBuyRemoveTime.Margin = new Padding(4, 3, 4, 3);
        EditGlobalBuyRemoveTime.Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 });
        EditGlobalBuyRemoveTime.Name = "EditGlobalBuyRemoveTime";
        EditGlobalBuyRemoveTime.Size = new Size(88, 23);
        EditGlobalBuyRemoveTime.TabIndex = 175;
        EditGlobalBuyRemoveTime.Value = new decimal(new int[] { 5, 0, 0, 0 });
        // 
        // tabPageLong
        // 
        tabPageLong.Controls.Add(tabControlLong);
        tabPageLong.Location = new Point(4, 27);
        tabPageLong.Name = "tabPageLong";
        tabPageLong.Padding = new Padding(3);
        tabPageLong.Size = new Size(1232, 777);
        tabPageLong.TabIndex = 13;
        tabPageLong.Text = "Long";
        tabPageLong.UseVisualStyleBackColor = true;
        // 
        // tabControlLong
        // 
        tabControlLong.Controls.Add(tabPageTradingLong);
        tabControlLong.Controls.Add(tabPageLongWhiteList);
        tabControlLong.Controls.Add(tabPageLongBlackList);
        tabControlLong.Dock = DockStyle.Fill;
        tabControlLong.Location = new Point(3, 3);
        tabControlLong.Name = "tabControlLong";
        tabControlLong.SelectedIndex = 0;
        tabControlLong.Size = new Size(1226, 771);
        tabControlLong.TabIndex = 0;
        // 
        // tabPageTradingLong
        // 
        tabPageTradingLong.Controls.Add(groupTrendOnInterval);
        tabPageTradingLong.Controls.Add(groupBox2);
        tabPageTradingLong.Controls.Add(groupTradeOnInterval);
        tabPageTradingLong.Location = new Point(4, 24);
        tabPageTradingLong.Name = "tabPageTradingLong";
        tabPageTradingLong.Padding = new Padding(3);
        tabPageTradingLong.Size = new Size(1218, 743);
        tabPageTradingLong.TabIndex = 2;
        tabPageTradingLong.Text = "Trading long";
        tabPageTradingLong.UseVisualStyleBackColor = true;
        // 
        // groupTrendOnInterval
        // 
        groupTrendOnInterval.Controls.Add(EditTrendLong6h);
        groupTrendOnInterval.Controls.Add(EditTrendLong8h);
        groupTrendOnInterval.Controls.Add(EditTrendLong12h);
        groupTrendOnInterval.Controls.Add(EditTrendLong1d);
        groupTrendOnInterval.Controls.Add(EditTrendLong1h);
        groupTrendOnInterval.Controls.Add(EditTrendLong2h);
        groupTrendOnInterval.Controls.Add(EditTrendLong4h);
        groupTrendOnInterval.Controls.Add(EditTrendLong1m);
        groupTrendOnInterval.Controls.Add(EditTrendLong2m);
        groupTrendOnInterval.Controls.Add(EditTrendLong3m);
        groupTrendOnInterval.Controls.Add(EditTrendLong5m);
        groupTrendOnInterval.Controls.Add(EditTrendLong10m);
        groupTrendOnInterval.Controls.Add(EditTrendLong15m);
        groupTrendOnInterval.Controls.Add(EditTrendLong30m);
        groupTrendOnInterval.Location = new Point(275, 60);
        groupTrendOnInterval.Margin = new Padding(4, 3, 4, 3);
        groupTrendOnInterval.Name = "groupTrendOnInterval";
        groupTrendOnInterval.Padding = new Padding(4, 3, 4, 3);
        groupTrendOnInterval.Size = new Size(234, 218);
        groupTrendOnInterval.TabIndex = 283;
        groupTrendOnInterval.TabStop = false;
        groupTrendOnInterval.Text = "Instap trend UP";
        // 
        // EditTrendLong6h
        // 
        EditTrendLong6h.AutoSize = true;
        EditTrendLong6h.Location = new Point(120, 102);
        EditTrendLong6h.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong6h.Name = "EditTrendLong6h";
        EditTrendLong6h.Size = new Size(53, 19);
        EditTrendLong6h.TabIndex = 153;
        EditTrendLong6h.Text = "6 uur";
        EditTrendLong6h.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong8h
        // 
        EditTrendLong8h.AutoSize = true;
        EditTrendLong8h.Location = new Point(120, 131);
        EditTrendLong8h.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong8h.Name = "EditTrendLong8h";
        EditTrendLong8h.Size = new Size(53, 19);
        EditTrendLong8h.TabIndex = 154;
        EditTrendLong8h.Text = "8 uur";
        EditTrendLong8h.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong12h
        // 
        EditTrendLong12h.AutoSize = true;
        EditTrendLong12h.Location = new Point(120, 158);
        EditTrendLong12h.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong12h.Name = "EditTrendLong12h";
        EditTrendLong12h.Size = new Size(59, 19);
        EditTrendLong12h.TabIndex = 155;
        EditTrendLong12h.Text = "12 uur";
        EditTrendLong12h.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong1d
        // 
        EditTrendLong1d.AutoSize = true;
        EditTrendLong1d.Location = new Point(120, 185);
        EditTrendLong1d.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong1d.Name = "EditTrendLong1d";
        EditTrendLong1d.Size = new Size(55, 19);
        EditTrendLong1d.TabIndex = 156;
        EditTrendLong1d.Text = "1 dag";
        EditTrendLong1d.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong1h
        // 
        EditTrendLong1h.AutoSize = true;
        EditTrendLong1h.Location = new Point(120, 27);
        EditTrendLong1h.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong1h.Name = "EditTrendLong1h";
        EditTrendLong1h.Size = new Size(53, 19);
        EditTrendLong1h.TabIndex = 150;
        EditTrendLong1h.Text = "1 uur";
        EditTrendLong1h.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong2h
        // 
        EditTrendLong2h.AutoSize = true;
        EditTrendLong2h.Location = new Point(120, 50);
        EditTrendLong2h.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong2h.Name = "EditTrendLong2h";
        EditTrendLong2h.Size = new Size(53, 19);
        EditTrendLong2h.TabIndex = 151;
        EditTrendLong2h.Text = "2 uur";
        EditTrendLong2h.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong4h
        // 
        EditTrendLong4h.AutoSize = true;
        EditTrendLong4h.Location = new Point(120, 77);
        EditTrendLong4h.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong4h.Name = "EditTrendLong4h";
        EditTrendLong4h.Size = new Size(53, 19);
        EditTrendLong4h.TabIndex = 152;
        EditTrendLong4h.Text = "4 uur";
        EditTrendLong4h.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong1m
        // 
        EditTrendLong1m.AutoSize = true;
        EditTrendLong1m.Location = new Point(16, 27);
        EditTrendLong1m.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong1m.Name = "EditTrendLong1m";
        EditTrendLong1m.Size = new Size(56, 19);
        EditTrendLong1m.TabIndex = 143;
        EditTrendLong1m.Text = "1 min";
        EditTrendLong1m.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong2m
        // 
        EditTrendLong2m.AutoSize = true;
        EditTrendLong2m.Location = new Point(16, 52);
        EditTrendLong2m.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong2m.Name = "EditTrendLong2m";
        EditTrendLong2m.Size = new Size(56, 19);
        EditTrendLong2m.TabIndex = 144;
        EditTrendLong2m.Text = "2 min";
        EditTrendLong2m.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong3m
        // 
        EditTrendLong3m.AutoSize = true;
        EditTrendLong3m.Location = new Point(16, 79);
        EditTrendLong3m.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong3m.Name = "EditTrendLong3m";
        EditTrendLong3m.Size = new Size(56, 19);
        EditTrendLong3m.TabIndex = 145;
        EditTrendLong3m.Text = "3 min";
        EditTrendLong3m.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong5m
        // 
        EditTrendLong5m.AutoSize = true;
        EditTrendLong5m.Location = new Point(16, 105);
        EditTrendLong5m.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong5m.Name = "EditTrendLong5m";
        EditTrendLong5m.Size = new Size(56, 19);
        EditTrendLong5m.TabIndex = 146;
        EditTrendLong5m.Text = "5 min";
        EditTrendLong5m.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong10m
        // 
        EditTrendLong10m.AutoSize = true;
        EditTrendLong10m.Location = new Point(16, 132);
        EditTrendLong10m.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong10m.Name = "EditTrendLong10m";
        EditTrendLong10m.Size = new Size(62, 19);
        EditTrendLong10m.TabIndex = 147;
        EditTrendLong10m.Text = "10 min";
        EditTrendLong10m.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong15m
        // 
        EditTrendLong15m.AutoSize = true;
        EditTrendLong15m.Location = new Point(16, 158);
        EditTrendLong15m.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong15m.Name = "EditTrendLong15m";
        EditTrendLong15m.Size = new Size(62, 19);
        EditTrendLong15m.TabIndex = 148;
        EditTrendLong15m.Text = "15 min";
        EditTrendLong15m.UseVisualStyleBackColor = true;
        // 
        // EditTrendLong30m
        // 
        EditTrendLong30m.AutoSize = true;
        EditTrendLong30m.Location = new Point(16, 185);
        EditTrendLong30m.Margin = new Padding(4, 3, 4, 3);
        EditTrendLong30m.Name = "EditTrendLong30m";
        EditTrendLong30m.Size = new Size(62, 19);
        EditTrendLong30m.TabIndex = 149;
        EditTrendLong30m.Text = "30 min";
        EditTrendLong30m.UseVisualStyleBackColor = true;
        // 
        // groupBox2
        // 
        groupBox2.Controls.Add(EditBarometer15mBotLong);
        groupBox2.Controls.Add(label27);
        groupBox2.Controls.Add(EditBarometer24hBotLong);
        groupBox2.Controls.Add(label42);
        groupBox2.Controls.Add(EditBarometer4hBotLong);
        groupBox2.Controls.Add(label43);
        groupBox2.Controls.Add(EditBarometer1hBotLong);
        groupBox2.Controls.Add(label44);
        groupBox2.Controls.Add(label45);
        groupBox2.Controls.Add(EditBarometer30mBotLong);
        groupBox2.Location = new Point(17, 295);
        groupBox2.Name = "groupBox2";
        groupBox2.Size = new Size(234, 168);
        groupBox2.TabIndex = 282;
        groupBox2.TabStop = false;
        groupBox2.Text = "Barometer";
        // 
        // EditBarometer15mBotLong
        // 
        EditBarometer15mBotLong.DecimalPlaces = 2;
        EditBarometer15mBotLong.Location = new Point(130, 30);
        EditBarometer15mBotLong.Margin = new Padding(4, 3, 4, 3);
        EditBarometer15mBotLong.Name = "EditBarometer15mBotLong";
        EditBarometer15mBotLong.Size = new Size(88, 23);
        EditBarometer15mBotLong.TabIndex = 175;
        EditBarometer15mBotLong.Value = new decimal(new int[] { 25, 0, 0, 0 });
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
        // EditBarometer24hBotLong
        // 
        EditBarometer24hBotLong.DecimalPlaces = 2;
        EditBarometer24hBotLong.Location = new Point(130, 138);
        EditBarometer24hBotLong.Margin = new Padding(4, 3, 4, 3);
        EditBarometer24hBotLong.Name = "EditBarometer24hBotLong";
        EditBarometer24hBotLong.Size = new Size(88, 23);
        EditBarometer24hBotLong.TabIndex = 179;
        EditBarometer24hBotLong.Value = new decimal(new int[] { 25, 0, 0, 0 });
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
        // EditBarometer4hBotLong
        // 
        EditBarometer4hBotLong.DecimalPlaces = 2;
        EditBarometer4hBotLong.Location = new Point(130, 111);
        EditBarometer4hBotLong.Margin = new Padding(4, 3, 4, 3);
        EditBarometer4hBotLong.Name = "EditBarometer4hBotLong";
        EditBarometer4hBotLong.Size = new Size(88, 23);
        EditBarometer4hBotLong.TabIndex = 178;
        EditBarometer4hBotLong.Value = new decimal(new int[] { 25, 0, 0, 0 });
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
        // EditBarometer1hBotLong
        // 
        EditBarometer1hBotLong.DecimalPlaces = 2;
        EditBarometer1hBotLong.Location = new Point(130, 83);
        EditBarometer1hBotLong.Margin = new Padding(4, 3, 4, 3);
        EditBarometer1hBotLong.Name = "EditBarometer1hBotLong";
        EditBarometer1hBotLong.Size = new Size(88, 23);
        EditBarometer1hBotLong.TabIndex = 177;
        EditBarometer1hBotLong.Value = new decimal(new int[] { 25, 0, 0, 0 });
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
        // EditBarometer30mBotLong
        // 
        EditBarometer30mBotLong.DecimalPlaces = 2;
        EditBarometer30mBotLong.Location = new Point(130, 56);
        EditBarometer30mBotLong.Margin = new Padding(4, 3, 4, 3);
        EditBarometer30mBotLong.Name = "EditBarometer30mBotLong";
        EditBarometer30mBotLong.Size = new Size(88, 23);
        EditBarometer30mBotLong.TabIndex = 176;
        EditBarometer30mBotLong.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // groupTradeOnInterval
        // 
        groupTradeOnInterval.Controls.Add(EditTradingIntervalLong1h);
        groupTradeOnInterval.Controls.Add(EditTradingIntervalLong2h);
        groupTradeOnInterval.Controls.Add(EditTradingIntervalLong4h);
        groupTradeOnInterval.Controls.Add(EditTradingIntervalLong1m);
        groupTradeOnInterval.Controls.Add(EditTradingIntervalLong2m);
        groupTradeOnInterval.Controls.Add(EditTradingIntervalLong3m);
        groupTradeOnInterval.Controls.Add(EditTradingIntervalLong5m);
        groupTradeOnInterval.Controls.Add(EditTradingIntervalLong10m);
        groupTradeOnInterval.Controls.Add(EditTradingIntervalLong15m);
        groupTradeOnInterval.Controls.Add(EditTradingIntervalLong30m);
        groupTradeOnInterval.Location = new Point(17, 60);
        groupTradeOnInterval.Margin = new Padding(4, 3, 4, 3);
        groupTradeOnInterval.Name = "groupTradeOnInterval";
        groupTradeOnInterval.Padding = new Padding(4, 3, 4, 3);
        groupTradeOnInterval.Size = new Size(234, 218);
        groupTradeOnInterval.TabIndex = 243;
        groupTradeOnInterval.TabStop = false;
        groupTradeOnInterval.Text = "Trade on interval";
        // 
        // EditTradingIntervalLong1h
        // 
        EditTradingIntervalLong1h.AutoSize = true;
        EditTradingIntervalLong1h.Location = new Point(119, 27);
        EditTradingIntervalLong1h.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalLong1h.Name = "EditTradingIntervalLong1h";
        EditTradingIntervalLong1h.Size = new Size(53, 19);
        EditTradingIntervalLong1h.TabIndex = 150;
        EditTradingIntervalLong1h.Text = "1 uur";
        EditTradingIntervalLong1h.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalLong2h
        // 
        EditTradingIntervalLong2h.AutoSize = true;
        EditTradingIntervalLong2h.Location = new Point(119, 55);
        EditTradingIntervalLong2h.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalLong2h.Name = "EditTradingIntervalLong2h";
        EditTradingIntervalLong2h.Size = new Size(53, 19);
        EditTradingIntervalLong2h.TabIndex = 151;
        EditTradingIntervalLong2h.Text = "2 uur";
        EditTradingIntervalLong2h.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalLong4h
        // 
        EditTradingIntervalLong4h.AutoSize = true;
        EditTradingIntervalLong4h.Location = new Point(119, 82);
        EditTradingIntervalLong4h.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalLong4h.Name = "EditTradingIntervalLong4h";
        EditTradingIntervalLong4h.Size = new Size(53, 19);
        EditTradingIntervalLong4h.TabIndex = 152;
        EditTradingIntervalLong4h.Text = "4 uur";
        EditTradingIntervalLong4h.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalLong1m
        // 
        EditTradingIntervalLong1m.AutoSize = true;
        EditTradingIntervalLong1m.Location = new Point(15, 27);
        EditTradingIntervalLong1m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalLong1m.Name = "EditTradingIntervalLong1m";
        EditTradingIntervalLong1m.Size = new Size(56, 19);
        EditTradingIntervalLong1m.TabIndex = 143;
        EditTradingIntervalLong1m.Text = "1 min";
        EditTradingIntervalLong1m.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalLong2m
        // 
        EditTradingIntervalLong2m.AutoSize = true;
        EditTradingIntervalLong2m.Location = new Point(15, 52);
        EditTradingIntervalLong2m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalLong2m.Name = "EditTradingIntervalLong2m";
        EditTradingIntervalLong2m.Size = new Size(56, 19);
        EditTradingIntervalLong2m.TabIndex = 144;
        EditTradingIntervalLong2m.Text = "2 min";
        EditTradingIntervalLong2m.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalLong3m
        // 
        EditTradingIntervalLong3m.AutoSize = true;
        EditTradingIntervalLong3m.Location = new Point(15, 79);
        EditTradingIntervalLong3m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalLong3m.Name = "EditTradingIntervalLong3m";
        EditTradingIntervalLong3m.Size = new Size(56, 19);
        EditTradingIntervalLong3m.TabIndex = 145;
        EditTradingIntervalLong3m.Text = "3 min";
        EditTradingIntervalLong3m.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalLong5m
        // 
        EditTradingIntervalLong5m.AutoSize = true;
        EditTradingIntervalLong5m.Location = new Point(15, 105);
        EditTradingIntervalLong5m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalLong5m.Name = "EditTradingIntervalLong5m";
        EditTradingIntervalLong5m.Size = new Size(56, 19);
        EditTradingIntervalLong5m.TabIndex = 146;
        EditTradingIntervalLong5m.Text = "5 min";
        EditTradingIntervalLong5m.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalLong10m
        // 
        EditTradingIntervalLong10m.AutoSize = true;
        EditTradingIntervalLong10m.Location = new Point(15, 132);
        EditTradingIntervalLong10m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalLong10m.Name = "EditTradingIntervalLong10m";
        EditTradingIntervalLong10m.Size = new Size(62, 19);
        EditTradingIntervalLong10m.TabIndex = 147;
        EditTradingIntervalLong10m.Text = "10 min";
        EditTradingIntervalLong10m.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalLong15m
        // 
        EditTradingIntervalLong15m.AutoSize = true;
        EditTradingIntervalLong15m.Location = new Point(15, 158);
        EditTradingIntervalLong15m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalLong15m.Name = "EditTradingIntervalLong15m";
        EditTradingIntervalLong15m.Size = new Size(62, 19);
        EditTradingIntervalLong15m.TabIndex = 148;
        EditTradingIntervalLong15m.Text = "15 min";
        EditTradingIntervalLong15m.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalLong30m
        // 
        EditTradingIntervalLong30m.AutoSize = true;
        EditTradingIntervalLong30m.Location = new Point(15, 185);
        EditTradingIntervalLong30m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalLong30m.Name = "EditTradingIntervalLong30m";
        EditTradingIntervalLong30m.Size = new Size(62, 19);
        EditTradingIntervalLong30m.TabIndex = 149;
        EditTradingIntervalLong30m.Text = "30 min";
        EditTradingIntervalLong30m.UseVisualStyleBackColor = true;
        // 
        // tabPageLongWhiteList
        // 
        tabPageLongWhiteList.Controls.Add(textBoxWhiteListOversold);
        tabPageLongWhiteList.Controls.Add(panel3);
        tabPageLongWhiteList.Location = new Point(4, 24);
        tabPageLongWhiteList.Name = "tabPageLongWhiteList";
        tabPageLongWhiteList.Padding = new Padding(3);
        tabPageLongWhiteList.Size = new Size(1218, 743);
        tabPageLongWhiteList.TabIndex = 0;
        tabPageLongWhiteList.Text = "Whitelist long";
        tabPageLongWhiteList.UseVisualStyleBackColor = true;
        // 
        // textBoxWhiteListOversold
        // 
        textBoxWhiteListOversold.Dock = DockStyle.Fill;
        textBoxWhiteListOversold.Location = new Point(3, 60);
        textBoxWhiteListOversold.Margin = new Padding(4, 3, 4, 3);
        textBoxWhiteListOversold.Multiline = true;
        textBoxWhiteListOversold.Name = "textBoxWhiteListOversold";
        textBoxWhiteListOversold.Size = new Size(1212, 680);
        textBoxWhiteListOversold.TabIndex = 2;
        // 
        // panel3
        // 
        panel3.Controls.Add(label55);
        panel3.Dock = DockStyle.Top;
        panel3.Location = new Point(3, 3);
        panel3.Margin = new Padding(4, 3, 4, 3);
        panel3.Name = "panel3";
        panel3.Size = new Size(1212, 57);
        panel3.TabIndex = 3;
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
        // tabPageLongBlackList
        // 
        tabPageLongBlackList.Controls.Add(textBoxBlackListOversold);
        tabPageLongBlackList.Controls.Add(panel4);
        tabPageLongBlackList.Location = new Point(4, 24);
        tabPageLongBlackList.Name = "tabPageLongBlackList";
        tabPageLongBlackList.Padding = new Padding(3);
        tabPageLongBlackList.Size = new Size(1218, 743);
        tabPageLongBlackList.TabIndex = 1;
        tabPageLongBlackList.Text = "Blacklist long";
        tabPageLongBlackList.UseVisualStyleBackColor = true;
        // 
        // textBoxBlackListOversold
        // 
        textBoxBlackListOversold.Dock = DockStyle.Fill;
        textBoxBlackListOversold.Location = new Point(3, 60);
        textBoxBlackListOversold.Margin = new Padding(4, 3, 4, 3);
        textBoxBlackListOversold.Multiline = true;
        textBoxBlackListOversold.Name = "textBoxBlackListOversold";
        textBoxBlackListOversold.Size = new Size(1212, 680);
        textBoxBlackListOversold.TabIndex = 3;
        // 
        // panel4
        // 
        panel4.Controls.Add(label51);
        panel4.Dock = DockStyle.Top;
        panel4.Location = new Point(3, 3);
        panel4.Margin = new Padding(4, 3, 4, 3);
        panel4.Name = "panel4";
        panel4.Size = new Size(1212, 57);
        panel4.TabIndex = 4;
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
        // tabPageShort
        // 
        tabPageShort.Controls.Add(tabControlShort);
        tabPageShort.Location = new Point(4, 27);
        tabPageShort.Name = "tabPageShort";
        tabPageShort.Padding = new Padding(3);
        tabPageShort.Size = new Size(1232, 777);
        tabPageShort.TabIndex = 14;
        tabPageShort.Text = "Short";
        tabPageShort.UseVisualStyleBackColor = true;
        // 
        // tabControlShort
        // 
        tabControlShort.Controls.Add(tabPageTradingShort);
        tabControlShort.Controls.Add(tabPageShortWhiteList);
        tabControlShort.Controls.Add(tabPageShortBlackList);
        tabControlShort.Dock = DockStyle.Fill;
        tabControlShort.Location = new Point(3, 3);
        tabControlShort.Name = "tabControlShort";
        tabControlShort.SelectedIndex = 0;
        tabControlShort.Size = new Size(1226, 771);
        tabControlShort.TabIndex = 1;
        // 
        // tabPageTradingShort
        // 
        tabPageTradingShort.Controls.Add(groupBox3);
        tabPageTradingShort.Controls.Add(groupBox4);
        tabPageTradingShort.Controls.Add(groupBox1);
        tabPageTradingShort.Location = new Point(4, 24);
        tabPageTradingShort.Name = "tabPageTradingShort";
        tabPageTradingShort.Padding = new Padding(3);
        tabPageTradingShort.Size = new Size(1218, 743);
        tabPageTradingShort.TabIndex = 2;
        tabPageTradingShort.Text = "Trading Short";
        tabPageTradingShort.UseVisualStyleBackColor = true;
        // 
        // groupBox3
        // 
        groupBox3.Controls.Add(EditTrendShort6h);
        groupBox3.Controls.Add(EditTrendShort8h);
        groupBox3.Controls.Add(EditTrendShort12h);
        groupBox3.Controls.Add(EditTrendShort1d);
        groupBox3.Controls.Add(EditTrendShort1h);
        groupBox3.Controls.Add(EditTrendShort2h);
        groupBox3.Controls.Add(EditTrendShort4h);
        groupBox3.Controls.Add(EditTrendShort1m);
        groupBox3.Controls.Add(EditTrendShort2m);
        groupBox3.Controls.Add(EditTrendShort3m);
        groupBox3.Controls.Add(EditTrendShort5m);
        groupBox3.Controls.Add(EditTrendShort10m);
        groupBox3.Controls.Add(EditTrendShort15m);
        groupBox3.Controls.Add(EditTrendShort30m);
        groupBox3.Location = new Point(275, 60);
        groupBox3.Margin = new Padding(4, 3, 4, 3);
        groupBox3.Name = "groupBox3";
        groupBox3.Padding = new Padding(4, 3, 4, 3);
        groupBox3.Size = new Size(234, 218);
        groupBox3.TabIndex = 285;
        groupBox3.TabStop = false;
        groupBox3.Text = "Instap trend DOWN";
        // 
        // EditTrendShort6h
        // 
        EditTrendShort6h.AutoSize = true;
        EditTrendShort6h.Location = new Point(121, 102);
        EditTrendShort6h.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort6h.Name = "EditTrendShort6h";
        EditTrendShort6h.Size = new Size(53, 19);
        EditTrendShort6h.TabIndex = 153;
        EditTrendShort6h.Text = "6 uur";
        EditTrendShort6h.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort8h
        // 
        EditTrendShort8h.AutoSize = true;
        EditTrendShort8h.Location = new Point(121, 131);
        EditTrendShort8h.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort8h.Name = "EditTrendShort8h";
        EditTrendShort8h.Size = new Size(53, 19);
        EditTrendShort8h.TabIndex = 154;
        EditTrendShort8h.Text = "8 uur";
        EditTrendShort8h.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort12h
        // 
        EditTrendShort12h.AutoSize = true;
        EditTrendShort12h.Location = new Point(121, 158);
        EditTrendShort12h.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort12h.Name = "EditTrendShort12h";
        EditTrendShort12h.Size = new Size(59, 19);
        EditTrendShort12h.TabIndex = 155;
        EditTrendShort12h.Text = "12 uur";
        EditTrendShort12h.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort1d
        // 
        EditTrendShort1d.AutoSize = true;
        EditTrendShort1d.Location = new Point(121, 185);
        EditTrendShort1d.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort1d.Name = "EditTrendShort1d";
        EditTrendShort1d.Size = new Size(55, 19);
        EditTrendShort1d.TabIndex = 156;
        EditTrendShort1d.Text = "1 dag";
        EditTrendShort1d.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort1h
        // 
        EditTrendShort1h.AutoSize = true;
        EditTrendShort1h.Location = new Point(121, 27);
        EditTrendShort1h.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort1h.Name = "EditTrendShort1h";
        EditTrendShort1h.Size = new Size(53, 19);
        EditTrendShort1h.TabIndex = 150;
        EditTrendShort1h.Text = "1 uur";
        EditTrendShort1h.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort2h
        // 
        EditTrendShort2h.AutoSize = true;
        EditTrendShort2h.Location = new Point(121, 50);
        EditTrendShort2h.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort2h.Name = "EditTrendShort2h";
        EditTrendShort2h.Size = new Size(53, 19);
        EditTrendShort2h.TabIndex = 151;
        EditTrendShort2h.Text = "2 uur";
        EditTrendShort2h.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort4h
        // 
        EditTrendShort4h.AutoSize = true;
        EditTrendShort4h.Location = new Point(121, 77);
        EditTrendShort4h.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort4h.Name = "EditTrendShort4h";
        EditTrendShort4h.Size = new Size(53, 19);
        EditTrendShort4h.TabIndex = 152;
        EditTrendShort4h.Text = "4 uur";
        EditTrendShort4h.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort1m
        // 
        EditTrendShort1m.AutoSize = true;
        EditTrendShort1m.Location = new Point(17, 27);
        EditTrendShort1m.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort1m.Name = "EditTrendShort1m";
        EditTrendShort1m.Size = new Size(56, 19);
        EditTrendShort1m.TabIndex = 143;
        EditTrendShort1m.Text = "1 min";
        EditTrendShort1m.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort2m
        // 
        EditTrendShort2m.AutoSize = true;
        EditTrendShort2m.Location = new Point(17, 52);
        EditTrendShort2m.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort2m.Name = "EditTrendShort2m";
        EditTrendShort2m.Size = new Size(56, 19);
        EditTrendShort2m.TabIndex = 144;
        EditTrendShort2m.Text = "2 min";
        EditTrendShort2m.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort3m
        // 
        EditTrendShort3m.AutoSize = true;
        EditTrendShort3m.Location = new Point(17, 79);
        EditTrendShort3m.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort3m.Name = "EditTrendShort3m";
        EditTrendShort3m.Size = new Size(56, 19);
        EditTrendShort3m.TabIndex = 145;
        EditTrendShort3m.Text = "3 min";
        EditTrendShort3m.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort5m
        // 
        EditTrendShort5m.AutoSize = true;
        EditTrendShort5m.Location = new Point(17, 105);
        EditTrendShort5m.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort5m.Name = "EditTrendShort5m";
        EditTrendShort5m.Size = new Size(56, 19);
        EditTrendShort5m.TabIndex = 146;
        EditTrendShort5m.Text = "5 min";
        EditTrendShort5m.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort10m
        // 
        EditTrendShort10m.AutoSize = true;
        EditTrendShort10m.Location = new Point(17, 132);
        EditTrendShort10m.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort10m.Name = "EditTrendShort10m";
        EditTrendShort10m.Size = new Size(62, 19);
        EditTrendShort10m.TabIndex = 147;
        EditTrendShort10m.Text = "10 min";
        EditTrendShort10m.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort15m
        // 
        EditTrendShort15m.AutoSize = true;
        EditTrendShort15m.Location = new Point(17, 158);
        EditTrendShort15m.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort15m.Name = "EditTrendShort15m";
        EditTrendShort15m.Size = new Size(62, 19);
        EditTrendShort15m.TabIndex = 148;
        EditTrendShort15m.Text = "15 min";
        EditTrendShort15m.UseVisualStyleBackColor = true;
        // 
        // EditTrendShort30m
        // 
        EditTrendShort30m.AutoSize = true;
        EditTrendShort30m.Location = new Point(17, 185);
        EditTrendShort30m.Margin = new Padding(4, 3, 4, 3);
        EditTrendShort30m.Name = "EditTrendShort30m";
        EditTrendShort30m.Size = new Size(62, 19);
        EditTrendShort30m.TabIndex = 149;
        EditTrendShort30m.Text = "30 min";
        EditTrendShort30m.UseVisualStyleBackColor = true;
        // 
        // groupBox4
        // 
        groupBox4.Controls.Add(EditBarometer15mBotShort);
        groupBox4.Controls.Add(label91);
        groupBox4.Controls.Add(EditBarometer24hBotShort);
        groupBox4.Controls.Add(label92);
        groupBox4.Controls.Add(EditBarometer4hBotShort);
        groupBox4.Controls.Add(label93);
        groupBox4.Controls.Add(EditBarometer1hBotShort);
        groupBox4.Controls.Add(label94);
        groupBox4.Controls.Add(label95);
        groupBox4.Controls.Add(EditBarometer30mBotShort);
        groupBox4.Location = new Point(16, 295);
        groupBox4.Name = "groupBox4";
        groupBox4.Size = new Size(234, 168);
        groupBox4.TabIndex = 284;
        groupBox4.TabStop = false;
        groupBox4.Text = "Barometer";
        // 
        // EditBarometer15mBotShort
        // 
        EditBarometer15mBotShort.DecimalPlaces = 2;
        EditBarometer15mBotShort.Location = new Point(130, 30);
        EditBarometer15mBotShort.Margin = new Padding(4, 3, 4, 3);
        EditBarometer15mBotShort.Name = "EditBarometer15mBotShort";
        EditBarometer15mBotShort.Size = new Size(88, 23);
        EditBarometer15mBotShort.TabIndex = 175;
        EditBarometer15mBotShort.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label91
        // 
        label91.AutoSize = true;
        label91.Location = new Point(7, 33);
        label91.Margin = new Padding(4, 0, 4, 0);
        label91.Name = "label91";
        label91.Size = new Size(88, 15);
        label91.TabIndex = 173;
        label91.Text = "Barometer 15m";
        // 
        // EditBarometer24hBotShort
        // 
        EditBarometer24hBotShort.DecimalPlaces = 2;
        EditBarometer24hBotShort.Location = new Point(130, 138);
        EditBarometer24hBotShort.Margin = new Padding(4, 3, 4, 3);
        EditBarometer24hBotShort.Name = "EditBarometer24hBotShort";
        EditBarometer24hBotShort.Size = new Size(88, 23);
        EditBarometer24hBotShort.TabIndex = 179;
        EditBarometer24hBotShort.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label92
        // 
        label92.AutoSize = true;
        label92.Location = new Point(9, 140);
        label92.Margin = new Padding(4, 0, 4, 0);
        label92.Name = "label92";
        label92.Size = new Size(84, 15);
        label92.TabIndex = 182;
        label92.Text = "Barometer 24h";
        // 
        // EditBarometer4hBotShort
        // 
        EditBarometer4hBotShort.DecimalPlaces = 2;
        EditBarometer4hBotShort.Location = new Point(130, 111);
        EditBarometer4hBotShort.Margin = new Padding(4, 3, 4, 3);
        EditBarometer4hBotShort.Name = "EditBarometer4hBotShort";
        EditBarometer4hBotShort.Size = new Size(88, 23);
        EditBarometer4hBotShort.TabIndex = 178;
        EditBarometer4hBotShort.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label93
        // 
        label93.AutoSize = true;
        label93.Location = new Point(9, 112);
        label93.Margin = new Padding(4, 0, 4, 0);
        label93.Name = "label93";
        label93.Size = new Size(78, 15);
        label93.TabIndex = 181;
        label93.Text = "Barometer 4h";
        // 
        // EditBarometer1hBotShort
        // 
        EditBarometer1hBotShort.DecimalPlaces = 2;
        EditBarometer1hBotShort.Location = new Point(130, 83);
        EditBarometer1hBotShort.Margin = new Padding(4, 3, 4, 3);
        EditBarometer1hBotShort.Name = "EditBarometer1hBotShort";
        EditBarometer1hBotShort.Size = new Size(88, 23);
        EditBarometer1hBotShort.TabIndex = 177;
        EditBarometer1hBotShort.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label94
        // 
        label94.AutoSize = true;
        label94.Location = new Point(9, 58);
        label94.Margin = new Padding(4, 0, 4, 0);
        label94.Name = "label94";
        label94.Size = new Size(88, 15);
        label94.TabIndex = 174;
        label94.Text = "Barometer 30m";
        // 
        // label95
        // 
        label95.AutoSize = true;
        label95.Location = new Point(9, 86);
        label95.Margin = new Padding(4, 0, 4, 0);
        label95.Name = "label95";
        label95.Size = new Size(78, 15);
        label95.TabIndex = 180;
        label95.Text = "Barometer 1h";
        // 
        // EditBarometer30mBotShort
        // 
        EditBarometer30mBotShort.DecimalPlaces = 2;
        EditBarometer30mBotShort.Location = new Point(130, 56);
        EditBarometer30mBotShort.Margin = new Padding(4, 3, 4, 3);
        EditBarometer30mBotShort.Name = "EditBarometer30mBotShort";
        EditBarometer30mBotShort.Size = new Size(88, 23);
        EditBarometer30mBotShort.TabIndex = 176;
        EditBarometer30mBotShort.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // groupBox1
        // 
        groupBox1.Controls.Add(EditTradingIntervalShort1h);
        groupBox1.Controls.Add(EditTradingIntervalShort2h);
        groupBox1.Controls.Add(EditTradingIntervalShort4h);
        groupBox1.Controls.Add(EditTradingIntervalShort1m);
        groupBox1.Controls.Add(EditTradingIntervalShort2m);
        groupBox1.Controls.Add(EditTradingIntervalShort3m);
        groupBox1.Controls.Add(EditTradingIntervalShort5m);
        groupBox1.Controls.Add(EditTradingIntervalShort10m);
        groupBox1.Controls.Add(EditTradingIntervalShort15m);
        groupBox1.Controls.Add(EditTradingIntervalShort30m);
        groupBox1.Location = new Point(17, 60);
        groupBox1.Margin = new Padding(4, 3, 4, 3);
        groupBox1.Name = "groupBox1";
        groupBox1.Padding = new Padding(4, 3, 4, 3);
        groupBox1.Size = new Size(234, 218);
        groupBox1.TabIndex = 243;
        groupBox1.TabStop = false;
        groupBox1.Text = "Trade on interval";
        // 
        // EditTradingIntervalShort1h
        // 
        EditTradingIntervalShort1h.AutoSize = true;
        EditTradingIntervalShort1h.Location = new Point(119, 27);
        EditTradingIntervalShort1h.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalShort1h.Name = "EditTradingIntervalShort1h";
        EditTradingIntervalShort1h.Size = new Size(53, 19);
        EditTradingIntervalShort1h.TabIndex = 150;
        EditTradingIntervalShort1h.Text = "1 uur";
        EditTradingIntervalShort1h.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalShort2h
        // 
        EditTradingIntervalShort2h.AutoSize = true;
        EditTradingIntervalShort2h.Location = new Point(119, 55);
        EditTradingIntervalShort2h.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalShort2h.Name = "EditTradingIntervalShort2h";
        EditTradingIntervalShort2h.Size = new Size(53, 19);
        EditTradingIntervalShort2h.TabIndex = 151;
        EditTradingIntervalShort2h.Text = "2 uur";
        EditTradingIntervalShort2h.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalShort4h
        // 
        EditTradingIntervalShort4h.AutoSize = true;
        EditTradingIntervalShort4h.Location = new Point(119, 82);
        EditTradingIntervalShort4h.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalShort4h.Name = "EditTradingIntervalShort4h";
        EditTradingIntervalShort4h.Size = new Size(53, 19);
        EditTradingIntervalShort4h.TabIndex = 152;
        EditTradingIntervalShort4h.Text = "4 uur";
        EditTradingIntervalShort4h.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalShort1m
        // 
        EditTradingIntervalShort1m.AutoSize = true;
        EditTradingIntervalShort1m.Location = new Point(15, 27);
        EditTradingIntervalShort1m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalShort1m.Name = "EditTradingIntervalShort1m";
        EditTradingIntervalShort1m.Size = new Size(56, 19);
        EditTradingIntervalShort1m.TabIndex = 143;
        EditTradingIntervalShort1m.Text = "1 min";
        EditTradingIntervalShort1m.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalShort2m
        // 
        EditTradingIntervalShort2m.AutoSize = true;
        EditTradingIntervalShort2m.Location = new Point(15, 52);
        EditTradingIntervalShort2m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalShort2m.Name = "EditTradingIntervalShort2m";
        EditTradingIntervalShort2m.Size = new Size(56, 19);
        EditTradingIntervalShort2m.TabIndex = 144;
        EditTradingIntervalShort2m.Text = "2 min";
        EditTradingIntervalShort2m.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalShort3m
        // 
        EditTradingIntervalShort3m.AutoSize = true;
        EditTradingIntervalShort3m.Location = new Point(15, 79);
        EditTradingIntervalShort3m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalShort3m.Name = "EditTradingIntervalShort3m";
        EditTradingIntervalShort3m.Size = new Size(56, 19);
        EditTradingIntervalShort3m.TabIndex = 145;
        EditTradingIntervalShort3m.Text = "3 min";
        EditTradingIntervalShort3m.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalShort5m
        // 
        EditTradingIntervalShort5m.AutoSize = true;
        EditTradingIntervalShort5m.Location = new Point(15, 105);
        EditTradingIntervalShort5m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalShort5m.Name = "EditTradingIntervalShort5m";
        EditTradingIntervalShort5m.Size = new Size(56, 19);
        EditTradingIntervalShort5m.TabIndex = 146;
        EditTradingIntervalShort5m.Text = "5 min";
        EditTradingIntervalShort5m.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalShort10m
        // 
        EditTradingIntervalShort10m.AutoSize = true;
        EditTradingIntervalShort10m.Location = new Point(15, 132);
        EditTradingIntervalShort10m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalShort10m.Name = "EditTradingIntervalShort10m";
        EditTradingIntervalShort10m.Size = new Size(62, 19);
        EditTradingIntervalShort10m.TabIndex = 147;
        EditTradingIntervalShort10m.Text = "10 min";
        EditTradingIntervalShort10m.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalShort15m
        // 
        EditTradingIntervalShort15m.AutoSize = true;
        EditTradingIntervalShort15m.Location = new Point(15, 158);
        EditTradingIntervalShort15m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalShort15m.Name = "EditTradingIntervalShort15m";
        EditTradingIntervalShort15m.Size = new Size(62, 19);
        EditTradingIntervalShort15m.TabIndex = 148;
        EditTradingIntervalShort15m.Text = "15 min";
        EditTradingIntervalShort15m.UseVisualStyleBackColor = true;
        // 
        // EditTradingIntervalShort30m
        // 
        EditTradingIntervalShort30m.AutoSize = true;
        EditTradingIntervalShort30m.Location = new Point(15, 185);
        EditTradingIntervalShort30m.Margin = new Padding(4, 3, 4, 3);
        EditTradingIntervalShort30m.Name = "EditTradingIntervalShort30m";
        EditTradingIntervalShort30m.Size = new Size(62, 19);
        EditTradingIntervalShort30m.TabIndex = 149;
        EditTradingIntervalShort30m.Text = "30 min";
        EditTradingIntervalShort30m.UseVisualStyleBackColor = true;
        // 
        // tabPageShortWhiteList
        // 
        tabPageShortWhiteList.Controls.Add(textBoxWhiteListOverbought);
        tabPageShortWhiteList.Controls.Add(panel5);
        tabPageShortWhiteList.Location = new Point(4, 24);
        tabPageShortWhiteList.Name = "tabPageShortWhiteList";
        tabPageShortWhiteList.Padding = new Padding(3);
        tabPageShortWhiteList.Size = new Size(1218, 743);
        tabPageShortWhiteList.TabIndex = 0;
        tabPageShortWhiteList.Text = "Whitelist short";
        tabPageShortWhiteList.UseVisualStyleBackColor = true;
        // 
        // textBoxWhiteListOverbought
        // 
        textBoxWhiteListOverbought.Dock = DockStyle.Fill;
        textBoxWhiteListOverbought.Location = new Point(3, 60);
        textBoxWhiteListOverbought.Margin = new Padding(4, 3, 4, 3);
        textBoxWhiteListOverbought.Multiline = true;
        textBoxWhiteListOverbought.Name = "textBoxWhiteListOverbought";
        textBoxWhiteListOverbought.Size = new Size(1212, 680);
        textBoxWhiteListOverbought.TabIndex = 4;
        // 
        // panel5
        // 
        panel5.Controls.Add(label29);
        panel5.Dock = DockStyle.Top;
        panel5.Location = new Point(3, 3);
        panel5.Margin = new Padding(4, 3, 4, 3);
        panel5.Name = "panel5";
        panel5.Size = new Size(1212, 57);
        panel5.TabIndex = 5;
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
        // tabPageShortBlackList
        // 
        tabPageShortBlackList.Controls.Add(textBoxBlackListOverbought);
        tabPageShortBlackList.Controls.Add(panel6);
        tabPageShortBlackList.Location = new Point(4, 24);
        tabPageShortBlackList.Name = "tabPageShortBlackList";
        tabPageShortBlackList.Padding = new Padding(3);
        tabPageShortBlackList.Size = new Size(1218, 743);
        tabPageShortBlackList.TabIndex = 1;
        tabPageShortBlackList.Text = "Blacklist short";
        tabPageShortBlackList.UseVisualStyleBackColor = true;
        // 
        // textBoxBlackListOverbought
        // 
        textBoxBlackListOverbought.Dock = DockStyle.Fill;
        textBoxBlackListOverbought.Location = new Point(3, 60);
        textBoxBlackListOverbought.Margin = new Padding(4, 3, 4, 3);
        textBoxBlackListOverbought.Multiline = true;
        textBoxBlackListOverbought.Name = "textBoxBlackListOverbought";
        textBoxBlackListOverbought.Size = new Size(1212, 680);
        textBoxBlackListOverbought.TabIndex = 7;
        // 
        // panel6
        // 
        panel6.Controls.Add(label49);
        panel6.Dock = DockStyle.Top;
        panel6.Location = new Point(3, 3);
        panel6.Margin = new Padding(4, 3, 4, 3);
        panel6.Name = "panel6";
        panel6.Size = new Size(1212, 57);
        panel6.TabIndex = 8;
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
        groupBoxStoch.ResumeLayout(false);
        groupBoxStoch.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditStochValueOversold).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditStochValueOverbought).EndInit();
        groupBoxRsi.ResumeLayout(false);
        groupBoxRsi.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditRsiValueOversold).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditRsiValueOverbought).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGetCandleInterval).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalDataRemoveSignalAfterxCandles).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSoundHeartBeatMinutes).EndInit();
        tabTelegram.ResumeLayout(false);
        tabTelegram.PerformLayout();
        tabPageSignals.ResumeLayout(false);
        tabPageSignals.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMinEffective10DaysPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMaxEffective10DaysPercentage).EndInit();
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
        ((System.ComponentModel.ISupportInitialize)EditStobTrendShort).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditStobTrendLong).EndInit();
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
        groupBoxInstap.ResumeLayout(false);
        groupBoxInstap.PerformLayout();
        groupBoxFutures.ResumeLayout(false);
        groupBoxFutures.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditLeverage).EndInit();
        groupBoxSlots.ResumeLayout(false);
        groupBoxSlots.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalExchange).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalSymbol).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalBase).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditDcaCount).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditDcaFactor).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditDcaPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalStopLimitPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalStopPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditProfitPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyCooldownTime).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyVarying).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyRemoveTime).EndInit();
        tabPageLong.ResumeLayout(false);
        tabControlLong.ResumeLayout(false);
        tabPageTradingLong.ResumeLayout(false);
        groupTrendOnInterval.ResumeLayout(false);
        groupTrendOnInterval.PerformLayout();
        groupBox2.ResumeLayout(false);
        groupBox2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditBarometer15mBotLong).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer24hBotLong).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer4hBotLong).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer1hBotLong).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer30mBotLong).EndInit();
        groupTradeOnInterval.ResumeLayout(false);
        groupTradeOnInterval.PerformLayout();
        tabPageLongWhiteList.ResumeLayout(false);
        tabPageLongWhiteList.PerformLayout();
        panel3.ResumeLayout(false);
        panel3.PerformLayout();
        tabPageLongBlackList.ResumeLayout(false);
        tabPageLongBlackList.PerformLayout();
        panel4.ResumeLayout(false);
        panel4.PerformLayout();
        tabPageShort.ResumeLayout(false);
        tabControlShort.ResumeLayout(false);
        tabPageTradingShort.ResumeLayout(false);
        groupBox3.ResumeLayout(false);
        groupBox3.PerformLayout();
        groupBox4.ResumeLayout(false);
        groupBox4.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditBarometer15mBotShort).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer24hBotShort).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer4hBotShort).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer1hBotShort).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometer30mBotShort).EndInit();
        groupBox1.ResumeLayout(false);
        groupBox1.PerformLayout();
        tabPageShortWhiteList.ResumeLayout(false);
        tabPageShortWhiteList.PerformLayout();
        panel5.ResumeLayout(false);
        panel5.PerformLayout();
        tabPageShortBlackList.ResumeLayout(false);
        tabPageShortBlackList.PerformLayout();
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
    private Button buttonGotoAppDataFolder;
    private TabControl tabControl;
    private TabPage tabAlgemeen;
    private GroupBox groupBoxStoch;
    private NumericUpDown EditStochValueOversold;
    private Label label88;
    private Label label89;
    private NumericUpDown EditStochValueOverbought;
    private GroupBox groupBoxRsi;
    private NumericUpDown EditRsiValueOversold;
    private Label label87;
    private Label label90;
    private NumericUpDown EditRsiValueOverbought;
    private TextBox EditExtraCaption;
    private Label label74;
    private CheckBox EditHideSymbolsOnTheLeft;
    private Label label58;
    private ComboBox EditActivateExchange;
    private CheckBox EditShowInvalidSignals;
    private Label label84;
    private ComboBox EditExchange;
    private Label label16;
    private NumericUpDown EditGetCandleInterval;
    private Label label6;
    private NumericUpDown EditGlobalDataRemoveSignalAfterxCandles;
    private CheckBox EditBlackTheming;
    private Button buttonFontDialog;
    private Label label18;
    private NumericUpDown EditSoundHeartBeatMinutes;
    private Label label2;
    private ComboBox EditTradingApp;
    private TabPage tabTelegram;
    private Button buttonTelegramStart;
    private CheckBox EditSendSignalsToTelegram;
    private Button ButtonTestTelegram;
    private Label label24;
    private TextBox EditTelegramChatId;
    private TextBox EditTelegramToken;
    private Label label15;
    private TabPage tabBasismunten;
    private TabPage tabPageSignals;
    private Label label86;
    private NumericUpDown EditAnalysisMinEffective10DaysPercentage;
    private NumericUpDown EditAnalysisMaxEffective10DaysPercentage;
    private CheckBox EditLogAnalysisMinMaxEffective10DaysPercentage;
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
    private NumericUpDown EditStobTrendLong;
    private Label label77;
    private Label label75;
    private CheckBox EditStobIncludeSbmPercAndCrossing;
    private Label label30;
    private Label label28;
    private Button buttonColorStobbShort;
    private CheckBox EditStobIncludeSbmMaLines;
    private CheckBox EditStobIncludeRsi;
    private Button buttonPlaySoundStobbOversold;
    private Button buttonPlaySoundStobbOverbought;
    private Button buttonSelectSoundStobbOversold;
    private Panel panelColorStobbShort;
    private TextBox EditSoundStobbOversold;
    private TextBox EditSoundStobbOverbought;
    private Button buttonSelectSoundStobbOverbought;
    private CheckBox EditPlaySpeechStobbSignal;
    private CheckBox EditPlaySoundStobbSignal;
    private CheckBox EditAnalyzeStobbLong;
    private CheckBox EditAnalyzeStobbShort;
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
    private CheckBox EditAnalyzeSbm1Long;
    private CheckBox EditAnalyzeSbm1Short;
    private CheckBox EditPlaySoundSbmSignal;
    private Button buttonColorSbmShort;
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
    private CheckBox EditAnalyzeSbm3Short;
    private CheckBox EditAnalyzeSbm2Short;
    private Label label12;
    private NumericUpDown EditSbm2BbPercentage;
    private Panel panelColorSbmShort;
    private Label label13;
    private NumericUpDown EditSbm3CandlesForBBRecovery;
    private Label label14;
    private NumericUpDown EditSbm3CandlesForBBRecoveryPercentage;
    private Label label11;
    private NumericUpDown EditSbm2CandlesLookbackCount;
    private CheckBox EditAnalyzeSbm3Long;
    private CheckBox EditAnalyzeSbm2Long;
    private TabPage tabSignalJump;
    private Label label78;
    private Label label76;
    private Label label33;
    private Label label34;
    private Label label5;
    private NumericUpDown EditJumpCandlesLookbackCount;
    private CheckBox EditJumpUseLowHighCalculation;
    private Button buttonColorJumpShort;
    private Button buttonPlaySoundCandleJumpDown;
    private Button buttonPlaySoundCandleJumpUp;
    private Button buttonSelectSoundCandleJumpDown;
    private Panel panelColorJumpShort;
    private TextBox EditSoundFileCandleJumpDown;
    private TextBox EditSoundFileCandleJumpUp;
    private Button buttonSelectSoundCandleJumpUp;
    private CheckBox EditPlaySpeechCandleJumpSignal;
    private Label label3;
    private CheckBox EditPlaySoundCandleJumpSignal;
    private CheckBox EditAnalyzeCandleJumpUp;
    private CheckBox EditAnalyzeCandleJumpDown;
    private NumericUpDown EditAnalysisCandleJumpPercentage;
    private TabPage tabPageTrading;
    private TextBox EditApiSecret;
    private TextBox EditApiKey;
    private Label label80;
    private Label label65;
    private CheckBox EditLockProfits;
    private Label label59;
    private CheckBox EditLogCanceledOrders;
    private CheckBox EditSoundTradeNotification;
    private CheckBox EditDisableNewPositions;
    private Label label83;
    private ComboBox EditBuyStepInMethod;
    private Label label82;
    private ComboBox EditDcaStepInMethod;
    private CheckBox EditTradeViaExchange;
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
    private TabPage tabPageLong;
    private TabControl tabControlLong;
    private TabPage tabPageLongWhiteList;
    private TextBox textBoxWhiteListOversold;
    private Panel panel3;
    private Label label55;
    private TabPage tabPageLongBlackList;
    private TextBox textBoxBlackListOversold;
    private Panel panel4;
    private Label label51;
    private TabPage tabPageTradingLong;
    private GroupBox groupTradeOnInterval;
    private CheckBox EditTradingIntervalLong1h;
    private CheckBox EditTradingIntervalLong2h;
    private CheckBox EditTradingIntervalLong4h;
    private CheckBox EditTradingIntervalLong1m;
    private CheckBox EditTradingIntervalLong2m;
    private CheckBox EditTradingIntervalLong3m;
    private CheckBox EditTradingIntervalLong5m;
    private CheckBox EditTradingIntervalLong10m;
    private CheckBox EditTradingIntervalLong15m;
    private CheckBox EditTradingIntervalLong30m;
    private TabPage tabPageShort;
    private TabControl tabControlShort;
    private TabPage tabPageShortWhiteList;
    private TextBox textBoxWhiteListOverbought;
    private Panel panel5;
    private Label label29;
    private TabPage tabPageShortBlackList;
    private TextBox textBoxBlackListOverbought;
    private Panel panel6;
    private Label label49;
    private TabPage tabPageTradingShort;
    private GroupBox groupBox1;
    private CheckBox EditTradingIntervalShort1h;
    private CheckBox EditTradingIntervalShort2h;
    private CheckBox EditTradingIntervalShort4h;
    private CheckBox EditTradingIntervalShort1m;
    private CheckBox EditTradingIntervalShort2m;
    private CheckBox EditTradingIntervalShort3m;
    private CheckBox EditTradingIntervalShort5m;
    private CheckBox EditTradingIntervalShort10m;
    private CheckBox EditTradingIntervalShort15m;
    private CheckBox EditTradingIntervalShort30m;
    private GroupBox groupBoxFutures;
    private Label label19;
    private ComboBox EditCrossOrIsolated;
    private Label label23;
    private NumericUpDown EditLeverage;
    private GroupBox groupTrendOnInterval;
    private CheckBox EditTrendLong6h;
    private CheckBox EditTrendLong8h;
    private CheckBox EditTrendLong12h;
    private CheckBox EditTrendLong1d;
    private CheckBox EditTrendLong1h;
    private CheckBox EditTrendLong2h;
    private CheckBox EditTrendLong4h;
    private CheckBox EditTrendLong1m;
    private CheckBox EditTrendLong2m;
    private CheckBox EditTrendLong3m;
    private CheckBox EditTrendLong5m;
    private CheckBox EditTrendLong10m;
    private CheckBox EditTrendLong15m;
    private CheckBox EditTrendLong30m;
    private GroupBox groupBox2;
    private NumericUpDown EditBarometer15mBotLong;
    private Label label27;
    private NumericUpDown EditBarometer24hBotLong;
    private Label label42;
    private NumericUpDown EditBarometer4hBotLong;
    private Label label43;
    private NumericUpDown EditBarometer1hBotLong;
    private Label label44;
    private Label label45;
    private NumericUpDown EditBarometer30mBotLong;
    private GroupBox groupBox3;
    private CheckBox EditTrendShort6h;
    private CheckBox EditTrendShort8h;
    private CheckBox EditTrendShort12h;
    private CheckBox EditTrendShort1d;
    private CheckBox EditTrendShort1h;
    private CheckBox EditTrendShort2h;
    private CheckBox EditTrendShort4h;
    private CheckBox EditTrendShort1m;
    private CheckBox EditTrendShort2m;
    private CheckBox EditTrendShort3m;
    private CheckBox EditTrendShort5m;
    private CheckBox EditTrendShort10m;
    private CheckBox EditTrendShort15m;
    private CheckBox EditTrendShort30m;
    private GroupBox groupBox4;
    private NumericUpDown EditBarometer15mBotShort;
    private Label label91;
    private NumericUpDown EditBarometer24hBotShort;
    private Label label92;
    private NumericUpDown EditBarometer4hBotShort;
    private Label label93;
    private NumericUpDown EditBarometer1hBotShort;
    private Label label94;
    private Label label95;
    private NumericUpDown EditBarometer30mBotShort;
    private GroupBox groupBoxInstap;
    private CheckBox EditCheckIncreasingMacd;
    private CheckBox EditCheckIncreasingStoch;
    private CheckBox EditCheckIncreasingRsi;
    private Label label85;
    private NumericUpDown EditStobTrendShort;
    private Button buttonColorStobbLong;
    private Panel panelColorStobbLong;
    private Button buttonColorSbmLong;
    private Panel panelColorSbmLong;
    private Button buttonColorJumpLong;
    private Panel panelColorJumpLong;
    private Label label97;
    private Label label96;
}
