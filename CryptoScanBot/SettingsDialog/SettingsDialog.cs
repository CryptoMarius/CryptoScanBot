using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using CryptoScanBot.Settings;
using CryptoScanBot.SettingsDialog;

namespace CryptoScanBot;

public partial class FrmSettings : Form
{
    private SettingsBasic settings;

    private readonly List<SettingsQuoteCoin> BaseCoinList = [];


    public Model.CryptoExchange NewExchange { get; set; }


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

#if TRADEBOT
        UserControlTradingLong.InitControls(true, CryptoTradeSide.Long);
        UserControlTradingShort.InitControls(true, CryptoTradeSide.Short);
#endif
    }



    public void InitSettings(SettingsBasic settings)
    {
        this.settings = settings;

#if !TRADEBOT
        settings.Trading.Active = false;
        tabTrading.Parent = null;
        tabControlMain.TabPages.Remove(tabTrading);
#endif

        // Deze worden na de overgang naar .net 7 regelmatig gereset naar 0
        // Benieuwd waarom dit gebeurd (het zijn er gelukkig niet zo veel)
        EditGlobalBuyVarying.Minimum = -0.5m;

        EditAnalysisMinChangePercentage.Minimum = -100;
        EditAnalysisMinEffectivePercentage.Minimum = -1000;
        EditAnalysisMaxEffectivePercentage.Maximum = +1000;
        EditAnalysisMinEffective10DaysPercentage.Minimum = -1000;
        EditAnalysisMaxEffective10DaysPercentage.Maximum = +1000;

        EditStobTrendLong.Minimum = -1000;
        EditStobTrendShort.Minimum = -1000;

        // ------------------------------------------------------------------------------
        // General
        // ------------------------------------------------------------------------------
        EditExtraCaption.Text = settings.General.ExtraCaption;

        EditExchange.DataSource = new BindingSource(GlobalData.ExchangeListName, null);
        EditExchange.DisplayMember = "Key";
        EditExchange.ValueMember = "Value";
        EditExchange.SelectedValue = settings.General.Exchange;

        EditBlackTheming.Checked = settings.General.BlackTheming;
        EditTradingApp.SelectedIndex = (int)settings.General.TradingApp;
        EditActivateExchange.SelectedIndex = (int)settings.General.ActivateExchange;
        EditActivateExchangeInternExtern.SelectedIndex = (int)settings.General.TradingAppInternExtern;
        EditSoundHeartBeatMinutes.Value = settings.General.SoundHeartBeatMinutes;
        EditGetCandleInterval.Value = settings.General.GetCandleInterval;

        EditShowInvalidSignals.Checked = settings.General.ShowInvalidSignals;
        EditHideSymbolsOnTheLeft.Checked = settings.General.HideSymbolsOnTheLeft;
        EditGlobalDataRemoveSignalAfterxCandles.Value = settings.General.RemoveSignalAfterxCandles;

        // Grenswaarden voor oversold en overbought
        EditRsiValueOversold.Value = (decimal)settings.General.RsiValueOversold;
        EditRsiValueOverbought.Value = (decimal)settings.General.RsiValueOverbought;
        EditStochValueOversold.Value = (decimal)settings.General.StochValueOversold;
        EditStochValueOverbought.Value = (decimal)settings.General.StochValueOverbought;

        UserControlTelegram.LoadConfig();


        // ------------------------------------------------------------------------------
        // Base coins
        // ------------------------------------------------------------------------------
        // Er moet er eentje zijn, de USDT maar toevoegen
        if (GlobalData.Settings.QuoteCoins.Count == 0)
        {
            // Als default toch iets inschieten
            CryptoQuoteData quoteData = new()
            {
                Name = "USDT",
                FetchCandles = true,
                CreateSignals = true,
                MinimalVolume = 4500000,
            };

            GlobalData.Settings.QuoteCoins.Add(quoteData.Name, quoteData);
        }

        // Hey, een ouderwetse control, zou eigenlijk meerdere usercontrol moeten zijn die op een flowpanel worden geplaatst... later...
        int yInc = 0;
        int yPos = 75;
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.SymbolList.Count > 0) //|| quoteData.Name.Equals("BTC") || quoteData.Name.Equals("USDT")
            {
                var baseCoin = new SettingsQuoteCoin(quoteData, yPos, tabBasismunten.Controls);
                BaseCoinList.Add(baseCoin);

                if (yInc == 0)
                    yInc = (int)(baseCoin.MinVolume.Height * 1.2m);
                yPos += yInc;
            }
            else
            {
                quoteData.FetchCandles = false;
                quoteData.CreateSignals = false;
            }
        }

        foreach (SettingsQuoteCoin x in BaseCoinList)
            x.SetControlValues();
        BaseCoinList[0].AddHeaderLabels(40, tabBasismunten.Controls);

        // ------------------------------------------------------------------------------
        // Signals
        // ------------------------------------------------------------------------------
        EditAnalysisMinChangePercentage.Value = (decimal)settings.Signal.AnalysisMinChangePercentage;
        EditAnalysisMaxChangePercentage.Value = (decimal)settings.Signal.AnalysisMaxChangePercentage;
        EditLogAnalysisMinMaxChangePercentage.Checked = settings.Signal.LogAnalysisMinMaxChangePercentage;

        EditAnalysisMinEffectivePercentage.Value = (decimal)settings.Signal.AnalysisMinEffectivePercentage;
        EditAnalysisMaxEffectivePercentage.Value = (decimal)settings.Signal.AnalysisMaxEffectivePercentage;
        EditLogAnalysisMinMaxEffectivePercentage.Checked = settings.Signal.LogAnalysisMinMaxEffectivePercentage;

        EditAnalysisMinEffective10DaysPercentage.Value = (decimal)settings.Signal.AnalysisMinEffective10DaysPercentage;
        EditAnalysisMaxEffective10DaysPercentage.Value = (decimal)settings.Signal.AnalysisMaxEffective10DaysPercentage;
        EditLogAnalysisMinMaxEffective10DaysPercentage.Checked = settings.Signal.LogAnalysisMinMaxEffective10DaysPercentage;

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
        UserControlSettingsSoundAndColorsStoRsi.LoadConfig("STORSI", settings.Signal.StoRsi);

        // JUMP
        UserControlSettingsSoundAndColorsJump.LoadConfig("Jump", settings.Signal.Jump);

        EditAnalysisCandleJumpPercentage.Value = settings.Signal.Jump.CandlePercentage;
        EditJumpCandlesLookbackCount.Value = settings.Signal.Jump.CandlesLookbackCount;
        EditJumpUseLowHighCalculation.Checked = settings.Signal.Jump.UseLowHighCalculation;


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

        // --------------------------------------------------------------------------------
        UserControlSignalLong.LoadConfig(settings.Signal.Long);
        UserControlSignalShort.LoadConfig(settings.Signal.Short);

