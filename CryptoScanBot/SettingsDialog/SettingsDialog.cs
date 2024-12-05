using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.SettingsDialog;

namespace CryptoScanBot;

public partial class FrmSettings : Form
{
    private SettingsBasic? settings;

    private readonly SortedList<string, CryptoAccountType> TradeVia = [];


    public Core.Model.CryptoExchange? NewExchange { get; set; }



    public FrmSettings()
    {
        InitializeComponent();

        buttonReset.Click += ButtonReset_Click;
        buttonTestSpeech.Click += ButtonTestSpeech_Click;
        buttonFontDialog.Click += ButtonFontDialog_Click;
        buttonGotoAppDataFolder.Click += ButtonGotoAppDataFolder_Click;

        // Signals/Trading long/short initialize
        UserControlSignalLong.InitControls(false, CryptoTradeSide.Long);
        UserControlSignalShort.InitControls(false, CryptoTradeSide.Short);

        UserControlTradingLong.InitControls(true, CryptoTradeSide.Long);
        UserControlTradingShort.InitControls(true, CryptoTradeSide.Short);

        // Trading (excluding the backtest)
        //TradeVia.Add("Backtest", CryptoTradeAccountType.BackTest);
        TradeVia.Add("Papertrading", CryptoAccountType.PaperTrade);
        //TradeVia.Add("Trading exchange", CryptoAccountType.RealTrading); removed until further notice
        TradeVia.Add("Altrady webhook", CryptoAccountType.Altrady);

        EditTradeVia.DataSource = new BindingSource(TradeVia, null);
        EditTradeVia.DisplayMember = "Key";
        EditTradeVia.ValueMember = "Value";
    }



    public void InitSettings(SettingsBasic settings)
    {
        this.settings = settings;

#if !DEBUG
        EditDebugTrendCalculation.Visible = false;
        EditDebugKLineReceive.Visible = false;
        EditDebugSignalCreate.Visible = false;
        EditDebugSignalStrength.Visible = false;
        EditUseHighLowInTrendCalculation.Visible = false;
        EditDebugSymbol.Visible = false;
        LabelDebugSymbol.Visible = false;
        EditDebugAssetManagement.Visible = false;
#endif

        EditAnalysisMinChangePercentage.Minimum = -100;
        EditAnalysisEffectivePercentage.Maximum = +1000;

        EditStobTrendLong.Minimum = -1000;
        EditStobTrendShort.Minimum = -1000;

        EditStorsiAddRsiAmount.Minimum = -100;
        EditStorsiAddRsiAmount.Maximum = +100;
        EditStorsiAddStochAmount.Minimum = -100;
        EditStorsiAddStochAmount.Maximum = +100;

        // ------------------------------------------------------------------------------
        // General
        // ------------------------------------------------------------------------------
        EditExtraCaption.Text = settings.General.ExtraCaption;

        EditExchange.DataSource = new BindingSource(GlobalData.ExchangeListName, null);
        EditExchange.DisplayMember = "Key";
        EditExchange.ValueMember = "Value";
        try { EditExchange.SelectedValue = settings.General.Exchange; } catch { }


        EditActivateExchange.DataSource = new BindingSource(GlobalData.ExchangeListName, null);
        EditActivateExchange.DisplayMember = "Key";
        EditActivateExchange.ValueMember = "Value";
        if (GlobalData.ExchangeListName.TryGetValue(settings.General.ActivateExchangeName, out Core.Model.CryptoExchange? exchange))
            try { EditActivateExchange.SelectedValue = exchange; } catch { }
        else
            try { EditActivateExchange.SelectedValue = settings.General.Exchange; } catch { }


        EditBlackTheming.Checked = settings.General.BlackTheming;
        try { EditTradingApp.SelectedIndex = (int)settings.General.TradingApp; } catch { }
        try { EditTradingAppInternExtern.SelectedIndex = (int)settings.General.TradingAppInternExtern; } catch { }
        try { EditDoubleClickAction.SelectedIndex = (int)settings.General.DoubleClickAction; } catch { }
        
        EditSoundHeartBeatMinutes.Value = settings.General.SoundHeartBeatMinutes;
        EditGetCandleInterval.Value = settings.General.GetCandleInterval;

        EditShowInvalidSignals.Checked = settings.General.ShowInvalidSignals;
        EditHideSymbolsOnTheLeft.Checked = settings.General.HideSymbolsOnTheLeft;
        EditGlobalDataRemoveSignalAfterxCandles.Value = settings.General.RemoveSignalAfterxCandles;
        EditHideSelectedRow.Checked = settings.General.HideSelectedRow;

        // Grenswaarden voor oversold en overbought
        EditRsiValueOversold.Value = (decimal)settings.General.RsiValueOversold;
        EditRsiValueOverbought.Value = (decimal)settings.General.RsiValueOverbought;
        EditStochValueOversold.Value = (decimal)settings.General.StochValueOversold;
        EditStochValueOverbought.Value = (decimal)settings.General.StochValueOverbought;

        EditBbStdDeviation.Value = (decimal)settings.General.BbStdDeviation;

        UserControlTelegram.LoadConfig();

#if DEBUG
        EditDebugTrendCalculation.Checked = settings.General.DebugTrendCalculation;
        EditDebugKLineReceive.Checked = settings.General.DebugKLineReceive;
        EditDebugSignalCreate.Checked = settings.General.DebugSignalCreate;
        EditDebugSignalStrength.Checked = settings.General.DebugSignalStrength;
        EditUseHighLowInTrendCalculation.Checked = settings.General.UseHighLowInTrendCalculation;
        EditDebugAssetManagement.Checked = settings.General.DebugAssetManagement;
        EditDebugSymbol.Text = settings.General.DebugSymbol.Trim();
#endif

        // ------------------------------------------------------------------------------
        // Base coins
        // ------------------------------------------------------------------------------
        // There has to be one, add USDT if needed
        if (GlobalData.Settings.QuoteCoins.Count == 0)
        {
            CryptoQuoteData quoteData = new()
            {
                Name = "USDT",
                FetchCandles = true,
                MinimalVolume = 4500000,
            };
            GlobalData.Settings.QuoteCoins.Add(quoteData.Name, quoteData);
        }


        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            UserControlQuote control = new();
            flowLayoutPanelQuotes.Controls.Add(control);
            control.LoadConfig(quoteData);
        }

