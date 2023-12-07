using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;
using CryptoSbmScanner.Signal;
using Microsoft.IdentityModel.Tokens;
using CryptoSbmScanner.Trader;
using CryptoSbmScanner.SettingsDialog;

namespace CryptoSbmScanner;

public partial class FrmSettings : Form
{
    private SettingsBasic settings;

    private readonly List<SettingsQuoteCoin> BaseCoinList = new();

    // Welke intervallen willen we analyseren (create signals)
    private readonly Dictionary<Control, CryptoInterval> AnalyzeInterval = new();

    // 
    private readonly SortedList<CryptoSignalStrategy, SettingsStrategy> StrategyControlsLong = new();
    private readonly SortedList<CryptoSignalStrategy, SettingsStrategy> StrategyControlsShort = new();

    // Strategy definitions
    private readonly Dictionary<Control, AlgorithmDefinition> AnalyzeDefinitionIndexLong = new();
    private readonly Dictionary<Control, AlgorithmDefinition> AnalyzeDefinitionIndexShort = new();

    // Trade op interval
    private readonly Dictionary<Control, CryptoInterval> TradingIntervalLong = new();
    private readonly Dictionary<Control, CryptoInterval> TradingIntervalShort = new();

    // Gewenste trend op interval
    private readonly Dictionary<Control, CryptoInterval> TrendLongControls = new();
    private readonly Dictionary<Control, CryptoInterval> TrendShortControls = new();

    public Model.CryptoExchange NewExchange { get; set; }

#if TRADEBOT
    private readonly SortedList<string, CryptoStepInMethod> BuyStepInMethod = new();
    private readonly SortedList<string, CryptoStepInMethod> DcaStepInMethod = new();
    private readonly SortedList<string, CryptoBuyOrderMethod> BuyOrderMethod = new();
    private readonly SortedList<string, CryptoSellMethod> SellMethod = new();
#endif


