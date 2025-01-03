﻿namespace CryptoScanBot;

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
        panelButtons = new Panel();
        buttonGotoAppDataFolder = new Button();
        buttonReset = new Button();
        buttonTestSpeech = new Button();
        buttonCancel = new Button();
        buttonOk = new Button();
        panelFill = new Panel();
        tabControlMain = new TabControl();
        tabGeneral = new TabPage();
        flowLayoutPanel5 = new FlowLayoutPanel();
        groupBox1 = new GroupBox();
        label42 = new Label();
        EditDoubleClickAction = new ComboBox();
        EditHideSelectedRow = new CheckBox();
        groupBox7 = new GroupBox();
        label22 = new Label();
        EditBbStdDeviation = new NumericUpDown();
        label15 = new Label();
        EditTradingAppInternExtern = new ComboBox();
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
        UserControlTelegram = new SettingsDialog.UserControlTelegram();
        tabBasecoin = new TabPage();
        flowLayoutPanelQuotes = new FlowLayoutPanel();
        userControlQuoteHeader1 = new SettingsDialog.UserControlQuoteHeader();
        tabSignal = new TabPage();
        tabControlSignals = new TabControl();
        tabSignalsGeneral = new TabPage();
        groupBox10 = new GroupBox();
        label46 = new Label();
        label27 = new Label();
        EditCheckVolumeOverDays = new NumericUpDown();
        label45 = new Label();
        EditCheckVolumeOverPeriod = new CheckBox();
        groupBox9 = new GroupBox();
        EditAnalysisMinChangePercentage = new NumericUpDown();
        EditAnalysisMaxChangePercentage = new NumericUpDown();
        label53 = new Label();
        EditLogAnalysisMinMaxChangePercentage = new CheckBox();
        GroupBoxXDaysEffective = new GroupBox();
        label44 = new Label();
        label43 = new Label();
        label31 = new Label();
        EditAnalysisEffectiveDays = new NumericUpDown();
        label86 = new Label();
        EditAnalysisEffectivePercentage = new NumericUpDown();
        EditAnalysisMaxEffectiveLog = new CheckBox();
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
        EditLogMinimumTickPercentage = new CheckBox();
        EditMinimumTickPercentage = new NumericUpDown();
        label61 = new Label();
        EditLogSymbolMustExistsDays = new CheckBox();
        EditSymbolMustExistsDays = new NumericUpDown();
        label25 = new Label();
        tabSignalsLong = new TabPage();
        UserControlSignalLong = new SettingsDialog.UserControlEverything();
        tabSignalsShort = new TabPage();
        UserControlSignalShort = new SettingsDialog.UserControlEverything();
        tabSignalStobb = new TabPage();
        flowLayoutPanel6 = new FlowLayoutPanel();
        UserControlSettingsSoundAndColorsStobb = new SettingsDialog.UserControlSettingsPlaySoundAndColors();
        groupBox2 = new GroupBox();
        EditStobOnlyIfPreviousStobb = new CheckBox();
        label1 = new Label();
        EditStobbBBMinPercentage = new NumericUpDown();
        EditStobbBBMaxPercentage = new NumericUpDown();
        label85 = new Label();
        EditStobTrendShort = new NumericUpDown();
        label66 = new Label();
        EditStobTrendLong = new NumericUpDown();
        EditStobIncludeSbmPercAndCrossing = new CheckBox();
        EditStobIncludeSbmMaLines = new CheckBox();
        EditStobIncludeRsi = new CheckBox();
        EditStobbUseLowHigh = new CheckBox();
        tabSignalSbm = new TabPage();
        flowLayoutPanel7 = new FlowLayoutPanel();
        UserControlSettingsSoundAndColorsSbm = new SettingsDialog.UserControlSettingsPlaySoundAndColors();
        flowLayoutPanel9 = new FlowLayoutPanel();
        groupBox3 = new GroupBox();
        EditSbmUseLowHigh = new CheckBox();
        EditSbm2UseLowHigh = new CheckBox();
        label21 = new Label();
        label20 = new Label();
        label9 = new Label();
        label41 = new Label();
        EditSbm1CandlesLookbackCount = new NumericUpDown();
        label12 = new Label();
        EditSbm2BbPercentage = new NumericUpDown();
        label13 = new Label();
        EditSbm3CandlesForBBRecovery = new NumericUpDown();
        label14 = new Label();
        EditSbm3CandlesForBBRecoveryPercentage = new NumericUpDown();
        label11 = new Label();
        EditSbm2CandlesLookbackCount = new NumericUpDown();
        groupBox4 = new GroupBox();
        label39 = new Label();
        EditSbmCandlesForMacdRecovery = new NumericUpDown();
        label17 = new Label();
        EditSbmBBMinPercentage = new NumericUpDown();
        EditSbmBBMaxPercentage = new NumericUpDown();
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
        tabSignalStoRsi = new TabPage();
        flowLayoutPanel2 = new FlowLayoutPanel();
        UserControlSettingsSoundAndColorsStoRsi = new SettingsDialog.UserControlSettingsPlaySoundAndColors();
        groupBox6 = new GroupBox();
        label28 = new Label();
        EditStorsiBBMinPercentage = new NumericUpDown();
        EditStorsiBBMaxPercentage = new NumericUpDown();
        EditSkipFirstSignal = new CheckBox();
        EditCheckBollingerBandsCondition = new CheckBox();
        label24 = new Label();
        EditStorsiAddStochAmount = new NumericUpDown();
        label26 = new Label();
        EditStorsiAddRsiAmount = new NumericUpDown();
        tabSignalJump = new TabPage();
        flowLayoutPanel8 = new FlowLayoutPanel();
        UserControlSettingsSoundAndColorsJump = new SettingsDialog.UserControlSettingsPlaySoundAndColors();
        groupBox5 = new GroupBox();
        label5 = new Label();
        EditJumpCandlesLookbackCount = new NumericUpDown();
        EditJumpUseLowHighCalculation = new CheckBox();
        label3 = new Label();
        EditAnalysisCandleJumpPercentage = new NumericUpDown();
        tabSignalZones = new TabPage();
        flowLayoutPanel4 = new FlowLayoutPanel();
        UserControlSettingsSoundAndColorsZonesDominant = new SettingsDialog.UserControlSettingsPlaySoundAndColors();
        groupBox11 = new GroupBox();
        EditShowZoneSignalsShort = new CheckBox();
        EditShowZoneSignalsLong = new CheckBox();
        label60 = new Label();
        label62 = new Label();
        EditZonesCandleCount = new NumericUpDown();
        label63 = new Label();
        EditZonesWarnPercentage = new NumericUpDown();
        groupBox12 = new GroupBox();
        EditZonesApplyUnzoomed = new CheckBox();
        label47 = new Label();
        EditMaximumUnZoomedPercentage = new NumericUpDown();
        label54 = new Label();
        EditMinimumUnZoomedPercentage = new NumericUpDown();
        groupBox13 = new GroupBox();
        EditMinimumZoomedPercentage = new NumericUpDown();
        EditZoomLowerTimeFrames = new CheckBox();
        label56 = new Label();
        EditMaximumZoomedPercentage = new NumericUpDown();
        label57 = new Label();
        groupBox8 = new GroupBox();
        label40 = new Label();
        EditZoneStartPercentage = new NumericUpDown();
        EditZoneStartApply = new CheckBox();
        label30 = new Label();
        EditZoneStartCandleCount = new NumericUpDown();
        tabSignalZonesFvg = new TabPage();
        flowLayoutPanel10 = new FlowLayoutPanel();
        UserControlSettingsSoundAndColorsZonesFvg = new SettingsDialog.UserControlSettingsPlaySoundAndColors();
        groupBox14 = new GroupBox();
        EditShowFvgSignalsShort = new CheckBox();
        EditShowFvgSignalsLong = new CheckBox();
        label34 = new Label();
        EditZonesFvgMinimumPercentage = new NumericUpDown();
        tabTrading = new TabPage();
        tabControlTrading = new TabControl();
        tabTradingGeneral = new TabPage();
        flowLayoutPanel1 = new FlowLayoutPanel();
        UserControlTradeEntry = new SettingsDialog.UserControlTradeEntry();
        UserControlTradeTakeProfit = new SettingsDialog.UserControlTradeTakeProfit();
        UserControlTradeStopLoss = new SettingsDialog.UserControlTradeStopLoss();
        UserControlTradeDca = new SettingsDialog.UserControlTradeDca();
        panel7 = new Panel();
        label83 = new Label();
        EditTradeVia = new ComboBox();
        label73 = new Label();
        EditGlobalBuyCooldownTime = new NumericUpDown();
        groupBoxInstap = new GroupBox();
        EditCheckFurtherPriceMove = new CheckBox();
        EditCheckIncreasingMacd = new CheckBox();
        EditCheckIncreasingStoch = new CheckBox();
        EditCheckIncreasingRsi = new CheckBox();
        groupBoxSlots = new GroupBox();
        label50 = new Label();
        EditSlotsMaximalLong = new NumericUpDown();
        label52 = new Label();
        EditSlotsMaximalShort = new NumericUpDown();
        groupBoxFutures = new GroupBox();
        label19 = new Label();
        EditCrossOrIsolated = new ComboBox();
        label23 = new Label();
        EditLeverage = new NumericUpDown();
        EditLogCanceledOrders = new CheckBox();
        EditSoundTradeNotification = new CheckBox();
        EditDisableNewPositions = new CheckBox();
        tabTradingLong = new TabPage();
        UserControlTradingLong = new SettingsDialog.UserControlEverything();
        tabTradingShort = new TabPage();
        UserControlTradingShort = new SettingsDialog.UserControlEverything();
        tabPageTradingRules = new TabPage();
        UserControlTradeRules = new SettingsDialog.UserControlTradeRule();
        label59 = new Label();
        tabApi = new TabPage();
        flowLayoutPanel3 = new FlowLayoutPanel();
        UserControlExchangeApi = new SettingsDialog.UserControlExchangeApi();
        UserControlAltradyApi = new SettingsDialog.UserControlAltradyApi();
        tabWhiteBlack = new TabPage();
        tabControlWhiteBlack = new TabControl();
        tabLongWhiteList = new TabPage();
        textBoxWhiteListOversold = new TextBox();
        panel3 = new Panel();
        label55 = new Label();
        tabLongBlackList = new TabPage();
        textBoxBlackListOversold = new TextBox();
        panel4 = new Panel();
        label51 = new Label();
        tabShortWhiteList = new TabPage();
        textBoxWhiteListOverbought = new TextBox();
        panel5 = new Panel();
        label29 = new Label();
        tabShortBlackList = new TabPage();
        textBoxBlackListOverbought = new TextBox();
        panel6 = new Panel();
        label49 = new Label();
        tabPageOptions = new TabPage();
        EditDebugAssetManagement = new CheckBox();
        EditUseHighLowInTrendCalculation = new CheckBox();
        EditDebugTrendCalculation = new CheckBox();
        EditDebugSymbol = new TextBox();
        LabelDebugSymbol = new Label();
        EditDebugSignalStrength = new CheckBox();
        EditDebugSignalCreate = new CheckBox();
        EditDebugKLineReceive = new CheckBox();
        panelButtons.SuspendLayout();
        panelFill.SuspendLayout();
        tabControlMain.SuspendLayout();
        tabGeneral.SuspendLayout();
        flowLayoutPanel5.SuspendLayout();
        groupBox1.SuspendLayout();
        groupBox7.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditBbStdDeviation).BeginInit();
        groupBoxStoch.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditStochValueOversold).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditStochValueOverbought).BeginInit();
        groupBoxRsi.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditRsiValueOversold).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditRsiValueOverbought).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGetCandleInterval).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditGlobalDataRemoveSignalAfterxCandles).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSoundHeartBeatMinutes).BeginInit();
        tabBasecoin.SuspendLayout();
        flowLayoutPanelQuotes.SuspendLayout();
        tabSignal.SuspendLayout();
        tabControlSignals.SuspendLayout();
        tabSignalsGeneral.SuspendLayout();
        groupBox10.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditCheckVolumeOverDays).BeginInit();
        groupBox9.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMinChangePercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMaxChangePercentage).BeginInit();
        GroupBoxXDaysEffective.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisEffectiveDays).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisEffectivePercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditCandlesWithZeroVolume).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditCandlesWithFlatPrice).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumAboveBollingerBandsUpper).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumAboveBollingerBandsSma).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumTickPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSymbolMustExistsDays).BeginInit();
        tabSignalsLong.SuspendLayout();
        tabSignalsShort.SuspendLayout();
        tabSignalStobb.SuspendLayout();
        flowLayoutPanel6.SuspendLayout();
        groupBox2.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditStobbBBMinPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditStobbBBMaxPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditStobTrendShort).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditStobTrendLong).BeginInit();
        tabSignalSbm.SuspendLayout();
        flowLayoutPanel7.SuspendLayout();
        flowLayoutPanel9.SuspendLayout();
        groupBox3.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditSbm1CandlesLookbackCount).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm2BbPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm3CandlesForBBRecovery).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm3CandlesForBBRecoveryPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm2CandlesLookbackCount).BeginInit();
        groupBox4.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditSbmCandlesForMacdRecovery).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmBBMinPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmBBMaxPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa20Percentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa50AndMa20Percentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa50Percentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa50AndMa20Lookback).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa50Lookback).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa20Lookback).BeginInit();
        tabSignalStoRsi.SuspendLayout();
        flowLayoutPanel2.SuspendLayout();
        groupBox6.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditStorsiBBMinPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditStorsiBBMaxPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditStorsiAddStochAmount).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditStorsiAddRsiAmount).BeginInit();
        tabSignalJump.SuspendLayout();
        flowLayoutPanel8.SuspendLayout();
        groupBox5.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditJumpCandlesLookbackCount).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisCandleJumpPercentage).BeginInit();
        tabSignalZones.SuspendLayout();
        flowLayoutPanel4.SuspendLayout();
        groupBox11.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditZonesCandleCount).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditZonesWarnPercentage).BeginInit();
        groupBox12.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditMaximumUnZoomedPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumUnZoomedPercentage).BeginInit();
        groupBox13.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditMinimumZoomedPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditMaximumZoomedPercentage).BeginInit();
        groupBox8.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditZoneStartPercentage).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditZoneStartCandleCount).BeginInit();
        tabSignalZonesFvg.SuspendLayout();
        flowLayoutPanel10.SuspendLayout();
        groupBox14.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditZonesFvgMinimumPercentage).BeginInit();
        tabTrading.SuspendLayout();
        tabControlTrading.SuspendLayout();
        tabTradingGeneral.SuspendLayout();
        flowLayoutPanel1.SuspendLayout();
        panel7.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyCooldownTime).BeginInit();
        groupBoxInstap.SuspendLayout();
        groupBoxSlots.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalLong).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalShort).BeginInit();
        groupBoxFutures.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditLeverage).BeginInit();
        tabTradingLong.SuspendLayout();
        tabTradingShort.SuspendLayout();
        tabPageTradingRules.SuspendLayout();
        tabApi.SuspendLayout();
        flowLayoutPanel3.SuspendLayout();
        tabWhiteBlack.SuspendLayout();
        tabControlWhiteBlack.SuspendLayout();
        tabLongWhiteList.SuspendLayout();
        panel3.SuspendLayout();
        tabLongBlackList.SuspendLayout();
        panel4.SuspendLayout();
        tabShortWhiteList.SuspendLayout();
        panel5.SuspendLayout();
        tabShortBlackList.SuspendLayout();
        panel6.SuspendLayout();
        tabPageOptions.SuspendLayout();
        SuspendLayout();
        // 
        // panelButtons
        // 
        panelButtons.Controls.Add(buttonGotoAppDataFolder);
        panelButtons.Controls.Add(buttonReset);
        panelButtons.Controls.Add(buttonTestSpeech);
        panelButtons.Controls.Add(buttonCancel);
        panelButtons.Controls.Add(buttonOk);
        panelButtons.Dock = DockStyle.Bottom;
        panelButtons.Location = new Point(0, 646);
        panelButtons.Margin = new Padding(4, 3, 4, 3);
        panelButtons.Name = "panelButtons";
        panelButtons.Size = new Size(1150, 46);
        panelButtons.TabIndex = 1;
        // 
        // buttonGotoAppDataFolder
        // 
        buttonGotoAppDataFolder.Location = new Point(239, 10);
        buttonGotoAppDataFolder.Name = "buttonGotoAppDataFolder";
        buttonGotoAppDataFolder.Size = new Size(117, 27);
        buttonGotoAppDataFolder.TabIndex = 190;
        buttonGotoAppDataFolder.Text = "Open data folder";
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
        buttonCancel.Location = new Point(1050, 10);
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
        buttonOk.Location = new Point(955, 10);
        buttonOk.Margin = new Padding(4, 3, 4, 3);
        buttonOk.Name = "buttonOk";
        buttonOk.Size = new Size(88, 27);
        buttonOk.TabIndex = 0;
        buttonOk.Text = "&Ok";
        buttonOk.UseVisualStyleBackColor = true;
        buttonOk.Click += ButtonOk_Click;
        // 
        // panelFill
        // 
        panelFill.Controls.Add(tabControlMain);
        panelFill.Dock = DockStyle.Fill;
        panelFill.Location = new Point(0, 0);
        panelFill.Margin = new Padding(4, 3, 4, 3);
        panelFill.Name = "panelFill";
        panelFill.Size = new Size(1150, 646);
        panelFill.TabIndex = 0;
        // 
        // tabControlMain
        // 
        tabControlMain.Appearance = TabAppearance.FlatButtons;
        tabControlMain.Controls.Add(tabGeneral);
        tabControlMain.Controls.Add(tabBasecoin);
        tabControlMain.Controls.Add(tabSignal);
        tabControlMain.Controls.Add(tabTrading);
        tabControlMain.Controls.Add(tabApi);
        tabControlMain.Controls.Add(tabWhiteBlack);
        tabControlMain.Controls.Add(tabPageOptions);
        tabControlMain.Dock = DockStyle.Fill;
        tabControlMain.Location = new Point(0, 0);
        tabControlMain.Margin = new Padding(4, 3, 4, 3);
        tabControlMain.Name = "tabControlMain";
        tabControlMain.SelectedIndex = 0;
        tabControlMain.Size = new Size(1150, 646);
        tabControlMain.TabIndex = 100;
        // 
        // tabGeneral
        // 
        tabGeneral.Controls.Add(flowLayoutPanel5);
        tabGeneral.Location = new Point(4, 27);
        tabGeneral.Margin = new Padding(4, 3, 4, 3);
        tabGeneral.Name = "tabGeneral";
        tabGeneral.Padding = new Padding(4, 3, 4, 3);
        tabGeneral.Size = new Size(1142, 615);
        tabGeneral.TabIndex = 6;
        tabGeneral.Text = "Common";
        tabGeneral.UseVisualStyleBackColor = true;
        // 
        // flowLayoutPanel5
        // 
        flowLayoutPanel5.AutoScroll = true;
        flowLayoutPanel5.AutoSize = true;
        flowLayoutPanel5.Controls.Add(groupBox1);
        flowLayoutPanel5.Controls.Add(UserControlTelegram);
        flowLayoutPanel5.Dock = DockStyle.Fill;
        flowLayoutPanel5.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel5.Location = new Point(4, 3);
        flowLayoutPanel5.Name = "flowLayoutPanel5";
        flowLayoutPanel5.Size = new Size(1134, 609);
        flowLayoutPanel5.TabIndex = 247;
        // 
        // groupBox1
        // 
        groupBox1.AutoSize = true;
        groupBox1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupBox1.Controls.Add(label42);
        groupBox1.Controls.Add(EditDoubleClickAction);
        groupBox1.Controls.Add(EditHideSelectedRow);
        groupBox1.Controls.Add(groupBox7);
        groupBox1.Controls.Add(label15);
        groupBox1.Controls.Add(EditTradingAppInternExtern);
        groupBox1.Controls.Add(groupBoxStoch);
        groupBox1.Controls.Add(groupBoxRsi);
        groupBox1.Controls.Add(EditExtraCaption);
        groupBox1.Controls.Add(label74);
        groupBox1.Controls.Add(EditHideSymbolsOnTheLeft);
        groupBox1.Controls.Add(label58);
        groupBox1.Controls.Add(EditActivateExchange);
        groupBox1.Controls.Add(EditShowInvalidSignals);
        groupBox1.Controls.Add(label84);
        groupBox1.Controls.Add(EditExchange);
        groupBox1.Controls.Add(label16);
        groupBox1.Controls.Add(EditGetCandleInterval);
        groupBox1.Controls.Add(label6);
        groupBox1.Controls.Add(EditGlobalDataRemoveSignalAfterxCandles);
        groupBox1.Controls.Add(EditBlackTheming);
        groupBox1.Controls.Add(buttonFontDialog);
        groupBox1.Controls.Add(label18);
        groupBox1.Controls.Add(EditSoundHeartBeatMinutes);
        groupBox1.Controls.Add(label2);
        groupBox1.Controls.Add(EditTradingApp);
        groupBox1.Location = new Point(10, 10);
        groupBox1.Margin = new Padding(10);
        groupBox1.Name = "groupBox1";
        groupBox1.Padding = new Padding(10);
        groupBox1.Size = new Size(631, 474);
        groupBox1.TabIndex = 249;
        groupBox1.TabStop = false;
        groupBox1.Text = "Common";
        // 
        // label42
        // 
        label42.AutoSize = true;
        label42.Location = new Point(5, 181);
        label42.Margin = new Padding(4, 0, 4, 0);
        label42.Name = "label42";
        label42.Size = new Size(108, 15);
        label42.TabIndex = 297;
        label42.Text = "Double click action";
        // 
        // EditDoubleClickAction
        // 
        EditDoubleClickAction.DropDownStyle = ComboBoxStyle.DropDownList;
        EditDoubleClickAction.FormattingEnabled = true;
        EditDoubleClickAction.Items.AddRange(new object[] { "Show trading app", "Show chart form" });
        EditDoubleClickAction.Location = new Point(171, 176);
        EditDoubleClickAction.Margin = new Padding(4, 3, 4, 3);
        EditDoubleClickAction.Name = "EditDoubleClickAction";
        EditDoubleClickAction.Size = new Size(190, 23);
        EditDoubleClickAction.TabIndex = 296;
        // 
        // EditHideSelectedRow
        // 
        EditHideSelectedRow.AutoSize = true;
        EditHideSelectedRow.Location = new Point(9, 356);
        EditHideSelectedRow.Margin = new Padding(4, 3, 4, 3);
        EditHideSelectedRow.Name = "EditHideSelectedRow";
        EditHideSelectedRow.Size = new Size(138, 19);
        EditHideSelectedRow.TabIndex = 295;
        EditHideSelectedRow.Text = "Hide selection in grid";
        EditHideSelectedRow.UseVisualStyleBackColor = true;
        // 
        // groupBox7
        // 
        groupBox7.Controls.Add(label22);
        groupBox7.Controls.Add(EditBbStdDeviation);
        groupBox7.Location = new Point(384, 255);
        groupBox7.Name = "groupBox7";
        groupBox7.Size = new Size(234, 58);
        groupBox7.TabIndex = 177;
        groupBox7.TabStop = false;
        groupBox7.Text = "Bollinger bands";
        // 
        // label22
        // 
        label22.AutoSize = true;
        label22.Location = new Point(9, 26);
        label22.Margin = new Padding(4, 0, 4, 0);
        label22.Name = "label22";
        label22.Size = new Size(106, 15);
        label22.TabIndex = 290;
        label22.Text = "Standard deviation";
        // 
        // EditBbStdDeviation
        // 
        EditBbStdDeviation.DecimalPlaces = 2;
        EditBbStdDeviation.Location = new Point(130, 22);
        EditBbStdDeviation.Margin = new Padding(4, 3, 4, 3);
        EditBbStdDeviation.Name = "EditBbStdDeviation";
        EditBbStdDeviation.Size = new Size(88, 23);
        EditBbStdDeviation.TabIndex = 291;
        EditBbStdDeviation.Value = new decimal(new int[] { 20, 0, 0, 65536 });
        // 
        // label15
        // 
        label15.AutoSize = true;
        label15.Location = new Point(5, 152);
        label15.Margin = new Padding(4, 0, 4, 0);
        label15.Name = "label15";
        label15.Size = new Size(152, 15);
        label15.TabIndex = 287;
        label15.Text = "Activate Internal or external";
        // 
        // EditTradingAppInternExtern
        // 
        EditTradingAppInternExtern.DropDownStyle = ComboBoxStyle.DropDownList;
        EditTradingAppInternExtern.FormattingEnabled = true;
        EditTradingAppInternExtern.Items.AddRange(new object[] { "Internal browser", "External browser" });
        EditTradingAppInternExtern.Location = new Point(171, 147);
        EditTradingAppInternExtern.Margin = new Padding(4, 3, 4, 3);
        EditTradingAppInternExtern.Name = "EditTradingAppInternExtern";
        EditTradingAppInternExtern.Size = new Size(190, 23);
        EditTradingAppInternExtern.TabIndex = 286;
        // 
        // groupBoxStoch
        // 
        groupBoxStoch.Controls.Add(EditStochValueOversold);
        groupBoxStoch.Controls.Add(label88);
        groupBoxStoch.Controls.Add(label89);
        groupBoxStoch.Controls.Add(EditStochValueOverbought);
        groupBoxStoch.Location = new Point(384, 143);
        groupBoxStoch.Name = "groupBoxStoch";
        groupBoxStoch.Size = new Size(234, 96);
        groupBoxStoch.TabIndex = 285;
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
        groupBoxRsi.Location = new Point(384, 40);
        groupBoxRsi.Name = "groupBoxRsi";
        groupBoxRsi.Size = new Size(234, 96);
        groupBoxRsi.TabIndex = 284;
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
        EditExtraCaption.Location = new Point(171, 31);
        EditExtraCaption.Margin = new Padding(4, 3, 4, 3);
        EditExtraCaption.Name = "EditExtraCaption";
        EditExtraCaption.Size = new Size(193, 23);
        EditExtraCaption.TabIndex = 282;
        // 
        // label74
        // 
        label74.AutoSize = true;
        label74.Location = new Point(9, 35);
        label74.Margin = new Padding(4, 0, 4, 0);
        label74.Name = "label74";
        label74.Size = new Size(138, 15);
        label74.TabIndex = 283;
        label74.Text = "Extra application caption";
        // 
        // EditHideSymbolsOnTheLeft
        // 
        EditHideSymbolsOnTheLeft.AutoSize = true;
        EditHideSymbolsOnTheLeft.Location = new Point(9, 331);
        EditHideSymbolsOnTheLeft.Margin = new Padding(4, 3, 4, 3);
        EditHideSymbolsOnTheLeft.Name = "EditHideSymbolsOnTheLeft";
        EditHideSymbolsOnTheLeft.Size = new Size(131, 19);
        EditHideSymbolsOnTheLeft.TabIndex = 281;
        EditHideSymbolsOnTheLeft.Text = "Hide the symbol list";
        EditHideSymbolsOnTheLeft.UseVisualStyleBackColor = true;
        // 
        // label58
        // 
        label58.AutoSize = true;
        label58.Location = new Point(7, 94);
        label58.Margin = new Padding(4, 0, 4, 0);
        label58.Name = "label58";
        label58.Size = new Size(104, 15);
        label58.TabIndex = 280;
        label58.Text = "Activate exchange";
        // 
        // EditActivateExchange
        // 
        EditActivateExchange.DropDownStyle = ComboBoxStyle.DropDownList;
        EditActivateExchange.FormattingEnabled = true;
        EditActivateExchange.Location = new Point(171, 89);
        EditActivateExchange.Margin = new Padding(4, 3, 4, 3);
        EditActivateExchange.Name = "EditActivateExchange";
        EditActivateExchange.Size = new Size(190, 23);
        EditActivateExchange.TabIndex = 279;
        // 
        // EditShowInvalidSignals
        // 
        EditShowInvalidSignals.AutoSize = true;
        EditShowInvalidSignals.Location = new Point(9, 304);
        EditShowInvalidSignals.Margin = new Padding(4, 3, 4, 3);
        EditShowInvalidSignals.Name = "EditShowInvalidSignals";
        EditShowInvalidSignals.Size = new Size(132, 19);
        EditShowInvalidSignals.TabIndex = 278;
        EditShowInvalidSignals.Text = "Show invalid signals";
        EditShowInvalidSignals.UseVisualStyleBackColor = true;
        // 
        // label84
        // 
        label84.AutoSize = true;
        label84.Location = new Point(7, 65);
        label84.Margin = new Padding(4, 0, 4, 0);
        label84.Name = "label84";
        label84.Size = new Size(94, 15);
        label84.TabIndex = 277;
        label84.Text = "Active exchange";
        // 
        // EditExchange
        // 
        EditExchange.DropDownStyle = ComboBoxStyle.DropDownList;
        EditExchange.FormattingEnabled = true;
        EditExchange.Location = new Point(171, 60);
        EditExchange.Margin = new Padding(4, 3, 4, 3);
        EditExchange.Name = "EditExchange";
        EditExchange.Size = new Size(190, 23);
        EditExchange.TabIndex = 276;
        // 
        // label16
        // 
        label16.AutoSize = true;
        label16.Location = new Point(6, 271);
        label16.Margin = new Padding(4, 0, 4, 0);
        label16.Name = "label16";
        label16.Size = new Size(216, 15);
        label16.TabIndex = 274;
        label16.Text = "Check every x minutes for new symbols";
        // 
        // EditGetCandleInterval
        // 
        EditGetCandleInterval.Location = new Point(304, 268);
        EditGetCandleInterval.Margin = new Padding(4, 3, 4, 3);
        EditGetCandleInterval.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
        EditGetCandleInterval.Minimum = new decimal(new int[] { 30, 0, 0, 0 });
        EditGetCandleInterval.Name = "EditGetCandleInterval";
        EditGetCandleInterval.Size = new Size(57, 23);
        EditGetCandleInterval.TabIndex = 275;
        EditGetCandleInterval.Value = new decimal(new int[] { 30, 0, 0, 0 });
        // 
        // label6
        // 
        label6.AutoSize = true;
        label6.Location = new Point(6, 246);
        label6.Margin = new Padding(4, 0, 4, 0);
        label6.Name = "label6";
        label6.Size = new Size(168, 15);
        label6.TabIndex = 272;
        label6.Text = "Remove signals after x candles";
        // 
        // EditGlobalDataRemoveSignalAfterxCandles
        // 
        EditGlobalDataRemoveSignalAfterxCandles.Location = new Point(304, 242);
        EditGlobalDataRemoveSignalAfterxCandles.Margin = new Padding(4, 3, 4, 3);
        EditGlobalDataRemoveSignalAfterxCandles.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditGlobalDataRemoveSignalAfterxCandles.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
        EditGlobalDataRemoveSignalAfterxCandles.Name = "EditGlobalDataRemoveSignalAfterxCandles";
        EditGlobalDataRemoveSignalAfterxCandles.Size = new Size(57, 23);
        EditGlobalDataRemoveSignalAfterxCandles.TabIndex = 273;
        EditGlobalDataRemoveSignalAfterxCandles.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // EditBlackTheming
        // 
        EditBlackTheming.AutoSize = true;
        EditBlackTheming.Location = new Point(6, 426);
        EditBlackTheming.Margin = new Padding(4, 3, 4, 3);
        EditBlackTheming.Name = "EditBlackTheming";
        EditBlackTheming.Size = new Size(84, 19);
        EditBlackTheming.TabIndex = 271;
        EditBlackTheming.Text = "Gray mode";
        EditBlackTheming.UseVisualStyleBackColor = true;
        // 
        // buttonFontDialog
        // 
        buttonFontDialog.Location = new Point(4, 383);
        buttonFontDialog.Margin = new Padding(4, 3, 4, 3);
        buttonFontDialog.Name = "buttonFontDialog";
        buttonFontDialog.Size = new Size(139, 27);
        buttonFontDialog.TabIndex = 270;
        buttonFontDialog.Text = "Fonttype";
        buttonFontDialog.UseVisualStyleBackColor = true;
        // 
        // label18
        // 
        label18.AutoSize = true;
        label18.Location = new Point(6, 219);
        label18.Margin = new Padding(4, 0, 4, 0);
        label18.Name = "label18";
        label18.Size = new Size(207, 15);
        label18.TabIndex = 268;
        label18.Text = "Play heart beat sound every x minutes";
        // 
        // EditSoundHeartBeatMinutes
        // 
        EditSoundHeartBeatMinutes.Location = new Point(304, 216);
        EditSoundHeartBeatMinutes.Margin = new Padding(4, 3, 4, 3);
        EditSoundHeartBeatMinutes.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSoundHeartBeatMinutes.Name = "EditSoundHeartBeatMinutes";
        EditSoundHeartBeatMinutes.Size = new Size(57, 23);
        EditSoundHeartBeatMinutes.TabIndex = 269;
        EditSoundHeartBeatMinutes.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(6, 123);
        label2.Margin = new Padding(4, 0, 4, 0);
        label2.Name = "label2";
        label2.Size = new Size(69, 15);
        label2.TabIndex = 267;
        label2.Text = "Trading app";
        // 
        // EditTradingApp
        // 
        EditTradingApp.DropDownStyle = ComboBoxStyle.DropDownList;
        EditTradingApp.FormattingEnabled = true;
        EditTradingApp.Items.AddRange(new object[] { "Altrady", "Hypertrader", "TradingView", "Via de exchange" });
        EditTradingApp.Location = new Point(171, 118);
        EditTradingApp.Margin = new Padding(4, 3, 4, 3);
        EditTradingApp.Name = "EditTradingApp";
        EditTradingApp.Size = new Size(190, 23);
        EditTradingApp.TabIndex = 266;
        EditTradingApp.SelectionChangeCommitted += EditTradingApp_SelectionChangeCommitted;
        // 
        // UserControlTelegram
        // 
        UserControlTelegram.AutoScroll = true;
        UserControlTelegram.AutoSize = true;
        UserControlTelegram.Location = new Point(654, 3);
        UserControlTelegram.Name = "UserControlTelegram";
        UserControlTelegram.Padding = new Padding(10);
        UserControlTelegram.Size = new Size(427, 180);
        UserControlTelegram.TabIndex = 248;
        // 
        // tabBasecoin
        // 
        tabBasecoin.Controls.Add(flowLayoutPanelQuotes);
        tabBasecoin.Location = new Point(4, 27);
        tabBasecoin.Margin = new Padding(4, 3, 4, 3);
        tabBasecoin.Name = "tabBasecoin";
        tabBasecoin.Padding = new Padding(4, 3, 4, 3);
        tabBasecoin.Size = new Size(1142, 615);
        tabBasecoin.TabIndex = 0;
        tabBasecoin.Text = "Basecoins";
        tabBasecoin.UseVisualStyleBackColor = true;
        // 
        // flowLayoutPanelQuotes
        // 
        flowLayoutPanelQuotes.AutoScroll = true;
        flowLayoutPanelQuotes.AutoSize = true;
        flowLayoutPanelQuotes.Controls.Add(userControlQuoteHeader1);
        flowLayoutPanelQuotes.Dock = DockStyle.Fill;
        flowLayoutPanelQuotes.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanelQuotes.Location = new Point(4, 3);
        flowLayoutPanelQuotes.Margin = new Padding(0);
        flowLayoutPanelQuotes.Name = "flowLayoutPanelQuotes";
        flowLayoutPanelQuotes.Size = new Size(1134, 609);
        flowLayoutPanelQuotes.TabIndex = 0;
        // 
        // userControlQuoteHeader1
        // 
        userControlQuoteHeader1.AutoSize = true;
        userControlQuoteHeader1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        userControlQuoteHeader1.Location = new Point(0, 0);
        userControlQuoteHeader1.Margin = new Padding(0);
        userControlQuoteHeader1.Name = "userControlQuoteHeader1";
        userControlQuoteHeader1.Padding = new Padding(0, 0, 0, 3);
        userControlQuoteHeader1.Size = new Size(620, 23);
        userControlQuoteHeader1.TabIndex = 0;
        // 
        // tabSignal
        // 
        tabSignal.Controls.Add(tabControlSignals);
        tabSignal.Location = new Point(4, 27);
        tabSignal.Margin = new Padding(4, 3, 4, 3);
        tabSignal.Name = "tabSignal";
        tabSignal.Padding = new Padding(4, 3, 4, 3);
        tabSignal.Size = new Size(1142, 615);
        tabSignal.TabIndex = 10;
        tabSignal.Text = "Signals";
        tabSignal.UseVisualStyleBackColor = true;
        // 
        // tabControlSignals
        // 
        tabControlSignals.Appearance = TabAppearance.FlatButtons;
        tabControlSignals.Controls.Add(tabSignalsGeneral);
        tabControlSignals.Controls.Add(tabSignalsLong);
        tabControlSignals.Controls.Add(tabSignalsShort);
        tabControlSignals.Controls.Add(tabSignalStobb);
        tabControlSignals.Controls.Add(tabSignalSbm);
        tabControlSignals.Controls.Add(tabSignalStoRsi);
        tabControlSignals.Controls.Add(tabSignalJump);
        tabControlSignals.Controls.Add(tabSignalZones);
        tabControlSignals.Controls.Add(tabSignalZonesFvg);
        tabControlSignals.Dock = DockStyle.Fill;
        tabControlSignals.Location = new Point(4, 3);
        tabControlSignals.Name = "tabControlSignals";
        tabControlSignals.SelectedIndex = 0;
        tabControlSignals.Size = new Size(1134, 609);
        tabControlSignals.TabIndex = 248;
        // 
        // tabSignalsGeneral
        // 
        tabSignalsGeneral.Controls.Add(groupBox10);
        tabSignalsGeneral.Controls.Add(groupBox9);
        tabSignalsGeneral.Controls.Add(GroupBoxXDaysEffective);
        tabSignalsGeneral.Controls.Add(label79);
        tabSignalsGeneral.Controls.Add(label48);
        tabSignalsGeneral.Controls.Add(label38);
        tabSignalsGeneral.Controls.Add(label37);
        tabSignalsGeneral.Controls.Add(label10);
        tabSignalsGeneral.Controls.Add(EditCandlesWithFlatPriceCheck);
        tabSignalsGeneral.Controls.Add(EditCandlesWithZeroVolumeCheck);
        tabSignalsGeneral.Controls.Add(EditMinimumAboveBollingerBandsSmaCheck);
        tabSignalsGeneral.Controls.Add(EditMinimumAboveBollingerBandsUpperCheck);
        tabSignalsGeneral.Controls.Add(EditCandlesWithZeroVolume);
        tabSignalsGeneral.Controls.Add(EditCandlesWithFlatPrice);
        tabSignalsGeneral.Controls.Add(EditMinimumAboveBollingerBandsUpper);
        tabSignalsGeneral.Controls.Add(EditMinimumAboveBollingerBandsSma);
        tabSignalsGeneral.Controls.Add(EditLogMinimumTickPercentage);
        tabSignalsGeneral.Controls.Add(EditMinimumTickPercentage);
        tabSignalsGeneral.Controls.Add(label61);
        tabSignalsGeneral.Controls.Add(EditLogSymbolMustExistsDays);
        tabSignalsGeneral.Controls.Add(EditSymbolMustExistsDays);
        tabSignalsGeneral.Controls.Add(label25);
        tabSignalsGeneral.Location = new Point(4, 27);
        tabSignalsGeneral.Name = "tabSignalsGeneral";
        tabSignalsGeneral.Padding = new Padding(3);
        tabSignalsGeneral.Size = new Size(1126, 578);
        tabSignalsGeneral.TabIndex = 0;
        tabSignalsGeneral.Text = "Signals common";
        tabSignalsGeneral.UseVisualStyleBackColor = true;
        // 
        // groupBox10
        // 
        groupBox10.AutoSize = true;
        groupBox10.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupBox10.Controls.Add(label46);
        groupBox10.Controls.Add(label27);
        groupBox10.Controls.Add(EditCheckVolumeOverDays);
        groupBox10.Controls.Add(label45);
        groupBox10.Controls.Add(EditCheckVolumeOverPeriod);
        groupBox10.Location = new Point(352, 124);
        groupBox10.Name = "groupBox10";
        groupBox10.Size = new Size(284, 119);
        groupBox10.TabIndex = 292;
        groupBox10.TabStop = false;
        groupBox10.Text = "Check volume multiple days";
        // 
        // label46
        // 
        label46.AutoSize = true;
        label46.Location = new Point(18, 85);
        label46.Margin = new Padding(4, 0, 4, 0);
        label46.Name = "label46";
        label46.Size = new Size(258, 15);
        label46.TabIndex = 312;
        label46.Text = "(using the minimal volume from the basecoins)";
        // 
        // label27
        // 
        label27.AutoSize = true;
        label27.Location = new Point(246, 59);
        label27.Margin = new Padding(4, 0, 4, 0);
        label27.Name = "label27";
        label27.Size = new Size(31, 15);
        label27.TabIndex = 311;
        label27.Text = "days";
        // 
        // EditCheckVolumeOverDays
        // 
        EditCheckVolumeOverDays.Location = new Point(137, 55);
        EditCheckVolumeOverDays.Margin = new Padding(4, 3, 4, 3);
        EditCheckVolumeOverDays.Maximum = new decimal(new int[] { 25, 0, 0, 0 });
        EditCheckVolumeOverDays.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditCheckVolumeOverDays.Name = "EditCheckVolumeOverDays";
        EditCheckVolumeOverDays.Size = new Size(88, 23);
        EditCheckVolumeOverDays.TabIndex = 310;
        EditCheckVolumeOverDays.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // label45
        // 
        label45.AutoSize = true;
        label45.Location = new Point(17, 59);
        label45.Margin = new Padding(4, 0, 4, 0);
        label45.Name = "label45";
        label45.Size = new Size(96, 15);
        label45.TabIndex = 309;
        label45.Text = "Check over day's";
        // 
        // EditCheckVolumeOverPeriod
        // 
        EditCheckVolumeOverPeriod.AutoSize = true;
        EditCheckVolumeOverPeriod.Location = new Point(17, 26);
        EditCheckVolumeOverPeriod.Margin = new Padding(4, 3, 4, 3);
        EditCheckVolumeOverPeriod.Name = "EditCheckVolumeOverPeriod";
        EditCheckVolumeOverPeriod.Size = new Size(202, 19);
        EditCheckVolumeOverPeriod.TabIndex = 285;
        EditCheckVolumeOverPeriod.Text = "Check volume over multiple days";
        EditCheckVolumeOverPeriod.UseVisualStyleBackColor = true;
        // 
        // groupBox9
        // 
        groupBox9.AutoSize = true;
        groupBox9.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupBox9.Controls.Add(EditAnalysisMinChangePercentage);
        groupBox9.Controls.Add(EditAnalysisMaxChangePercentage);
        groupBox9.Controls.Add(label53);
        groupBox9.Controls.Add(EditLogAnalysisMinMaxChangePercentage);
        groupBox9.Location = new Point(21, 10);
        groupBox9.Name = "groupBox9";
        groupBox9.Size = new Size(281, 101);
        groupBox9.TabIndex = 291;
        groupBox9.TabStop = false;
        groupBox9.Text = "Check relative change% 24 hours";
        // 
        // EditAnalysisMinChangePercentage
        // 
        EditAnalysisMinChangePercentage.Location = new Point(142, 31);
        EditAnalysisMinChangePercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMinChangePercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisMinChangePercentage.Name = "EditAnalysisMinChangePercentage";
        EditAnalysisMinChangePercentage.Size = new Size(57, 23);
        EditAnalysisMinChangePercentage.TabIndex = 264;
        // 
        // EditAnalysisMaxChangePercentage
        // 
        EditAnalysisMaxChangePercentage.Location = new Point(217, 31);
        EditAnalysisMaxChangePercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMaxChangePercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisMaxChangePercentage.Name = "EditAnalysisMaxChangePercentage";
        EditAnalysisMaxChangePercentage.Size = new Size(57, 23);
        EditAnalysisMaxChangePercentage.TabIndex = 265;
        EditAnalysisMaxChangePercentage.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label53
        // 
        label53.AutoSize = true;
        label53.Location = new Point(7, 31);
        label53.Margin = new Padding(4, 0, 4, 0);
        label53.Name = "label53";
        label53.Size = new Size(82, 15);
        label53.TabIndex = 263;
        label53.Text = "24 uur change";
        // 
        // EditLogAnalysisMinMaxChangePercentage
        // 
        EditLogAnalysisMinMaxChangePercentage.AutoSize = true;
        EditLogAnalysisMinMaxChangePercentage.Location = new Point(7, 60);
        EditLogAnalysisMinMaxChangePercentage.Margin = new Padding(4, 3, 4, 3);
        EditLogAnalysisMinMaxChangePercentage.Name = "EditLogAnalysisMinMaxChangePercentage";
        EditLogAnalysisMinMaxChangePercentage.Size = new Size(152, 19);
        EditLogAnalysisMinMaxChangePercentage.TabIndex = 262;
        EditLogAnalysisMinMaxChangePercentage.Text = "Log if outside boundary";
        EditLogAnalysisMinMaxChangePercentage.UseVisualStyleBackColor = true;
        // 
        // GroupBoxXDaysEffective
        // 
        GroupBoxXDaysEffective.AutoSize = true;
        GroupBoxXDaysEffective.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        GroupBoxXDaysEffective.Controls.Add(label44);
        GroupBoxXDaysEffective.Controls.Add(label43);
        GroupBoxXDaysEffective.Controls.Add(label31);
        GroupBoxXDaysEffective.Controls.Add(EditAnalysisEffectiveDays);
        GroupBoxXDaysEffective.Controls.Add(label86);
        GroupBoxXDaysEffective.Controls.Add(EditAnalysisEffectivePercentage);
        GroupBoxXDaysEffective.Controls.Add(EditAnalysisMaxEffectiveLog);
        GroupBoxXDaysEffective.Location = new Point(21, 117);
        GroupBoxXDaysEffective.Name = "GroupBoxXDaysEffective";
        GroupBoxXDaysEffective.Size = new Size(311, 123);
        GroupBoxXDaysEffective.TabIndex = 290;
        GroupBoxXDaysEffective.TabStop = false;
        GroupBoxXDaysEffective.Text = "Check effective change% over multiple day's";
        // 
        // label44
        // 
        label44.AutoSize = true;
        label44.Location = new Point(273, 55);
        label44.Margin = new Padding(4, 0, 4, 0);
        label44.Name = "label44";
        label44.Size = new Size(31, 15);
        label44.TabIndex = 310;
        label44.Text = "days";
        // 
        // label43
        // 
        label43.AutoSize = true;
        label43.Location = new Point(273, 29);
        label43.Margin = new Padding(4, 0, 4, 0);
        label43.Name = "label43";
        label43.Size = new Size(17, 15);
        label43.TabIndex = 309;
        label43.Text = "%";
        // 
        // label31
        // 
        label31.AutoSize = true;
        label31.Location = new Point(7, 55);
        label31.Margin = new Padding(4, 0, 4, 0);
        label31.Name = "label31";
        label31.Size = new Size(118, 15);
        label31.TabIndex = 308;
        label31.Text = "Check effective day's";
        // 
        // EditAnalysisEffectiveDays
        // 
        EditAnalysisEffectiveDays.Location = new Point(197, 53);
        EditAnalysisEffectiveDays.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisEffectiveDays.Maximum = new decimal(new int[] { 25, 0, 0, 0 });
        EditAnalysisEffectiveDays.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditAnalysisEffectiveDays.Name = "EditAnalysisEffectiveDays";
        EditAnalysisEffectiveDays.Size = new Size(57, 23);
        EditAnalysisEffectiveDays.TabIndex = 307;
        EditAnalysisEffectiveDays.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // label86
        // 
        label86.AutoSize = true;
        label86.Location = new Point(7, 29);
        label86.Margin = new Padding(4, 0, 4, 0);
        label86.Name = "label86";
        label86.Size = new Size(150, 15);
        label86.TabIndex = 305;
        label86.Text = "Check effective percentage";
        // 
        // EditAnalysisEffectivePercentage
        // 
        EditAnalysisEffectivePercentage.Location = new Point(197, 27);
        EditAnalysisEffectivePercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisEffectivePercentage.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditAnalysisEffectivePercentage.Name = "EditAnalysisEffectivePercentage";
        EditAnalysisEffectivePercentage.Size = new Size(57, 23);
        EditAnalysisEffectivePercentage.TabIndex = 306;
        EditAnalysisEffectivePercentage.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // EditAnalysisMaxEffectiveLog
        // 
        EditAnalysisMaxEffectiveLog.AutoSize = true;
        EditAnalysisMaxEffectiveLog.Location = new Point(7, 82);
        EditAnalysisMaxEffectiveLog.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisMaxEffectiveLog.Name = "EditAnalysisMaxEffectiveLog";
        EditAnalysisMaxEffectiveLog.Size = new Size(152, 19);
        EditAnalysisMaxEffectiveLog.TabIndex = 304;
        EditAnalysisMaxEffectiveLog.Text = "Log if outside boundary";
        EditAnalysisMaxEffectiveLog.UseVisualStyleBackColor = true;
        // 
        // label79
        // 
        label79.AutoSize = true;
        label79.Location = new Point(462, 465);
        label79.Margin = new Padding(4, 0, 4, 0);
        label79.Name = "label79";
        label79.Size = new Size(97, 15);
        label79.TabIndex = 275;
        label79.Text = "Smaller is a nogo";
        // 
        // label48
        // 
        label48.AutoSize = true;
        label48.Location = new Point(462, 433);
        label48.Margin = new Padding(4, 0, 4, 0);
        label48.Name = "label48";
        label48.Size = new Size(97, 15);
        label48.TabIndex = 274;
        label48.Text = "Smaller is a nogo";
        // 
        // label38
        // 
        label38.AutoSize = true;
        label38.Location = new Point(462, 403);
        label38.Margin = new Padding(4, 0, 4, 0);
        label38.Name = "label38";
        label38.Size = new Size(91, 15);
        label38.TabIndex = 273;
        label38.Text = "Larger is a nogo";
        // 
        // label37
        // 
        label37.AutoSize = true;
        label37.Location = new Point(462, 376);
        label37.Margin = new Padding(4, 0, 4, 0);
        label37.Name = "label37";
        label37.Size = new Size(91, 15);
        label37.TabIndex = 272;
        label37.Text = "Larger is a nogo";
        // 
        // label10
        // 
        label10.AutoSize = true;
        label10.Location = new Point(22, 348);
        label10.Margin = new Padding(4, 0, 4, 0);
        label10.Name = "label10";
        label10.Size = new Size(139, 15);
        label10.TabIndex = 271;
        label10.Text = "Check the last 60 candles";
        // 
        // EditCandlesWithFlatPriceCheck
        // 
        EditCandlesWithFlatPriceCheck.AutoSize = true;
        EditCandlesWithFlatPriceCheck.Location = new Point(22, 378);
        EditCandlesWithFlatPriceCheck.Margin = new Padding(4, 3, 4, 3);
        EditCandlesWithFlatPriceCheck.Name = "EditCandlesWithFlatPriceCheck";
        EditCandlesWithFlatPriceCheck.Size = new Size(201, 19);
        EditCandlesWithFlatPriceCheck.TabIndex = 270;
        EditCandlesWithFlatPriceCheck.Text = "Check the number of flat candles";
        EditCandlesWithFlatPriceCheck.UseVisualStyleBackColor = true;
        // 
        // EditCandlesWithZeroVolumeCheck
        // 
        EditCandlesWithZeroVolumeCheck.AutoSize = true;
        EditCandlesWithZeroVolumeCheck.Location = new Point(23, 407);
        EditCandlesWithZeroVolumeCheck.Margin = new Padding(4, 3, 4, 3);
        EditCandlesWithZeroVolumeCheck.Name = "EditCandlesWithZeroVolumeCheck";
        EditCandlesWithZeroVolumeCheck.Size = new Size(290, 19);
        EditCandlesWithZeroVolumeCheck.TabIndex = 269;
        EditCandlesWithZeroVolumeCheck.Text = "Check the number of candles without any volume";
        EditCandlesWithZeroVolumeCheck.UseVisualStyleBackColor = true;
        // 
        // EditMinimumAboveBollingerBandsSmaCheck
        // 
        EditMinimumAboveBollingerBandsSmaCheck.AutoSize = true;
        EditMinimumAboveBollingerBandsSmaCheck.Location = new Point(23, 437);
        EditMinimumAboveBollingerBandsSmaCheck.Margin = new Padding(4, 3, 4, 3);
        EditMinimumAboveBollingerBandsSmaCheck.Name = "EditMinimumAboveBollingerBandsSmaCheck";
        EditMinimumAboveBollingerBandsSmaCheck.Size = new Size(219, 19);
        EditMinimumAboveBollingerBandsSmaCheck.TabIndex = 268;
        EditMinimumAboveBollingerBandsSmaCheck.Text = "Check if candles are above bb.center";
        EditMinimumAboveBollingerBandsSmaCheck.UseVisualStyleBackColor = true;
        // 
        // EditMinimumAboveBollingerBandsUpperCheck
        // 
        EditMinimumAboveBollingerBandsUpperCheck.AutoSize = true;
        EditMinimumAboveBollingerBandsUpperCheck.Location = new Point(23, 467);
        EditMinimumAboveBollingerBandsUpperCheck.Margin = new Padding(4, 3, 4, 3);
        EditMinimumAboveBollingerBandsUpperCheck.Name = "EditMinimumAboveBollingerBandsUpperCheck";
        EditMinimumAboveBollingerBandsUpperCheck.Size = new Size(247, 19);
        EditMinimumAboveBollingerBandsUpperCheck.TabIndex = 267;
        EditMinimumAboveBollingerBandsUpperCheck.Text = "Check number of candles above bb.upper";
        EditMinimumAboveBollingerBandsUpperCheck.UseVisualStyleBackColor = true;
        // 
        // EditCandlesWithZeroVolume
        // 
        EditCandlesWithZeroVolume.Location = new Point(353, 403);
        EditCandlesWithZeroVolume.Margin = new Padding(4, 3, 4, 3);
        EditCandlesWithZeroVolume.Name = "EditCandlesWithZeroVolume";
        EditCandlesWithZeroVolume.Size = new Size(88, 23);
        EditCandlesWithZeroVolume.TabIndex = 266;
        EditCandlesWithZeroVolume.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // EditCandlesWithFlatPrice
        // 
        EditCandlesWithFlatPrice.Location = new Point(353, 374);
        EditCandlesWithFlatPrice.Margin = new Padding(4, 3, 4, 3);
        EditCandlesWithFlatPrice.Name = "EditCandlesWithFlatPrice";
        EditCandlesWithFlatPrice.Size = new Size(88, 23);
        EditCandlesWithFlatPrice.TabIndex = 265;
        EditCandlesWithFlatPrice.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // EditMinimumAboveBollingerBandsUpper
        // 
        EditMinimumAboveBollingerBandsUpper.Location = new Point(352, 463);
        EditMinimumAboveBollingerBandsUpper.Margin = new Padding(4, 3, 4, 3);
        EditMinimumAboveBollingerBandsUpper.Name = "EditMinimumAboveBollingerBandsUpper";
        EditMinimumAboveBollingerBandsUpper.Size = new Size(88, 23);
        EditMinimumAboveBollingerBandsUpper.TabIndex = 264;
        EditMinimumAboveBollingerBandsUpper.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // EditMinimumAboveBollingerBandsSma
        // 
        EditMinimumAboveBollingerBandsSma.Location = new Point(353, 433);
        EditMinimumAboveBollingerBandsSma.Margin = new Padding(4, 3, 4, 3);
        EditMinimumAboveBollingerBandsSma.Name = "EditMinimumAboveBollingerBandsSma";
        EditMinimumAboveBollingerBandsSma.Size = new Size(88, 23);
        EditMinimumAboveBollingerBandsSma.TabIndex = 263;
        EditMinimumAboveBollingerBandsSma.Value = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // EditLogMinimumTickPercentage
        // 
        EditLogMinimumTickPercentage.AutoSize = true;
        EditLogMinimumTickPercentage.Location = new Point(307, 294);
        EditLogMinimumTickPercentage.Margin = new Padding(4, 3, 4, 3);
        EditLogMinimumTickPercentage.Name = "EditLogMinimumTickPercentage";
        EditLogMinimumTickPercentage.Size = new Size(128, 19);
        EditLogMinimumTickPercentage.TabIndex = 257;
        EditLogMinimumTickPercentage.Text = "Log if its above this";
        EditLogMinimumTickPercentage.UseVisualStyleBackColor = true;
        // 
        // EditMinimumTickPercentage
        // 
        EditMinimumTickPercentage.DecimalPlaces = 2;
        EditMinimumTickPercentage.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
        EditMinimumTickPercentage.Location = new Point(194, 293);
        EditMinimumTickPercentage.Margin = new Padding(4, 3, 4, 3);
        EditMinimumTickPercentage.Name = "EditMinimumTickPercentage";
        EditMinimumTickPercentage.Size = new Size(75, 23);
        EditMinimumTickPercentage.TabIndex = 256;
        EditMinimumTickPercentage.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // label61
        // 
        label61.AutoSize = true;
        label61.Location = new Point(23, 296);
        label61.Margin = new Padding(4, 0, 4, 0);
        label61.Name = "label61";
        label61.Size = new Size(90, 15);
        label61.TabIndex = 255;
        label61.Text = "Tick percentage";
        // 
        // EditLogSymbolMustExistsDays
        // 
        EditLogSymbolMustExistsDays.AutoSize = true;
        EditLogSymbolMustExistsDays.Location = new Point(307, 265);
        EditLogSymbolMustExistsDays.Margin = new Padding(4, 3, 4, 3);
        EditLogSymbolMustExistsDays.Name = "EditLogSymbolMustExistsDays";
        EditLogSymbolMustExistsDays.Size = new Size(137, 19);
        EditLogSymbolMustExistsDays.TabIndex = 254;
        EditLogSymbolMustExistsDays.Text = "Log if it is a new coin";
        EditLogSymbolMustExistsDays.UseVisualStyleBackColor = true;
        // 
        // EditSymbolMustExistsDays
        // 
        EditSymbolMustExistsDays.Location = new Point(193, 265);
        EditSymbolMustExistsDays.Margin = new Padding(4, 3, 4, 3);
        EditSymbolMustExistsDays.Name = "EditSymbolMustExistsDays";
        EditSymbolMustExistsDays.Size = new Size(75, 23);
        EditSymbolMustExistsDays.TabIndex = 253;
        EditSymbolMustExistsDays.Value = new decimal(new int[] { 15, 0, 0, 0 });
        // 
        // label25
        // 
        label25.AutoSize = true;
        label25.Location = new Point(23, 269);
        label25.Margin = new Padding(4, 0, 4, 0);
        label25.Name = "label25";
        label25.Size = new Size(155, 15);
        label25.TabIndex = 252;
        label25.Text = "Symbol exists at least x days";
        // 
        // tabSignalsLong
        // 
        tabSignalsLong.Controls.Add(UserControlSignalLong);
        tabSignalsLong.Location = new Point(4, 27);
        tabSignalsLong.Name = "tabSignalsLong";
        tabSignalsLong.Padding = new Padding(3);
        tabSignalsLong.Size = new Size(1126, 578);
        tabSignalsLong.TabIndex = 1;
        tabSignalsLong.Text = "Signals long";
        tabSignalsLong.UseVisualStyleBackColor = true;
        // 
        // UserControlSignalLong
        // 
        UserControlSignalLong.AutoScroll = true;
        UserControlSignalLong.AutoSize = true;
        UserControlSignalLong.Dock = DockStyle.Fill;
        UserControlSignalLong.Location = new Point(3, 3);
        UserControlSignalLong.Name = "UserControlSignalLong";
        UserControlSignalLong.Size = new Size(1120, 572);
        UserControlSignalLong.TabIndex = 0;
        // 
        // tabSignalsShort
        // 
        tabSignalsShort.Controls.Add(UserControlSignalShort);
        tabSignalsShort.Location = new Point(4, 27);
        tabSignalsShort.Name = "tabSignalsShort";
        tabSignalsShort.Padding = new Padding(3);
        tabSignalsShort.Size = new Size(1126, 578);
        tabSignalsShort.TabIndex = 2;
        tabSignalsShort.Text = "Signals short";
        tabSignalsShort.UseVisualStyleBackColor = true;
        // 
        // UserControlSignalShort
        // 
        UserControlSignalShort.AutoScroll = true;
        UserControlSignalShort.AutoSize = true;
        UserControlSignalShort.Dock = DockStyle.Fill;
        UserControlSignalShort.Location = new Point(3, 3);
        UserControlSignalShort.Name = "UserControlSignalShort";
        UserControlSignalShort.Size = new Size(1120, 572);
        UserControlSignalShort.TabIndex = 0;
        // 
        // tabSignalStobb
        // 
        tabSignalStobb.Controls.Add(flowLayoutPanel6);
        tabSignalStobb.Location = new Point(4, 27);
        tabSignalStobb.Margin = new Padding(4, 3, 4, 3);
        tabSignalStobb.Name = "tabSignalStobb";
        tabSignalStobb.Padding = new Padding(4, 3, 4, 3);
        tabSignalStobb.Size = new Size(1126, 578);
        tabSignalStobb.TabIndex = 3;
        tabSignalStobb.Text = "STOBB";
        tabSignalStobb.UseVisualStyleBackColor = true;
        // 
        // flowLayoutPanel6
        // 
        flowLayoutPanel6.AutoScroll = true;
        flowLayoutPanel6.AutoSize = true;
        flowLayoutPanel6.Controls.Add(UserControlSettingsSoundAndColorsStobb);
        flowLayoutPanel6.Controls.Add(groupBox2);
        flowLayoutPanel6.Dock = DockStyle.Fill;
        flowLayoutPanel6.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel6.Location = new Point(4, 3);
        flowLayoutPanel6.Name = "flowLayoutPanel6";
        flowLayoutPanel6.Size = new Size(1118, 572);
        flowLayoutPanel6.TabIndex = 158;
        // 
        // UserControlSettingsSoundAndColorsStobb
        // 
        UserControlSettingsSoundAndColorsStobb.AutoScroll = true;
        UserControlSettingsSoundAndColorsStobb.AutoSize = true;
        UserControlSettingsSoundAndColorsStobb.Location = new Point(0, 0);
        UserControlSettingsSoundAndColorsStobb.Margin = new Padding(0);
        UserControlSettingsSoundAndColorsStobb.Name = "UserControlSettingsSoundAndColorsStobb";
        UserControlSettingsSoundAndColorsStobb.Padding = new Padding(10);
        UserControlSettingsSoundAndColorsStobb.Size = new Size(807, 176);
        UserControlSettingsSoundAndColorsStobb.TabIndex = 157;
        // 
        // groupBox2
        // 
        groupBox2.AutoSize = true;
        groupBox2.Controls.Add(EditStobOnlyIfPreviousStobb);
        groupBox2.Controls.Add(label1);
        groupBox2.Controls.Add(EditStobbBBMinPercentage);
        groupBox2.Controls.Add(EditStobbBBMaxPercentage);
        groupBox2.Controls.Add(label85);
        groupBox2.Controls.Add(EditStobTrendShort);
        groupBox2.Controls.Add(label66);
        groupBox2.Controls.Add(EditStobTrendLong);
        groupBox2.Controls.Add(EditStobIncludeSbmPercAndCrossing);
        groupBox2.Controls.Add(EditStobIncludeSbmMaLines);
        groupBox2.Controls.Add(EditStobIncludeRsi);
        groupBox2.Controls.Add(EditStobbUseLowHigh);
        groupBox2.Location = new Point(10, 186);
        groupBox2.Margin = new Padding(10);
        groupBox2.Name = "groupBox2";
        groupBox2.Padding = new Padding(10);
        groupBox2.Size = new Size(428, 291);
        groupBox2.TabIndex = 158;
        groupBox2.TabStop = false;
        groupBox2.Text = "Settings";
        // 
        // EditStobOnlyIfPreviousStobb
        // 
        EditStobOnlyIfPreviousStobb.AutoSize = true;
        EditStobOnlyIfPreviousStobb.Location = new Point(21, 173);
        EditStobOnlyIfPreviousStobb.Margin = new Padding(4, 3, 4, 3);
        EditStobOnlyIfPreviousStobb.Name = "EditStobOnlyIfPreviousStobb";
        EditStobOnlyIfPreviousStobb.Size = new Size(184, 19);
        EditStobOnlyIfPreviousStobb.TabIndex = 167;
        EditStobOnlyIfPreviousStobb.Text = "Only if a previous signal exists";
        EditStobOnlyIfPreviousStobb.UseVisualStyleBackColor = true;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(21, 33);
        label1.Margin = new Padding(4, 0, 4, 0);
        label1.Name = "label1";
        label1.Size = new Size(77, 15);
        label1.TabIndex = 164;
        label1.Text = "Filter on BB%";
        // 
        // EditStobbBBMinPercentage
        // 
        EditStobbBBMinPercentage.DecimalPlaces = 2;
        EditStobbBBMinPercentage.Location = new Point(129, 29);
        EditStobbBBMinPercentage.Margin = new Padding(4, 3, 4, 3);
        EditStobbBBMinPercentage.Name = "EditStobbBBMinPercentage";
        EditStobbBBMinPercentage.Size = new Size(65, 23);
        EditStobbBBMinPercentage.TabIndex = 165;
        EditStobbBBMinPercentage.Value = new decimal(new int[] { 150, 0, 0, 131072 });
        // 
        // EditStobbBBMaxPercentage
        // 
        EditStobbBBMaxPercentage.DecimalPlaces = 2;
        EditStobbBBMaxPercentage.Location = new Point(213, 29);
        EditStobbBBMaxPercentage.Margin = new Padding(4, 3, 4, 3);
        EditStobbBBMaxPercentage.Name = "EditStobbBBMaxPercentage";
        EditStobbBBMaxPercentage.Size = new Size(65, 23);
        EditStobbBBMaxPercentage.TabIndex = 166;
        EditStobbBBMaxPercentage.Value = new decimal(new int[] { 6, 0, 0, 0 });
        // 
        // label85
        // 
        label85.AutoSize = true;
        label85.Location = new Point(21, 243);
        label85.Margin = new Padding(4, 0, 4, 0);
        label85.Name = "label85";
        label85.Size = new Size(112, 15);
        label85.TabIndex = 162;
        label85.Text = "Minimal trend short";
        // 
        // EditStobTrendShort
        // 
        EditStobTrendShort.DecimalPlaces = 2;
        EditStobTrendShort.Location = new Point(177, 239);
        EditStobTrendShort.Margin = new Padding(4, 3, 4, 3);
        EditStobTrendShort.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditStobTrendShort.Minimum = new decimal(new int[] { 1000, 0, 0, int.MinValue });
        EditStobTrendShort.Name = "EditStobTrendShort";
        EditStobTrendShort.Size = new Size(65, 23);
        EditStobTrendShort.TabIndex = 163;
        EditStobTrendShort.Value = new decimal(new int[] { 150, 0, 0, 131072 });
        // 
        // label66
        // 
        label66.AutoSize = true;
        label66.Location = new Point(21, 213);
        label66.Margin = new Padding(4, 0, 4, 0);
        label66.Name = "label66";
        label66.Size = new Size(109, 15);
        label66.TabIndex = 160;
        label66.Text = "Minimal trend long";
        // 
        // EditStobTrendLong
        // 
        EditStobTrendLong.DecimalPlaces = 2;
        EditStobTrendLong.Location = new Point(177, 209);
        EditStobTrendLong.Margin = new Padding(4, 3, 4, 3);
        EditStobTrendLong.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        EditStobTrendLong.Minimum = new decimal(new int[] { 1000, 0, 0, int.MinValue });
        EditStobTrendLong.Name = "EditStobTrendLong";
        EditStobTrendLong.Size = new Size(65, 23);
        EditStobTrendLong.TabIndex = 161;
        EditStobTrendLong.Value = new decimal(new int[] { 150, 0, 0, 131072 });
        // 
        // EditStobIncludeSbmPercAndCrossing
        // 
        EditStobIncludeSbmPercAndCrossing.AutoSize = true;
        EditStobIncludeSbmPercAndCrossing.Location = new Point(21, 148);
        EditStobIncludeSbmPercAndCrossing.Margin = new Padding(4, 3, 4, 3);
        EditStobIncludeSbmPercAndCrossing.Name = "EditStobIncludeSbmPercAndCrossing";
        EditStobIncludeSbmPercAndCrossing.Size = new Size(261, 19);
        EditStobIncludeSbmPercAndCrossing.TabIndex = 159;
        EditStobIncludeSbmPercAndCrossing.Text = "With SBM conditiions percentages/crossings";
        EditStobIncludeSbmPercAndCrossing.UseVisualStyleBackColor = true;
        // 
        // EditStobIncludeSbmMaLines
        // 
        EditStobIncludeSbmMaLines.AutoSize = true;
        EditStobIncludeSbmMaLines.Location = new Point(21, 123);
        EditStobIncludeSbmMaLines.Margin = new Padding(4, 3, 4, 3);
        EditStobIncludeSbmMaLines.Name = "EditStobIncludeSbmMaLines";
        EditStobIncludeSbmMaLines.Size = new Size(188, 19);
        EditStobIncludeSbmMaLines.TabIndex = 158;
        EditStobIncludeSbmMaLines.Text = "With SBM conditions MA-lines";
        EditStobIncludeSbmMaLines.UseVisualStyleBackColor = true;
        // 
        // EditStobIncludeRsi
        // 
        EditStobIncludeRsi.AutoSize = true;
        EditStobIncludeRsi.Location = new Point(21, 98);
        EditStobIncludeRsi.Margin = new Padding(4, 3, 4, 3);
        EditStobIncludeRsi.Name = "EditStobIncludeRsi";
        EditStobIncludeRsi.Size = new Size(244, 19);
        EditStobIncludeRsi.TabIndex = 157;
        EditStobIncludeRsi.Text = "With RSI oversold/overbought conditions";
        EditStobIncludeRsi.UseVisualStyleBackColor = true;
        // 
        // EditStobbUseLowHigh
        // 
        EditStobbUseLowHigh.AutoSize = true;
        EditStobbUseLowHigh.Location = new Point(21, 73);
        EditStobbUseLowHigh.Margin = new Padding(4, 3, 4, 3);
        EditStobbUseLowHigh.Name = "EditStobbUseLowHigh";
        EditStobbUseLowHigh.Size = new Size(393, 19);
        EditStobbUseLowHigh.TabIndex = 156;
        EditStobbUseLowHigh.Text = "Calculate BB oversold/overbought via low/high instead of open/close";
        EditStobbUseLowHigh.UseVisualStyleBackColor = true;
        // 
        // tabSignalSbm
        // 
        tabSignalSbm.Controls.Add(flowLayoutPanel7);
        tabSignalSbm.Location = new Point(4, 27);
        tabSignalSbm.Margin = new Padding(4, 3, 4, 3);
        tabSignalSbm.Name = "tabSignalSbm";
        tabSignalSbm.Padding = new Padding(4, 3, 4, 3);
        tabSignalSbm.Size = new Size(1126, 578);
        tabSignalSbm.TabIndex = 6;
        tabSignalSbm.Text = "SBM";
        tabSignalSbm.UseVisualStyleBackColor = true;
        // 
        // flowLayoutPanel7
        // 
        flowLayoutPanel7.AutoScroll = true;
        flowLayoutPanel7.AutoSize = true;
        flowLayoutPanel7.Controls.Add(UserControlSettingsSoundAndColorsSbm);
        flowLayoutPanel7.Controls.Add(flowLayoutPanel9);
        flowLayoutPanel7.Dock = DockStyle.Fill;
        flowLayoutPanel7.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel7.Location = new Point(4, 3);
        flowLayoutPanel7.Name = "flowLayoutPanel7";
        flowLayoutPanel7.Size = new Size(1118, 572);
        flowLayoutPanel7.TabIndex = 160;
        // 
        // UserControlSettingsSoundAndColorsSbm
        // 
        UserControlSettingsSoundAndColorsSbm.AutoScroll = true;
        UserControlSettingsSoundAndColorsSbm.AutoSize = true;
        UserControlSettingsSoundAndColorsSbm.Location = new Point(0, 0);
        UserControlSettingsSoundAndColorsSbm.Margin = new Padding(0);
        UserControlSettingsSoundAndColorsSbm.Name = "UserControlSettingsSoundAndColorsSbm";
        UserControlSettingsSoundAndColorsSbm.Padding = new Padding(10);
        UserControlSettingsSoundAndColorsSbm.Size = new Size(807, 176);
        UserControlSettingsSoundAndColorsSbm.TabIndex = 163;
        // 
        // flowLayoutPanel9
        // 
        flowLayoutPanel9.AutoScroll = true;
        flowLayoutPanel9.AutoSize = true;
        flowLayoutPanel9.Controls.Add(groupBox3);
        flowLayoutPanel9.Controls.Add(groupBox4);
        flowLayoutPanel9.Location = new Point(3, 179);
        flowLayoutPanel9.Name = "flowLayoutPanel9";
        flowLayoutPanel9.Size = new Size(876, 370);
        flowLayoutPanel9.TabIndex = 161;
        // 
        // groupBox3
        // 
        groupBox3.AutoSize = true;
        groupBox3.Controls.Add(EditSbmUseLowHigh);
        groupBox3.Controls.Add(EditSbm2UseLowHigh);
        groupBox3.Controls.Add(label21);
        groupBox3.Controls.Add(label20);
        groupBox3.Controls.Add(label9);
        groupBox3.Controls.Add(label41);
        groupBox3.Controls.Add(EditSbm1CandlesLookbackCount);
        groupBox3.Controls.Add(label12);
        groupBox3.Controls.Add(EditSbm2BbPercentage);
        groupBox3.Controls.Add(label13);
        groupBox3.Controls.Add(EditSbm3CandlesForBBRecovery);
        groupBox3.Controls.Add(label14);
        groupBox3.Controls.Add(EditSbm3CandlesForBBRecoveryPercentage);
        groupBox3.Controls.Add(label11);
        groupBox3.Controls.Add(EditSbm2CandlesLookbackCount);
        groupBox3.Location = new Point(10, 10);
        groupBox3.Margin = new Padding(10);
        groupBox3.Name = "groupBox3";
        groupBox3.Padding = new Padding(10);
        groupBox3.Size = new Size(327, 350);
        groupBox3.TabIndex = 160;
        groupBox3.TabStop = false;
        groupBox3.Text = "Settings";
        // 
        // EditSbmUseLowHigh
        // 
        EditSbmUseLowHigh.AutoSize = true;
        EditSbmUseLowHigh.Location = new Point(14, 91);
        EditSbmUseLowHigh.Margin = new Padding(4, 3, 4, 3);
        EditSbmUseLowHigh.Name = "EditSbmUseLowHigh";
        EditSbmUseLowHigh.Size = new Size(213, 19);
        EditSbmUseLowHigh.TabIndex = 168;
        EditSbmUseLowHigh.Text = "Use low/high instead of open/close";
        EditSbmUseLowHigh.UseVisualStyleBackColor = true;
        // 
        // EditSbm2UseLowHigh
        // 
        EditSbm2UseLowHigh.AutoSize = true;
        EditSbm2UseLowHigh.Location = new Point(18, 179);
        EditSbm2UseLowHigh.Margin = new Padding(4, 3, 4, 3);
        EditSbm2UseLowHigh.Name = "EditSbm2UseLowHigh";
        EditSbm2UseLowHigh.Size = new Size(213, 19);
        EditSbm2UseLowHigh.TabIndex = 167;
        EditSbm2UseLowHigh.Text = "Use low/high instead of open/close";
        EditSbm2UseLowHigh.UseVisualStyleBackColor = true;
        // 
        // label21
        // 
        label21.AutoSize = true;
        label21.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        label21.Location = new Point(15, 254);
        label21.Margin = new Padding(4, 0, 4, 0);
        label21.Name = "label21";
        label21.Size = new Size(40, 15);
        label21.TabIndex = 166;
        label21.Text = "SBM3";
        // 
        // label20
        // 
        label20.AutoSize = true;
        label20.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        label20.Location = new Point(14, 129);
        label20.Margin = new Padding(4, 0, 4, 0);
        label20.Name = "label20";
        label20.Size = new Size(40, 15);
        label20.TabIndex = 165;
        label20.Text = "SBM2";
        // 
        // label9
        // 
        label9.AutoSize = true;
        label9.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        label9.Location = new Point(14, 34);
        label9.Margin = new Padding(4, 0, 4, 0);
        label9.Name = "label9";
        label9.Size = new Size(40, 15);
        label9.TabIndex = 164;
        label9.Text = "SBM1";
        // 
        // label41
        // 
        label41.AutoSize = true;
        label41.Location = new Point(14, 64);
        label41.Margin = new Padding(4, 0, 4, 0);
        label41.Name = "label41";
        label41.Size = new Size(95, 15);
        label41.TabIndex = 162;
        label41.Text = "Candle lookback";
        // 
        // EditSbm1CandlesLookbackCount
        // 
        EditSbm1CandlesLookbackCount.Location = new Point(255, 62);
        EditSbm1CandlesLookbackCount.Margin = new Padding(4, 3, 4, 3);
        EditSbm1CandlesLookbackCount.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbm1CandlesLookbackCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbm1CandlesLookbackCount.Name = "EditSbm1CandlesLookbackCount";
        EditSbm1CandlesLookbackCount.Size = new Size(57, 23);
        EditSbm1CandlesLookbackCount.TabIndex = 163;
        EditSbm1CandlesLookbackCount.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // label12
        // 
        label12.AutoSize = true;
        label12.Location = new Point(14, 156);
        label12.Margin = new Padding(4, 0, 4, 0);
        label12.Name = "label12";
        label12.Size = new Size(208, 15);
        label12.TabIndex = 159;
        label12.Text = "Percentage in relation to the BB bands";
        // 
        // EditSbm2BbPercentage
        // 
        EditSbm2BbPercentage.DecimalPlaces = 2;
        EditSbm2BbPercentage.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        EditSbm2BbPercentage.Location = new Point(255, 154);
        EditSbm2BbPercentage.Margin = new Padding(4, 3, 4, 3);
        EditSbm2BbPercentage.Name = "EditSbm2BbPercentage";
        EditSbm2BbPercentage.Size = new Size(57, 23);
        EditSbm2BbPercentage.TabIndex = 160;
        EditSbm2BbPercentage.Value = new decimal(new int[] { 50, 0, 0, 131072 });
        // 
        // label13
        // 
        label13.AutoSize = true;
        label13.Location = new Point(15, 306);
        label13.Margin = new Padding(4, 0, 4, 0);
        label13.Name = "label13";
        label13.Size = new Size(95, 15);
        label13.TabIndex = 157;
        label13.Text = "Candle lookback";
        // 
        // EditSbm3CandlesForBBRecovery
        // 
        EditSbm3CandlesForBBRecovery.Location = new Point(256, 298);
        EditSbm3CandlesForBBRecovery.Margin = new Padding(4, 3, 4, 3);
        EditSbm3CandlesForBBRecovery.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbm3CandlesForBBRecovery.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbm3CandlesForBBRecovery.Name = "EditSbm3CandlesForBBRecovery";
        EditSbm3CandlesForBBRecovery.Size = new Size(57, 23);
        EditSbm3CandlesForBBRecovery.TabIndex = 158;
        EditSbm3CandlesForBBRecovery.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // label14
        // 
        label14.AutoSize = true;
        label14.Location = new Point(15, 282);
        label14.Margin = new Padding(4, 0, 4, 0);
        label14.Name = "label14";
        label14.Size = new Size(139, 15);
        label14.TabIndex = 155;
        label14.Text = "Percentage BB stretching";
        // 
        // EditSbm3CandlesForBBRecoveryPercentage
        // 
        EditSbm3CandlesForBBRecoveryPercentage.Location = new Point(256, 275);
        EditSbm3CandlesForBBRecoveryPercentage.Margin = new Padding(4, 3, 4, 3);
        EditSbm3CandlesForBBRecoveryPercentage.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
        EditSbm3CandlesForBBRecoveryPercentage.Name = "EditSbm3CandlesForBBRecoveryPercentage";
        EditSbm3CandlesForBBRecoveryPercentage.Size = new Size(57, 23);
        EditSbm3CandlesForBBRecoveryPercentage.TabIndex = 156;
        EditSbm3CandlesForBBRecoveryPercentage.Value = new decimal(new int[] { 225, 0, 0, 0 });
        // 
        // label11
        // 
        label11.AutoSize = true;
        label11.Location = new Point(14, 203);
        label11.Margin = new Padding(4, 0, 4, 0);
        label11.Name = "label11";
        label11.Size = new Size(95, 15);
        label11.TabIndex = 153;
        label11.Text = "Candle lookback";
        // 
        // EditSbm2CandlesLookbackCount
        // 
        EditSbm2CandlesLookbackCount.Location = new Point(255, 201);
        EditSbm2CandlesLookbackCount.Margin = new Padding(4, 3, 4, 3);
        EditSbm2CandlesLookbackCount.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbm2CandlesLookbackCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbm2CandlesLookbackCount.Name = "EditSbm2CandlesLookbackCount";
        EditSbm2CandlesLookbackCount.Size = new Size(57, 23);
        EditSbm2CandlesLookbackCount.TabIndex = 154;
        EditSbm2CandlesLookbackCount.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // groupBox4
        // 
        groupBox4.AutoSize = true;
        groupBox4.Controls.Add(label39);
        groupBox4.Controls.Add(EditSbmCandlesForMacdRecovery);
        groupBox4.Controls.Add(label17);
        groupBox4.Controls.Add(EditSbmBBMinPercentage);
        groupBox4.Controls.Add(EditSbmBBMaxPercentage);
        groupBox4.Controls.Add(label4);
        groupBox4.Controls.Add(EditSbmMa200AndMa20Percentage);
        groupBox4.Controls.Add(label8);
        groupBox4.Controls.Add(EditSbmMa50AndMa20Percentage);
        groupBox4.Controls.Add(label7);
        groupBox4.Controls.Add(EditSbmMa200AndMa50Percentage);
        groupBox4.Controls.Add(EditSbmMa50AndMa20Lookback);
        groupBox4.Controls.Add(EditSbmMa50AndMa20Crossing);
        groupBox4.Controls.Add(EditSbmMa200AndMa50Lookback);
        groupBox4.Controls.Add(EditSbmMa200AndMa50Crossing);
        groupBox4.Controls.Add(EditSbmMa200AndMa20Lookback);
        groupBox4.Controls.Add(EditSbmMa200AndMa20Crossing);
        groupBox4.Location = new Point(357, 10);
        groupBox4.Margin = new Padding(10);
        groupBox4.Name = "groupBox4";
        groupBox4.Padding = new Padding(10);
        groupBox4.Size = new Size(509, 318);
        groupBox4.TabIndex = 161;
        groupBox4.TabStop = false;
        groupBox4.Text = "Settings for SBM methods";
        // 
        // label39
        // 
        label39.AutoSize = true;
        label39.Location = new Point(18, 79);
        label39.Margin = new Padding(4, 0, 4, 0);
        label39.Name = "label39";
        label39.Size = new Size(133, 15);
        label39.TabIndex = 159;
        label39.Text = "MACD recovery candles";
        // 
        // EditSbmCandlesForMacdRecovery
        // 
        EditSbmCandlesForMacdRecovery.Location = new Point(438, 79);
        EditSbmCandlesForMacdRecovery.Margin = new Padding(4, 3, 4, 3);
        EditSbmCandlesForMacdRecovery.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        EditSbmCandlesForMacdRecovery.Name = "EditSbmCandlesForMacdRecovery";
        EditSbmCandlesForMacdRecovery.Size = new Size(57, 23);
        EditSbmCandlesForMacdRecovery.TabIndex = 160;
        EditSbmCandlesForMacdRecovery.Value = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // label17
        // 
        label17.AutoSize = true;
        label17.Location = new Point(18, 36);
        label17.Margin = new Padding(4, 0, 4, 0);
        label17.Name = "label17";
        label17.Size = new Size(77, 15);
        label17.TabIndex = 156;
        label17.Text = "Filter on BB%";
        // 
        // EditSbmBBMinPercentage
        // 
        EditSbmBBMinPercentage.DecimalPlaces = 2;
        EditSbmBBMinPercentage.Location = new Point(113, 34);
        EditSbmBBMinPercentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmBBMinPercentage.Name = "EditSbmBBMinPercentage";
        EditSbmBBMinPercentage.Size = new Size(65, 23);
        EditSbmBBMinPercentage.TabIndex = 157;
        EditSbmBBMinPercentage.Value = new decimal(new int[] { 150, 0, 0, 131072 });
        // 
        // EditSbmBBMaxPercentage
        // 
        EditSbmBBMaxPercentage.DecimalPlaces = 2;
        EditSbmBBMaxPercentage.Location = new Point(198, 34);
        EditSbmBBMaxPercentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmBBMaxPercentage.Name = "EditSbmBBMaxPercentage";
        EditSbmBBMaxPercentage.Size = new Size(65, 23);
        EditSbmBBMaxPercentage.TabIndex = 158;
        EditSbmBBMaxPercentage.Value = new decimal(new int[] { 6, 0, 0, 0 });
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new Point(16, 242);
        label4.Margin = new Padding(4, 0, 4, 0);
        label4.Name = "label4";
        label4.Size = new Size(254, 15);
        label4.TabIndex = 153;
        label4.Text = "Minimal percentage between ma200 and ma20";
        // 
        // EditSbmMa200AndMa20Percentage
        // 
        EditSbmMa200AndMa20Percentage.DecimalPlaces = 2;
        EditSbmMa200AndMa20Percentage.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        EditSbmMa200AndMa20Percentage.Location = new Point(438, 239);
        EditSbmMa200AndMa20Percentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa20Percentage.Name = "EditSbmMa200AndMa20Percentage";
        EditSbmMa200AndMa20Percentage.Size = new Size(57, 23);
        EditSbmMa200AndMa20Percentage.TabIndex = 154;
        EditSbmMa200AndMa20Percentage.Value = new decimal(new int[] { 3, 0, 0, 65536 });
        // 
        // label8
        // 
        label8.AutoSize = true;
        label8.Location = new Point(16, 267);
        label8.Margin = new Padding(4, 0, 4, 0);
        label8.Name = "label8";
        label8.Size = new Size(248, 15);
        label8.TabIndex = 151;
        label8.Text = "Minimal percentage between ma50 and ma20";
        // 
        // EditSbmMa50AndMa20Percentage
        // 
        EditSbmMa50AndMa20Percentage.DecimalPlaces = 2;
        EditSbmMa50AndMa20Percentage.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        EditSbmMa50AndMa20Percentage.Location = new Point(438, 266);
        EditSbmMa50AndMa20Percentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa50AndMa20Percentage.Name = "EditSbmMa50AndMa20Percentage";
        EditSbmMa50AndMa20Percentage.Size = new Size(57, 23);
        EditSbmMa50AndMa20Percentage.TabIndex = 152;
        EditSbmMa50AndMa20Percentage.Value = new decimal(new int[] { 3, 0, 0, 65536 });
        // 
        // label7
        // 
        label7.AutoSize = true;
        label7.Location = new Point(16, 213);
        label7.Margin = new Padding(4, 0, 4, 0);
        label7.Name = "label7";
        label7.Size = new Size(254, 15);
        label7.TabIndex = 149;
        label7.Text = "Minimal percentage between ma200 and ma50";
        // 
        // EditSbmMa200AndMa50Percentage
        // 
        EditSbmMa200AndMa50Percentage.DecimalPlaces = 2;
        EditSbmMa200AndMa50Percentage.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        EditSbmMa200AndMa50Percentage.Location = new Point(438, 213);
        EditSbmMa200AndMa50Percentage.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa50Percentage.Name = "EditSbmMa200AndMa50Percentage";
        EditSbmMa200AndMa50Percentage.Size = new Size(57, 23);
        EditSbmMa200AndMa50Percentage.TabIndex = 150;
        EditSbmMa200AndMa50Percentage.Value = new decimal(new int[] { 3, 0, 0, 65536 });
        // 
        // EditSbmMa50AndMa20Lookback
        // 
        EditSbmMa50AndMa20Lookback.Location = new Point(438, 171);
        EditSbmMa50AndMa20Lookback.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa50AndMa20Lookback.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbmMa50AndMa20Lookback.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbmMa50AndMa20Lookback.Name = "EditSbmMa50AndMa20Lookback";
        EditSbmMa50AndMa20Lookback.Size = new Size(57, 23);
        EditSbmMa50AndMa20Lookback.TabIndex = 148;
        EditSbmMa50AndMa20Lookback.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // EditSbmMa50AndMa20Crossing
        // 
        EditSbmMa50AndMa20Crossing.AutoSize = true;
        EditSbmMa50AndMa20Crossing.Location = new Point(18, 170);
        EditSbmMa50AndMa20Crossing.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa50AndMa20Crossing.Name = "EditSbmMa50AndMa20Crossing";
        EditSbmMa50AndMa20Crossing.Size = new Size(360, 19);
        EditSbmMa50AndMa20Crossing.TabIndex = 147;
        EditSbmMa50AndMa20Crossing.Text = "Check for a crossing of the ma50 and ma20 in the last x candles";
        EditSbmMa50AndMa20Crossing.UseVisualStyleBackColor = true;
        // 
        // EditSbmMa200AndMa50Lookback
        // 
        EditSbmMa200AndMa50Lookback.Location = new Point(438, 119);
        EditSbmMa200AndMa50Lookback.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa50Lookback.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbmMa200AndMa50Lookback.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbmMa200AndMa50Lookback.Name = "EditSbmMa200AndMa50Lookback";
        EditSbmMa200AndMa50Lookback.Size = new Size(57, 23);
        EditSbmMa200AndMa50Lookback.TabIndex = 146;
        EditSbmMa200AndMa50Lookback.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // EditSbmMa200AndMa50Crossing
        // 
        EditSbmMa200AndMa50Crossing.AutoSize = true;
        EditSbmMa200AndMa50Crossing.Location = new Point(18, 119);
        EditSbmMa200AndMa50Crossing.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa50Crossing.Name = "EditSbmMa200AndMa50Crossing";
        EditSbmMa200AndMa50Crossing.Size = new Size(366, 19);
        EditSbmMa200AndMa50Crossing.TabIndex = 145;
        EditSbmMa200AndMa50Crossing.Text = "Check for a crossing of the ma200 and ma50 in the last x candles";
        EditSbmMa200AndMa50Crossing.UseVisualStyleBackColor = true;
        // 
        // EditSbmMa200AndMa20Lookback
        // 
        EditSbmMa200AndMa20Lookback.Location = new Point(438, 145);
        EditSbmMa200AndMa20Lookback.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa20Lookback.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        EditSbmMa200AndMa20Lookback.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditSbmMa200AndMa20Lookback.Name = "EditSbmMa200AndMa20Lookback";
        EditSbmMa200AndMa20Lookback.Size = new Size(57, 23);
        EditSbmMa200AndMa20Lookback.TabIndex = 144;
        EditSbmMa200AndMa20Lookback.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // EditSbmMa200AndMa20Crossing
        // 
        EditSbmMa200AndMa20Crossing.AutoSize = true;
        EditSbmMa200AndMa20Crossing.Location = new Point(18, 145);
        EditSbmMa200AndMa20Crossing.Margin = new Padding(4, 3, 4, 3);
        EditSbmMa200AndMa20Crossing.Name = "EditSbmMa200AndMa20Crossing";
        EditSbmMa200AndMa20Crossing.Size = new Size(366, 19);
        EditSbmMa200AndMa20Crossing.TabIndex = 143;
        EditSbmMa200AndMa20Crossing.Text = "Check for a crossing of the ma200 and ma20 in the last x candles";
        EditSbmMa200AndMa20Crossing.UseVisualStyleBackColor = true;
        // 
        // tabSignalStoRsi
        // 
        tabSignalStoRsi.Controls.Add(flowLayoutPanel2);
        tabSignalStoRsi.Location = new Point(4, 27);
        tabSignalStoRsi.Name = "tabSignalStoRsi";
        tabSignalStoRsi.Padding = new Padding(3);
        tabSignalStoRsi.Size = new Size(1126, 578);
        tabSignalStoRsi.TabIndex = 11;
        tabSignalStoRsi.Text = "STORSI";
        tabSignalStoRsi.UseVisualStyleBackColor = true;
        // 
        // flowLayoutPanel2
        // 
        flowLayoutPanel2.AutoScroll = true;
        flowLayoutPanel2.AutoSize = true;
        flowLayoutPanel2.Controls.Add(UserControlSettingsSoundAndColorsStoRsi);
        flowLayoutPanel2.Controls.Add(groupBox6);
        flowLayoutPanel2.Dock = DockStyle.Fill;
        flowLayoutPanel2.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel2.Location = new Point(3, 3);
        flowLayoutPanel2.Name = "flowLayoutPanel2";
        flowLayoutPanel2.Size = new Size(1120, 572);
        flowLayoutPanel2.TabIndex = 160;
        // 
        // UserControlSettingsSoundAndColorsStoRsi
        // 
        UserControlSettingsSoundAndColorsStoRsi.AutoScroll = true;
        UserControlSettingsSoundAndColorsStoRsi.AutoSize = true;
        UserControlSettingsSoundAndColorsStoRsi.Location = new Point(0, 0);
        UserControlSettingsSoundAndColorsStoRsi.Margin = new Padding(0);
        UserControlSettingsSoundAndColorsStoRsi.Name = "UserControlSettingsSoundAndColorsStoRsi";
        UserControlSettingsSoundAndColorsStoRsi.Padding = new Padding(10);
        UserControlSettingsSoundAndColorsStoRsi.Size = new Size(807, 176);
        UserControlSettingsSoundAndColorsStoRsi.TabIndex = 158;
        // 
        // groupBox6
        // 
        groupBox6.AutoSize = true;
        groupBox6.Controls.Add(label28);
        groupBox6.Controls.Add(EditStorsiBBMinPercentage);
        groupBox6.Controls.Add(EditStorsiBBMaxPercentage);
        groupBox6.Controls.Add(EditSkipFirstSignal);
        groupBox6.Controls.Add(EditCheckBollingerBandsCondition);
        groupBox6.Controls.Add(label24);
        groupBox6.Controls.Add(EditStorsiAddStochAmount);
        groupBox6.Controls.Add(label26);
        groupBox6.Controls.Add(EditStorsiAddRsiAmount);
        groupBox6.Location = new Point(10, 186);
        groupBox6.Margin = new Padding(10);
        groupBox6.Name = "groupBox6";
        groupBox6.Padding = new Padding(10);
        groupBox6.Size = new Size(284, 198);
        groupBox6.TabIndex = 160;
        groupBox6.TabStop = false;
        groupBox6.Text = "Settings";
        // 
        // label28
        // 
        label28.AutoSize = true;
        label28.Location = new Point(13, 150);
        label28.Margin = new Padding(4, 0, 4, 0);
        label28.Name = "label28";
        label28.Size = new Size(77, 15);
        label28.TabIndex = 171;
        label28.Text = "Filter on BB%";
        // 
        // EditStorsiBBMinPercentage
        // 
        EditStorsiBBMinPercentage.DecimalPlaces = 2;
        EditStorsiBBMinPercentage.Location = new Point(121, 146);
        EditStorsiBBMinPercentage.Margin = new Padding(4, 3, 4, 3);
        EditStorsiBBMinPercentage.Name = "EditStorsiBBMinPercentage";
        EditStorsiBBMinPercentage.Size = new Size(65, 23);
        EditStorsiBBMinPercentage.TabIndex = 172;
        EditStorsiBBMinPercentage.Value = new decimal(new int[] { 150, 0, 0, 131072 });
        // 
        // EditStorsiBBMaxPercentage
        // 
        EditStorsiBBMaxPercentage.DecimalPlaces = 2;
        EditStorsiBBMaxPercentage.Location = new Point(205, 146);
        EditStorsiBBMaxPercentage.Margin = new Padding(4, 3, 4, 3);
        EditStorsiBBMaxPercentage.Name = "EditStorsiBBMaxPercentage";
        EditStorsiBBMaxPercentage.Size = new Size(65, 23);
        EditStorsiBBMaxPercentage.TabIndex = 173;
        EditStorsiBBMaxPercentage.Value = new decimal(new int[] { 6, 0, 0, 0 });
        // 
        // EditSkipFirstSignal
        // 
        EditSkipFirstSignal.AutoSize = true;
        EditSkipFirstSignal.Location = new Point(16, 121);
        EditSkipFirstSignal.Margin = new Padding(4, 3, 4, 3);
        EditSkipFirstSignal.Name = "EditSkipFirstSignal";
        EditSkipFirstSignal.Size = new Size(217, 19);
        EditSkipFirstSignal.TabIndex = 170;
        EditSkipFirstSignal.Text = "Only if there is a previous storsi sigal";
        EditSkipFirstSignal.UseVisualStyleBackColor = true;
        // 
        // EditCheckBollingerBandsCondition
        // 
        EditCheckBollingerBandsCondition.AutoSize = true;
        EditCheckBollingerBandsCondition.Location = new Point(16, 96);
        EditCheckBollingerBandsCondition.Margin = new Padding(4, 3, 4, 3);
        EditCheckBollingerBandsCondition.Name = "EditCheckBollingerBandsCondition";
        EditCheckBollingerBandsCondition.Size = new Size(223, 19);
        EditCheckBollingerBandsCondition.TabIndex = 169;
        EditCheckBollingerBandsCondition.Text = "Check if price is outside the BB bands";
        EditCheckBollingerBandsCondition.UseVisualStyleBackColor = true;
        // 
        // label24
        // 
        label24.AutoSize = true;
        label24.Location = new Point(16, 60);
        label24.Margin = new Padding(4, 0, 4, 0);
        label24.Name = "label24";
        label24.Size = new Size(103, 15);
        label24.TabIndex = 127;
        label24.Text = "Correction STOCH";
        // 
        // EditStorsiAddStochAmount
        // 
        EditStorsiAddStochAmount.Location = new Point(157, 58);
        EditStorsiAddStochAmount.Margin = new Padding(4, 3, 4, 3);
        EditStorsiAddStochAmount.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
        EditStorsiAddStochAmount.Name = "EditStorsiAddStochAmount";
        EditStorsiAddStochAmount.Size = new Size(56, 23);
        EditStorsiAddStochAmount.TabIndex = 128;
        // 
        // label26
        // 
        label26.AutoSize = true;
        label26.Location = new Point(16, 31);
        label26.Margin = new Padding(4, 0, 4, 0);
        label26.Name = "label26";
        label26.Size = new Size(82, 15);
        label26.TabIndex = 125;
        label26.Text = "Correction RSI";
        // 
        // EditStorsiAddRsiAmount
        // 
        EditStorsiAddRsiAmount.Location = new Point(157, 29);
        EditStorsiAddRsiAmount.Margin = new Padding(4, 3, 4, 3);
        EditStorsiAddRsiAmount.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
        EditStorsiAddRsiAmount.Name = "EditStorsiAddRsiAmount";
        EditStorsiAddRsiAmount.Size = new Size(56, 23);
        EditStorsiAddRsiAmount.TabIndex = 126;
        // 
        // tabSignalJump
        // 
        tabSignalJump.Controls.Add(flowLayoutPanel8);
        tabSignalJump.Location = new Point(4, 27);
        tabSignalJump.Margin = new Padding(4, 3, 4, 3);
        tabSignalJump.Name = "tabSignalJump";
        tabSignalJump.Padding = new Padding(4, 3, 4, 3);
        tabSignalJump.Size = new Size(1126, 578);
        tabSignalJump.TabIndex = 10;
        tabSignalJump.Text = "JUMP";
        tabSignalJump.UseVisualStyleBackColor = true;
        // 
        // flowLayoutPanel8
        // 
        flowLayoutPanel8.AutoScroll = true;
        flowLayoutPanel8.AutoSize = true;
        flowLayoutPanel8.Controls.Add(UserControlSettingsSoundAndColorsJump);
        flowLayoutPanel8.Controls.Add(groupBox5);
        flowLayoutPanel8.Dock = DockStyle.Fill;
        flowLayoutPanel8.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel8.Location = new Point(4, 3);
        flowLayoutPanel8.Name = "flowLayoutPanel8";
        flowLayoutPanel8.Size = new Size(1118, 572);
        flowLayoutPanel8.TabIndex = 159;
        // 
        // UserControlSettingsSoundAndColorsJump
        // 
        UserControlSettingsSoundAndColorsJump.AutoScroll = true;
        UserControlSettingsSoundAndColorsJump.AutoSize = true;
        UserControlSettingsSoundAndColorsJump.Location = new Point(0, 0);
        UserControlSettingsSoundAndColorsJump.Margin = new Padding(0);
        UserControlSettingsSoundAndColorsJump.Name = "UserControlSettingsSoundAndColorsJump";
        UserControlSettingsSoundAndColorsJump.Padding = new Padding(10);
        UserControlSettingsSoundAndColorsJump.Size = new Size(807, 176);
        UserControlSettingsSoundAndColorsJump.TabIndex = 158;
        // 
        // groupBox5
        // 
        groupBox5.AutoSize = true;
        groupBox5.Controls.Add(label5);
        groupBox5.Controls.Add(EditJumpCandlesLookbackCount);
        groupBox5.Controls.Add(EditJumpUseLowHighCalculation);
        groupBox5.Controls.Add(label3);
        groupBox5.Controls.Add(EditAnalysisCandleJumpPercentage);
        groupBox5.Location = new Point(10, 186);
        groupBox5.Margin = new Padding(10);
        groupBox5.Name = "groupBox5";
        groupBox5.Padding = new Padding(10);
        groupBox5.Size = new Size(257, 151);
        groupBox5.TabIndex = 159;
        groupBox5.TabStop = false;
        groupBox5.Text = "Settings";
        // 
        // label5
        // 
        label5.AutoSize = true;
        label5.Location = new Point(21, 66);
        label5.Margin = new Padding(4, 0, 4, 0);
        label5.Name = "label5";
        label5.Size = new Size(95, 15);
        label5.TabIndex = 128;
        label5.Text = "Candle lookback";
        // 
        // EditJumpCandlesLookbackCount
        // 
        EditJumpCandlesLookbackCount.Location = new Point(157, 64);
        EditJumpCandlesLookbackCount.Margin = new Padding(4, 3, 4, 3);
        EditJumpCandlesLookbackCount.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
        EditJumpCandlesLookbackCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        EditJumpCandlesLookbackCount.Name = "EditJumpCandlesLookbackCount";
        EditJumpCandlesLookbackCount.Size = new Size(57, 23);
        EditJumpCandlesLookbackCount.TabIndex = 129;
        EditJumpCandlesLookbackCount.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // EditJumpUseLowHighCalculation
        // 
        EditJumpUseLowHighCalculation.AutoSize = true;
        EditJumpUseLowHighCalculation.Location = new Point(21, 103);
        EditJumpUseLowHighCalculation.Margin = new Padding(4, 3, 4, 3);
        EditJumpUseLowHighCalculation.Name = "EditJumpUseLowHighCalculation";
        EditJumpUseLowHighCalculation.Size = new Size(222, 19);
        EditJumpUseLowHighCalculation.TabIndex = 127;
        EditJumpUseLowHighCalculation.Text = "Use High/Low instead of Open/Close";
        EditJumpUseLowHighCalculation.UseVisualStyleBackColor = true;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(21, 36);
        label3.Margin = new Padding(4, 0, 4, 0);
        label3.Name = "label3";
        label3.Size = new Size(98, 15);
        label3.TabIndex = 125;
        label3.Text = "Jump percentage";
        // 
        // EditAnalysisCandleJumpPercentage
        // 
        EditAnalysisCandleJumpPercentage.DecimalPlaces = 2;
        EditAnalysisCandleJumpPercentage.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditAnalysisCandleJumpPercentage.Location = new Point(157, 34);
        EditAnalysisCandleJumpPercentage.Margin = new Padding(4, 3, 4, 3);
        EditAnalysisCandleJumpPercentage.Name = "EditAnalysisCandleJumpPercentage";
        EditAnalysisCandleJumpPercentage.Size = new Size(56, 23);
        EditAnalysisCandleJumpPercentage.TabIndex = 126;
        // 
        // tabSignalZones
        // 
        tabSignalZones.Controls.Add(flowLayoutPanel4);
        tabSignalZones.Location = new Point(4, 27);
        tabSignalZones.Name = "tabSignalZones";
        tabSignalZones.Padding = new Padding(3);
        tabSignalZones.Size = new Size(1126, 578);
        tabSignalZones.TabIndex = 12;
        tabSignalZones.Text = "Zones";
        tabSignalZones.UseVisualStyleBackColor = true;
        // 
        // flowLayoutPanel4
        // 
        flowLayoutPanel4.AutoScroll = true;
        flowLayoutPanel4.AutoSize = true;
        flowLayoutPanel4.Controls.Add(UserControlSettingsSoundAndColorsZonesDominant);
        flowLayoutPanel4.Controls.Add(groupBox11);
        flowLayoutPanel4.Controls.Add(groupBox12);
        flowLayoutPanel4.Controls.Add(groupBox13);
        flowLayoutPanel4.Controls.Add(groupBox8);
        flowLayoutPanel4.Dock = DockStyle.Fill;
        flowLayoutPanel4.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel4.Location = new Point(3, 3);
        flowLayoutPanel4.Name = "flowLayoutPanel4";
        flowLayoutPanel4.Size = new Size(1120, 572);
        flowLayoutPanel4.TabIndex = 160;
        // 
        // UserControlSettingsSoundAndColorsZonesDominant
        // 
        UserControlSettingsSoundAndColorsZonesDominant.AutoScroll = true;
        UserControlSettingsSoundAndColorsZonesDominant.AutoSize = true;
        UserControlSettingsSoundAndColorsZonesDominant.Location = new Point(0, 0);
        UserControlSettingsSoundAndColorsZonesDominant.Margin = new Padding(0);
        UserControlSettingsSoundAndColorsZonesDominant.Name = "UserControlSettingsSoundAndColorsZonesDominant";
        UserControlSettingsSoundAndColorsZonesDominant.Padding = new Padding(10);
        UserControlSettingsSoundAndColorsZonesDominant.Size = new Size(807, 176);
        UserControlSettingsSoundAndColorsZonesDominant.TabIndex = 158;
        // 
        // groupBox11
        // 
        groupBox11.AutoSize = true;
        groupBox11.Controls.Add(EditShowZoneSignalsShort);
        groupBox11.Controls.Add(EditShowZoneSignalsLong);
        groupBox11.Controls.Add(label60);
        groupBox11.Controls.Add(label62);
        groupBox11.Controls.Add(EditZonesCandleCount);
        groupBox11.Controls.Add(label63);
        groupBox11.Controls.Add(EditZonesWarnPercentage);
        groupBox11.Location = new Point(10, 186);
        groupBox11.Margin = new Padding(10);
        groupBox11.Name = "groupBox11";
        groupBox11.Size = new Size(291, 156);
        groupBox11.TabIndex = 160;
        groupBox11.TabStop = false;
        groupBox11.Text = "Settings dominant zones";
        // 
        // EditShowZoneSignalsShort
        // 
        EditShowZoneSignalsShort.AutoSize = true;
        EditShowZoneSignalsShort.Location = new Point(7, 50);
        EditShowZoneSignalsShort.Margin = new Padding(4, 3, 4, 3);
        EditShowZoneSignalsShort.Name = "EditShowZoneSignalsShort";
        EditShowZoneSignalsShort.Size = new Size(124, 19);
        EditShowZoneSignalsShort.TabIndex = 141;
        EditShowZoneSignalsShort.Text = "Show signals short";
        EditShowZoneSignalsShort.UseVisualStyleBackColor = true;
        // 
        // EditShowZoneSignalsLong
        // 
        EditShowZoneSignalsLong.AutoSize = true;
        EditShowZoneSignalsLong.Location = new Point(7, 22);
        EditShowZoneSignalsLong.Margin = new Padding(4, 3, 4, 3);
        EditShowZoneSignalsLong.Name = "EditShowZoneSignalsLong";
        EditShowZoneSignalsLong.Size = new Size(121, 19);
        EditShowZoneSignalsLong.TabIndex = 140;
        EditShowZoneSignalsLong.Text = "Show signals long";
        EditShowZoneSignalsLong.UseVisualStyleBackColor = true;
        // 
        // label60
        // 
        label60.AutoSize = true;
        label60.Location = new Point(213, 82);
        label60.Margin = new Padding(4, 0, 4, 0);
        label60.Name = "label60";
        label60.Size = new Size(71, 15);
        label60.TabIndex = 130;
        label60.Text = "(1h candles)";
        // 
        // label62
        // 
        label62.AutoSize = true;
        label62.Location = new Point(6, 82);
        label62.Margin = new Padding(4, 0, 4, 0);
        label62.Name = "label62";
        label62.Size = new Size(77, 15);
        label62.TabIndex = 128;
        label62.Text = "Candles back";
        // 
        // EditZonesCandleCount
        // 
        EditZonesCandleCount.Location = new Point(135, 80);
        EditZonesCandleCount.Margin = new Padding(4, 3, 4, 3);
        EditZonesCandleCount.Maximum = new decimal(new int[] { 6000, 0, 0, 0 });
        EditZonesCandleCount.Name = "EditZonesCandleCount";
        EditZonesCandleCount.Size = new Size(56, 23);
        EditZonesCandleCount.TabIndex = 129;
        // 
        // label63
        // 
        label63.AutoSize = true;
        label63.Location = new Point(7, 113);
        label63.Margin = new Padding(4, 0, 4, 0);
        label63.Name = "label63";
        label63.Size = new Size(167, 15);
        label63.TabIndex = 125;
        label63.Text = "Approach warning percentage";
        // 
        // EditZonesWarnPercentage
        // 
        EditZonesWarnPercentage.DecimalPlaces = 2;
        EditZonesWarnPercentage.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditZonesWarnPercentage.Location = new Point(215, 111);
        EditZonesWarnPercentage.Margin = new Padding(4, 3, 4, 3);
        EditZonesWarnPercentage.Name = "EditZonesWarnPercentage";
        EditZonesWarnPercentage.Size = new Size(56, 23);
        EditZonesWarnPercentage.TabIndex = 126;
        // 
        // groupBox12
        // 
        groupBox12.AutoSize = true;
        groupBox12.Controls.Add(EditZonesApplyUnzoomed);
        groupBox12.Controls.Add(label47);
        groupBox12.Controls.Add(EditMaximumUnZoomedPercentage);
        groupBox12.Controls.Add(label54);
        groupBox12.Controls.Add(EditMinimumUnZoomedPercentage);
        groupBox12.Location = new Point(10, 362);
        groupBox12.Margin = new Padding(10);
        groupBox12.Name = "groupBox12";
        groupBox12.Size = new Size(276, 120);
        groupBox12.TabIndex = 161;
        groupBox12.TabStop = false;
        groupBox12.Text = "Settings unzoomed box";
        // 
        // EditZonesApplyUnzoomed
        // 
        EditZonesApplyUnzoomed.AutoSize = true;
        EditZonesApplyUnzoomed.Location = new Point(7, 20);
        EditZonesApplyUnzoomed.Margin = new Padding(4, 3, 4, 3);
        EditZonesApplyUnzoomed.Name = "EditZonesApplyUnzoomed";
        EditZonesApplyUnzoomed.Size = new Size(144, 19);
        EditZonesApplyUnzoomed.TabIndex = 144;
        EditZonesApplyUnzoomed.Text = "Apply unzoomed filter";
        EditZonesApplyUnzoomed.UseVisualStyleBackColor = true;
        // 
        // label47
        // 
        label47.AutoSize = true;
        label47.Location = new Point(7, 75);
        label47.Margin = new Padding(4, 0, 4, 0);
        label47.Name = "label47";
        label47.Size = new Size(184, 15);
        label47.TabIndex = 142;
        label47.Text = "Maximum unzoomed percentage";
        // 
        // EditMaximumUnZoomedPercentage
        // 
        EditMaximumUnZoomedPercentage.DecimalPlaces = 2;
        EditMaximumUnZoomedPercentage.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditMaximumUnZoomedPercentage.Location = new Point(213, 75);
        EditMaximumUnZoomedPercentage.Margin = new Padding(4, 3, 4, 3);
        EditMaximumUnZoomedPercentage.Name = "EditMaximumUnZoomedPercentage";
        EditMaximumUnZoomedPercentage.Size = new Size(56, 23);
        EditMaximumUnZoomedPercentage.TabIndex = 143;
        // 
        // label54
        // 
        label54.AutoSize = true;
        label54.Location = new Point(7, 45);
        label54.Margin = new Padding(4, 0, 4, 0);
        label54.Name = "label54";
        label54.Size = new Size(182, 15);
        label54.TabIndex = 140;
        label54.Text = "Minimum unzoomed percentage";
        // 
        // EditMinimumUnZoomedPercentage
        // 
        EditMinimumUnZoomedPercentage.DecimalPlaces = 2;
        EditMinimumUnZoomedPercentage.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditMinimumUnZoomedPercentage.Location = new Point(213, 43);
        EditMinimumUnZoomedPercentage.Margin = new Padding(4, 3, 4, 3);
        EditMinimumUnZoomedPercentage.Name = "EditMinimumUnZoomedPercentage";
        EditMinimumUnZoomedPercentage.Size = new Size(56, 23);
        EditMinimumUnZoomedPercentage.TabIndex = 141;
        // 
        // groupBox13
        // 
        groupBox13.AutoSize = true;
        groupBox13.Controls.Add(EditMinimumZoomedPercentage);
        groupBox13.Controls.Add(EditZoomLowerTimeFrames);
        groupBox13.Controls.Add(label56);
        groupBox13.Controls.Add(EditMaximumZoomedPercentage);
        groupBox13.Controls.Add(label57);
        groupBox13.Location = new Point(817, 10);
        groupBox13.Margin = new Padding(10);
        groupBox13.Name = "groupBox13";
        groupBox13.Size = new Size(277, 118);
        groupBox13.TabIndex = 162;
        groupBox13.TabStop = false;
        groupBox13.Text = "Settings zoomed box";
        // 
        // EditMinimumZoomedPercentage
        // 
        EditMinimumZoomedPercentage.DecimalPlaces = 2;
        EditMinimumZoomedPercentage.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditMinimumZoomedPercentage.Location = new Point(214, 44);
        EditMinimumZoomedPercentage.Margin = new Padding(4, 3, 4, 3);
        EditMinimumZoomedPercentage.Name = "EditMinimumZoomedPercentage";
        EditMinimumZoomedPercentage.Size = new Size(56, 23);
        EditMinimumZoomedPercentage.TabIndex = 149;
        // 
        // EditZoomLowerTimeFrames
        // 
        EditZoomLowerTimeFrames.AutoSize = true;
        EditZoomLowerTimeFrames.Location = new Point(7, 17);
        EditZoomLowerTimeFrames.Margin = new Padding(4, 3, 4, 3);
        EditZoomLowerTimeFrames.Name = "EditZoomLowerTimeFrames";
        EditZoomLowerTimeFrames.Size = new Size(167, 19);
        EditZoomLowerTimeFrames.TabIndex = 148;
        EditZoomLowerTimeFrames.Text = "Zoom in on lower intervals";
        EditZoomLowerTimeFrames.UseVisualStyleBackColor = true;
        // 
        // label56
        // 
        label56.AutoSize = true;
        label56.Location = new Point(7, 75);
        label56.Margin = new Padding(4, 0, 4, 0);
        label56.Name = "label56";
        label56.Size = new Size(170, 15);
        label56.TabIndex = 146;
        label56.Text = "Maximum zoomed percentage";
        // 
        // EditMaximumZoomedPercentage
        // 
        EditMaximumZoomedPercentage.DecimalPlaces = 2;
        EditMaximumZoomedPercentage.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditMaximumZoomedPercentage.Location = new Point(214, 73);
        EditMaximumZoomedPercentage.Margin = new Padding(4, 3, 4, 3);
        EditMaximumZoomedPercentage.Name = "EditMaximumZoomedPercentage";
        EditMaximumZoomedPercentage.Size = new Size(56, 23);
        EditMaximumZoomedPercentage.TabIndex = 147;
        // 
        // label57
        // 
        label57.AutoSize = true;
        label57.Location = new Point(7, 46);
        label57.Margin = new Padding(4, 0, 4, 0);
        label57.Name = "label57";
        label57.Size = new Size(168, 15);
        label57.TabIndex = 144;
        label57.Text = "Minimum zoomed percentage";
        // 
        // groupBox8
        // 
        groupBox8.AutoSize = true;
        groupBox8.Controls.Add(label40);
        groupBox8.Controls.Add(EditZoneStartPercentage);
        groupBox8.Controls.Add(EditZoneStartApply);
        groupBox8.Controls.Add(label30);
        groupBox8.Controls.Add(EditZoneStartCandleCount);
        groupBox8.Location = new Point(817, 148);
        groupBox8.Margin = new Padding(10);
        groupBox8.Name = "groupBox8";
        groupBox8.Size = new Size(204, 133);
        groupBox8.TabIndex = 159;
        groupBox8.TabStop = false;
        groupBox8.Text = "Filter";
        // 
        // label40
        // 
        label40.AutoSize = true;
        label40.Location = new Point(7, 90);
        label40.Margin = new Padding(4, 0, 4, 0);
        label40.Name = "label40";
        label40.Size = new Size(66, 15);
        label40.TabIndex = 136;
        label40.Text = "Percentage";
        // 
        // EditZoneStartPercentage
        // 
        EditZoneStartPercentage.DecimalPlaces = 2;
        EditZoneStartPercentage.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditZoneStartPercentage.Location = new Point(141, 88);
        EditZoneStartPercentage.Margin = new Padding(4, 3, 4, 3);
        EditZoneStartPercentage.Name = "EditZoneStartPercentage";
        EditZoneStartPercentage.Size = new Size(56, 23);
        EditZoneStartPercentage.TabIndex = 137;
        // 
        // EditZoneStartApply
        // 
        EditZoneStartApply.AutoSize = true;
        EditZoneStartApply.Location = new Point(7, 22);
        EditZoneStartApply.Margin = new Padding(4, 3, 4, 3);
        EditZoneStartApply.Name = "EditZoneStartApply";
        EditZoneStartApply.Size = new Size(137, 19);
        EditZoneStartApply.TabIndex = 138;
        EditZoneStartApply.Text = "Filter on start of zone";
        EditZoneStartApply.UseVisualStyleBackColor = true;
        // 
        // label30
        // 
        label30.AutoSize = true;
        label30.Location = new Point(6, 57);
        label30.Margin = new Padding(4, 0, 4, 0);
        label30.Name = "label30";
        label30.Size = new Size(83, 15);
        label30.TabIndex = 128;
        label30.Text = "Candles count";
        // 
        // EditZoneStartCandleCount
        // 
        EditZoneStartCandleCount.Location = new Point(141, 55);
        EditZoneStartCandleCount.Margin = new Padding(4, 3, 4, 3);
        EditZoneStartCandleCount.Maximum = new decimal(new int[] { 6000, 0, 0, 0 });
        EditZoneStartCandleCount.Name = "EditZoneStartCandleCount";
        EditZoneStartCandleCount.Size = new Size(56, 23);
        EditZoneStartCandleCount.TabIndex = 129;
        // 
        // tabSignalZonesFvg
        // 
        tabSignalZonesFvg.Controls.Add(flowLayoutPanel10);
        tabSignalZonesFvg.Location = new Point(4, 27);
        tabSignalZonesFvg.Name = "tabSignalZonesFvg";
        tabSignalZonesFvg.Padding = new Padding(3);
        tabSignalZonesFvg.Size = new Size(1126, 578);
        tabSignalZonesFvg.TabIndex = 13;
        tabSignalZonesFvg.Text = "FVG";
        tabSignalZonesFvg.UseVisualStyleBackColor = true;
        // 
        // flowLayoutPanel10
        // 
        flowLayoutPanel10.AutoScroll = true;
        flowLayoutPanel10.AutoSize = true;
        flowLayoutPanel10.Controls.Add(UserControlSettingsSoundAndColorsZonesFvg);
        flowLayoutPanel10.Controls.Add(groupBox14);
        flowLayoutPanel10.Dock = DockStyle.Fill;
        flowLayoutPanel10.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel10.Location = new Point(3, 3);
        flowLayoutPanel10.Name = "flowLayoutPanel10";
        flowLayoutPanel10.Size = new Size(1120, 572);
        flowLayoutPanel10.TabIndex = 161;
        // 
        // UserControlSettingsSoundAndColorsZonesFvg
        // 
        UserControlSettingsSoundAndColorsZonesFvg.AutoScroll = true;
        UserControlSettingsSoundAndColorsZonesFvg.AutoSize = true;
        UserControlSettingsSoundAndColorsZonesFvg.Location = new Point(0, 0);
        UserControlSettingsSoundAndColorsZonesFvg.Margin = new Padding(0);
        UserControlSettingsSoundAndColorsZonesFvg.Name = "UserControlSettingsSoundAndColorsZonesFvg";
        UserControlSettingsSoundAndColorsZonesFvg.Padding = new Padding(10);
        UserControlSettingsSoundAndColorsZonesFvg.Size = new Size(807, 176);
        UserControlSettingsSoundAndColorsZonesFvg.TabIndex = 158;
        // 
        // groupBox14
        // 
        groupBox14.AutoSize = true;
        groupBox14.Controls.Add(EditShowFvgSignalsShort);
        groupBox14.Controls.Add(EditShowFvgSignalsLong);
        groupBox14.Controls.Add(label34);
        groupBox14.Controls.Add(EditZonesFvgMinimumPercentage);
        groupBox14.Location = new Point(10, 186);
        groupBox14.Margin = new Padding(10);
        groupBox14.Name = "groupBox14";
        groupBox14.Size = new Size(278, 123);
        groupBox14.TabIndex = 160;
        groupBox14.TabStop = false;
        groupBox14.Text = "Settings FVG";
        // 
        // EditShowFvgSignalsShort
        // 
        EditShowFvgSignalsShort.AutoSize = true;
        EditShowFvgSignalsShort.Location = new Point(7, 47);
        EditShowFvgSignalsShort.Margin = new Padding(4, 3, 4, 3);
        EditShowFvgSignalsShort.Name = "EditShowFvgSignalsShort";
        EditShowFvgSignalsShort.Size = new Size(124, 19);
        EditShowFvgSignalsShort.TabIndex = 141;
        EditShowFvgSignalsShort.Text = "Show short signals";
        EditShowFvgSignalsShort.UseVisualStyleBackColor = true;
        // 
        // EditShowFvgSignalsLong
        // 
        EditShowFvgSignalsLong.AutoSize = true;
        EditShowFvgSignalsLong.Location = new Point(7, 22);
        EditShowFvgSignalsLong.Margin = new Padding(4, 3, 4, 3);
        EditShowFvgSignalsLong.Name = "EditShowFvgSignalsLong";
        EditShowFvgSignalsLong.Size = new Size(121, 19);
        EditShowFvgSignalsLong.TabIndex = 140;
        EditShowFvgSignalsLong.Text = "Show long signals";
        EditShowFvgSignalsLong.UseVisualStyleBackColor = true;
        // 
        // label34
        // 
        label34.AutoSize = true;
        label34.Location = new Point(7, 80);
        label34.Margin = new Padding(4, 0, 4, 0);
        label34.Name = "label34";
        label34.Size = new Size(122, 15);
        label34.TabIndex = 125;
        label34.Text = "Minimum percentage";
        // 
        // EditZonesFvgMinimumPercentage
        // 
        EditZonesFvgMinimumPercentage.DecimalPlaces = 2;
        EditZonesFvgMinimumPercentage.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditZonesFvgMinimumPercentage.Location = new Point(215, 78);
        EditZonesFvgMinimumPercentage.Margin = new Padding(4, 3, 4, 3);
        EditZonesFvgMinimumPercentage.Name = "EditZonesFvgMinimumPercentage";
        EditZonesFvgMinimumPercentage.Size = new Size(56, 23);
        EditZonesFvgMinimumPercentage.TabIndex = 126;
        // 
        // tabTrading
        // 
        tabTrading.Controls.Add(tabControlTrading);
        tabTrading.Controls.Add(label59);
        tabTrading.Location = new Point(4, 27);
        tabTrading.Margin = new Padding(4, 3, 4, 3);
        tabTrading.Name = "tabTrading";
        tabTrading.Padding = new Padding(4, 3, 4, 3);
        tabTrading.Size = new Size(1142, 615);
        tabTrading.TabIndex = 11;
        tabTrading.Text = "Trading";
        tabTrading.UseVisualStyleBackColor = true;
        // 
        // tabControlTrading
        // 
        tabControlTrading.Appearance = TabAppearance.FlatButtons;
        tabControlTrading.Controls.Add(tabTradingGeneral);
        tabControlTrading.Controls.Add(tabTradingLong);
        tabControlTrading.Controls.Add(tabTradingShort);
        tabControlTrading.Controls.Add(tabPageTradingRules);
        tabControlTrading.Dock = DockStyle.Fill;
        tabControlTrading.Location = new Point(4, 3);
        tabControlTrading.Name = "tabControlTrading";
        tabControlTrading.SelectedIndex = 0;
        tabControlTrading.Size = new Size(1134, 609);
        tabControlTrading.TabIndex = 283;
        // 
        // tabTradingGeneral
        // 
        tabTradingGeneral.Controls.Add(flowLayoutPanel1);
        tabTradingGeneral.Controls.Add(panel7);
        tabTradingGeneral.Location = new Point(4, 27);
        tabTradingGeneral.Name = "tabTradingGeneral";
        tabTradingGeneral.Padding = new Padding(3);
        tabTradingGeneral.Size = new Size(1126, 578);
        tabTradingGeneral.TabIndex = 0;
        tabTradingGeneral.Text = "Trading common";
        tabTradingGeneral.UseVisualStyleBackColor = true;
        // 
        // flowLayoutPanel1
        // 
        flowLayoutPanel1.AutoScroll = true;
        flowLayoutPanel1.Controls.Add(UserControlTradeEntry);
        flowLayoutPanel1.Controls.Add(UserControlTradeTakeProfit);
        flowLayoutPanel1.Controls.Add(UserControlTradeStopLoss);
        flowLayoutPanel1.Controls.Add(UserControlTradeDca);
        flowLayoutPanel1.Dock = DockStyle.Fill;
        flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel1.Location = new Point(275, 3);
        flowLayoutPanel1.Margin = new Padding(0);
        flowLayoutPanel1.MinimumSize = new Size(450, 0);
        flowLayoutPanel1.Name = "flowLayoutPanel1";
        flowLayoutPanel1.Size = new Size(848, 572);
        flowLayoutPanel1.TabIndex = 336;
        // 
        // UserControlTradeEntry
        // 
        UserControlTradeEntry.AutoSize = true;
        UserControlTradeEntry.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        UserControlTradeEntry.Location = new Point(3, 3);
        UserControlTradeEntry.Name = "UserControlTradeEntry";
        UserControlTradeEntry.Padding = new Padding(5);
        UserControlTradeEntry.Size = new Size(397, 166);
        UserControlTradeEntry.TabIndex = 335;
        // 
        // UserControlTradeTakeProfit
        // 
        UserControlTradeTakeProfit.AutoSize = true;
        UserControlTradeTakeProfit.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        UserControlTradeTakeProfit.Location = new Point(3, 175);
        UserControlTradeTakeProfit.Name = "UserControlTradeTakeProfit";
        UserControlTradeTakeProfit.Padding = new Padding(5);
        UserControlTradeTakeProfit.Size = new Size(396, 173);
        UserControlTradeTakeProfit.TabIndex = 336;
        // 
        // UserControlTradeStopLoss
        // 
        UserControlTradeStopLoss.AutoSize = true;
        UserControlTradeStopLoss.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        UserControlTradeStopLoss.Location = new Point(3, 354);
        UserControlTradeStopLoss.Name = "UserControlTradeStopLoss";
        UserControlTradeStopLoss.Padding = new Padding(5);
        UserControlTradeStopLoss.Size = new Size(284, 112);
        UserControlTradeStopLoss.TabIndex = 337;
        // 
        // UserControlTradeDca
        // 
        UserControlTradeDca.AutoScroll = true;
        UserControlTradeDca.AutoSize = true;
        UserControlTradeDca.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        UserControlTradeDca.Location = new Point(403, 0);
        UserControlTradeDca.Margin = new Padding(0);
        UserControlTradeDca.Name = "UserControlTradeDca";
        UserControlTradeDca.Padding = new Padding(5);
        UserControlTradeDca.Size = new Size(426, 178);
        UserControlTradeDca.TabIndex = 334;
        // 
        // panel7
        // 
        panel7.Controls.Add(label83);
        panel7.Controls.Add(EditTradeVia);
        panel7.Controls.Add(label73);
        panel7.Controls.Add(EditGlobalBuyCooldownTime);
        panel7.Controls.Add(groupBoxInstap);
        panel7.Controls.Add(groupBoxFutures);
        panel7.Controls.Add(EditLogCanceledOrders);
        panel7.Controls.Add(EditSoundTradeNotification);
        panel7.Controls.Add(EditDisableNewPositions);
        panel7.Dock = DockStyle.Left;
        panel7.Location = new Point(3, 3);
        panel7.Name = "panel7";
        panel7.Size = new Size(272, 572);
        panel7.TabIndex = 335;
        // 
        // label83
        // 
        label83.AutoSize = true;
        label83.Location = new Point(12, 10);
        label83.Margin = new Padding(4, 0, 4, 0);
        label83.Name = "label83";
        label83.Size = new Size(46, 15);
        label83.TabIndex = 350;
        label83.Text = "Trading";
        // 
        // EditTradeVia
        // 
        EditTradeVia.DropDownStyle = ComboBoxStyle.DropDownList;
        EditTradeVia.FormattingEnabled = true;
        EditTradeVia.Location = new Point(71, 7);
        EditTradeVia.Margin = new Padding(4, 3, 4, 3);
        EditTradeVia.Name = "EditTradeVia";
        EditTradeVia.Size = new Size(160, 23);
        EditTradeVia.TabIndex = 349;
        // 
        // label73
        // 
        label73.AutoSize = true;
        label73.Location = new Point(10, 468);
        label73.Margin = new Padding(4, 0, 4, 0);
        label73.Name = "label73";
        label73.Size = new Size(114, 15);
        label73.TabIndex = 348;
        label73.Text = "Cool down time (m)";
        // 
        // EditGlobalBuyCooldownTime
        // 
        EditGlobalBuyCooldownTime.Location = new Point(141, 466);
        EditGlobalBuyCooldownTime.Margin = new Padding(4, 3, 4, 3);
        EditGlobalBuyCooldownTime.Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 });
        EditGlobalBuyCooldownTime.Name = "EditGlobalBuyCooldownTime";
        EditGlobalBuyCooldownTime.Size = new Size(88, 23);
        EditGlobalBuyCooldownTime.TabIndex = 347;
        // 
        // groupBoxInstap
        // 
        groupBoxInstap.Controls.Add(EditCheckFurtherPriceMove);
        groupBoxInstap.Controls.Add(EditCheckIncreasingMacd);
        groupBoxInstap.Controls.Add(EditCheckIncreasingStoch);
        groupBoxInstap.Controls.Add(EditCheckIncreasingRsi);
        groupBoxInstap.Controls.Add(groupBoxSlots);
        groupBoxInstap.Location = new Point(12, 126);
        groupBoxInstap.Name = "groupBoxInstap";
        groupBoxInstap.Size = new Size(234, 230);
        groupBoxInstap.TabIndex = 346;
        groupBoxInstap.TabStop = false;
        groupBoxInstap.Text = "Entry condities";
        // 
        // EditCheckFurtherPriceMove
        // 
        EditCheckFurtherPriceMove.AutoSize = true;
        EditCheckFurtherPriceMove.Location = new Point(21, 91);
        EditCheckFurtherPriceMove.Margin = new Padding(4, 3, 4, 3);
        EditCheckFurtherPriceMove.Name = "EditCheckFurtherPriceMove";
        EditCheckFurtherPriceMove.Size = new Size(185, 19);
        EditCheckFurtherPriceMove.TabIndex = 278;
        EditCheckFurtherPriceMove.Text = "Check price decrease/increase";
        EditCheckFurtherPriceMove.UseVisualStyleBackColor = true;
        // 
        // EditCheckIncreasingMacd
        // 
        EditCheckIncreasingMacd.AutoSize = true;
        EditCheckIncreasingMacd.Location = new Point(20, 47);
        EditCheckIncreasingMacd.Margin = new Padding(4, 3, 4, 3);
        EditCheckIncreasingMacd.Name = "EditCheckIncreasingMacd";
        EditCheckIncreasingMacd.Size = new Size(226, 19);
        EditCheckIncreasingMacd.TabIndex = 277;
        EditCheckIncreasingMacd.Text = "MACD must be increasing/decreasing";
        EditCheckIncreasingMacd.UseVisualStyleBackColor = true;
        // 
        // EditCheckIncreasingStoch
        // 
        EditCheckIncreasingStoch.AutoSize = true;
        EditCheckIncreasingStoch.Location = new Point(20, 68);
        EditCheckIncreasingStoch.Margin = new Padding(4, 3, 4, 3);
        EditCheckIncreasingStoch.Name = "EditCheckIncreasingStoch";
        EditCheckIncreasingStoch.Size = new Size(192, 19);
        EditCheckIncreasingStoch.TabIndex = 276;
        EditCheckIncreasingStoch.Text = "Stoch not overbought/oversold";
        EditCheckIncreasingStoch.UseVisualStyleBackColor = true;
        // 
        // EditCheckIncreasingRsi
        // 
        EditCheckIncreasingRsi.AutoSize = true;
        EditCheckIncreasingRsi.Location = new Point(20, 26);
        EditCheckIncreasingRsi.Margin = new Padding(4, 3, 4, 3);
        EditCheckIncreasingRsi.Name = "EditCheckIncreasingRsi";
        EditCheckIncreasingRsi.Size = new Size(207, 19);
        EditCheckIncreasingRsi.TabIndex = 275;
        EditCheckIncreasingRsi.Text = "RSI must be increasing/decreasing";
        EditCheckIncreasingRsi.UseVisualStyleBackColor = true;
        // 
        // groupBoxSlots
        // 
        groupBoxSlots.Controls.Add(label50);
        groupBoxSlots.Controls.Add(EditSlotsMaximalLong);
        groupBoxSlots.Controls.Add(label52);
        groupBoxSlots.Controls.Add(EditSlotsMaximalShort);
        groupBoxSlots.Font = new Font("Segoe UI", 9F);
        groupBoxSlots.Location = new Point(0, 140);
        groupBoxSlots.Name = "groupBoxSlots";
        groupBoxSlots.Size = new Size(234, 82);
        groupBoxSlots.TabIndex = 334;
        groupBoxSlots.TabStop = false;
        groupBoxSlots.Text = "Slot limits";
        // 
        // label50
        // 
        label50.AutoSize = true;
        label50.Location = new Point(5, 26);
        label50.Margin = new Padding(4, 0, 4, 0);
        label50.Name = "label50";
        label50.Size = new Size(34, 15);
        label50.TabIndex = 194;
        label50.Text = "Long";
        // 
        // EditSlotsMaximalLong
        // 
        EditSlotsMaximalLong.Location = new Point(129, 24);
        EditSlotsMaximalLong.Margin = new Padding(4, 3, 4, 3);
        EditSlotsMaximalLong.Name = "EditSlotsMaximalLong";
        EditSlotsMaximalLong.Size = new Size(88, 23);
        EditSlotsMaximalLong.TabIndex = 195;
        EditSlotsMaximalLong.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // label52
        // 
        label52.AutoSize = true;
        label52.Location = new Point(5, 53);
        label52.Margin = new Padding(4, 0, 4, 0);
        label52.Name = "label52";
        label52.Size = new Size(35, 15);
        label52.TabIndex = 196;
        label52.Text = "Short";
        // 
        // EditSlotsMaximalShort
        // 
        EditSlotsMaximalShort.Location = new Point(129, 50);
        EditSlotsMaximalShort.Margin = new Padding(4, 3, 4, 3);
        EditSlotsMaximalShort.Name = "EditSlotsMaximalShort";
        EditSlotsMaximalShort.Size = new Size(88, 23);
        EditSlotsMaximalShort.TabIndex = 197;
        EditSlotsMaximalShort.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // groupBoxFutures
        // 
        groupBoxFutures.Controls.Add(label19);
        groupBoxFutures.Controls.Add(EditCrossOrIsolated);
        groupBoxFutures.Controls.Add(label23);
        groupBoxFutures.Controls.Add(EditLeverage);
        groupBoxFutures.Location = new Point(12, 362);
        groupBoxFutures.Name = "groupBoxFutures";
        groupBoxFutures.Size = new Size(234, 86);
        groupBoxFutures.TabIndex = 345;
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
        // EditLogCanceledOrders
        // 
        EditLogCanceledOrders.AutoSize = true;
        EditLogCanceledOrders.Location = new Point(12, 90);
        EditLogCanceledOrders.Margin = new Padding(4, 3, 4, 3);
        EditLogCanceledOrders.Name = "EditLogCanceledOrders";
        EditLogCanceledOrders.Size = new Size(135, 19);
        EditLogCanceledOrders.TabIndex = 340;
        EditLogCanceledOrders.Text = "Log cancelled orders";
        EditLogCanceledOrders.UseVisualStyleBackColor = true;
        // 
        // EditSoundTradeNotification
        // 
        EditSoundTradeNotification.AutoSize = true;
        EditSoundTradeNotification.Location = new Point(12, 65);
        EditSoundTradeNotification.Margin = new Padding(4, 3, 4, 3);
        EditSoundTradeNotification.Name = "EditSoundTradeNotification";
        EditSoundTradeNotification.Size = new Size(141, 19);
        EditSoundTradeNotification.TabIndex = 339;
        EditSoundTradeNotification.Text = "Play sound for a trade";
        EditSoundTradeNotification.UseVisualStyleBackColor = true;
        // 
        // EditDisableNewPositions
        // 
        EditDisableNewPositions.AutoSize = true;
        EditDisableNewPositions.Location = new Point(12, 40);
        EditDisableNewPositions.Margin = new Padding(4, 3, 4, 3);
        EditDisableNewPositions.Name = "EditDisableNewPositions";
        EditDisableNewPositions.Size = new Size(158, 19);
        EditDisableNewPositions.TabIndex = 338;
        EditDisableNewPositions.Text = "Dont open new positions";
        EditDisableNewPositions.UseVisualStyleBackColor = true;
        // 
        // tabTradingLong
        // 
        tabTradingLong.Controls.Add(UserControlTradingLong);
        tabTradingLong.Location = new Point(4, 27);
        tabTradingLong.Name = "tabTradingLong";
        tabTradingLong.Padding = new Padding(3);
        tabTradingLong.Size = new Size(1126, 578);
        tabTradingLong.TabIndex = 1;
        tabTradingLong.Text = "Trading long";
        tabTradingLong.UseVisualStyleBackColor = true;
        // 
        // UserControlTradingLong
        // 
        UserControlTradingLong.AutoScroll = true;
        UserControlTradingLong.AutoSize = true;
        UserControlTradingLong.Dock = DockStyle.Fill;
        UserControlTradingLong.Location = new Point(3, 3);
        UserControlTradingLong.Name = "UserControlTradingLong";
        UserControlTradingLong.Size = new Size(1120, 572);
        UserControlTradingLong.TabIndex = 0;
        // 
        // tabTradingShort
        // 
        tabTradingShort.Controls.Add(UserControlTradingShort);
        tabTradingShort.Location = new Point(4, 27);
        tabTradingShort.Name = "tabTradingShort";
        tabTradingShort.Padding = new Padding(3);
        tabTradingShort.Size = new Size(1126, 578);
        tabTradingShort.TabIndex = 2;
        tabTradingShort.Text = "Trading short";
        tabTradingShort.UseVisualStyleBackColor = true;
        // 
        // UserControlTradingShort
        // 
        UserControlTradingShort.AutoScroll = true;
        UserControlTradingShort.AutoSize = true;
        UserControlTradingShort.Dock = DockStyle.Fill;
        UserControlTradingShort.Location = new Point(3, 3);
        UserControlTradingShort.Name = "UserControlTradingShort";
        UserControlTradingShort.Size = new Size(1120, 572);
        UserControlTradingShort.TabIndex = 0;
        // 
        // tabPageTradingRules
        // 
        tabPageTradingRules.Controls.Add(UserControlTradeRules);
        tabPageTradingRules.Location = new Point(4, 27);
        tabPageTradingRules.Name = "tabPageTradingRules";
        tabPageTradingRules.Padding = new Padding(3);
        tabPageTradingRules.Size = new Size(1126, 578);
        tabPageTradingRules.TabIndex = 3;
        tabPageTradingRules.Text = "Rulez";
        tabPageTradingRules.UseVisualStyleBackColor = true;
        // 
        // UserControlTradeRules
        // 
        UserControlTradeRules.AutoScroll = true;
        UserControlTradeRules.AutoSize = true;
        UserControlTradeRules.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        UserControlTradeRules.Dock = DockStyle.Fill;
        UserControlTradeRules.Location = new Point(3, 3);
        UserControlTradeRules.Margin = new Padding(0);
        UserControlTradeRules.Name = "UserControlTradeRules";
        UserControlTradeRules.Size = new Size(1120, 572);
        UserControlTradeRules.TabIndex = 0;
        // 
        // label59
        // 
        label59.AutoSize = true;
        label59.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        label59.Location = new Point(-170, 282);
        label59.Margin = new Padding(4, 0, 4, 0);
        label59.Name = "label59";
        label59.Size = new Size(96, 15);
        label59.TabIndex = 271;
        label59.Text = "Entry conditions";
        // 
        // tabApi
        // 
        tabApi.Controls.Add(flowLayoutPanel3);
        tabApi.Location = new Point(4, 27);
        tabApi.Name = "tabApi";
        tabApi.Padding = new Padding(3);
        tabApi.Size = new Size(1142, 615);
        tabApi.TabIndex = 14;
        tabApi.Text = "API keys";
        tabApi.UseVisualStyleBackColor = true;
        // 
        // flowLayoutPanel3
        // 
        flowLayoutPanel3.AutoSize = true;
        flowLayoutPanel3.Controls.Add(UserControlExchangeApi);
        flowLayoutPanel3.Controls.Add(UserControlAltradyApi);
        flowLayoutPanel3.Dock = DockStyle.Fill;
        flowLayoutPanel3.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel3.Location = new Point(3, 3);
        flowLayoutPanel3.Name = "flowLayoutPanel3";
        flowLayoutPanel3.Size = new Size(1136, 609);
        flowLayoutPanel3.TabIndex = 342;
        // 
        // UserControlExchangeApi
        // 
        UserControlExchangeApi.AutoScroll = true;
        UserControlExchangeApi.AutoSize = true;
        UserControlExchangeApi.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        UserControlExchangeApi.Location = new Point(3, 3);
        UserControlExchangeApi.Name = "UserControlExchangeApi";
        UserControlExchangeApi.Size = new Size(404, 96);
        UserControlExchangeApi.TabIndex = 343;
        // 
        // UserControlAltradyApi
        // 
        UserControlAltradyApi.AutoScroll = true;
        UserControlAltradyApi.AutoSize = true;
        UserControlAltradyApi.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        UserControlAltradyApi.Location = new Point(3, 105);
        UserControlAltradyApi.Name = "UserControlAltradyApi";
        UserControlAltradyApi.Size = new Size(407, 96);
        UserControlAltradyApi.TabIndex = 342;
        // 
        // tabWhiteBlack
        // 
        tabWhiteBlack.Controls.Add(tabControlWhiteBlack);
        tabWhiteBlack.Location = new Point(4, 27);
        tabWhiteBlack.Name = "tabWhiteBlack";
        tabWhiteBlack.Padding = new Padding(3);
        tabWhiteBlack.Size = new Size(1142, 615);
        tabWhiteBlack.TabIndex = 13;
        tabWhiteBlack.Text = "Black/White list";
        tabWhiteBlack.UseVisualStyleBackColor = true;
        // 
        // tabControlWhiteBlack
        // 
        tabControlWhiteBlack.Appearance = TabAppearance.FlatButtons;
        tabControlWhiteBlack.Controls.Add(tabLongWhiteList);
        tabControlWhiteBlack.Controls.Add(tabLongBlackList);
        tabControlWhiteBlack.Controls.Add(tabShortWhiteList);
        tabControlWhiteBlack.Controls.Add(tabShortBlackList);
        tabControlWhiteBlack.Dock = DockStyle.Fill;
        tabControlWhiteBlack.Location = new Point(3, 3);
        tabControlWhiteBlack.Name = "tabControlWhiteBlack";
        tabControlWhiteBlack.SelectedIndex = 0;
        tabControlWhiteBlack.Size = new Size(1136, 609);
        tabControlWhiteBlack.TabIndex = 0;
        // 
        // tabLongWhiteList
        // 
        tabLongWhiteList.Controls.Add(textBoxWhiteListOversold);
        tabLongWhiteList.Controls.Add(panel3);
        tabLongWhiteList.Location = new Point(4, 27);
        tabLongWhiteList.Name = "tabLongWhiteList";
        tabLongWhiteList.Padding = new Padding(3);
        tabLongWhiteList.Size = new Size(1128, 578);
        tabLongWhiteList.TabIndex = 0;
        tabLongWhiteList.Text = "Whitelist long";
        tabLongWhiteList.UseVisualStyleBackColor = true;
        // 
        // textBoxWhiteListOversold
        // 
        textBoxWhiteListOversold.Dock = DockStyle.Fill;
        textBoxWhiteListOversold.Location = new Point(3, 60);
        textBoxWhiteListOversold.Margin = new Padding(4, 3, 4, 3);
        textBoxWhiteListOversold.Multiline = true;
        textBoxWhiteListOversold.Name = "textBoxWhiteListOversold";
        textBoxWhiteListOversold.Size = new Size(1122, 515);
        textBoxWhiteListOversold.TabIndex = 2;
        // 
        // panel3
        // 
        panel3.Controls.Add(label55);
        panel3.Dock = DockStyle.Top;
        panel3.Location = new Point(3, 3);
        panel3.Margin = new Padding(4, 3, 4, 3);
        panel3.Name = "panel3";
        panel3.Size = new Size(1122, 57);
        panel3.TabIndex = 3;
        // 
        // label55
        // 
        label55.AutoSize = true;
        label55.Location = new Point(20, 36);
        label55.Margin = new Padding(4, 0, 4, 0);
        label55.Name = "label55";
        label55.Size = new Size(223, 15);
        label55.TabIndex = 222;
        label55.Text = "(1 symbol per line with optional remarks)";
        // 
        // tabLongBlackList
        // 
        tabLongBlackList.Controls.Add(textBoxBlackListOversold);
        tabLongBlackList.Controls.Add(panel4);
        tabLongBlackList.Location = new Point(4, 27);
        tabLongBlackList.Name = "tabLongBlackList";
        tabLongBlackList.Padding = new Padding(3);
        tabLongBlackList.Size = new Size(1128, 578);
        tabLongBlackList.TabIndex = 1;
        tabLongBlackList.Text = "Blacklist long";
        tabLongBlackList.UseVisualStyleBackColor = true;
        // 
        // textBoxBlackListOversold
        // 
        textBoxBlackListOversold.Dock = DockStyle.Fill;
        textBoxBlackListOversold.Location = new Point(3, 60);
        textBoxBlackListOversold.Margin = new Padding(4, 3, 4, 3);
        textBoxBlackListOversold.Multiline = true;
        textBoxBlackListOversold.Name = "textBoxBlackListOversold";
        textBoxBlackListOversold.Size = new Size(1122, 515);
        textBoxBlackListOversold.TabIndex = 3;
        // 
        // panel4
        // 
        panel4.Controls.Add(label51);
        panel4.Dock = DockStyle.Top;
        panel4.Location = new Point(3, 3);
        panel4.Margin = new Padding(4, 3, 4, 3);
        panel4.Name = "panel4";
        panel4.Size = new Size(1122, 57);
        panel4.TabIndex = 4;
        // 
        // label51
        // 
        label51.AutoSize = true;
        label51.Location = new Point(20, 36);
        label51.Margin = new Padding(4, 0, 4, 0);
        label51.Name = "label51";
        label51.Size = new Size(0, 15);
        label51.TabIndex = 222;
        // 
        // tabShortWhiteList
        // 
        tabShortWhiteList.Controls.Add(textBoxWhiteListOverbought);
        tabShortWhiteList.Controls.Add(panel5);
        tabShortWhiteList.Location = new Point(4, 27);
        tabShortWhiteList.Name = "tabShortWhiteList";
        tabShortWhiteList.Padding = new Padding(3);
        tabShortWhiteList.Size = new Size(1128, 578);
        tabShortWhiteList.TabIndex = 2;
        tabShortWhiteList.Text = "Whitelist short";
        tabShortWhiteList.UseVisualStyleBackColor = true;
        // 
        // textBoxWhiteListOverbought
        // 
        textBoxWhiteListOverbought.Dock = DockStyle.Fill;
        textBoxWhiteListOverbought.Location = new Point(3, 60);
        textBoxWhiteListOverbought.Margin = new Padding(4, 3, 4, 3);
        textBoxWhiteListOverbought.Multiline = true;
        textBoxWhiteListOverbought.Name = "textBoxWhiteListOverbought";
        textBoxWhiteListOverbought.Size = new Size(1122, 515);
        textBoxWhiteListOverbought.TabIndex = 4;
        // 
        // panel5
        // 
        panel5.Controls.Add(label29);
        panel5.Dock = DockStyle.Top;
        panel5.Location = new Point(3, 3);
        panel5.Margin = new Padding(4, 3, 4, 3);
        panel5.Name = "panel5";
        panel5.Size = new Size(1122, 57);
        panel5.TabIndex = 5;
        // 
        // label29
        // 
        label29.AutoSize = true;
        label29.Location = new Point(20, 36);
        label29.Margin = new Padding(4, 0, 4, 0);
        label29.Name = "label29";
        label29.Size = new Size(0, 15);
        label29.TabIndex = 221;
        // 
        // tabShortBlackList
        // 
        tabShortBlackList.Controls.Add(textBoxBlackListOverbought);
        tabShortBlackList.Controls.Add(panel6);
        tabShortBlackList.Location = new Point(4, 27);
        tabShortBlackList.Name = "tabShortBlackList";
        tabShortBlackList.Padding = new Padding(3);
        tabShortBlackList.Size = new Size(1128, 578);
        tabShortBlackList.TabIndex = 3;
        tabShortBlackList.Text = "Blacklist short";
        tabShortBlackList.UseVisualStyleBackColor = true;
        // 
        // textBoxBlackListOverbought
        // 
        textBoxBlackListOverbought.Dock = DockStyle.Fill;
        textBoxBlackListOverbought.Location = new Point(3, 60);
        textBoxBlackListOverbought.Margin = new Padding(4, 3, 4, 3);
        textBoxBlackListOverbought.Multiline = true;
        textBoxBlackListOverbought.Name = "textBoxBlackListOverbought";
        textBoxBlackListOverbought.Size = new Size(1122, 515);
        textBoxBlackListOverbought.TabIndex = 7;
        // 
        // panel6
        // 
        panel6.Controls.Add(label49);
        panel6.Dock = DockStyle.Top;
        panel6.Location = new Point(3, 3);
        panel6.Margin = new Padding(4, 3, 4, 3);
        panel6.Name = "panel6";
        panel6.Size = new Size(1122, 57);
        panel6.TabIndex = 8;
        // 
        // label49
        // 
        label49.AutoSize = true;
        label49.Location = new Point(20, 36);
        label49.Margin = new Padding(4, 0, 4, 0);
        label49.Name = "label49";
        label49.Size = new Size(0, 15);
        label49.TabIndex = 222;
        // 
        // tabPageOptions
        // 
        tabPageOptions.Controls.Add(EditDebugAssetManagement);
        tabPageOptions.Controls.Add(EditUseHighLowInTrendCalculation);
        tabPageOptions.Controls.Add(EditDebugTrendCalculation);
        tabPageOptions.Controls.Add(EditDebugSymbol);
        tabPageOptions.Controls.Add(LabelDebugSymbol);
        tabPageOptions.Controls.Add(EditDebugSignalStrength);
        tabPageOptions.Controls.Add(EditDebugSignalCreate);
        tabPageOptions.Controls.Add(EditDebugKLineReceive);
        tabPageOptions.Location = new Point(4, 27);
        tabPageOptions.Name = "tabPageOptions";
        tabPageOptions.Padding = new Padding(3);
        tabPageOptions.Size = new Size(1142, 615);
        tabPageOptions.TabIndex = 15;
        tabPageOptions.Text = "Debug";
        tabPageOptions.UseVisualStyleBackColor = true;
        // 
        // EditDebugAssetManagement
        // 
        EditDebugAssetManagement.AutoSize = true;
        EditDebugAssetManagement.Location = new Point(23, 211);
        EditDebugAssetManagement.Margin = new Padding(4, 3, 4, 3);
        EditDebugAssetManagement.Name = "EditDebugAssetManagement";
        EditDebugAssetManagement.Size = new Size(296, 19);
        EditDebugAssetManagement.TabIndex = 304;
        EditDebugAssetManagement.Text = "Debug asset management (papertrading/emulator)";
        EditDebugAssetManagement.UseVisualStyleBackColor = true;
        // 
        // EditUseHighLowInTrendCalculation
        // 
        EditUseHighLowInTrendCalculation.AutoSize = true;
        EditUseHighLowInTrendCalculation.Location = new Point(25, 24);
        EditUseHighLowInTrendCalculation.Margin = new Padding(4, 3, 4, 3);
        EditUseHighLowInTrendCalculation.Name = "EditUseHighLowInTrendCalculation";
        EditUseHighLowInTrendCalculation.Size = new Size(327, 19);
        EditUseHighLowInTrendCalculation.TabIndex = 303;
        EditUseHighLowInTrendCalculation.Text = "Use High/Low in trend calculation instead of Open/Close";
        EditUseHighLowInTrendCalculation.UseVisualStyleBackColor = true;
        // 
        // EditDebugTrendCalculation
        // 
        EditDebugTrendCalculation.AutoSize = true;
        EditDebugTrendCalculation.Location = new Point(25, 50);
        EditDebugTrendCalculation.Margin = new Padding(4, 3, 4, 3);
        EditDebugTrendCalculation.Name = "EditDebugTrendCalculation";
        EditDebugTrendCalculation.Size = new Size(322, 19);
        EditDebugTrendCalculation.TabIndex = 302;
        EditDebugTrendCalculation.Text = "Show more information during TrendCalculation (in file)";
        EditDebugTrendCalculation.UseVisualStyleBackColor = true;
        // 
        // EditDebugSymbol
        // 
        EditDebugSymbol.CharacterCasing = CharacterCasing.Upper;
        EditDebugSymbol.Location = new Point(23, 132);
        EditDebugSymbol.Margin = new Padding(4, 3, 4, 3);
        EditDebugSymbol.Name = "EditDebugSymbol";
        EditDebugSymbol.Size = new Size(103, 23);
        EditDebugSymbol.TabIndex = 300;
        // 
        // LabelDebugSymbol
        // 
        LabelDebugSymbol.AutoSize = true;
        LabelDebugSymbol.Location = new Point(134, 135);
        LabelDebugSymbol.Margin = new Padding(4, 0, 4, 0);
        LabelDebugSymbol.Name = "LabelDebugSymbol";
        LabelDebugSymbol.Size = new Size(273, 15);
        LabelDebugSymbol.TabIndex = 301;
        LabelDebugSymbol.Text = "Limit Debug stuff to this symbol (less information)";
        // 
        // EditDebugSignalStrength
        // 
        EditDebugSignalStrength.AutoSize = true;
        EditDebugSignalStrength.Location = new Point(25, 75);
        EditDebugSignalStrength.Margin = new Padding(4, 3, 4, 3);
        EditDebugSignalStrength.Name = "EditDebugSignalStrength";
        EditDebugSignalStrength.Size = new Size(394, 19);
        EditDebugSignalStrength.TabIndex = 299;
        EditDebugSignalStrength.Text = "Calculate statistics (min and max price + perc for signal and position) ";
        EditDebugSignalStrength.UseVisualStyleBackColor = true;
        // 
        // EditDebugSignalCreate
        // 
        EditDebugSignalCreate.AutoSize = true;
        EditDebugSignalCreate.Location = new Point(23, 186);
        EditDebugSignalCreate.Margin = new Padding(4, 3, 4, 3);
        EditDebugSignalCreate.Name = "EditDebugSignalCreate";
        EditDebugSignalCreate.Size = new Size(269, 19);
        EditDebugSignalCreate.TabIndex = 298;
        EditDebugSignalCreate.Text = "Debug SignalCreate (does coin go to analyzer)";
        EditDebugSignalCreate.UseVisualStyleBackColor = true;
        // 
        // EditDebugKLineReceive
        // 
        EditDebugKLineReceive.AutoSize = true;
        EditDebugKLineReceive.Location = new Point(23, 161);
        EditDebugKLineReceive.Margin = new Padding(4, 3, 4, 3);
        EditDebugKLineReceive.Name = "EditDebugKLineReceive";
        EditDebugKLineReceive.Size = new Size(258, 19);
        EditDebugKLineReceive.TabIndex = 297;
        EditDebugKLineReceive.Text = "Debug KLineReceive (does kline ticker work)";
        EditDebugKLineReceive.UseVisualStyleBackColor = true;
        // 
        // FrmSettings
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        ClientSize = new Size(1150, 692);
        Controls.Add(panelFill);
        Controls.Add(panelButtons);
        Margin = new Padding(4, 3, 4, 3);
        Name = "FrmSettings";
        Text = "Settings";
        panelButtons.ResumeLayout(false);
        panelFill.ResumeLayout(false);
        tabControlMain.ResumeLayout(false);
        tabGeneral.ResumeLayout(false);
        tabGeneral.PerformLayout();
        flowLayoutPanel5.ResumeLayout(false);
        flowLayoutPanel5.PerformLayout();
        groupBox1.ResumeLayout(false);
        groupBox1.PerformLayout();
        groupBox7.ResumeLayout(false);
        groupBox7.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditBbStdDeviation).EndInit();
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
        tabBasecoin.ResumeLayout(false);
        tabBasecoin.PerformLayout();
        flowLayoutPanelQuotes.ResumeLayout(false);
        flowLayoutPanelQuotes.PerformLayout();
        tabSignal.ResumeLayout(false);
        tabControlSignals.ResumeLayout(false);
        tabSignalsGeneral.ResumeLayout(false);
        tabSignalsGeneral.PerformLayout();
        groupBox10.ResumeLayout(false);
        groupBox10.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditCheckVolumeOverDays).EndInit();
        groupBox9.ResumeLayout(false);
        groupBox9.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMinChangePercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisMaxChangePercentage).EndInit();
        GroupBoxXDaysEffective.ResumeLayout(false);
        GroupBoxXDaysEffective.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisEffectiveDays).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisEffectivePercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditCandlesWithZeroVolume).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditCandlesWithFlatPrice).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumAboveBollingerBandsUpper).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumAboveBollingerBandsSma).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumTickPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSymbolMustExistsDays).EndInit();
        tabSignalsLong.ResumeLayout(false);
        tabSignalsLong.PerformLayout();
        tabSignalsShort.ResumeLayout(false);
        tabSignalsShort.PerformLayout();
        tabSignalStobb.ResumeLayout(false);
        tabSignalStobb.PerformLayout();
        flowLayoutPanel6.ResumeLayout(false);
        flowLayoutPanel6.PerformLayout();
        groupBox2.ResumeLayout(false);
        groupBox2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditStobbBBMinPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditStobbBBMaxPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditStobTrendShort).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditStobTrendLong).EndInit();
        tabSignalSbm.ResumeLayout(false);
        tabSignalSbm.PerformLayout();
        flowLayoutPanel7.ResumeLayout(false);
        flowLayoutPanel7.PerformLayout();
        flowLayoutPanel9.ResumeLayout(false);
        flowLayoutPanel9.PerformLayout();
        groupBox3.ResumeLayout(false);
        groupBox3.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditSbm1CandlesLookbackCount).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm2BbPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm3CandlesForBBRecovery).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm3CandlesForBBRecoveryPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbm2CandlesLookbackCount).EndInit();
        groupBox4.ResumeLayout(false);
        groupBox4.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditSbmCandlesForMacdRecovery).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmBBMinPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmBBMaxPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa20Percentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa50AndMa20Percentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa50Percentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa50AndMa20Lookback).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa50Lookback).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSbmMa200AndMa20Lookback).EndInit();
        tabSignalStoRsi.ResumeLayout(false);
        tabSignalStoRsi.PerformLayout();
        flowLayoutPanel2.ResumeLayout(false);
        flowLayoutPanel2.PerformLayout();
        groupBox6.ResumeLayout(false);
        groupBox6.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditStorsiBBMinPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditStorsiBBMaxPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditStorsiAddStochAmount).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditStorsiAddRsiAmount).EndInit();
        tabSignalJump.ResumeLayout(false);
        tabSignalJump.PerformLayout();
        flowLayoutPanel8.ResumeLayout(false);
        flowLayoutPanel8.PerformLayout();
        groupBox5.ResumeLayout(false);
        groupBox5.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditJumpCandlesLookbackCount).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditAnalysisCandleJumpPercentage).EndInit();
        tabSignalZones.ResumeLayout(false);
        tabSignalZones.PerformLayout();
        flowLayoutPanel4.ResumeLayout(false);
        flowLayoutPanel4.PerformLayout();
        groupBox11.ResumeLayout(false);
        groupBox11.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditZonesCandleCount).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditZonesWarnPercentage).EndInit();
        groupBox12.ResumeLayout(false);
        groupBox12.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditMaximumUnZoomedPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimumUnZoomedPercentage).EndInit();
        groupBox13.ResumeLayout(false);
        groupBox13.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditMinimumZoomedPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditMaximumZoomedPercentage).EndInit();
        groupBox8.ResumeLayout(false);
        groupBox8.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditZoneStartPercentage).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditZoneStartCandleCount).EndInit();
        tabSignalZonesFvg.ResumeLayout(false);
        tabSignalZonesFvg.PerformLayout();
        flowLayoutPanel10.ResumeLayout(false);
        flowLayoutPanel10.PerformLayout();
        groupBox14.ResumeLayout(false);
        groupBox14.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditZonesFvgMinimumPercentage).EndInit();
        tabTrading.ResumeLayout(false);
        tabTrading.PerformLayout();
        tabControlTrading.ResumeLayout(false);
        tabTradingGeneral.ResumeLayout(false);
        flowLayoutPanel1.ResumeLayout(false);
        flowLayoutPanel1.PerformLayout();
        panel7.ResumeLayout(false);
        panel7.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditGlobalBuyCooldownTime).EndInit();
        groupBoxInstap.ResumeLayout(false);
        groupBoxInstap.PerformLayout();
        groupBoxSlots.ResumeLayout(false);
        groupBoxSlots.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalLong).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditSlotsMaximalShort).EndInit();
        groupBoxFutures.ResumeLayout(false);
        groupBoxFutures.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditLeverage).EndInit();
        tabTradingLong.ResumeLayout(false);
        tabTradingLong.PerformLayout();
        tabTradingShort.ResumeLayout(false);
        tabTradingShort.PerformLayout();
        tabPageTradingRules.ResumeLayout(false);
        tabPageTradingRules.PerformLayout();
        tabApi.ResumeLayout(false);
        tabApi.PerformLayout();
        flowLayoutPanel3.ResumeLayout(false);
        flowLayoutPanel3.PerformLayout();
        tabWhiteBlack.ResumeLayout(false);
        tabControlWhiteBlack.ResumeLayout(false);
        tabLongWhiteList.ResumeLayout(false);
        tabLongWhiteList.PerformLayout();
        panel3.ResumeLayout(false);
        panel3.PerformLayout();
        tabLongBlackList.ResumeLayout(false);
        tabLongBlackList.PerformLayout();
        panel4.ResumeLayout(false);
        panel4.PerformLayout();
        tabShortWhiteList.ResumeLayout(false);
        tabShortWhiteList.PerformLayout();
        panel5.ResumeLayout(false);
        panel5.PerformLayout();
        tabShortBlackList.ResumeLayout(false);
        tabShortBlackList.PerformLayout();
        panel6.ResumeLayout(false);
        panel6.PerformLayout();
        tabPageOptions.ResumeLayout(false);
        tabPageOptions.PerformLayout();
        ResumeLayout(false);
    }

    #endregion
    private Panel panelButtons;
    private Button buttonCancel;
    private Button buttonOk;
    private Panel panelFill;
    private Button buttonTestSpeech;
    private Button buttonReset;
    private Button buttonGotoAppDataFolder;
    private TabControl tabControlMain;
    private TabPage tabGeneral;
    private TabPage tabBasecoin;
    private TabPage tabSignal;
    private TabControl tabControlSignals;
    private TabPage tabSignalsGeneral;
    private Label label79;
    private Label label48;
    private Label label38;
    private Label label37;
    private Label label10;
    private CheckBox EditCandlesWithFlatPriceCheck;
    private CheckBox EditCandlesWithZeroVolumeCheck;
    private CheckBox EditMinimumAboveBollingerBandsSmaCheck;
    private NumericUpDown EditCandlesWithZeroVolume;
    private NumericUpDown EditCandlesWithFlatPrice;
    private NumericUpDown EditMinimumAboveBollingerBandsUpper;
    private NumericUpDown EditMinimumAboveBollingerBandsSma;
    private CheckBox EditLogMinimumTickPercentage;
    private NumericUpDown EditMinimumTickPercentage;
    private Label label61;
    private CheckBox EditLogSymbolMustExistsDays;
    private NumericUpDown EditSymbolMustExistsDays;
    private Label label25;
    private TabPage tabSignalsLong;
    private TabPage tabSignalsShort;
    private TabPage tabTrading;
    private TabControl tabControlTrading;
    private TabPage tabTradingGeneral;
    private TabPage tabTradingLong;
    private TabPage tabTradingShort;
    private Label label59;
    private TabPage tabWhiteBlack;
    private TabControl tabControlWhiteBlack;
    private TabPage tabLongWhiteList;
    private TextBox textBoxWhiteListOversold;
    private Panel panel3;
    private Label label55;
    private TabPage tabLongBlackList;
    private TextBox textBoxBlackListOversold;
    private Panel panel4;
    private Label label51;
    private TabPage tabShortWhiteList;
    private TextBox textBoxWhiteListOverbought;
    private Panel panel5;
    private Label label29;
    private TabPage tabShortBlackList;
    private TextBox textBoxBlackListOverbought;
    private Panel panel6;
    private Label label49;
    private TabPage tabSignalStobb;
    private TabPage tabSignalSbm;
    private TabPage tabSignalJump;
    private FlowLayoutPanel flowLayoutPanel5;
    private SettingsDialog.UserControlTelegram UserControlTelegram;
    private GroupBox groupBox1;
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
    private FlowLayoutPanel flowLayoutPanel6;
    private SettingsDialog.UserControlSettingsPlaySoundAndColors UserControlSettingsSoundAndColorsStobb;
    private GroupBox groupBox2;
    private Label label1;
    private NumericUpDown EditStobbBBMinPercentage;
    private NumericUpDown EditStobbBBMaxPercentage;
    private Label label85;
    private NumericUpDown EditStobTrendShort;
    private Label label66;
    private NumericUpDown EditStobTrendLong;
    private CheckBox EditStobIncludeSbmPercAndCrossing;
    private CheckBox EditStobIncludeSbmMaLines;
    private CheckBox EditStobIncludeRsi;
    private CheckBox EditStobbUseLowHigh;
    private FlowLayoutPanel flowLayoutPanel7;
    private FlowLayoutPanel flowLayoutPanel8;
    private SettingsDialog.UserControlSettingsPlaySoundAndColors UserControlSettingsSoundAndColorsJump;
    private GroupBox groupBox5;
    private Label label5;
    private NumericUpDown EditJumpCandlesLookbackCount;
    private CheckBox EditJumpUseLowHighCalculation;
    private Label label3;
    private NumericUpDown EditAnalysisCandleJumpPercentage;
    private FlowLayoutPanel flowLayoutPanel9;
    private GroupBox groupBox3;
    private CheckBox EditSbm2UseLowHigh;
    private Label label21;
    private Label label20;
    private Label label9;
    private Label label41;
    private NumericUpDown EditSbm1CandlesLookbackCount;
    private Label label12;
    private NumericUpDown EditSbm2BbPercentage;
    private Label label13;
    private NumericUpDown EditSbm3CandlesForBBRecovery;
    private Label label14;
    private NumericUpDown EditSbm3CandlesForBBRecoveryPercentage;
    private Label label11;
    private NumericUpDown EditSbm2CandlesLookbackCount;
    private GroupBox groupBox4;
    private Label label39;
    private NumericUpDown EditSbmCandlesForMacdRecovery;
    private Label label17;
    private NumericUpDown EditSbmBBMinPercentage;
    private NumericUpDown EditSbmBBMaxPercentage;
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
    private SettingsDialog.UserControlSettingsPlaySoundAndColors UserControlSettingsSoundAndColorsSbm;
    private CheckBox EditSbmUseLowHigh;
    private SettingsDialog.UserControlEverything UserControlSignalLong;
    private SettingsDialog.UserControlEverything UserControlSignalShort;
    private SettingsDialog.UserControlEverything UserControlTradingLong;
    private SettingsDialog.UserControlEverything UserControlTradingShort;
    private Panel panel7;
    private GroupBox groupBoxInstap;
    private CheckBox EditCheckIncreasingMacd;
    private CheckBox EditCheckIncreasingStoch;
    private CheckBox EditCheckIncreasingRsi;
    private GroupBox groupBoxFutures;
    private Label label19;
    private ComboBox EditCrossOrIsolated;
    private Label label23;
    private NumericUpDown EditLeverage;
    private CheckBox EditLogCanceledOrders;
    private CheckBox EditSoundTradeNotification;
    private CheckBox EditDisableNewPositions;
    private GroupBox groupBoxSlots;
    private Label label50;
    private NumericUpDown EditSlotsMaximalLong;
    private Label label52;
    private NumericUpDown EditSlotsMaximalShort;
    private FlowLayoutPanel flowLayoutPanel1;
    private SettingsDialog.UserControlTradeDca UserControlTradeDca;
    private Label label73;
    private NumericUpDown EditGlobalBuyCooldownTime;
    private TabPage tabPageTradingRules;
    private SettingsDialog.UserControlTradeRule UserControlTradeRules;
    private CheckBox EditStobOnlyIfPreviousStobb;
    private TabPage tabSignalStoRsi;
    private FlowLayoutPanel flowLayoutPanel2;
    private SettingsDialog.UserControlSettingsPlaySoundAndColors UserControlSettingsSoundAndColorsStoRsi;
    private Label label15;
    private ComboBox EditTradingAppInternExtern;
    private CheckBox EditCheckFurtherPriceMove;
    private Label label83;
    private ComboBox EditTradeVia;
    private GroupBox groupBox6;
    private Label label26;
    private NumericUpDown EditStorsiAddRsiAmount;
    private SettingsDialog.UserControlTradeEntry UserControlTradeEntry;
    private SettingsDialog.UserControlTradeTakeProfit UserControlTradeTakeProfit;
    private SettingsDialog.UserControlTradeStopLoss UserControlTradeStopLoss;
    private TabPage tabApi;
    private FlowLayoutPanel flowLayoutPanel3;
    private SettingsDialog.UserControlExchangeApi UserControlExchangeApi;
    private SettingsDialog.UserControlAltradyApi UserControlAltradyApi;
    private Label label24;
    private NumericUpDown EditStorsiAddStochAmount;
    private FlowLayoutPanel flowLayoutPanelQuotes;
    private SettingsDialog.UserControlQuoteHeader userControlQuoteHeader1;
    private CheckBox EditCheckBollingerBandsCondition;
    private GroupBox groupBox7;
    private Label label22;
    private NumericUpDown EditBbStdDeviation;
    private CheckBox EditHideSelectedRow;
    private TabPage tabPageOptions;
    private CheckBox EditDebugTrendCalculation;
    private TextBox EditDebugSymbol;
    private Label LabelDebugSymbol;
    private CheckBox EditDebugSignalStrength;
    private CheckBox EditDebugSignalCreate;
    private CheckBox EditDebugKLineReceive;
    private CheckBox EditSkipFirstSignal;
    private Label label28;
    private NumericUpDown EditStorsiBBMinPercentage;
    private NumericUpDown EditStorsiBBMaxPercentage;
    private GroupBox GroupBoxXDaysEffective;
    private CheckBox EditUseHighLowInTrendCalculation;
    private CheckBox EditDebugAssetManagement;
    private TabPage tabSignalZones;
    private FlowLayoutPanel flowLayoutPanel4;
    private SettingsDialog.UserControlSettingsPlaySoundAndColors UserControlSettingsSoundAndColorsZonesDominant;
    private GroupBox groupBox8;
    private Label label30;
    private NumericUpDown EditZoneStartCandleCount;
    private NumericUpDown EditZonesWarnPercentage;
    private NumericUpDown EditMaximumUnZoomedPercentage;
    private Label label40;
    private NumericUpDown EditZoneStartPercentage;
    private Label label42;
    private ComboBox EditDoubleClickAction;
    private GroupBox groupBox9;
    private Label label44;
    private Label label43;
    private Label label31;
    private NumericUpDown EditAnalysisEffectiveDays;
    private Label label86;
    private NumericUpDown EditAnalysisEffectivePercentage;
    private CheckBox EditAnalysisMaxEffectiveLog;
    private NumericUpDown EditAnalysisMinChangePercentage;
    private NumericUpDown EditAnalysisMaxChangePercentage;
    private Label label53;
    private CheckBox EditLogAnalysisMinMaxChangePercentage;
    private GroupBox groupBox10;
    private CheckBox EditCheckVolumeOverPeriod;
    private CheckBox EditMinimumAboveBollingerBandsUpperCheck;
    private Label label46;
    private Label label27;
    private NumericUpDown EditCheckVolumeOverDays;
    private Label label45;
    private GroupBox groupBox11;
    private Label label47;
    private Label label54;
    private NumericUpDown EditMinimumUnZoomedPercentage;
    private Label label56;
    private NumericUpDown EditMaximumZoomedPercentage;
    private Label label57;
    private CheckBox EditZoomLowerTimeFrames;
    private Label label60;
    private Label label62;
    private Label label63;
    private GroupBox groupBox12;
    private CheckBox EditShowZoneSignalsLong;
    private GroupBox groupBox13;
    private CheckBox EditZoneStartApply;
    private NumericUpDown EditZonesCandleCount;
    private NumericUpDown EditMinimumZoomedPercentage;
    private CheckBox EditZonesApplyUnzoomed;
    private TabPage tabSignalZonesFvg;
    private FlowLayoutPanel flowLayoutPanel10;
    private SettingsDialog.UserControlSettingsPlaySoundAndColors UserControlSettingsSoundAndColorsZonesFvg;
    private GroupBox groupBox14;
    private CheckBox EditShowFvgSignalsLong;
    private Label label34;
    private NumericUpDown EditZonesFvgMinimumPercentage;
    private CheckBox EditShowZoneSignalsShort;
    private CheckBox EditShowFvgSignalsShort;
}