        // ------------------------------------------------------------------------------
        // Signals
        // ------------------------------------------------------------------------------
        EditAnalysisMinChangePercentage.Value = (decimal)settings.Signal.AnalysisMinChangePercentage;
        EditAnalysisMaxChangePercentage.Value = (decimal)settings.Signal.AnalysisMaxChangePercentage;
        EditLogAnalysisMinMaxChangePercentage.Checked = settings.Signal.LogAnalysisMinMaxChangePercentage;

        EditAnalysisEffectiveDays.Value = (decimal)settings.Signal.AnalysisEffectiveDays;
        EditAnalysisEffectivePercentage.Value = (decimal)settings.Signal.AnalysisEffectivePercentage;
        EditAnalysisMaxEffectiveLog.Checked = settings.Signal.AnalysisMaxEffectiveLog;

        EditSymbolMustExistsDays.Value = settings.Signal.SymbolMustExistsDays;
        EditLogSymbolMustExistsDays.Checked = settings.Signal.LogSymbolMustExistsDays;

        EditMinimumTickPercentage.Value = settings.Signal.MinimumTickPercentage;
        EditLogMinimumTickPercentage.Checked = settings.Signal.LogMinimumTickPercentage;

        // ------------------------------------------------------------------------------
        // Signal types
        // ------------------------------------------------------------------------------

        // STOBB
        UserControlSettingsSoundAndColorsStobb.LoadConfig("STOBB", settings.Signal.Stobb);

        EditStobbBBMinPercentage.Value = (decimal)settings.Signal.Stobb.BBMinPercentage;
        EditStobbBBMaxPercentage.Value = (decimal)settings.Signal.Stobb.BBMaxPercentage;
        EditStobbUseLowHigh.Checked = settings.Signal.Stobb.UseLowHigh;
        EditStobIncludeRsi.Checked = settings.Signal.Stobb.IncludeRsi;
        EditStobIncludeSbmMaLines.Checked = settings.Signal.Stobb.IncludeSoftSbm;
        EditStobIncludeSbmPercAndCrossing.Checked = settings.Signal.Stobb.IncludeSbmPercAndCrossing;
        EditStobOnlyIfPreviousStobb.Checked = settings.Signal.Stobb.OnlyIfPreviousStobb;

        EditStobTrendLong.Value = settings.Signal.Stobb.TrendLong;
        EditStobTrendShort.Value = settings.Signal.Stobb.TrendShort;

        // SBM algemeen
        UserControlSettingsSoundAndColorsSbm.LoadConfig("SBM", settings.Signal.Sbm);