    public FrmSettings()
    {
        InitializeComponent();

        toolTip1.SetToolTip(EditAnalyzeStobbShort, "Dit type signaal is een dubbele indicatie dat een munt overbought is en die bestaat uit:" +
            "\n-een candle die opent of sluit boven de bovenste bollingerband\n" +
            "-zowel de %d als %k van de stochastic zijn boven de 80\n" +
            "(dit kan een instapmoment zijn voor een short positie)");
        toolTip1.SetToolTip(EditAnalyzeStobbLong, "Dit type signaal is een dubbele indicatie dat een munt oversold is en bestaat uit:\n" +
            "-een candle die opent of sluit onder de onderste bollingerbands\n" +
            "-zowel de % d als % k van de stochastic zijn onder de 20\n" +
            "(dit kan een instapmoment zijn voor een long positie).");

        toolTip1.SetToolTip(EditAnalyzeSbm1Short, "Dit is een variatie op de stobb overbought signaal en bestaat uit:\n" +
            "-een stobb overbought signaal\n" +
            "-de ma200 onder de ma50 is\n" +
            "-de ma50 onder de ma20 is\n" +
            "-de psar op of boven de ma20\n" +
            "(dit kan een instapmoment zijn voor een short positie)");
        toolTip1.SetToolTip(EditAnalyzeSbm1Long, "Dit is een variatie op de stobb oversold signaal en bestaat uit:\n" +
            "-een stobb oversold signaal\n" +
            "-de ma200 boven de ma50 is\n" +
            "-de ma50 boven de ma20 is\n" +
            "-de psar op of onder de ma20\n" +
            "(dit kan een instapmoment zijn voor een long positie)");

        toolTip1.SetToolTip(EditAnalyzeCandleJumpUp, "Een signaal dat een munt een bepaald percentage naar boven \"spingt\" (info)");
        toolTip1.SetToolTip(EditAnalyzeCandleJumpDown, "Een signaal dat een munt een bepaald percentage naar beneden \"spingt\"(info)");
        toolTip1.SetToolTip(EditAnalysisCandleJumpPercentage, "Percentage dat de munt naar boven of beneden moet bewegen");


        // VS-Designer removes events (after cut/paste)
        EditPlaySoundSbmSignal.Click += SetGrayed;
        EditPlaySoundStobbSignal.Click += SetGrayed;
        EditPlaySoundCandleJumpSignal.Click += SetGrayed;

        buttonReset.Click += ButtonReset_Click;
        buttonTestSpeech.Click += ButtonTestSpeech_Click;
        buttonFontDialog.Click += ButtonFontDialog_Click;
        buttonGotoAppDataFolder.Click += ButtonGotoAppDataFolder_Click;

        buttonColorStobbShort.Tag = panelColorStobbShort;
        buttonColorStobbShort.Click += ButtonColorClick;
        buttonColorStobbLong.Tag = panelColorStobbLong;
        buttonColorStobbLong.Click += ButtonColorClick;

        buttonColorSbmShort.Tag = panelColorSbmShort;
        buttonColorSbmShort.Click += ButtonColorClick;
        buttonColorSbmLong.Tag = panelColorSbmLong;
        buttonColorSbmLong.Click += ButtonColorClick;

        buttonColorJumpShort.Tag = panelColorJumpShort;
        buttonColorJumpShort.Click += ButtonColorClick;
        buttonColorJumpLong.Tag = panelColorJumpLong;
        buttonColorJumpLong.Click += ButtonColorClick;

        buttonSelectSoundStobbOverbought.Click += ButtonSelectSoundStobbOverbought_Click;
        buttonSelectSoundStobbOversold.Click += ButtonSelectSoundStobbOversold_Click;
        buttonSelectSoundSbmOverbought.Click += ButtonSelectSoundSbmOverbought_Click;
        buttonSelectSoundSbmOversold.Click += ButtonSelectSoundSbmOversold_Click;
        buttonSelectSoundCandleJumpUp.Click += ButtonSelectSoundCandleJumpUp_Click;
        buttonSelectSoundCandleJumpDown.Click += ButtonSelectSoundCandleJumpDown_Click;

        buttonPlaySoundStobbOverbought.Click += ButtonPlaySoundStobbOverbought_Click;
        buttonPlaySoundStobbOversold.Click += ButtonPlaySoundStobbOversold_Click;
        buttonPlaySoundSbmOverbought.Click += ButtonPlaySoundSbmOverbought_Click;
        buttonPlaySoundSbmOversold.Click += ButtonPlaySoundSbmOversold_Click;
        buttonPlaySoundCandleJumpUp.Click += ButtonPlaySoundCandleJumpUp_Click;
        buttonPlaySoundCandleJumpDown.Click += ButtonPlaySoundCandleJumpDown_Click;


        // Deze moeten op het formulier komen te staan (om de edits te kunnen refereren/chainen)
        int yPos = 40;
        foreach (var signalDefinition in SignalHelper.AlgorithmDefinitionIndex.Values)
            StrategyControlsLong.Add(signalDefinition.Strategy, new SettingsStrategy(GlobalData.Settings.Trading.Long, signalDefinition, 910, yPos += 30, tabPageTradingLong.Controls));
        StrategyControlsLong.Values[0].AddHeaderLabelsMain(20, tabPageTradingLong.Controls);
        StrategyControlsLong.Values[0].AddHeaderLabels(40, tabPageTradingLong.Controls);

        yPos = 40;
        foreach (var signalDefinition in SignalHelper.AlgorithmDefinitionIndex.Values)
            StrategyControlsShort.Add(signalDefinition.Strategy, new SettingsStrategy(GlobalData.Settings.Trading.Short, signalDefinition, 910, yPos += 30, tabPageTradingShort.Controls));
        StrategyControlsShort.Values[0].AddHeaderLabelsMain(20, tabPageTradingShort.Controls);
        StrategyControlsShort.Values[0].AddHeaderLabels(40, tabPageTradingShort.Controls);

        // analyze interval
        AnalyzeInterval.Add(EditAnalyzeInterval1m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m]);
        AnalyzeInterval.Add(EditAnalyzeInterval2m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2m]);
        AnalyzeInterval.Add(EditAnalyzeInterval3m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval3m]);
        AnalyzeInterval.Add(EditAnalyzeInterval5m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m]);
        AnalyzeInterval.Add(EditAnalyzeInterval10m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m]);
        AnalyzeInterval.Add(EditAnalyzeInterval15m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m]);
        AnalyzeInterval.Add(EditAnalyzeInterval30m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval30m]);
        AnalyzeInterval.Add(EditAnalyzeInterval1h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h]);
        AnalyzeInterval.Add(EditAnalyzeInterval2h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2h]);
        AnalyzeInterval.Add(EditAnalyzeInterval4h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval4h]);
        AnalyzeInterval.Add(EditAnalyzeInterval6h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval6h]);
        AnalyzeInterval.Add(EditAnalyzeInterval8h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval8h]);
        AnalyzeInterval.Add(EditAnalyzeInterval12h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval12h]);
        AnalyzeInterval.Add(EditAnalyzeInterval1d, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d]);

        // analyze long
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeSbm1Long, SignalHelper.AlgorithmDefinitionIndex[CryptoSignalStrategy.Sbm1]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeSbm2Long, SignalHelper.AlgorithmDefinitionIndex[CryptoSignalStrategy.Sbm2]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeSbm3Long, SignalHelper.AlgorithmDefinitionIndex[CryptoSignalStrategy.Sbm3]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeStobbLong, SignalHelper.AlgorithmDefinitionIndex[CryptoSignalStrategy.Stobb]);
        AnalyzeDefinitionIndexLong.Add(EditAnalyzeCandleJumpUp, SignalHelper.AlgorithmDefinitionIndex[CryptoSignalStrategy.Jump]);

        // analyze short
        AnalyzeDefinitionIndexShort.Add(EditAnalyzeSbm1Short, SignalHelper.AlgorithmDefinitionIndex[CryptoSignalStrategy.Sbm1]);
        AnalyzeDefinitionIndexShort.Add(EditAnalyzeSbm2Short, SignalHelper.AlgorithmDefinitionIndex[CryptoSignalStrategy.Sbm2]);
        AnalyzeDefinitionIndexShort.Add(EditAnalyzeSbm3Short, SignalHelper.AlgorithmDefinitionIndex[CryptoSignalStrategy.Sbm3]);
        AnalyzeDefinitionIndexShort.Add(EditAnalyzeStobbShort, SignalHelper.AlgorithmDefinitionIndex[CryptoSignalStrategy.Stobb]);
        AnalyzeDefinitionIndexShort.Add(EditAnalyzeCandleJumpDown, SignalHelper.AlgorithmDefinitionIndex[CryptoSignalStrategy.Jump]);

        // Vanwege dubbele checkboxes op meerdere tabbladen
        StrategyControlsLong[CryptoSignalStrategy.Sbm1].ChainTo(CryptoOrderSide.Buy, EditAnalyzeSbm1Long);
        StrategyControlsLong[CryptoSignalStrategy.Sbm2].ChainTo(CryptoOrderSide.Buy, EditAnalyzeSbm2Long);
        StrategyControlsLong[CryptoSignalStrategy.Sbm3].ChainTo(CryptoOrderSide.Buy, EditAnalyzeSbm3Long);
        StrategyControlsLong[CryptoSignalStrategy.Stobb].ChainTo(CryptoOrderSide.Buy, EditAnalyzeStobbLong);
        StrategyControlsLong[CryptoSignalStrategy.Jump].ChainTo(CryptoOrderSide.Buy, EditAnalyzeCandleJumpUp);

        StrategyControlsShort[CryptoSignalStrategy.Sbm1].ChainTo(CryptoOrderSide.Sell, EditAnalyzeSbm1Short);
        StrategyControlsShort[CryptoSignalStrategy.Sbm2].ChainTo(CryptoOrderSide.Sell, EditAnalyzeSbm2Short);
        StrategyControlsShort[CryptoSignalStrategy.Sbm3].ChainTo(CryptoOrderSide.Sell, EditAnalyzeSbm3Short);
        StrategyControlsShort[CryptoSignalStrategy.Stobb].ChainTo(CryptoOrderSide.Sell, EditAnalyzeStobbShort);
        StrategyControlsShort[CryptoSignalStrategy.Jump].ChainTo(CryptoOrderSide.Sell, EditAnalyzeCandleJumpDown);

        // Trading interval long
        TradingIntervalLong.Add(EditTradingIntervalLong1m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m]);
        TradingIntervalLong.Add(EditTradingIntervalLong2m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2m]);
        TradingIntervalLong.Add(EditTradingIntervalLong3m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval3m]);
        TradingIntervalLong.Add(EditTradingIntervalLong5m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m]);
        TradingIntervalLong.Add(EditTradingIntervalLong10m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m]);
        TradingIntervalLong.Add(EditTradingIntervalLong15m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m]);
        TradingIntervalLong.Add(EditTradingIntervalLong30m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval30m]);
        TradingIntervalLong.Add(EditTradingIntervalLong1h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h]);
        TradingIntervalLong.Add(EditTradingIntervalLong2h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2h]);
        TradingIntervalLong.Add(EditTradingIntervalLong4h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval4h]);

        // Trading interval short
        TradingIntervalShort.Add(EditTradingIntervalShort1m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m]);
        TradingIntervalShort.Add(EditTradingIntervalShort2m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2m]);
        TradingIntervalShort.Add(EditTradingIntervalShort3m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval3m]);
        TradingIntervalShort.Add(EditTradingIntervalShort5m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m]);
        TradingIntervalShort.Add(EditTradingIntervalShort10m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m]);
        TradingIntervalShort.Add(EditTradingIntervalShort15m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m]);
        TradingIntervalShort.Add(EditTradingIntervalShort30m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval30m]);
        TradingIntervalShort.Add(EditTradingIntervalShort1h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h]);
        TradingIntervalShort.Add(EditTradingIntervalShort2h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2h]);
        TradingIntervalShort.Add(EditTradingIntervalShort4h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval4h]);