#if TRADEBOT
        // ------------------------------------------------------------------------------
        // Trading
        // ------------------------------------------------------------------------------

        // Hoe gaan we traden
        EditTradeViaExchange.Checked = settings.Trading.TradeViaExchange;
        EditTradeViaPaperTrading.Checked = settings.Trading.TradeViaPaperTrading;
        EditDisableNewPositions.Checked = settings.Trading.DisableNewPositions;
        EditSoundTradeNotification.Checked = settings.General.SoundTradeNotification;

        // Logging
        EditLogCanceledOrders.Checked = settings.Trading.LogCanceledOrders;

        // Api
        EditApiKey.Text = GlobalData.TradingApi.Key;
        EditApiSecret.Text = GlobalData.TradingApi.Secret;
        EditGlobalBuyVarying.Value = settings.Trading.GlobalBuyVarying;
        EditGlobalBuyCooldownTime.Value = settings.Trading.GlobalBuyCooldownTime;

        // slots
        EditSlotsMaximalLong.Value = settings.Trading.SlotsMaximalLong;
        EditSlotsMaximalShort.Value = settings.Trading.SlotsMaximalShort;

        // Instap
        EditCheckIncreasingRsi.Checked = settings.Trading.CheckIncreasingRsi;
        EditCheckIncreasingMacd.Checked = settings.Trading.CheckIncreasingMacd;
        EditCheckIncreasingStoch.Checked = settings.Trading.CheckIncreasingStoch;


        UserControlTradeBuy.LoadConfig(settings.Trading);
        UserControlTradeDca.LoadConfig(settings.Trading);
        UserControlTradeSell.LoadConfig(settings.Trading);

        // Stop loss
        //EditGlobalStopPercentage.Value = Math.Abs(settings.Trading.GlobalStopPercentage);
        //EditGlobalStopLimitPercentage.Value = Math.Abs(settings.Trading.GlobalStopLimitPercentage);

        EditLeverage.Value = settings.Trading.Leverage;
        EditCrossOrIsolated.SelectedIndex = settings.Trading.CrossOrIsolated;

        UserControlTradingLong.LoadConfig(settings.Trading.Long);
        UserControlTradingShort.LoadConfig(settings.Trading.Short);
        UserControlTradeRules.LoadConfig(settings.Trading);