        EditSbmBBMinPercentage.Value = (decimal)settings.Signal.Sbm.BBMinPercentage;
        EditSbmBBMaxPercentage.Value = (decimal)settings.Signal.Sbm.BBMaxPercentage;
        EditSbmUseLowHigh.Checked = settings.Signal.Sbm.UseLowHigh;

        // SBM 1
        EditSbm1CandlesLookbackCount.Value = settings.Signal.Sbm.Sbm1CandlesLookbackCount;

        // SBM 2
        EditSbm2CandlesLookbackCount.Value = settings.Signal.Sbm.Sbm2CandlesLookbackCount;
        EditSbm2BbPercentage.Value = settings.Signal.Sbm.Sbm2BbPercentage;
        EditSbm2UseLowHigh.Checked = settings.Signal.Sbm.Sbm2UseLowHigh;

        // SBM 3
        EditSbm3CandlesForBBRecovery.Value = settings.Signal.Sbm.Sbm3CandlesLookbackCount;
        EditSbm3CandlesForBBRecoveryPercentage.Value = settings.Signal.Sbm.Sbm3CandlesBbRecoveryPercentage;

        // SBM aanvullend
        EditSbmCandlesForMacdRecovery.Value = settings.Signal.Sbm.CandlesForMacdRecovery;

        EditSbmMa200AndMa50Percentage.Value = settings.Signal.Sbm.Ma200AndMa50Percentage;
        EditSbmMa50AndMa20Percentage.Value = settings.Signal.Sbm.Ma50AndMa20Percentage;
        EditSbmMa200AndMa20Percentage.Value = settings.Signal.Sbm.Ma200AndMa20Percentage;

        EditSbmMa200AndMa20Crossing.Checked = settings.Signal.Sbm.Ma200AndMa20Crossing;
        EditSbmMa200AndMa20Lookback.Value = settings.Signal.Sbm.Ma200AndMa20Lookback;
        EditSbmMa200AndMa50Crossing.Checked = settings.Signal.Sbm.Ma200AndMa50Crossing;
        EditSbmMa200AndMa50Lookback.Value = settings.Signal.Sbm.Ma200AndMa50Lookback;
        EditSbmMa50AndMa20Crossing.Checked = settings.Signal.Sbm.Ma50AndMa20Crossing;
        EditSbmMa50AndMa20Lookback.Value = settings.Signal.Sbm.Ma50AndMa20Lookback;


        // STORSI
        EditStorsiBBMinPercentage.Value = (decimal)settings.Signal.StoRsi.BBMinPercentage;
        EditStorsiBBMaxPercentage.Value = (decimal)settings.Signal.StoRsi.BBMaxPercentage;
        UserControlSettingsSoundAndColorsStoRsi.LoadConfig("STORSI", settings.Signal.StoRsi);
        EditStorsiAddRsiAmount.Value = settings.Signal.StoRsi.AddRsiAmount;
        EditStorsiAddStochAmount.Value = settings.Signal.StoRsi.AddStochAmount;
        EditSkipFirstSignal.Checked = settings.Signal.StoRsi.SkipFirstSignal;
        EditCheckBollingerBandsCondition.Checked = settings.Signal.StoRsi.CheckBollingerBandsCondition;

        // JUMP
        UserControlSettingsSoundAndColorsJump.LoadConfig("Jump", settings.Signal.Jump);
        EditAnalysisCandleJumpPercentage.Value = settings.Signal.Jump.CandlePercentage;
        EditJumpCandlesLookbackCount.Value = settings.Signal.Jump.CandlesLookbackCount;
        EditJumpUseLowHighCalculation.Checked = settings.Signal.Jump.UseLowHighCalculation;

        // Zones
        UserControlSettingsSoundAndColorsDominantLevel.LoadConfig("Zones", settings.Signal.Zones);

        EditShowZoneSignals.Checked = settings.Signal.Zones.ShowZoneSignals;
        EditZonesUseLowHigh.Checked = settings.Signal.Zones.UseHighLow;
        EditZonesCandleCount.Value = settings.Signal.Zones.CandleCount;
        EditZonesWarnPercentage.Value = (decimal)settings.Signal.Zones.WarnPercentage;
        //EditZonesInterval.Value = settings.Signal.Zones.Interval; hardcoded 1h for now

        EditZoomLowerTimeFrames.Checked = settings.Signal.Zones.ZoomLowerTimeFrames;
        EditMinimumZoomedPercentage.Value = settings.Signal.Zones.MinimumZoomedPercentage;
        EditMaximumZoomedPercentage.Value = settings.Signal.Zones.MaximumZoomedPercentage;