#if TRADEBOT

        // BUY
        BuyStepInMethod.Add("Na een signaal", CryptoStepInMethod.AfterNextSignal);
        //BuyStepInMethod.Add("Trace via de Keltner Channel en PSAR", CryptoBuyStepInMethod.TrailViaKcPsar);

        // DCA
        //DcaStepInMethod.Add("Direct na het signaal", CryptoBuyStepInMethod.Immediately);
        DcaStepInMethod.Add("Op het opgegeven percentage", CryptoStepInMethod.FixedPercentage);
        DcaStepInMethod.Add("Na een signaal (sbm/stobb/enz)", CryptoStepInMethod.AfterNextSignal);
        DcaStepInMethod.Add("Trace via de Keltner Channel en PSAR", CryptoStepInMethod.TrailViaKcPsar);

        // SELL
        SellMethod.Add("Limit order op een vaste winst percentage", CryptoSellMethod.FixedPercentage);
        //SellMethod.Add("Limit order op dynamisch percentage van de BB", CryptoSellMethod.DynamicPercentage);
        SellMethod.Add("Trace via de Keltner Channel en PSAR", CryptoSellMethod.TrailViaKcPsar);

        // BUY/DCA - Manier van kopen
        BuyOrderMethod.Add("Market order", CryptoBuyOrderMethod.MarketOrder);
        BuyOrderMethod.Add("Limit order signaal prijs", CryptoBuyOrderMethod.SignalPrice);
        BuyOrderMethod.Add("Limit order op bied prijs", CryptoBuyOrderMethod.BidPrice);
        BuyOrderMethod.Add("Limit order op vraag prijs", CryptoBuyOrderMethod.AskPrice);
        BuyOrderMethod.Add("Limit order op gemiddelde van bied en vraag prijs", CryptoBuyOrderMethod.BidAndAskPriceAvg);


        // Trading trend long
        TrendLongControls.Add(EditTrendLong1m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m]);
        TrendLongControls.Add(EditTrendLong2m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2m]);
        TrendLongControls.Add(EditTrendLong3m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval3m]);
        TrendLongControls.Add(EditTrendLong5m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m]);
        TrendLongControls.Add(EditTrendLong10m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m]);
        TrendLongControls.Add(EditTrendLong15m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m]);
        TrendLongControls.Add(EditTrendLong30m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval30m]);
        TrendLongControls.Add(EditTrendLong1h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h]);
        TrendLongControls.Add(EditTrendLong2h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2h]);
        TrendLongControls.Add(EditTrendLong4h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval4h]);
        TrendLongControls.Add(EditTrendLong6h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval6h]);
        TrendLongControls.Add(EditTrendLong8h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval8h]);
        TrendLongControls.Add(EditTrendLong12h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval12h]);
        TrendLongControls.Add(EditTrendLong1d, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d]);

        // Trading trend short
        TrendShortControls.Add(EditTrendShort1m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m]);
        TrendShortControls.Add(EditTrendShort2m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2m]);
        TrendShortControls.Add(EditTrendShort3m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval3m]);
        TrendShortControls.Add(EditTrendShort5m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m]);
        TrendShortControls.Add(EditTrendShort10m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m]);
        TrendShortControls.Add(EditTrendShort15m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m]);
        TrendShortControls.Add(EditTrendShort30m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval30m]);
        TrendShortControls.Add(EditTrendShort1h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h]);
        TrendShortControls.Add(EditTrendShort2h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2h]);
        TrendShortControls.Add(EditTrendShort4h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval4h]);
        TrendShortControls.Add(EditTrendShort6h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval6h]);
        TrendShortControls.Add(EditTrendShort8h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval8h]);
        TrendShortControls.Add(EditTrendShort12h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval12h]);
        TrendShortControls.Add(EditTrendShort1d, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d]);