#endif

        // --------------------------------------------------------------------------------
        // Black & White list
        // --------------------------------------------------------------------------------
        textBoxBlackListOversold.Text = string.Join(Environment.NewLine, settings.BlackListOversold);
        textBoxWhiteListOversold.Text = string.Join(Environment.NewLine, settings.WhiteListOversold);
        textBoxBlackListOverbought.Text = string.Join(Environment.NewLine, settings.BlackListOverbought);
        textBoxWhiteListOverbought.Text = string.Join(Environment.NewLine, settings.WhiteListOverbought);



        // ------------------------------------------------------------------------------
        // Balance bot
        // ------------------------------------------------------------------------------
#if BALANCING
        //EditBlanceBotActive.Checked = settings.BalanceBot.Active;
        //numericStartAmount.Value = settings.BalanceBot.StartAmount;
        //EditShowAdviceOnly.Checked = settings.BalanceBot.ShowAdviceOnly;
        //EditIntervalPeriod.Value = settings.BalanceBot.IntervalPeriod;
        //EditMinimalBuyBarometer.Value = settings.BalanceBot.MinimalBuyBarometer;
        //EditMinimalSellBarometer.Value = settings.BalanceBot.MinimalSellBarometer;
        //EditBuyBalanceThreshold.Value = settings.BalanceBot.BuyThresholdPercentage;
        //EditSellBalanceThreshold.Value = settings.BalanceBot.SellThresholdPercentage;