        EditZonesApplyUnzoomed.Checked = settings.Signal.Zones.ZonesApplyUnzoomed;
        EditMinimumUnZoomedPercentage.Value = settings.Signal.Zones.MinimumUnZoomedPercentage;
        EditMaximumUnZoomedPercentage.Value = settings.Signal.Zones.MaximumUnZoomedPercentage;

        // Filter
        EditZoneStartApply.Checked = settings.Signal.Zones.ZoneStartApply;
        EditZoneStartCandleCount.Value = settings.Signal.Zones.ZoneStartCandleCount;
        EditZoneStartPercentage.Value = settings.Signal.Zones.ZoneStartPercentage;

        // --------------------------------------------------------------------------------
        // Extra instap condities
        // --------------------------------------------------------------------------------

        EditMinimumAboveBollingerBandsSma.Value = settings.Signal.AboveBollingerBandsSma;
        EditMinimumAboveBollingerBandsSmaCheck.Checked = settings.Signal.AboveBollingerBandsSmaCheck;

        EditMinimumAboveBollingerBandsUpper.Value = settings.Signal.AboveBollingerBandsUpper;
        EditMinimumAboveBollingerBandsUpperCheck.Checked = settings.Signal.AboveBollingerBandsUpperCheck;

        EditCandlesWithZeroVolume.Value = settings.Signal.CandlesWithZeroVolume;
        EditCandlesWithZeroVolumeCheck.Checked = settings.Signal.CandlesWithZeroVolumeCheck;

        EditCandlesWithFlatPrice.Value = settings.Signal.CandlesWithFlatPrice;
        EditCandlesWithFlatPriceCheck.Checked = settings.Signal.CandlesWithFlatPriceCheck;

        EditCheckVolumeOverPeriod.Checked = settings.Signal.CheckVolumeOverPeriod;
        EditCheckVolumeOverDays.Value = settings.Signal.CheckVolumeOverDays;

        // --------------------------------------------------------------------------------
        UserControlSignalLong.LoadConfig(settings.Signal.Long);
        UserControlSignalShort.LoadConfig(settings.Signal.Short);

        // ------------------------------------------------------------------------------
        // Trading
        // ------------------------------------------------------------------------------

        // Hoe gaan we traden
        try { EditTradeVia.SelectedValue = settings.Trading.TradeVia; } catch { }
        EditDisableNewPositions.Checked = settings.Trading.DisableNewPositions;
        EditSoundTradeNotification.Checked = settings.General.SoundTradeNotification;

        // Logging
        EditLogCanceledOrders.Checked = settings.Trading.LogCanceledOrders;

        EditGlobalBuyCooldownTime.Value = settings.Trading.GlobalBuyCooldownTime;

        // slots
        EditSlotsMaximalLong.Value = settings.Trading.SlotsMaximalLong;
        EditSlotsMaximalShort.Value = settings.Trading.SlotsMaximalShort;

        // Entry conditions
        EditCheckIncreasingRsi.Checked = settings.Trading.CheckIncreasingRsi;
        EditCheckIncreasingMacd.Checked = settings.Trading.CheckIncreasingMacd;
        EditCheckIncreasingStoch.Checked = settings.Trading.CheckIncreasingStoch;
        EditCheckFurtherPriceMove.Checked = settings.Trading.CheckFurtherPriceMove;

        // Entry/Dca/Tp/Sl
        UserControlTradeEntry.LoadConfig(settings.Trading);
        UserControlTradeDca.LoadConfig(settings.Trading);
        UserControlTradeTakeProfit.LoadConfig(settings.Trading);
        UserControlTradeStopLoss.LoadConfig(settings.Trading);

        // Futures
        EditLeverage.Value = settings.Trading.Leverage;
        try { EditCrossOrIsolated.SelectedIndex = settings.Trading.CrossOrIsolated; } catch { }

        // Loads of settings
        UserControlTradingLong.LoadConfig(settings.Trading.Long);
        UserControlTradingShort.LoadConfig(settings.Trading.Short);
        UserControlTradeRules.LoadConfig(settings.Trading);
        UserControlAltradyApi.LoadConfig(GlobalData.AltradyApi);
        UserControlExchangeApi.LoadConfig(GlobalData.TradingApi);
        UserControlExchangeApi.Visible = false;