#endif
    }


    private static void SetCheckBoxFrom(Control control, object obj, List<string> text)
    {
        // probleem met de 5m en 15m, daarom geprefixed
        if (control is CheckBox checkBox)
        {
            if (obj is CryptoInterval interval)
                checkBox.Checked = text.Contains(interval.Name);
            if (obj is AlgorithmDefinition definition)
                checkBox.Checked = text.Contains(definition.Name);
        }
    }

    private static void GetValueFromCheckBox(Control control, object obj, List<string> text)
    {
        // probleem met de 5m en 15m, daarom geprefixed
        if (control is CheckBox checkBox && checkBox.Checked)
        {
            if (obj is CryptoInterval interval)
                text.Add(interval.Name);
            if (obj is AlgorithmDefinition definition)
                text.Add(definition.Name);
        }
    }


    private void SetGrayed(object sender, EventArgs e)
    {
        // Stobb
        EditSoundStobbOverbought.Enabled = EditPlaySoundStobbSignal.Checked;
        buttonPlaySoundStobbOverbought.Enabled = EditPlaySoundStobbSignal.Checked;
        buttonSelectSoundStobbOverbought.Enabled = EditPlaySoundStobbSignal.Checked;
        buttonPlaySoundStobbOverbought.Enabled = EditPlaySoundStobbSignal.Checked;

        EditSoundStobbOversold.Enabled = EditPlaySoundStobbSignal.Checked;
        buttonPlaySoundStobbOversold.Enabled = EditPlaySoundStobbSignal.Checked;
        buttonSelectSoundStobbOversold.Enabled = EditPlaySoundStobbSignal.Checked;
        buttonPlaySoundStobbOversold.Enabled = EditPlaySoundStobbSignal.Checked;

        // Sbm
        EditSoundFileSbmOverbought.Enabled = EditPlaySoundSbmSignal.Checked;
        buttonPlaySoundSbmOverbought.Enabled = EditPlaySoundSbmSignal.Checked;
        buttonSelectSoundSbmOverbought.Enabled = EditPlaySoundSbmSignal.Checked;
        buttonPlaySoundSbmOverbought.Enabled = EditPlaySoundSbmSignal.Checked;

        EditSoundFileSbmOversold.Enabled = EditPlaySoundSbmSignal.Checked;
        buttonPlaySoundSbmOversold.Enabled = EditPlaySoundSbmSignal.Checked;
        buttonSelectSoundSbmOversold.Enabled = EditPlaySoundSbmSignal.Checked;
        buttonPlaySoundSbmOversold.Enabled = EditPlaySoundSbmSignal.Checked;

        // Candle jump UP
        EditSoundFileCandleJumpUp.Enabled = EditPlaySoundCandleJumpSignal.Checked;
        buttonPlaySoundCandleJumpUp.Enabled = EditPlaySoundCandleJumpSignal.Checked;
        buttonSelectSoundCandleJumpUp.Enabled = EditPlaySoundCandleJumpSignal.Checked;
        buttonPlaySoundCandleJumpUp.Enabled = EditPlaySoundCandleJumpSignal.Checked;

        // Candle jump Down
        EditSoundFileCandleJumpDown.Enabled = EditPlaySoundCandleJumpSignal.Checked;
        buttonPlaySoundCandleJumpDown.Enabled = EditPlaySoundCandleJumpSignal.Checked;
        buttonSelectSoundCandleJumpDown.Enabled = EditPlaySoundCandleJumpSignal.Checked;
        buttonPlaySoundCandleJumpDown.Enabled = EditPlaySoundCandleJumpSignal.Checked;
    }


    public void InitSettings(SettingsBasic settings)
    {
        this.settings = settings;

#if !TRADEBOT
        // Oppassen: Een tabPage.Visible=false doet niets
        tabPageTrading.Parent = null;
        settings.Trading.Active = false;
        tabControl.TabPages.Remove(tabPageTrading);
        tabControlLong.TabPages.Remove(tabPageTradingLong);
        tabControlShort.TabPages.Remove(tabPageTradingShort);
#endif

        // Deze worden na de overgang naar .net 7 regelmatig gereset naar 0
        // Benieuwd waarom dit gebeurd (het zijn er gelukkig niet zo veel)
        EditGlobalBuyVarying.Minimum = -0.5m;

        EditBarometer1hMinimal.Minimum = -100;
        EditBarometer15mBotLong.Minimum = -100;
        EditBarometer30mBotLong.Minimum = -100;
        EditBarometer1hBotLong.Minimum = -100;
        EditBarometer4hBotLong.Minimum = -100;
        EditBarometer24hBotLong.Minimum = -100;

        EditBarometer15mBotShort.Minimum = -100;
        EditBarometer30mBotShort.Minimum = -100;
        EditBarometer1hBotShort.Minimum = -100;
        EditBarometer4hBotShort.Minimum = -100;
        EditBarometer24hBotShort.Minimum = -100;

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


        // ------------------------------------------------------------------------------
        // Telegram
        // ------------------------------------------------------------------------------
        EditTelegramToken.Text = settings.Telegram.Token;
        EditTelegramChatId.Text = settings.Telegram.ChatId;
        EditSendSignalsToTelegram.Checked = settings.Telegram.SendSignalsToTelegram;


        // ------------------------------------------------------------------------------
        // Base coins
        // ------------------------------------------------------------------------------

        int yPos = 40;
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.SymbolList.Count > 5 || quoteData.Name.Equals("BTC") || quoteData.Name.Equals("USDT"))
                BaseCoinList.Add(new SettingsQuoteCoin(quoteData, yPos += 26, tabBasismunten.Controls));
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
        foreach (var item in AnalyzeInterval)
            SetCheckBoxFrom(item.Key, item.Value, settings.Signal.Long.Interval);

        EditAnalysisMinChangePercentage.Value = (decimal)settings.Signal.AnalysisMinChangePercentage;
        EditAnalysisMaxChangePercentage.Value = (decimal)settings.Signal.AnalysisMaxChangePercentage;
        EditLogAnalysisMinMaxChangePercentage.Checked = settings.Signal.LogAnalysisMinMaxChangePercentage;

        EditAnalysisMinEffectivePercentage.Value = (decimal)settings.Signal.AnalysisMinEffectivePercentage;
        EditAnalysisMaxEffectivePercentage.Value = (decimal)settings.Signal.AnalysisMaxEffectivePercentage;
        EditLogAnalysisMinMaxEffectivePercentage.Checked = settings.Signal.LogAnalysisMinMaxEffectivePercentage;

        EditAnalysisMinEffective10DaysPercentage.Value = (decimal)settings.Signal.AnalysisMinEffective10DaysPercentage;
        EditAnalysisMaxEffective10DaysPercentage.Value = (decimal)settings.Signal.AnalysisMaxEffective10DaysPercentage;
        EditLogAnalysisMinMaxEffective10DaysPercentage.Checked = settings.Signal.LogAnalysisMinMaxEffective10DaysPercentage;

        EditBarometer1hMinimal.Value = settings.Signal.Barometer1hMinimal;
        EditLogBarometerToLow.Checked = settings.Signal.LogBarometerToLow;

        EditSymbolMustExistsDays.Value = settings.Signal.SymbolMustExistsDays;
        EditLogSymbolMustExistsDays.Checked = settings.Signal.LogSymbolMustExistsDays;

        EditMinimumTickPercentage.Value = settings.Signal.MinimumTickPercentage;
        EditLogMinimumTickPercentage.Checked = settings.Signal.LogMinimumTickPercentage;

        // ------------------------------------------------------------------------------
        // Signal types
        // ------------------------------------------------------------------------------

        foreach (var item in AnalyzeDefinitionIndexLong)
            SetCheckBoxFrom(item.Key, item.Value, settings.Signal.Long.Strategy);
        foreach (var item in AnalyzeDefinitionIndexShort)
            SetCheckBoxFrom(item.Key, item.Value, settings.Signal.Long.Strategy);

        // STOBB
        EditStobbBBMinPercentage.Value = (decimal)settings.Signal.StobbBBMinPercentage;
        EditStobbBBMaxPercentage.Value = (decimal)settings.Signal.StobbBBMaxPercentage;
        EditStobbUseLowHigh.Checked = settings.Signal.StobbUseLowHigh;
        EditPlaySoundStobbSignal.Checked = settings.Signal.PlaySoundStobbSignal;
        EditPlaySpeechStobbSignal.Checked = settings.Signal.PlaySpeechStobbSignal;
        EditSoundStobbOverbought.Text = settings.Signal.SoundStobbOverbought;
        EditSoundStobbOversold.Text = settings.Signal.SoundStobbOversold;
        EditStobIncludeRsi.Checked = settings.Signal.StobIncludeRsi;
        EditStobIncludeSbmMaLines.Checked = settings.Signal.StobIncludeSoftSbm;
        EditStobIncludeSbmPercAndCrossing.Checked = settings.Signal.StobIncludeSbmPercAndCrossing;
        panelColorStobbLong.BackColor = settings.Signal.ColorStobb;
        panelColorStobbShort.BackColor = settings.Signal.ColorStobbShort;
        EditStobTrendLong.Value = settings.Signal.StobTrendLong;
        EditStobTrendShort.Value = settings.Signal.StobTrendShort;

        // SBM 1
        EditSbmBBMinPercentage.Value = (decimal)settings.Signal.SbmBBMinPercentage;
        EditSbmBBMaxPercentage.Value = (decimal)settings.Signal.SbmBBMaxPercentage;
        EditSbmUseLowHigh.Checked = settings.Signal.SbmUseLowHigh;
        EditPlaySoundSbmSignal.Checked = settings.Signal.PlaySoundSbmSignal;
        EditPlaySpeechSbmSignal.Checked = settings.Signal.PlaySpeechSbmSignal;
        EditSoundFileSbmOverbought.Text = settings.Signal.SoundSbmOverbought;
        EditSoundFileSbmOversold.Text = settings.Signal.SoundSbmOversold;
        panelColorSbmLong.BackColor = settings.Signal.ColorSbm;
        panelColorSbmShort.BackColor = settings.Signal.ColorSbmShort;
        EditSbm1CandlesLookbackCount.Value = settings.Signal.Sbm1CandlesLookbackCount;

        // JUMP
        EditPlaySoundCandleJumpSignal.Checked = settings.Signal.PlaySoundCandleJumpSignal;
        EditPlaySpeechCandleJumpSignal.Checked = settings.Signal.PlaySpeechCandleJumpSignal;
        EditSoundFileCandleJumpDown.Text = settings.Signal.SoundCandleJumpDown;
        EditSoundFileCandleJumpUp.Text = settings.Signal.SoundCandleJumpUp;
        EditAnalysisCandleJumpPercentage.Value = settings.Signal.AnalysisCandleJumpPercentage;
        EditJumpCandlesLookbackCount.Value = settings.Signal.JumpCandlesLookbackCount;
        EditJumpUseLowHighCalculation.Checked = settings.Signal.JumpUseLowHighCalculation;
        panelColorJumpLong.BackColor = settings.Signal.ColorJump;
        panelColorJumpShort.BackColor = settings.Signal.ColorJumpShort;

        // SBM 2
        EditSbm2CandlesLookbackCount.Value = settings.Signal.Sbm2CandlesLookbackCount;
        EditSbm2BbPercentage.Value = settings.Signal.Sbm2BbPercentage;
        EditSbm2UseLowHigh.Checked = settings.Signal.Sbm2UseLowHigh;


        // SBM 3
        EditSbm3CandlesForBBRecovery.Value = settings.Signal.Sbm3CandlesLookbackCount;
        EditSbm3CandlesForBBRecoveryPercentage.Value = settings.Signal.Sbm3CandlesBbRecoveryPercentage;

        // SBM aanvullend
        EditSbmCandlesForMacdRecovery.Value = settings.Signal.SbmCandlesForMacdRecovery;

        EditSbmMa200AndMa50Percentage.Value = settings.Signal.SbmMa200AndMa50Percentage;
        EditSbmMa50AndMa20Percentage.Value = settings.Signal.SbmMa50AndMa20Percentage;
        EditSbmMa200AndMa20Percentage.Value = settings.Signal.SbmMa200AndMa20Percentage;

        EditSbmMa200AndMa20Crossing.Checked = settings.Signal.SbmMa200AndMa20Crossing;
        EditSbmMa200AndMa20Lookback.Value = settings.Signal.SbmMa200AndMa20Lookback;
        EditSbmMa200AndMa50Crossing.Checked = settings.Signal.SbmMa200AndMa50Crossing;
        EditSbmMa200AndMa50Lookback.Value = settings.Signal.SbmMa200AndMa50Lookback;
        EditSbmMa50AndMa20Crossing.Checked = settings.Signal.SbmMa50AndMa20Crossing;
        EditSbmMa50AndMa20Lookback.Value = settings.Signal.SbmMa50AndMa20Lookback;


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

        // ------------------------------------------------------------------------------
        // Trading
        // ------------------------------------------------------------------------------