#endif

        // --------------------------------------------------------------------------------
        // Font (pas op het einde zodat de dynamisch gegenereerde controls netjes meesizen)
        // --------------------------------------------------------------------------------
        if (GlobalData.Settings.General.FontSizeNew != Font.Size || GlobalData.Settings.General.FontNameNew.Equals(Font.Name))
        {
            Font = new Font(GlobalData.Settings.General.FontNameNew, GlobalData.Settings.General.FontSizeNew,
                FontStyle.Regular, GraphicsUnit.Point, 0);
        }
    }


    private void ButtonCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
    }


    private void ButtonOk_Click(object sender, EventArgs e)
    {
        // ------------------------------------------------------------------------------
        // General
        // ------------------------------------------------------------------------------
        settings.General.ExtraCaption = EditExtraCaption.Text;
        NewExchange = (Model.CryptoExchange)EditExchange.SelectedValue;
        // Niet direct zetten, eerst moet alles uitgezet worden
        //settings.General.Exchange = (Model.CryptoExchange)EditExchange.SelectedValue;
        //settings.General.ExchangeId = settings.General.Exchange.Id;
        //settings.General.ExchangeName = settings.General.Exchange.Name;
        settings.General.BlackTheming = EditBlackTheming.Checked;
        settings.General.TradingApp = (CryptoTradingApp)EditTradingApp.SelectedIndex;
        settings.General.ActivateExchange = EditActivateExchange.SelectedIndex;
        settings.General.TradingAppInternExtern = (CryptoExternalUrlType)EditActivateExchangeInternExtern.SelectedIndex;
        settings.General.SoundHeartBeatMinutes = (int)EditSoundHeartBeatMinutes.Value;
        settings.General.GetCandleInterval = (int)EditGetCandleInterval.Value;
        settings.General.FontNameNew = Font.Name;
        settings.General.FontSizeNew = Font.Size;

        settings.General.ShowInvalidSignals = EditShowInvalidSignals.Checked;
        settings.General.HideSymbolsOnTheLeft = EditHideSymbolsOnTheLeft.Checked;
        settings.General.RemoveSignalAfterxCandles = (int)EditGlobalDataRemoveSignalAfterxCandles.Value;

        // Grenswaarden voor oversold en overbought
        settings.General.RsiValueOversold = (double)EditRsiValueOversold.Value;
        settings.General.RsiValueOverbought = (double)EditRsiValueOverbought.Value;
        settings.General.StochValueOversold = (double)EditStochValueOversold.Value;
        settings.General.StochValueOverbought = (double)EditStochValueOverbought.Value;

        UserControlTelegram.SaveConfig();


        // ------------------------------------------------------------------------------
        // Base coins
        // ------------------------------------------------------------------------------
        foreach (SettingsQuoteCoin x in BaseCoinList)
            x.GetControlValues();


        // ------------------------------------------------------------------------------
        // Signals
        // ------------------------------------------------------------------------------
        settings.Signal.AnalysisMinChangePercentage = (double)EditAnalysisMinChangePercentage.Value;
        settings.Signal.AnalysisMaxChangePercentage = (double)EditAnalysisMaxChangePercentage.Value;
        settings.Signal.LogAnalysisMinMaxChangePercentage = EditLogAnalysisMinMaxChangePercentage.Checked;

        settings.Signal.AnalysisMinEffectivePercentage = (double)EditAnalysisMinEffectivePercentage.Value;
        settings.Signal.AnalysisMaxEffectivePercentage = (double)EditAnalysisMaxEffectivePercentage.Value;
        settings.Signal.LogAnalysisMinMaxEffectivePercentage = EditLogAnalysisMinMaxEffectivePercentage.Checked;

        settings.Signal.AnalysisMinEffective10DaysPercentage = (double)EditAnalysisMinEffective10DaysPercentage.Value;
        settings.Signal.AnalysisMaxEffective10DaysPercentage = (double)EditAnalysisMaxEffective10DaysPercentage.Value;
        settings.Signal.LogAnalysisMinMaxEffective10DaysPercentage = EditLogAnalysisMinMaxEffective10DaysPercentage.Checked;

        //settings.Signal.Barometer1hMinimal = EditBarometerLong1hMinimal.Value;
        //settings.Signal.LogBarometerToLow = EditBarometerLong1hMinimalLog.Checked;

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


        // SBM
        UserControlSettingsSoundAndColorsSbm.SaveConfig(settings.Signal.Sbm);

        settings.Signal.Sbm.BBMinPercentage = (double)EditSbmBBMinPercentage.Value;
        settings.Signal.Sbm.BBMaxPercentage = (double)EditSbmBBMaxPercentage.Value;
        settings.Signal.Sbm.UseLowHigh = EditStobbUseLowHigh.Checked;

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
        UserControlSettingsSoundAndColorsStoRsi.SaveConfig(settings.Signal.StoRsi);

        // JUMP
        UserControlSettingsSoundAndColorsJump.SaveConfig(settings.Signal.Jump);

        settings.Signal.Jump.CandlePercentage = EditAnalysisCandleJumpPercentage.Value;
        settings.Signal.Jump.CandlesLookbackCount = (int)EditJumpCandlesLookbackCount.Value;
        settings.Signal.Jump.UseLowHighCalculation = EditJumpUseLowHighCalculation.Checked;

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


        // --------------------------------------------------------------------------------
        UserControlSignalLong.SaveConfig(settings.Signal.Long);
        UserControlSignalShort.SaveConfig(settings.Signal.Short);

        // --------------------------------------------------------------------------------
        // Black & White list
        // --------------------------------------------------------------------------------

        settings.BlackListOversold = textBoxBlackListOversold.Text.Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.WhiteListOversold.Sort();

        settings.WhiteListOversold = textBoxWhiteListOversold.Text.Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.WhiteListOversold.Sort();

        settings.BlackListOverbought = textBoxBlackListOverbought.Text.Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.WhiteListOverbought.Sort();

        settings.WhiteListOverbought = textBoxWhiteListOverbought.Text.Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.WhiteListOverbought.Sort();

#if TRADEBOT
        // --------------------------------------------------------------------------------
        // Trade bot
        // --------------------------------------------------------------------------------
        settings.Trading.TradeViaExchange = EditTradeViaExchange.Checked;
        settings.Trading.TradeViaPaperTrading = EditTradeViaPaperTrading.Checked;
        settings.Trading.DisableNewPositions = EditDisableNewPositions.Checked;
        settings.General.SoundTradeNotification = EditSoundTradeNotification.Checked;

        // Logging
        settings.Trading.LogCanceledOrders = EditLogCanceledOrders.Checked;

        // Api
        GlobalData.TradingApi.Key = EditApiKey.Text;
        GlobalData.TradingApi.Secret = EditApiSecret.Text;
        settings.Trading.GlobalBuyVarying = EditGlobalBuyVarying.Value;
        settings.Trading.GlobalBuyCooldownTime = (int)EditGlobalBuyCooldownTime.Value;

        // slots
        settings.Trading.SlotsMaximalLong = (int)EditSlotsMaximalLong.Value;
        settings.Trading.SlotsMaximalShort = (int)EditSlotsMaximalShort.Value;

        // Instap
        settings.Trading.CheckIncreasingRsi = EditCheckIncreasingRsi.Checked;
        settings.Trading.CheckIncreasingMacd = EditCheckIncreasingMacd.Checked;
        settings.Trading.CheckIncreasingStoch = EditCheckIncreasingStoch.Checked;


        UserControlTradeBuy.SaveConfig(settings.Trading);
        UserControlTradeDca.SaveConfig(settings.Trading);
        UserControlTradeSell.SaveConfig(settings.Trading);

        // Stop loss
        //settings.Trading.GlobalStopPercentage = EditGlobalStopPercentage.Value;
        //settings.Trading.GlobalStopLimitPercentage = EditGlobalStopLimitPercentage.Value;

        settings.Trading.Leverage = EditLeverage.Value;
        settings.Trading.CrossOrIsolated = EditCrossOrIsolated.SelectedIndex;


        UserControlTradingLong.SaveConfig(settings.Trading.Long);
        UserControlTradingShort.SaveConfig(settings.Trading.Short);
        UserControlTradeRules.SaveConfig(settings.Trading);
#endif

        // ------------------------------------------------------------------------------
        // Balance bot
        // ------------------------------------------------------------------------------
#if BALANCING
        //settings.BalanceBot.Active = EditBlanceBotActive.Checked;
        //settings.BalanceBot.StartAmount = numericStartAmount.Value;
        //settings.BalanceBot.IntervalPeriod = (int)EditIntervalPeriod.Value;
        //settings.BalanceBot.ShowAdviceOnly = EditShowAdviceOnly.Checked;
        //settings.BalanceBot.MinimalBuyBarometer = EditMinimalBuyBarometer.Value;
        //settings.BalanceBot.MinimalSellBarometer = EditMinimalSellBarometer.Value;
        //settings.BalanceBot.BuyThresholdPercentage = EditBuyBalanceThreshold.Value;
        //settings.BalanceBot.SellThresholdPercentage = EditSellBalanceThreshold.Value;
#endif

        DialogResult = DialogResult.OK;
    }

    private void ButtonTestSpeech_Click(object sender, EventArgs e) =>
        GlobalData.PlaySomeSpeech("Found a signal for BTCUSDT interval 1m (it is going to the moon)", true);

    private void ButtonReset_Click(object sender, EventArgs e)
    {
        if (MessageBox.Show("Alle instellingen resetten?", "Attentie!", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            GlobalData.Settings = new();
            GlobalData.DefaultSettings();
            InitSettings(GlobalData.Settings);
        }
    }

    private void ButtonFontDialog_Click(object sender, EventArgs e)
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

    private void ButtonGotoAppDataFolder_Click(object sender, EventArgs e)
    {
        string folder = GlobalData.GetBaseDir();
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder) { UseShellExecute = true });
    }

}