        // --------------------------------------------------------------------------------
        // Black & White list
        // --------------------------------------------------------------------------------
        textBoxBlackListOversold.Text = string.Join(Environment.NewLine, settings.BlackListOversold);
        textBoxWhiteListOversold.Text = string.Join(Environment.NewLine, settings.WhiteListOversold);
        textBoxBlackListOverbought.Text = string.Join(Environment.NewLine, settings.BlackListOverbought);
        textBoxWhiteListOverbought.Text = string.Join(Environment.NewLine, settings.WhiteListOverbought);



        // --------------------------------------------------------------------------------
        // Font (pas op het einde zodat de dynamisch gegenereerde controls netjes meesizen)
        // --------------------------------------------------------------------------------
        if (GlobalData.Settings.General.FontSizeNew != Font.Size || GlobalData.Settings.General.FontNameNew.Equals(Font.Name))
        {
            Font = new Font(GlobalData.Settings.General.FontNameNew, GlobalData.Settings.General.FontSizeNew,
                FontStyle.Regular, GraphicsUnit.Point, 0);
        }
    }


    private void ButtonCancel_Click(object? sender, EventArgs? e)
    {
        DialogResult = DialogResult.Cancel;
    }


    private void ButtonOk_Click(object? sender, EventArgs? e)
    {
        // ------------------------------------------------------------------------------
        // General
        // ------------------------------------------------------------------------------
        // Don't save exchange immediately, lots of data still in memory etc
        settings!.General.ExtraCaption = EditExtraCaption.Text;
        NewExchange = (Core.Model.CryptoExchange)EditExchange.SelectedValue;
        Core.Model.CryptoExchange? NewActivateExchange = (Core.Model.CryptoExchange)EditActivateExchange.SelectedValue;
        if (NewActivateExchange != null)
            settings.General.ActivateExchangeName = NewActivateExchange.Name;
        else
            settings.General.ActivateExchangeName = NewExchange.Name;

        // Don't save immediately, lots of data still in memory etc
        //settings.General.ActivateExchange = EditActivateExchange.SelectedIndex;
        //settings.General.Exchange = (Model.CryptoExchange)EditExchange.SelectedValue;
        //settings.General.ExchangeId = settings.General.Exchange.Id;
        //settings.General.ExchangeName = settings.General.Exchange.Name;

        settings.General.BlackTheming = EditBlackTheming.Checked;
        settings.General.TradingApp = (CryptoTradingApp)EditTradingApp.SelectedIndex;
        settings.General.TradingAppInternExtern = (CryptoExternalUrlType)EditTradingAppInternExtern.SelectedIndex;
        settings.General.DoubleClickAction = (CryptoDoubleClickAction)EditDoubleClickAction.SelectedIndex;
        settings.General.SoundHeartBeatMinutes = (int)EditSoundHeartBeatMinutes.Value;
        settings.General.GetCandleInterval = (int)EditGetCandleInterval.Value;
        settings.General.FontNameNew = Font.Name;
        settings.General.FontSizeNew = Font.Size;

        settings.General.ShowInvalidSignals = EditShowInvalidSignals.Checked;
        settings.General.HideSymbolsOnTheLeft = EditHideSymbolsOnTheLeft.Checked;
        settings.General.RemoveSignalAfterxCandles = (int)EditGlobalDataRemoveSignalAfterxCandles.Value;
        settings.General.HideSelectedRow = EditHideSelectedRow.Checked;

        // Grenswaarden voor oversold en overbought
        settings.General.RsiValueOversold = (double)EditRsiValueOversold.Value;
        settings.General.RsiValueOverbought = (double)EditRsiValueOverbought.Value;
        settings.General.StochValueOversold = (double)EditStochValueOversold.Value;
        settings.General.StochValueOverbought = (double)EditStochValueOverbought.Value;

        settings.General.BbStdDeviation = (double)EditBbStdDeviation.Value;

        UserControlTelegram.SaveConfig();


#if DEBUG
        settings.General.DebugTrendCalculation = EditDebugTrendCalculation.Checked;
        settings.General.DebugKLineReceive = EditDebugKLineReceive.Checked;
        settings.General.DebugSignalCreate = EditDebugSignalCreate.Checked;
        settings.General.DebugSignalStrength = EditDebugSignalStrength.Checked;
        settings.General.DebugAssetManagement = EditDebugAssetManagement.Checked;
        settings.General.UseHighLowInTrendCalculation = EditUseHighLowInTrendCalculation.Checked;
        settings.General.DebugSymbol = EditDebugSymbol.Text.Trim();
#else
        settings.General.DebugSymbol = "";
        settings.General.DebugKLineReceive = false;
        settings.General.DebugSignalCreate = false;
        settings.General.DebugSignalStrength = false;
#endif

        // ------------------------------------------------------------------------------
        // Base coins
        // ------------------------------------------------------------------------------
        foreach (var control in flowLayoutPanelQuotes.Controls)
        {
            if (control is UserControlQuote controlQuote)
                controlQuote.SaveConfig(controlQuote.QuoteData);
        }

        // ------------------------------------------------------------------------------
        // Signals
        // ------------------------------------------------------------------------------
        settings.Signal.AnalysisMinChangePercentage = (double)EditAnalysisMinChangePercentage.Value;
        settings.Signal.AnalysisMaxChangePercentage = (double)EditAnalysisMaxChangePercentage.Value;
        settings.Signal.LogAnalysisMinMaxChangePercentage = EditLogAnalysisMinMaxChangePercentage.Checked;

        settings.Signal.AnalysisEffectiveDays = (int)EditAnalysisEffectiveDays.Value;
        settings.Signal.AnalysisEffectivePercentage = (double)EditAnalysisEffectivePercentage.Value;
        settings.Signal.AnalysisMaxEffectiveLog = EditAnalysisMaxEffectiveLog.Checked;

        settings.Signal.SymbolMustExistsDays = (int)EditSymbolMustExistsDays.Value;
        settings.Signal.LogSymbolMustExistsDays = EditLogSymbolMustExistsDays.Checked;

        settings.Signal.MinimumTickPercentage = EditMinimumTickPercentage.Value;
        settings.Signal.LogMinimumTickPercentage = EditLogMinimumTickPercentage.Checked;


        // ------------------------------------------------------------------------------
        // Signal types
        // ------------------------------------------------------------------------------

        // STOBB
        UserControlSettingsSoundAndColorsStobb.SaveConfig(settings.Signal.Stobb);

        settings.Signal.Stobb.BBMinPercentage = (double)EditStobbBBMinPercentage.Value;
        settings.Signal.Stobb.BBMaxPercentage = (double)EditStobbBBMaxPercentage.Value;
        settings.Signal.Stobb.IncludeRsi = EditStobIncludeRsi.Checked;
        settings.Signal.Stobb.IncludeSoftSbm = EditStobIncludeSbmMaLines.Checked;
        settings.Signal.Stobb.IncludeSbmPercAndCrossing = EditStobIncludeSbmPercAndCrossing.Checked;
        settings.Signal.Stobb.OnlyIfPreviousStobb = EditStobOnlyIfPreviousStobb.Checked;
        settings.Signal.Stobb.TrendLong = EditStobTrendLong.Value;
        settings.Signal.Stobb.TrendShort = EditStobTrendShort.Value;
        settings.Signal.Stobb.UseLowHigh = EditStobbUseLowHigh.Checked;


        // SBM
        UserControlSettingsSoundAndColorsSbm.SaveConfig(settings.Signal.Sbm);

        settings.Signal.Sbm.BBMinPercentage = (double)EditSbmBBMinPercentage.Value;
        settings.Signal.Sbm.BBMaxPercentage = (double)EditSbmBBMaxPercentage.Value;
        settings.Signal.Sbm.UseLowHigh = EditSbmUseLowHigh.Checked;

        // SBM 1
        settings.Signal.Sbm.Sbm1CandlesLookbackCount = (int)EditSbm1CandlesLookbackCount.Value;

        // SBM2
        settings.Signal.Sbm.Sbm2CandlesLookbackCount = (int)EditSbm2CandlesLookbackCount.Value;
        settings.Signal.Sbm.Sbm2BbPercentage = EditSbm2BbPercentage.Value;
        settings.Signal.Sbm.Sbm2UseLowHigh = EditSbm2UseLowHigh.Checked;

        // SBM3
        settings.Signal.Sbm.Sbm3CandlesLookbackCount = (int)EditSbm3CandlesForBBRecovery.Value;
        settings.Signal.Sbm.Sbm3CandlesBbRecoveryPercentage = EditSbm3CandlesForBBRecoveryPercentage.Value;

        // SBM aanvullend
        settings.Signal.Sbm.CandlesForMacdRecovery = (int)EditSbmCandlesForMacdRecovery.Value;
        settings.Signal.Sbm.Ma200AndMa50Percentage = EditSbmMa200AndMa50Percentage.Value;
        settings.Signal.Sbm.Ma50AndMa20Percentage = EditSbmMa50AndMa20Percentage.Value;
        settings.Signal.Sbm.Ma200AndMa20Percentage = EditSbmMa200AndMa20Percentage.Value;
        settings.Signal.Sbm.Ma200AndMa20Crossing = EditSbmMa200AndMa20Crossing.Checked;
        settings.Signal.Sbm.Ma200AndMa20Lookback = (int)EditSbmMa200AndMa20Lookback.Value;
        settings.Signal.Sbm.Ma200AndMa50Crossing = EditSbmMa200AndMa50Crossing.Checked;
        settings.Signal.Sbm.Ma200AndMa50Lookback = (int)EditSbmMa200AndMa50Lookback.Value;
        settings.Signal.Sbm.Ma50AndMa20Crossing = EditSbmMa50AndMa20Crossing.Checked;
        settings.Signal.Sbm.Ma50AndMa20Lookback = (int)EditSbmMa50AndMa20Lookback.Value;

        // STORSI
        settings.Signal.StoRsi.BBMinPercentage = (double)EditStorsiBBMinPercentage.Value;
        settings.Signal.StoRsi.BBMaxPercentage = (double)EditStorsiBBMaxPercentage.Value;
        UserControlSettingsSoundAndColorsStoRsi.SaveConfig(settings.Signal.StoRsi);
        settings.Signal.StoRsi.AddRsiAmount = (int)EditStorsiAddRsiAmount.Value;
        settings.Signal.StoRsi.AddStochAmount = (int)EditStorsiAddStochAmount.Value;
        settings.Signal.StoRsi.SkipFirstSignal = EditSkipFirstSignal.Checked;
        settings.Signal.StoRsi.CheckBollingerBandsCondition = EditCheckBollingerBandsCondition.Checked;

        // JUMP
        UserControlSettingsSoundAndColorsJump.SaveConfig(settings.Signal.Jump);
        settings.Signal.Jump.CandlePercentage = EditAnalysisCandleJumpPercentage.Value;
        settings.Signal.Jump.CandlesLookbackCount = (int)EditJumpCandlesLookbackCount.Value;
        settings.Signal.Jump.UseLowHighCalculation = EditJumpUseLowHighCalculation.Checked;


        // Zones
        UserControlSettingsSoundAndColorsDominantLevel.SaveConfig(settings.Signal.Zones);

        settings.Signal.Zones.ShowZoneSignals = EditShowZoneSignals.Checked;
        settings.Signal.Zones.UseHighLow = EditZonesUseLowHigh.Checked;
        settings.Signal.Zones.CandleCount = (int)EditZonesCandleCount.Value;
        settings.Signal.Zones.WarnPercentage = EditZonesWarnPercentage.Value;
        //settings.Signal.Zones.Interval = CryptoIntervalPeriod.interval1h; //EditZonesInterval.Value; hardcoded 1h for now

        settings.Signal.Zones.ZoomLowerTimeFrames = EditZoomLowerTimeFrames.Checked;
        settings.Signal.Zones.MinimumZoomedPercentage = EditMinimumZoomedPercentage.Value;
        settings.Signal.Zones.MaximumZoomedPercentage = EditMaximumZoomedPercentage.Value;

        settings.Signal.Zones.ZonesApplyUnzoomed = EditZonesApplyUnzoomed.Checked;
        settings.Signal.Zones.MinimumUnZoomedPercentage = EditMinimumUnZoomedPercentage.Value;
        settings.Signal.Zones.MaximumUnZoomedPercentage = EditMaximumUnZoomedPercentage.Value;

        // Filter
        settings.Signal.Zones.ZoneStartApply = EditZoneStartApply.Checked;
        settings.Signal.Zones.ZoneStartCandleCount = (int)EditZoneStartCandleCount.Value;
        settings.Signal.Zones.ZoneStartPercentage = EditZoneStartPercentage.Value;


        // --------------------------------------------------------------------------------
        // Extra instap condities
        // --------------------------------------------------------------------------------
        settings.Signal.AboveBollingerBandsSma = (int)EditMinimumAboveBollingerBandsSma.Value;
        settings.Signal.AboveBollingerBandsSmaCheck = EditMinimumAboveBollingerBandsSmaCheck.Checked;

        settings.Signal.AboveBollingerBandsUpper = (int)EditMinimumAboveBollingerBandsUpper.Value;
        settings.Signal.AboveBollingerBandsUpperCheck = EditMinimumAboveBollingerBandsUpperCheck.Checked;

        settings.Signal.CandlesWithZeroVolume = (int)EditCandlesWithZeroVolume.Value;
        settings.Signal.CandlesWithZeroVolumeCheck = EditCandlesWithZeroVolumeCheck.Checked;

        settings.Signal.CandlesWithFlatPrice = (int)EditCandlesWithFlatPrice.Value;
        settings.Signal.CandlesWithFlatPriceCheck = EditCandlesWithFlatPriceCheck.Checked;

        settings.Signal.CheckVolumeOverPeriod = EditCheckVolumeOverPeriod.Checked;
        settings.Signal.CheckVolumeOverDays = (int)EditCheckVolumeOverDays.Value;


        // --------------------------------------------------------------------------------
        UserControlSignalLong.SaveConfig(settings.Signal.Long);
        UserControlSignalShort.SaveConfig(settings.Signal.Short);

        // --------------------------------------------------------------------------------
        // Black & White list
        // --------------------------------------------------------------------------------

        settings.BlackListOversold = [.. textBoxBlackListOversold.Text.Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)];
        settings.WhiteListOversold.Sort();

        settings.WhiteListOversold = [.. textBoxWhiteListOversold.Text.Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)];
        settings.WhiteListOversold.Sort();

        settings.BlackListOverbought = [.. textBoxBlackListOverbought.Text.Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)];
        settings.WhiteListOverbought.Sort();

        settings.WhiteListOverbought = [.. textBoxWhiteListOverbought.Text.Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)];
        settings.WhiteListOverbought.Sort();

        // --------------------------------------------------------------------------------
        // Trade bot
        // --------------------------------------------------------------------------------
        settings.Trading.TradeVia = (CryptoAccountType)EditTradeVia.SelectedValue;
        settings.Trading.DisableNewPositions = EditDisableNewPositions.Checked;
        settings.General.SoundTradeNotification = EditSoundTradeNotification.Checked;

        // Logging
        settings.Trading.LogCanceledOrders = EditLogCanceledOrders.Checked;

        settings.Trading.GlobalBuyCooldownTime = (int)EditGlobalBuyCooldownTime.Value;

        // slots
        settings.Trading.SlotsMaximalLong = (int)EditSlotsMaximalLong.Value;
        settings.Trading.SlotsMaximalShort = (int)EditSlotsMaximalShort.Value;

        // Entry conditions
        settings.Trading.CheckIncreasingRsi = EditCheckIncreasingRsi.Checked;
        settings.Trading.CheckIncreasingMacd = EditCheckIncreasingMacd.Checked;
        settings.Trading.CheckIncreasingStoch = EditCheckIncreasingStoch.Checked;
        settings.Trading.CheckFurtherPriceMove = EditCheckFurtherPriceMove.Checked;

        // Entry/Dca/Tp/Sl
        UserControlTradeEntry.SaveConfig(settings.Trading);
        UserControlTradeDca.SaveConfig(settings.Trading);
        UserControlTradeTakeProfit.SaveConfig(settings.Trading);
        UserControlTradeStopLoss.SaveConfig(settings.Trading);

        // Futures
        settings.Trading.Leverage = EditLeverage.Value;
        settings.Trading.CrossOrIsolated = EditCrossOrIsolated.SelectedIndex;

        // Loads of settings
        UserControlTradingLong.SaveConfig(settings.Trading.Long);
        UserControlTradingShort.SaveConfig(settings.Trading.Short);
        UserControlTradeRules.SaveConfig(settings.Trading);
        UserControlAltradyApi.SaveConfig(GlobalData.AltradyApi);
        UserControlExchangeApi.SaveConfig(GlobalData.TradingApi);


        DialogResult = DialogResult.OK;
    }

    private void ButtonTestSpeech_Click(object? sender, EventArgs? e) =>
        GlobalData.PlaySomeSpeech("Found a signal for BTCUSDT interval 1m (it is going to the moon)", true);

    private void ButtonReset_Click(object? sender, EventArgs? e)
    {
        if (MessageBox.Show("Alle instellingen resetten?", "Attentie!", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            GlobalData.Settings = new();
            GlobalData.DefaultSettings();
            InitSettings(GlobalData.Settings);
        }
    }

    private void ButtonFontDialog_Click(object? sender, EventArgs? e)
    {
        FontDialog dialog = new()
        {
            Font = this.Font
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            Font = dialog.Font;
        }

    }

    private void ButtonGotoAppDataFolder_Click(object? sender, EventArgs? e)
    {
        string folder = GlobalData.GetBaseDir();
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder) { UseShellExecute = true });
    }

}