#if TRADEBOT

        // Hoe gaan we traden
        EditTradeViaExchange.Checked = settings.Trading.TradeViaExchange;
        EditTradeViaPaperTrading.Checked = settings.Trading.TradeViaPaperTrading;
        EditDisableNewPositions.Checked = settings.Trading.DisableNewPositions;
        EditSoundTradeNotification.Checked = settings.General.SoundTradeNotification;

        // Logging
        EditLogCanceledOrders.Checked = settings.Trading.LogCanceledOrders;

        // api
        EditApiKey.Text = settings.Trading.ApiKey;
        EditApiSecret.Text = settings.Trading.ApiSecret;

        // slots
        EditSlotsMaximalExchange.Value = settings.Trading.SlotsMaximalExchange;
        EditSlotsMaximalSymbol.Value = settings.Trading.SlotsMaximalSymbol;
        EditSlotsMaximalBase.Value = settings.Trading.SlotsMaximalBase;

        // Instap
        EditCheckIncreasingRsi.Checked = settings.Trading.CheckIncreasingRsi;
        EditCheckIncreasingMacd.Checked = settings.Trading.CheckIncreasingMacd;
        EditCheckIncreasingStoch.Checked = settings.Trading.CheckIncreasingStoch;


        // Buy
        EditBuyStepInMethod.DataSource = new BindingSource(BuyStepInMethod, null);
        EditBuyStepInMethod.DisplayMember = "Key";
        EditBuyStepInMethod.ValueMember = "Value";
        EditBuyStepInMethod.SelectedValue = settings.Trading.BuyStepInMethod;
        EditBuyOrderMethod.DataSource = new BindingSource(BuyOrderMethod, null); // DCA opties zijn gelijk aan de BUY variant
        EditBuyOrderMethod.DisplayMember = "Key";
        EditBuyOrderMethod.ValueMember = "Value";
        EditBuyOrderMethod.SelectedValue = settings.Trading.BuyOrderMethod;
        EditGlobalBuyRemoveTime.Value = settings.Trading.GlobalBuyRemoveTime;
        EditGlobalBuyVarying.Value = settings.Trading.GlobalBuyVarying;

        // dca
        EditDcaStepInMethod.DataSource = new BindingSource(DcaStepInMethod, null);
        EditDcaStepInMethod.DisplayMember = "Key";
        EditDcaStepInMethod.ValueMember = "Value";
        EditDcaStepInMethod.SelectedValue = settings.Trading.DcaStepInMethod;
        EditDcaOrderMethod.DataSource = new BindingSource(BuyOrderMethod, null); // DCA opties zijn gelijk aan de BUY variant
        EditDcaOrderMethod.DisplayMember = "Key";
        EditDcaOrderMethod.ValueMember = "Value";
        EditDcaOrderMethod.SelectedValue = settings.Trading.DcaOrderMethod;
        EditDcaPercentage.Value = Math.Abs(settings.Trading.DcaPercentage);
        EditDcaFactor.Value = settings.Trading.DcaFactor;
        EditDcaCount.Value = settings.Trading.DcaCount;
        EditGlobalBuyCooldownTime.Value = settings.Trading.GlobalBuyCooldownTime;

        // sell
        EditSellMethod.DataSource = new BindingSource(SellMethod, null);
        EditSellMethod.DisplayMember = "Key";
        EditSellMethod.ValueMember = "Value";
        EditSellMethod.SelectedValue = settings.Trading.SellMethod;
        EditProfitPercentage.Value = settings.Trading.ProfitPercentage;
        EditLockProfits.Checked = settings.Trading.LockProfits;

        // Stop loss
        EditGlobalStopPercentage.Value = Math.Abs(settings.Trading.GlobalStopPercentage);
        EditGlobalStopLimitPercentage.Value = Math.Abs(settings.Trading.GlobalStopLimitPercentage);



        EditLeverage.Value = settings.Trading.Leverage;
        EditCrossOrIsolated.SelectedIndex = settings.Trading.CrossOrIsolated;

        // --------------------------------------------------------------------------------
        // Trade bot
        // --------------------------------------------------------------------------------
        // interval long
        foreach (var item in TradingIntervalLong)
            SetCheckBoxFrom(item.Key, item.Value, settings.Trading.Long.Interval);

        // strategy long
        foreach (var strategy in StrategyControlsLong.Values)
            strategy.SetControlValues();

        // trend long
        foreach (var item in TrendLongControls)
            SetCheckBoxFrom(item.Key, item.Value, settings.Trading.Long.Trend);

        // Barometer long
        if (settings.Trading.Long.Barometer.TryGetValue("15m", out var value))
            EditBarometer15mBotLong.Value = value;
        else
            EditBarometer15mBotLong.Value = -99;
        if (settings.Trading.Long.Barometer.TryGetValue("30m", out value))
            EditBarometer30mBotLong.Value = value;
        else
            EditBarometer30mBotLong.Value = -99;
        if (settings.Trading.Long.Barometer.TryGetValue("1h", out value))
            EditBarometer1hBotLong.Value = value;
        else
            EditBarometer1hBotLong.Value = -99;
        if (settings.Trading.Long.Barometer.TryGetValue("4h", out value))
            EditBarometer4hBotLong.Value = value;
        else
            EditBarometer4hBotLong.Value = -99;
        if (settings.Trading.Long.Barometer.TryGetValue("24h", out value))
            EditBarometer24hBotLong.Value = value;
        else
            EditBarometer24hBotLong.Value = -99;


        // interval short
        foreach (var item in TradingIntervalShort)
            SetCheckBoxFrom(item.Key, item.Value, settings.Trading.Short.Interval);

        // strategy short
        foreach (var strategy in StrategyControlsShort.Values)
            strategy.SetControlValues();

        // trend short
        foreach (var item in TrendShortControls)
            SetCheckBoxFrom(item.Key, item.Value, settings.Trading.Short.Trend);

        // barometer short
        if (settings.Trading.Short.Barometer.TryGetValue("15m", out value))
            EditBarometer15mBotShort.Value = value;
        else
            EditBarometer15mBotShort.Value = -99;
        if (settings.Trading.Short.Barometer.TryGetValue("30m", out value))
            EditBarometer30mBotShort.Value = value;
        else
            EditBarometer30mBotShort.Value = -99;
        if (settings.Trading.Short.Barometer.TryGetValue("1h", out value))
            EditBarometer1hBotShort.Value = value;
        else
            EditBarometer1hBotShort.Value = -99;
        if (settings.Trading.Short.Barometer.TryGetValue("4h", out value))
            EditBarometer4hBotShort.Value = value;
        else
            EditBarometer4hBotShort.Value = -99;
        if (settings.Trading.Short.Barometer.TryGetValue("24h", out value))
            EditBarometer24hBotShort.Value = value;
        else
            EditBarometer24hBotShort.Value = -99;

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

        SetGrayed(null, null);
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

        // ------------------------------------------------------------------------------
        // Telegram
        // ------------------------------------------------------------------------------
        settings.Telegram.Token = EditTelegramToken.Text.Trim();
        settings.Telegram.ChatId = EditTelegramChatId.Text.Trim();
        settings.Telegram.SendSignalsToTelegram = EditSendSignalsToTelegram.Checked;


        // ------------------------------------------------------------------------------
        // Base coins
        // ------------------------------------------------------------------------------
        foreach (SettingsQuoteCoin x in BaseCoinList)
            x.GetControlValues();


        // ------------------------------------------------------------------------------
        // Signals
        // ------------------------------------------------------------------------------
        settings.Signal.Long.Interval.Clear();
        foreach (var item in AnalyzeInterval)
            GetValueFromCheckBox(item.Key, item.Value, settings.Signal.Long.Interval);

        settings.Signal.AnalysisMinChangePercentage = (double)EditAnalysisMinChangePercentage.Value;
        settings.Signal.AnalysisMaxChangePercentage = (double)EditAnalysisMaxChangePercentage.Value;
        settings.Signal.LogAnalysisMinMaxChangePercentage = EditLogAnalysisMinMaxChangePercentage.Checked;

        settings.Signal.AnalysisMinEffectivePercentage = (double)EditAnalysisMinEffectivePercentage.Value;
        settings.Signal.AnalysisMaxEffectivePercentage = (double)EditAnalysisMaxEffectivePercentage.Value;
        settings.Signal.LogAnalysisMinMaxEffectivePercentage = EditLogAnalysisMinMaxEffectivePercentage.Checked;

        settings.Signal.AnalysisMinEffective10DaysPercentage = (double)EditAnalysisMinEffective10DaysPercentage.Value;
        settings.Signal.AnalysisMaxEffective10DaysPercentage = (double)EditAnalysisMaxEffective10DaysPercentage.Value;
        settings.Signal.LogAnalysisMinMaxEffective10DaysPercentage = EditLogAnalysisMinMaxEffective10DaysPercentage.Checked;

        settings.Signal.Barometer1hMinimal = EditBarometer1hMinimal.Value;
        settings.Signal.LogBarometerToLow = EditLogBarometerToLow.Checked;

        settings.Signal.SymbolMustExistsDays = (int)EditSymbolMustExistsDays.Value;
        settings.Signal.LogSymbolMustExistsDays = EditLogSymbolMustExistsDays.Checked;

        settings.Signal.MinimumTickPercentage = EditMinimumTickPercentage.Value;
        settings.Signal.LogMinimumTickPercentage = EditLogMinimumTickPercentage.Checked;


        // ------------------------------------------------------------------------------
        // Signal types
        // ------------------------------------------------------------------------------

        // STOBB
        settings.Signal.StobbBBMinPercentage = (double)EditStobbBBMinPercentage.Value;
        settings.Signal.StobbBBMaxPercentage = (double)EditStobbBBMaxPercentage.Value;
        settings.Signal.PlaySoundStobbSignal = EditPlaySoundStobbSignal.Checked;
        settings.Signal.PlaySpeechStobbSignal = EditPlaySpeechStobbSignal.Checked;
        settings.Signal.SoundStobbOverbought = EditSoundStobbOverbought.Text;
        settings.Signal.SoundStobbOversold = EditSoundStobbOversold.Text;
        settings.Signal.StobIncludeRsi = EditStobIncludeRsi.Checked;
        settings.Signal.StobIncludeSoftSbm = EditStobIncludeSbmMaLines.Checked;
        settings.Signal.StobIncludeSbmPercAndCrossing = EditStobIncludeSbmPercAndCrossing.Checked;
        settings.Signal.ColorStobb = panelColorStobbLong.BackColor;
        settings.Signal.ColorStobbShort = panelColorStobbShort.BackColor;
        settings.Signal.StobTrendLong = EditStobTrendLong.Value;
        settings.Signal.StobTrendShort = EditStobTrendShort.Value;

        // SBM x
        settings.Signal.SbmBBMinPercentage = (double)EditSbmBBMinPercentage.Value;
        settings.Signal.SbmBBMaxPercentage = (double)EditSbmBBMaxPercentage.Value;
        settings.Signal.SbmUseLowHigh = EditStobbUseLowHigh.Checked;

        // SBM 1
        settings.Signal.PlaySoundSbmSignal = EditPlaySoundSbmSignal.Checked;
        settings.Signal.PlaySpeechSbmSignal = EditPlaySpeechSbmSignal.Checked;
        settings.Signal.SoundSbmOverbought = EditSoundFileSbmOverbought.Text;
        settings.Signal.SoundSbmOversold = EditSoundFileSbmOversold.Text;
        settings.Signal.ColorSbm = panelColorSbmLong.BackColor;
        settings.Signal.ColorSbmShort = panelColorSbmShort.BackColor;
        settings.Signal.Sbm1CandlesLookbackCount = (int)EditSbm1CandlesLookbackCount.Value;

        // SBM2
        settings.Signal.Sbm2CandlesLookbackCount = (int)EditSbm2CandlesLookbackCount.Value;
        settings.Signal.Sbm2BbPercentage = EditSbm2BbPercentage.Value;
        settings.Signal.Sbm2UseLowHigh = EditSbm2UseLowHigh.Checked;

        // SBM3
        settings.Signal.Sbm3CandlesLookbackCount = (int)EditSbm3CandlesForBBRecovery.Value;
        settings.Signal.Sbm3CandlesBbRecoveryPercentage = EditSbm3CandlesForBBRecoveryPercentage.Value;

        // SBM aanvullend
        settings.Signal.SbmCandlesForMacdRecovery = (int)EditSbmCandlesForMacdRecovery.Value;

        settings.Signal.SbmMa200AndMa50Percentage = EditSbmMa200AndMa50Percentage.Value;
        settings.Signal.SbmMa50AndMa20Percentage = EditSbmMa50AndMa20Percentage.Value;
        settings.Signal.SbmMa200AndMa20Percentage = EditSbmMa200AndMa20Percentage.Value;

        settings.Signal.SbmMa200AndMa20Crossing = EditSbmMa200AndMa20Crossing.Checked;
        settings.Signal.SbmMa200AndMa20Lookback = (int)EditSbmMa200AndMa20Lookback.Value;
        settings.Signal.SbmMa200AndMa50Crossing = EditSbmMa200AndMa50Crossing.Checked;
        settings.Signal.SbmMa200AndMa50Lookback = (int)EditSbmMa200AndMa50Lookback.Value;
        settings.Signal.SbmMa50AndMa20Crossing = EditSbmMa50AndMa20Crossing.Checked;
        settings.Signal.SbmMa50AndMa20Lookback = (int)EditSbmMa50AndMa20Lookback.Value;

        // JUMP
        settings.Signal.PlaySoundCandleJumpSignal = EditPlaySoundCandleJumpSignal.Checked;
        settings.Signal.PlaySpeechCandleJumpSignal = EditPlaySpeechCandleJumpSignal.Checked;
        settings.Signal.SoundCandleJumpDown = EditSoundFileCandleJumpDown.Text;
        settings.Signal.SoundCandleJumpUp = EditSoundFileCandleJumpUp.Text;
        settings.Signal.AnalysisCandleJumpPercentage = EditAnalysisCandleJumpPercentage.Value;
        settings.Signal.JumpCandlesLookbackCount = (int)EditJumpCandlesLookbackCount.Value;
        settings.Signal.JumpUseLowHighCalculation = EditJumpUseLowHighCalculation.Checked;
        settings.Signal.ColorJump = panelColorJumpLong.BackColor;
        settings.Signal.ColorJumpShort = panelColorJumpShort.BackColor;


        settings.Signal.Long.Strategy.Clear();
        foreach (var item in AnalyzeDefinitionIndexLong)
            GetValueFromCheckBox(item.Key, item.Value, settings.Signal.Long.Strategy);

        settings.Signal.Short.Strategy.Clear();
        foreach (var item in AnalyzeDefinitionIndexShort)
            GetValueFromCheckBox(item.Key, item.Value, settings.Signal.Short.Strategy);


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
        // Black & White list
        // --------------------------------------------------------------------------------

        settings.BlackListOversold = textBoxBlackListOversold.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.WhiteListOversold.Sort();

        settings.WhiteListOversold = textBoxWhiteListOversold.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.WhiteListOversold.Sort();

        settings.BlackListOverbought = textBoxBlackListOverbought.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.WhiteListOverbought.Sort();

        settings.WhiteListOverbought = textBoxWhiteListOverbought.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.WhiteListOverbought.Sort();

        // --------------------------------------------------------------------------------
        // Trade bot
        // --------------------------------------------------------------------------------
#if TRADEBOT

        settings.Trading.TradeViaExchange = EditTradeViaExchange.Checked;
        settings.Trading.TradeViaPaperTrading = EditTradeViaPaperTrading.Checked;
        settings.Trading.DisableNewPositions = EditDisableNewPositions.Checked;
        settings.General.SoundTradeNotification = EditSoundTradeNotification.Checked;

        // Logging
        settings.Trading.LogCanceledOrders = EditLogCanceledOrders.Checked;

        // api
        settings.Trading.ApiKey = EditApiKey.Text;
        settings.Trading.ApiSecret = EditApiSecret.Text;

        // slots
        settings.Trading.SlotsMaximalExchange = (int)EditSlotsMaximalExchange.Value;
        settings.Trading.SlotsMaximalSymbol = (int)EditSlotsMaximalSymbol.Value;
        settings.Trading.SlotsMaximalBase = (int)EditSlotsMaximalBase.Value;

        // Instap
        settings.Trading.CheckIncreasingRsi = EditCheckIncreasingRsi.Checked;
        settings.Trading.CheckIncreasingMacd = EditCheckIncreasingMacd.Checked;
        settings.Trading.CheckIncreasingStoch = EditCheckIncreasingStoch.Checked;

        // buy
        settings.Trading.BuyStepInMethod = (CryptoStepInMethod)EditBuyStepInMethod.SelectedValue;
        settings.Trading.BuyOrderMethod = (CryptoBuyOrderMethod)EditBuyOrderMethod.SelectedValue;
        settings.Trading.GlobalBuyRemoveTime = (int)EditGlobalBuyRemoveTime.Value;
        settings.Trading.GlobalBuyVarying = EditGlobalBuyVarying.Value;

        // dca
        settings.Trading.DcaStepInMethod = (CryptoStepInMethod)EditDcaStepInMethod.SelectedValue;
        settings.Trading.DcaOrderMethod = (CryptoBuyOrderMethod)EditDcaOrderMethod.SelectedValue;
        settings.Trading.DcaPercentage = EditDcaPercentage.Value;
        settings.Trading.DcaFactor = EditDcaFactor.Value;
        settings.Trading.DcaCount = (int)EditDcaCount.Value;
        settings.Trading.GlobalBuyCooldownTime = (int)EditGlobalBuyCooldownTime.Value;

        // sell
        settings.Trading.SellMethod = (CryptoSellMethod)EditSellMethod.SelectedValue;
        settings.Trading.ProfitPercentage = EditProfitPercentage.Value;
        settings.Trading.LockProfits = EditLockProfits.Checked;

        // Stop loss
        settings.Trading.GlobalStopPercentage = EditGlobalStopPercentage.Value;
        settings.Trading.GlobalStopLimitPercentage = EditGlobalStopLimitPercentage.Value;

        settings.Trading.Leverage = EditLeverage.Value;
        settings.Trading.CrossOrIsolated = EditCrossOrIsolated.SelectedIndex;



        // interval long
        settings.Trading.Long.Interval.Clear();
        foreach (var item in TradingIntervalLong)
            GetValueFromCheckBox(item.Key, item.Value, settings.Trading.Long.Interval);

        // strategy Long
        settings.Trading.Long.Strategy.Clear();
        foreach (var strategy in StrategyControlsLong.Values)
            strategy.GetControlValues();

        // trend long
        settings.Trading.Long.Trend.Clear();
        foreach (var item in TrendLongControls)
            GetValueFromCheckBox(item.Key, item.Value, settings.Trading.Long.Trend);

        // barometer Long
        settings.Trading.Long.Barometer.Clear();
        settings.Trading.Long.Barometer.Add("15m", EditBarometer15mBotLong.Value);
        settings.Trading.Long.Barometer.Add("30m", EditBarometer30mBotLong.Value);
        settings.Trading.Long.Barometer.Add("1h", EditBarometer1hBotLong.Value);
        settings.Trading.Long.Barometer.Add("4h", EditBarometer4hBotLong.Value);
        settings.Trading.Long.Barometer.Add("24h", EditBarometer24hBotLong.Value);



        // interval short
        settings.Trading.Short.Interval.Clear();
        foreach (var item in TradingIntervalShort)
            GetValueFromCheckBox(item.Key, item.Value, settings.Trading.Short.Interval);

        // strategy short
        settings.Trading.Short.Strategy.Clear();
        foreach (var strategy in StrategyControlsShort.Values)
            strategy.GetControlValues();

        // trend short
        settings.Trading.Short.Trend.Clear();
        foreach (var item in TrendShortControls)
            GetValueFromCheckBox(item.Key, item.Value, settings.Trading.Short.Trend);

        // barometer short
        settings.Trading.Short.Barometer.Clear();
        settings.Trading.Short.Barometer.Add("15m", EditBarometer15mBotShort.Value);
        settings.Trading.Short.Barometer.Add("30m", EditBarometer30mBotShort.Value);
        settings.Trading.Short.Barometer.Add("1h", EditBarometer1hBotShort.Value);
        settings.Trading.Short.Barometer.Add("4h", EditBarometer4hBotShort.Value);
        settings.Trading.Short.Barometer.Add("24h", EditBarometer24hBotShort.Value);

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

    private void ButtonTestSpeech_Click(object sender, EventArgs e) => GlobalData.PlaySomeSpeech("Found a signal for BTCUSDT interval 1m (it is going to the moon)", true);

    private static void BrowseForWavFile(ref TextBox textBox)
    {
        OpenFileDialog openFileDialog = new()
        {
            Filter = "wav bestanden|*.wav"
        };
        if (!textBox.Text.IsNullOrEmpty())
            openFileDialog.FileName = Path.GetFileName(textBox.Text);
        if (!textBox.Text.IsNullOrEmpty())
            openFileDialog.InitialDirectory = Path.GetDirectoryName(textBox.Text);

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            string fileName = openFileDialog.FileName;
            if (File.Exists(fileName))
            {
                textBox.Text = fileName;
            }
            else
            {
                MessageBox.Show("Selected file doesn't exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ButtonSelectSoundStobbOverbought_Click(object sender, EventArgs e)
        => BrowseForWavFile(ref EditSoundStobbOverbought);

    private void ButtonSelectSoundStobbOversold_Click(object sender, EventArgs e)
        => BrowseForWavFile(ref EditSoundStobbOversold);

    private void ButtonSelectSoundSbmOverbought_Click(object sender, EventArgs e)
        => BrowseForWavFile(ref EditSoundFileSbmOverbought);

    private void ButtonSelectSoundSbmOversold_Click(object sender, EventArgs e)
        => BrowseForWavFile(ref EditSoundFileSbmOversold);

    private void ButtonSelectSoundCandleJumpUp_Click(object sender, EventArgs e)
        => BrowseForWavFile(ref EditSoundFileCandleJumpUp);

    private void ButtonSelectSoundCandleJumpDown_Click(object sender, EventArgs e)
        => BrowseForWavFile(ref EditSoundFileCandleJumpDown);

    private void ButtonPlaySoundStobbOverbought_Click(object sender, EventArgs e)
        => GlobalData.PlaySomeMusic(EditSoundStobbOverbought.Text, true);

    private void ButtonPlaySoundStobbOversold_Click(object sender, EventArgs e)
        => GlobalData.PlaySomeMusic(EditSoundStobbOversold.Text, true);

    private void ButtonPlaySoundSbmOverbought_Click(object sender, EventArgs e)
        => GlobalData.PlaySomeMusic(EditSoundFileSbmOverbought.Text, true);

    private void ButtonPlaySoundSbmOversold_Click(object sender, EventArgs e)
        => GlobalData.PlaySomeMusic(EditSoundFileSbmOversold.Text, true);

    private void ButtonPlaySoundCandleJumpUp_Click(object sender, EventArgs e)
        => GlobalData.PlaySomeMusic(EditSoundFileCandleJumpUp.Text, true);

    private void ButtonPlaySoundCandleJumpDown_Click(object sender, EventArgs e)
        => GlobalData.PlaySomeMusic(EditSoundFileCandleJumpDown.Text, true);

    private static void PickColor(ref Panel panel)
    {
        ColorDialog dlg = new()
        {
            Color = panel.BackColor
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            panel.BackColor = dlg.Color;
        }
    }

    private void ButtonColorClick(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            if (button.Tag is Panel panel)
            {
                PickColor(ref panel);
            }
        }
    }

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

    private void ButtonTestTelegram_Click(object sender, EventArgs e)
    {
        ThreadTelegramBot.ChatId = EditTelegramChatId.Text;
        GlobalData.AddTextToTelegram("Dit is een test bericht van de CryptoScanner");
    }

    private async void ButtonTelegramStart_Click(object sender, EventArgs e)
    {
        await ThreadTelegramBot.Start(EditTelegramToken.Text, EditTelegramChatId.Text);
    }

    private void ButtonGotoAppDataFolder_Click(object sender, EventArgs e)
    {
        string folder = GlobalData.GetBaseDir();
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder) { UseShellExecute = true });
    }

}
