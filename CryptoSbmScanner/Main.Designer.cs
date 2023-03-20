namespace CryptoSbmScanner
{
    partial class FrmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            this.panelLeft = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.listBoxSymbols = new System.Windows.Forms.ListBox();
            this.listBoxSymbolsMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.listBoxSymbolsMenuItemActivateTradingApp = new System.Windows.Forms.ToolStripMenuItem();
            this.listBoxSymbolsMenuItemActivateTradingApps = new System.Windows.Forms.ToolStripMenuItem();
            this.listBoxSymbolsMenuItemActivateTradingviewInternal = new System.Windows.Forms.ToolStripMenuItem();
            this.listBoxSymbolsMenuItemActivateTradingviewExternal = new System.Windows.Forms.ToolStripMenuItem();
            this.listBoxSymbolsMenuItemShowTrendInformation = new System.Windows.Forms.ToolStripMenuItem();
            this.listBoxSymbolsMenuItemCopy = new System.Windows.Forms.ToolStripMenuItem();
            this.panel3 = new System.Windows.Forms.Panel();
            this.labelVersion = new System.Windows.Forms.Label();
            this.applicationMenuStrip = new System.Windows.Forms.MenuStrip();
            this.MenuMain = new System.Windows.Forms.ToolStripMenuItem();
            this.ApplicationPlaySounds = new System.Windows.Forms.ToolStripMenuItem();
            this.ApplicationCreateSignals = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItemSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItemRefresh = new System.Windows.Forms.ToolStripMenuItem();
            this.clearMenusToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.applicationMenuItemAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.symbolFilter = new System.Windows.Forms.TextBox();
            this.panelClient = new System.Windows.Forms.Panel();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPageSignals = new System.Windows.Forms.TabPage();
            this.listViewSignals = new CryptoSbmScanner.ListViewDoubleBuffered();
            this.listViewSignalsMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.listViewSignalsMenuItemActivateTradingApp = new System.Windows.Forms.ToolStripMenuItem();
            this.listViewSignalsMenuItemActivateTradingApps = new System.Windows.Forms.ToolStripMenuItem();
            this.listViewSignalsMenuItemActivateTradingViewInternal = new System.Windows.Forms.ToolStripMenuItem();
            this.listViewSignalsMenuItemActivateTradingViewExternal = new System.Windows.Forms.ToolStripMenuItem();
            this.listViewSignalsMenuItemShowTrendInformation = new System.Windows.Forms.ToolStripMenuItem();
            this.listViewSignalsMenuItemClearSignals = new System.Windows.Forms.ToolStripMenuItem();
            this.listViewSignalsMenuItemCopySignal = new System.Windows.Forms.ToolStripMenuItem();
            this.tabPageBrowser = new System.Windows.Forms.TabPage();
            this.webViewTradingView = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.tabPageLog = new System.Windows.Forms.TabPage();
            this.TextBoxLog = new System.Windows.Forms.TextBox();
            this.tabPageAltrady = new System.Windows.Forms.TabPage();
            this.webViewAltrady = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.panelTop = new System.Windows.Forms.Panel();
            this.listViewInformation = new CryptoSbmScanner.ListViewDoubleBuffered();
            this.listViewSymbolPrices = new CryptoSbmScanner.ListViewDoubleBuffered();
            this.labelBarometerDateValue = new System.Windows.Forms.Label();
            this.comboBoxBarometerInterval = new System.Windows.Forms.ComboBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.labelKLinesTickerCount = new System.Windows.Forms.Label();
            this.labelAnalyseCount = new System.Windows.Forms.Label();
            this.labelPriceTicker = new System.Windows.Forms.Label();
            this.comboBoxBarometerQuote = new System.Windows.Forms.ComboBox();
            this.panelClient1 = new System.Windows.Forms.Panel();
            this.timerBarometer = new System.Windows.Forms.Timer(this.components);
            this.timerClearEvents = new System.Windows.Forms.Timer(this.components);
            this.timerSoundHeartBeat = new System.Windows.Forms.Timer(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.timerCandles = new System.Windows.Forms.Timer(this.components);
            this.timerAddSignal = new System.Windows.Forms.Timer(this.components);
            this.panelLeft.SuspendLayout();
            this.panel4.SuspendLayout();
            this.listBoxSymbolsMenuStrip.SuspendLayout();
            this.panel3.SuspendLayout();
            this.applicationMenuStrip.SuspendLayout();
            this.panelClient.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabPageSignals.SuspendLayout();
            this.listViewSignalsMenuStrip.SuspendLayout();
            this.tabPageBrowser.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webViewTradingView)).BeginInit();
            this.tabPageLog.SuspendLayout();
            this.tabPageAltrady.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webViewAltrady)).BeginInit();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panelClient1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelLeft
            // 
            this.panelLeft.Controls.Add(this.panel4);
            this.panelLeft.Controls.Add(this.panel3);
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeft.Location = new System.Drawing.Point(0, 0);
            this.panelLeft.Margin = new System.Windows.Forms.Padding(2);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Size = new System.Drawing.Size(163, 642);
            this.panelLeft.TabIndex = 12;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.listBoxSymbols);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(0, 72);
            this.panel4.Margin = new System.Windows.Forms.Padding(2);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(163, 570);
            this.panel4.TabIndex = 2;
            // 
            // listBoxSymbols
            // 
            this.listBoxSymbols.ContextMenuStrip = this.listBoxSymbolsMenuStrip;
            this.listBoxSymbols.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxSymbols.FormattingEnabled = true;
            this.listBoxSymbols.Location = new System.Drawing.Point(0, 0);
            this.listBoxSymbols.Margin = new System.Windows.Forms.Padding(2);
            this.listBoxSymbols.Name = "listBoxSymbols";
            this.listBoxSymbols.Size = new System.Drawing.Size(163, 570);
            this.listBoxSymbols.Sorted = true;
            this.listBoxSymbols.TabIndex = 0;
            // 
            // listBoxSymbolsMenuStrip
            // 
            this.listBoxSymbolsMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.listBoxSymbolsMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.listBoxSymbolsMenuItemActivateTradingApp,
            this.listBoxSymbolsMenuItemActivateTradingApps,
            this.listBoxSymbolsMenuItemActivateTradingviewInternal,
            this.listBoxSymbolsMenuItemActivateTradingviewExternal,
            this.listBoxSymbolsMenuItemShowTrendInformation,
            this.listBoxSymbolsMenuItemCopy});
            this.listBoxSymbolsMenuStrip.Name = "contextMenuStrip1";
            this.listBoxSymbolsMenuStrip.Size = new System.Drawing.Size(215, 136);
            // 
            // listBoxSymbolsMenuItemActivateTradingApp
            // 
            this.listBoxSymbolsMenuItemActivateTradingApp.Name = "listBoxSymbolsMenuItemActivateTradingApp";
            this.listBoxSymbolsMenuItemActivateTradingApp.Size = new System.Drawing.Size(214, 22);
            this.listBoxSymbolsMenuItemActivateTradingApp.Text = "Activate trading app";
            this.listBoxSymbolsMenuItemActivateTradingApp.Click += new System.EventHandler(this.listBoxSymbolsMenuItemActivateTradingApp_Click);
            // 
            // listBoxSymbolsMenuItemActivateTradingApps
            // 
            this.listBoxSymbolsMenuItemActivateTradingApps.Name = "listBoxSymbolsMenuItemActivateTradingApps";
            this.listBoxSymbolsMenuItemActivateTradingApps.Size = new System.Drawing.Size(214, 22);
            this.listBoxSymbolsMenuItemActivateTradingApps.Text = "Trading app + TradingView";
            this.listBoxSymbolsMenuItemActivateTradingApps.Click += new System.EventHandler(this.listBoxSymbolsMenuItemActivateTradingApps_Click);
            // 
            // listBoxSymbolsMenuItemActivateTradingviewInternal
            // 
            this.listBoxSymbolsMenuItemActivateTradingviewInternal.Name = "listBoxSymbolsMenuItemActivateTradingviewInternal";
            this.listBoxSymbolsMenuItemActivateTradingviewInternal.Size = new System.Drawing.Size(214, 22);
            this.listBoxSymbolsMenuItemActivateTradingviewInternal.Text = "Tradingview browser";
            this.listBoxSymbolsMenuItemActivateTradingviewInternal.Click += new System.EventHandler(this.listBoxSymbolsMenuItemActivateTradingviewInternal_Click);
            // 
            // listBoxSymbolsMenuItemActivateTradingviewExternal
            // 
            this.listBoxSymbolsMenuItemActivateTradingviewExternal.Name = "listBoxSymbolsMenuItemActivateTradingviewExternal";
            this.listBoxSymbolsMenuItemActivateTradingviewExternal.Size = new System.Drawing.Size(214, 22);
            this.listBoxSymbolsMenuItemActivateTradingviewExternal.Text = "Tradingview external";
            this.listBoxSymbolsMenuItemActivateTradingviewExternal.Click += new System.EventHandler(this.listBoxSymbolsMenuItemActivateTradingviewExternal_Click);
            // 
            // listBoxSymbolsMenuItemShowTrendInformation
            // 
            this.listBoxSymbolsMenuItemShowTrendInformation.Name = "listBoxSymbolsMenuItemShowTrendInformation";
            this.listBoxSymbolsMenuItemShowTrendInformation.Size = new System.Drawing.Size(214, 22);
            this.listBoxSymbolsMenuItemShowTrendInformation.Text = "Trend informatie";
            this.listBoxSymbolsMenuItemShowTrendInformation.Click += new System.EventHandler(this.MenuSymbolsShowTrendInformation_Click);
            // 
            // listBoxSymbolsMenuItemCopy
            // 
            this.listBoxSymbolsMenuItemCopy.Name = "listBoxSymbolsMenuItemCopy";
            this.listBoxSymbolsMenuItemCopy.Size = new System.Drawing.Size(214, 22);
            this.listBoxSymbolsMenuItemCopy.Text = "Copy";
            this.listBoxSymbolsMenuItemCopy.Click += new System.EventHandler(this.listBoxSymbolsMenuItemCopy_Click);
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.labelVersion);
            this.panel3.Controls.Add(this.applicationMenuStrip);
            this.panel3.Controls.Add(this.symbolFilter);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Margin = new System.Windows.Forms.Padding(2);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(163, 72);
            this.panel3.TabIndex = 1;
            // 
            // labelVersion
            // 
            this.labelVersion.AutoSize = true;
            this.labelVersion.Location = new System.Drawing.Point(12, 27);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(98, 13);
            this.labelVersion.TabIndex = 50;
            this.labelVersion.Text = "CryptoScanner x.xx";
            // 
            // applicationMenuStrip
            // 
            this.applicationMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.applicationMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuMain});
            this.applicationMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.applicationMenuStrip.Name = "applicationMenuStrip";
            this.applicationMenuStrip.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.applicationMenuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.applicationMenuStrip.Size = new System.Drawing.Size(163, 24);
            this.applicationMenuStrip.TabIndex = 16;
            this.applicationMenuStrip.Text = "menuStrip1";
            // 
            // MenuMain
            // 
            this.MenuMain.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ApplicationPlaySounds,
            this.ApplicationCreateSignals,
            this.ToolStripMenuItemSettings,
            this.ToolStripMenuItemRefresh,
            this.clearMenusToolStripMenuItem,
            this.applicationMenuItemAbout});
            this.MenuMain.Name = "MenuMain";
            this.MenuMain.Size = new System.Drawing.Size(50, 20);
            this.MenuMain.Text = "Menu";
            // 
            // ApplicationPlaySounds
            // 
            this.ApplicationPlaySounds.Checked = true;
            this.ApplicationPlaySounds.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ApplicationPlaySounds.Name = "ApplicationPlaySounds";
            this.ApplicationPlaySounds.Size = new System.Drawing.Size(168, 22);
            this.ApplicationPlaySounds.Text = "Geluiden afspelen";
            this.ApplicationPlaySounds.Click += new System.EventHandler(this.ApplicationPlaySounds_Click);
            // 
            // ApplicationCreateSignals
            // 
            this.ApplicationCreateSignals.Checked = true;
            this.ApplicationCreateSignals.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ApplicationCreateSignals.Name = "ApplicationCreateSignals";
            this.ApplicationCreateSignals.Size = new System.Drawing.Size(168, 22);
            this.ApplicationCreateSignals.Text = "Signalen maken";
            this.ApplicationCreateSignals.Click += new System.EventHandler(this.ApplicationCreateSignals_Click);
            // 
            // ToolStripMenuItemSettings
            // 
            this.ToolStripMenuItemSettings.Name = "ToolStripMenuItemSettings";
            this.ToolStripMenuItemSettings.Size = new System.Drawing.Size(168, 22);
            this.ToolStripMenuItemSettings.Text = "Instellingen";
            this.ToolStripMenuItemSettings.Click += new System.EventHandler(this.ToolStripMenuItemSettings_Click);
            // 
            // ToolStripMenuItemRefresh
            // 
            this.ToolStripMenuItemRefresh.Name = "ToolStripMenuItemRefresh";
            this.ToolStripMenuItemRefresh.Size = new System.Drawing.Size(168, 22);
            this.ToolStripMenuItemRefresh.Text = "Verversen";
            this.ToolStripMenuItemRefresh.Click += new System.EventHandler(this.ToolStripMenuItemRefresh_Click_1);
            // 
            // clearMenusToolStripMenuItem
            // 
            this.clearMenusToolStripMenuItem.Name = "clearMenusToolStripMenuItem";
            this.clearMenusToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.clearMenusToolStripMenuItem.Text = "Clear";
            this.clearMenusToolStripMenuItem.Click += new System.EventHandler(this.mainMenuClearAll_Click);
            // 
            // applicationMenuItemAbout
            // 
            this.applicationMenuItemAbout.Name = "applicationMenuItemAbout";
            this.applicationMenuItemAbout.Size = new System.Drawing.Size(168, 22);
            this.applicationMenuItemAbout.Text = "About";
            this.applicationMenuItemAbout.Click += new System.EventHandler(this.applicationMenuItemAbout_Click);
            // 
            // symbolFilter
            // 
            this.symbolFilter.Location = new System.Drawing.Point(11, 47);
            this.symbolFilter.Margin = new System.Windows.Forms.Padding(2);
            this.symbolFilter.Name = "symbolFilter";
            this.symbolFilter.Size = new System.Drawing.Size(141, 20);
            this.symbolFilter.TabIndex = 0;
            this.symbolFilter.KeyDown += new System.Windows.Forms.KeyEventHandler(this.symbolFilter_KeyDown);
            // 
            // panelClient
            // 
            this.panelClient.Controls.Add(this.tabControl);
            this.panelClient.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelClient.Location = new System.Drawing.Point(0, 73);
            this.panelClient.Margin = new System.Windows.Forms.Padding(2);
            this.panelClient.Name = "panelClient";
            this.panelClient.Size = new System.Drawing.Size(1322, 569);
            this.panelClient.TabIndex = 13;
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabPageSignals);
            this.tabControl.Controls.Add(this.tabPageBrowser);
            this.tabControl.Controls.Add(this.tabPageLog);
            this.tabControl.Controls.Add(this.tabPageAltrady);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Margin = new System.Windows.Forms.Padding(2);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1322, 569);
            this.tabControl.TabIndex = 12;
            // 
            // tabPageSignals
            // 
            this.tabPageSignals.Controls.Add(this.listViewSignals);
            this.tabPageSignals.Location = new System.Drawing.Point(4, 22);
            this.tabPageSignals.Name = "tabPageSignals";
            this.tabPageSignals.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSignals.Size = new System.Drawing.Size(1314, 543);
            this.tabPageSignals.TabIndex = 4;
            this.tabPageSignals.Text = "Signals";
            this.tabPageSignals.UseVisualStyleBackColor = true;
            // 
            // listViewSignals
            // 
            this.listViewSignals.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listViewSignals.ContextMenuStrip = this.listViewSignalsMenuStrip;
            this.listViewSignals.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewSignals.HideSelection = false;
            this.listViewSignals.Location = new System.Drawing.Point(3, 3);
            this.listViewSignals.Name = "listViewSignals";
            this.listViewSignals.Size = new System.Drawing.Size(1308, 537);
            this.listViewSignals.TabIndex = 1;
            this.listViewSignals.UseCompatibleStateImageBehavior = false;
            this.listViewSignals.View = System.Windows.Forms.View.Details;
            // 
            // listViewSignalsMenuStrip
            // 
            this.listViewSignalsMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.listViewSignalsMenuItemActivateTradingApp,
            this.listViewSignalsMenuItemActivateTradingApps,
            this.listViewSignalsMenuItemActivateTradingViewInternal,
            this.listViewSignalsMenuItemActivateTradingViewExternal,
            this.listViewSignalsMenuItemShowTrendInformation,
            this.listViewSignalsMenuItemClearSignals,
            this.listViewSignalsMenuItemCopySignal});
            this.listViewSignalsMenuStrip.Name = "contextMenuStripSignals";
            this.listViewSignalsMenuStrip.Size = new System.Drawing.Size(215, 158);
            // 
            // listViewSignalsMenuItemActivateTradingApp
            // 
            this.listViewSignalsMenuItemActivateTradingApp.Name = "listViewSignalsMenuItemActivateTradingApp";
            this.listViewSignalsMenuItemActivateTradingApp.Size = new System.Drawing.Size(214, 22);
            this.listViewSignalsMenuItemActivateTradingApp.Text = "Activate trading app";
            this.listViewSignalsMenuItemActivateTradingApp.Click += new System.EventHandler(this.listViewSignalsMenuItemActivateTradingApp_Click);
            // 
            // listViewSignalsMenuItemActivateTradingApps
            // 
            this.listViewSignalsMenuItemActivateTradingApps.Name = "listViewSignalsMenuItemActivateTradingApps";
            this.listViewSignalsMenuItemActivateTradingApps.Size = new System.Drawing.Size(214, 22);
            this.listViewSignalsMenuItemActivateTradingApps.Text = "Trading app + TradingView";
            this.listViewSignalsMenuItemActivateTradingApps.Click += new System.EventHandler(this.listViewSignalsMenuItemActivateTradingApps_Click);
            // 
            // listViewSignalsMenuItemActivateTradingViewInternal
            // 
            this.listViewSignalsMenuItemActivateTradingViewInternal.Name = "listViewSignalsMenuItemActivateTradingViewInternal";
            this.listViewSignalsMenuItemActivateTradingViewInternal.Size = new System.Drawing.Size(214, 22);
            this.listViewSignalsMenuItemActivateTradingViewInternal.Text = "TradingView browser";
            this.listViewSignalsMenuItemActivateTradingViewInternal.Click += new System.EventHandler(this.listViewSignalsMenuItemActivateTradingViewInternal_Click);
            // 
            // listViewSignalsMenuItemActivateTradingViewExternal
            // 
            this.listViewSignalsMenuItemActivateTradingViewExternal.Name = "listViewSignalsMenuItemActivateTradingViewExternal";
            this.listViewSignalsMenuItemActivateTradingViewExternal.Size = new System.Drawing.Size(214, 22);
            this.listViewSignalsMenuItemActivateTradingViewExternal.Text = "Tradingview extern";
            this.listViewSignalsMenuItemActivateTradingViewExternal.Click += new System.EventHandler(this.listViewSignalsMenuItemActivateTradingviewExternal_Click);
            // 
            // listViewSignalsMenuItemShowTrendInformation
            // 
            this.listViewSignalsMenuItemShowTrendInformation.Name = "listViewSignalsMenuItemShowTrendInformation";
            this.listViewSignalsMenuItemShowTrendInformation.Size = new System.Drawing.Size(214, 22);
            this.listViewSignalsMenuItemShowTrendInformation.Text = "Trend informatie";
            this.listViewSignalsMenuItemShowTrendInformation.Click += new System.EventHandler(this.MenuSignalsShowTrendInformation_Click);
            // 
            // listViewSignalsMenuItemClearSignals
            // 
            this.listViewSignalsMenuItemClearSignals.Name = "listViewSignalsMenuItemClearSignals";
            this.listViewSignalsMenuItemClearSignals.Size = new System.Drawing.Size(214, 22);
            this.listViewSignalsMenuItemClearSignals.Text = "Clear";
            this.listViewSignalsMenuItemClearSignals.Click += new System.EventHandler(this.listViewSignalsMenuItemClearSignals_Click);
            // 
            // listViewSignalsMenuItemCopySignal
            // 
            this.listViewSignalsMenuItemCopySignal.Name = "listViewSignalsMenuItemCopySignal";
            this.listViewSignalsMenuItemCopySignal.Size = new System.Drawing.Size(214, 22);
            this.listViewSignalsMenuItemCopySignal.Text = "Copy";
            this.listViewSignalsMenuItemCopySignal.Click += new System.EventHandler(this.listViewSignalsMenuItemCopySignal_Click);
            // 
            // tabPageBrowser
            // 
            this.tabPageBrowser.Controls.Add(this.webViewTradingView);
            this.tabPageBrowser.Location = new System.Drawing.Point(4, 22);
            this.tabPageBrowser.Name = "tabPageBrowser";
            this.tabPageBrowser.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageBrowser.Size = new System.Drawing.Size(1314, 543);
            this.tabPageBrowser.TabIndex = 5;
            this.tabPageBrowser.Text = "Tradingview";
            this.tabPageBrowser.UseVisualStyleBackColor = true;
            // 
            // webViewTradingView
            // 
            this.webViewTradingView.AllowExternalDrop = true;
            this.webViewTradingView.CreationProperties = null;
            this.webViewTradingView.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webViewTradingView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webViewTradingView.Location = new System.Drawing.Point(3, 3);
            this.webViewTradingView.Name = "webViewTradingView";
            this.webViewTradingView.Size = new System.Drawing.Size(1308, 537);
            this.webViewTradingView.TabIndex = 0;
            this.webViewTradingView.ZoomFactor = 1D;
            // 
            // tabPageLog
            // 
            this.tabPageLog.Controls.Add(this.TextBoxLog);
            this.tabPageLog.Location = new System.Drawing.Point(4, 22);
            this.tabPageLog.Margin = new System.Windows.Forms.Padding(2);
            this.tabPageLog.Name = "tabPageLog";
            this.tabPageLog.Padding = new System.Windows.Forms.Padding(2);
            this.tabPageLog.Size = new System.Drawing.Size(1314, 543);
            this.tabPageLog.TabIndex = 1;
            this.tabPageLog.Text = "Log";
            this.tabPageLog.UseVisualStyleBackColor = true;
            // 
            // TextBoxLog
            // 
            this.TextBoxLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TextBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TextBoxLog.Location = new System.Drawing.Point(2, 2);
            this.TextBoxLog.Margin = new System.Windows.Forms.Padding(2);
            this.TextBoxLog.Multiline = true;
            this.TextBoxLog.Name = "TextBoxLog";
            this.TextBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.TextBoxLog.Size = new System.Drawing.Size(1310, 539);
            this.TextBoxLog.TabIndex = 1;
            // 
            // tabPageAltrady
            // 
            this.tabPageAltrady.Controls.Add(this.webViewAltrady);
            this.tabPageAltrady.Location = new System.Drawing.Point(4, 22);
            this.tabPageAltrady.Name = "tabPageAltrady";
            this.tabPageAltrady.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAltrady.Size = new System.Drawing.Size(1314, 543);
            this.tabPageAltrady.TabIndex = 6;
            this.tabPageAltrady.Text = "tabPage1";
            this.tabPageAltrady.UseVisualStyleBackColor = true;
            // 
            // webViewAltrady
            // 
            this.webViewAltrady.AllowExternalDrop = true;
            this.webViewAltrady.CreationProperties = null;
            this.webViewAltrady.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webViewAltrady.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webViewAltrady.Location = new System.Drawing.Point(3, 3);
            this.webViewAltrady.Name = "webViewAltrady";
            this.webViewAltrady.Size = new System.Drawing.Size(1308, 537);
            this.webViewAltrady.TabIndex = 1;
            this.webViewAltrady.ZoomFactor = 1D;
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.listViewInformation);
            this.panelTop.Controls.Add(this.listViewSymbolPrices);
            this.panelTop.Controls.Add(this.labelBarometerDateValue);
            this.panelTop.Controls.Add(this.comboBoxBarometerInterval);
            this.panelTop.Controls.Add(this.pictureBox1);
            this.panelTop.Controls.Add(this.labelKLinesTickerCount);
            this.panelTop.Controls.Add(this.labelAnalyseCount);
            this.panelTop.Controls.Add(this.labelPriceTicker);
            this.panelTop.Controls.Add(this.comboBoxBarometerQuote);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Margin = new System.Windows.Forms.Padding(2);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1322, 73);
            this.panelTop.TabIndex = 14;
            // 
            // listViewInformation
            // 
            this.listViewInformation.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.listViewInformation.BackColor = System.Drawing.SystemColors.Control;
            this.listViewInformation.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listViewInformation.GridLines = true;
            this.listViewInformation.HideSelection = false;
            this.listViewInformation.Location = new System.Drawing.Point(5, 5);
            this.listViewInformation.Margin = new System.Windows.Forms.Padding(0);
            this.listViewInformation.MultiSelect = false;
            this.listViewInformation.Name = "listViewInformation";
            this.listViewInformation.Scrollable = false;
            this.listViewInformation.Size = new System.Drawing.Size(337, 63);
            this.listViewInformation.TabIndex = 76;
            this.listViewInformation.UseCompatibleStateImageBehavior = false;
            this.listViewInformation.View = System.Windows.Forms.View.Details;
            // 
            // listViewSymbolPrices
            // 
            this.listViewSymbolPrices.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.listViewSymbolPrices.BackColor = System.Drawing.SystemColors.Control;
            this.listViewSymbolPrices.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listViewSymbolPrices.GridLines = true;
            this.listViewSymbolPrices.HideSelection = false;
            this.listViewSymbolPrices.Location = new System.Drawing.Point(676, 0);
            this.listViewSymbolPrices.Name = "listViewSymbolPrices";
            this.listViewSymbolPrices.Scrollable = false;
            this.listViewSymbolPrices.Size = new System.Drawing.Size(556, 73);
            this.listViewSymbolPrices.TabIndex = 71;
            this.listViewSymbolPrices.UseCompatibleStateImageBehavior = false;
            this.listViewSymbolPrices.View = System.Windows.Forms.View.Details;
            // 
            // labelBarometerDateValue
            // 
            this.labelBarometerDateValue.AutoSize = true;
            this.labelBarometerDateValue.Location = new System.Drawing.Point(345, 53);
            this.labelBarometerDateValue.Name = "labelBarometerDateValue";
            this.labelBarometerDateValue.Size = new System.Drawing.Size(55, 13);
            this.labelBarometerDateValue.TabIndex = 70;
            this.labelBarometerDateValue.Text = "Barometer";
            // 
            // comboBoxBarometerInterval
            // 
            this.comboBoxBarometerInterval.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBarometerInterval.FormattingEnabled = true;
            this.comboBoxBarometerInterval.Location = new System.Drawing.Point(348, 27);
            this.comboBoxBarometerInterval.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxBarometerInterval.Name = "comboBoxBarometerInterval";
            this.comboBoxBarometerInterval.Size = new System.Drawing.Size(68, 21);
            this.comboBoxBarometerInterval.TabIndex = 69;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.pictureBox1.Location = new System.Drawing.Point(431, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(235, 71);
            this.pictureBox1.TabIndex = 67;
            this.pictureBox1.TabStop = false;
            // 
            // labelKLinesTickerCount
            // 
            this.labelKLinesTickerCount.AutoSize = true;
            this.labelKLinesTickerCount.Location = new System.Drawing.Point(999, 53);
            this.labelKLinesTickerCount.Name = "labelKLinesTickerCount";
            this.labelKLinesTickerCount.Size = new System.Drawing.Size(90, 13);
            this.labelKLinesTickerCount.TabIndex = 66;
            this.labelKLinesTickerCount.Text = "1m candle stream";
            // 
            // labelAnalyseCount
            // 
            this.labelAnalyseCount.AutoSize = true;
            this.labelAnalyseCount.Location = new System.Drawing.Point(999, 33);
            this.labelAnalyseCount.Name = "labelAnalyseCount";
            this.labelAnalyseCount.Size = new System.Drawing.Size(74, 13);
            this.labelAnalyseCount.TabIndex = 60;
            this.labelAnalyseCount.Text = "Analyse count";
            // 
            // labelPriceTicker
            // 
            this.labelPriceTicker.AutoSize = true;
            this.labelPriceTicker.Location = new System.Drawing.Point(999, 11);
            this.labelPriceTicker.Name = "labelPriceTicker";
            this.labelPriceTicker.Size = new System.Drawing.Size(90, 13);
            this.labelPriceTicker.TabIndex = 59;
            this.labelPriceTicker.Text = "Price ticker count";
            // 
            // comboBoxBarometerQuote
            // 
            this.comboBoxBarometerQuote.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBarometerQuote.FormattingEnabled = true;
            this.comboBoxBarometerQuote.Location = new System.Drawing.Point(348, 3);
            this.comboBoxBarometerQuote.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxBarometerQuote.Name = "comboBoxBarometerQuote";
            this.comboBoxBarometerQuote.Size = new System.Drawing.Size(68, 21);
            this.comboBoxBarometerQuote.TabIndex = 41;
            // 
            // panelClient1
            // 
            this.panelClient1.Controls.Add(this.panelClient);
            this.panelClient1.Controls.Add(this.panelTop);
            this.panelClient1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelClient1.Location = new System.Drawing.Point(163, 0);
            this.panelClient1.Margin = new System.Windows.Forms.Padding(2);
            this.panelClient1.Name = "panelClient1";
            this.panelClient1.Size = new System.Drawing.Size(1322, 642);
            this.panelClient1.TabIndex = 12;
            // 
            // timerBarometer
            // 
            this.timerBarometer.Enabled = true;
            this.timerBarometer.Interval = 5000;
            this.timerBarometer.Tick += new System.EventHandler(this.timerBarometer_Tick);
            // 
            // timerSoundHeartBeat
            // 
            this.timerSoundHeartBeat.Interval = 600000;
            this.timerSoundHeartBeat.Tick += new System.EventHandler(this.timerSoundHeartBeat_Tick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // timerCandles
            // 
            this.timerCandles.Interval = 10000;
            this.timerCandles.Tick += new System.EventHandler(this.timerCandles_Tick);
            // 
            // timerAddSignal
            // 
            this.timerAddSignal.Enabled = true;
            this.timerAddSignal.Interval = 1250;
            this.timerAddSignal.Tick += new System.EventHandler(this.timerAddSignal_Tick);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1485, 642);
            this.Controls.Add(this.panelClient1);
            this.Controls.Add(this.panelLeft);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "FrmMain";
            this.panelLeft.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.listBoxSymbolsMenuStrip.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.applicationMenuStrip.ResumeLayout(false);
            this.applicationMenuStrip.PerformLayout();
            this.panelClient.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.tabPageSignals.ResumeLayout(false);
            this.listViewSignalsMenuStrip.ResumeLayout(false);
            this.tabPageBrowser.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.webViewTradingView)).EndInit();
            this.tabPageLog.ResumeLayout(false);
            this.tabPageLog.PerformLayout();
            this.tabPageAltrady.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.webViewAltrady)).EndInit();
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panelClient1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.Panel panelClient;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.ListBox listBoxSymbols;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panelClient1;
        private System.Windows.Forms.TabPage tabPageLog;
        private System.Windows.Forms.TextBox TextBoxLog;
        private System.Windows.Forms.ContextMenuStrip listBoxSymbolsMenuStrip;
        private System.Windows.Forms.TextBox symbolFilter;
        private System.Windows.Forms.Timer timerBarometer;
        private System.Windows.Forms.ComboBox comboBoxBarometerQuote;
        private System.Windows.Forms.MenuStrip applicationMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem MenuMain;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemSettings;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemRefresh;
        private System.Windows.Forms.ToolStripMenuItem clearMenusToolStripMenuItem;
        private System.Windows.Forms.Label labelAnalyseCount;
        private System.Windows.Forms.Label labelPriceTicker;
        private System.Windows.Forms.TabPage tabPageSignals;
        private System.Windows.Forms.Timer timerClearEvents;
        private ListViewDoubleBuffered listViewSignals;
        private System.Windows.Forms.TabPage tabPageBrowser;
        private Microsoft.Web.WebView2.WinForms.WebView2 webViewTradingView;
        private System.Windows.Forms.ContextMenuStrip listViewSignalsMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem listViewSignalsMenuItemClearSignals;
        private System.Windows.Forms.ToolStripMenuItem listViewSignalsMenuItemActivateTradingApp;
        private System.Windows.Forms.ToolStripMenuItem listViewSignalsMenuItemActivateTradingViewInternal;
        private System.Windows.Forms.ToolStripMenuItem listViewSignalsMenuItemActivateTradingApps;
        private System.Windows.Forms.ToolStripMenuItem listViewSignalsMenuItemActivateTradingViewExternal;
        private System.Windows.Forms.Label labelKLinesTickerCount;
        private System.Windows.Forms.ToolStripMenuItem listBoxSymbolsMenuItemActivateTradingviewInternal;
        private System.Windows.Forms.ToolStripMenuItem listBoxSymbolsMenuItemActivateTradingviewExternal;
        private System.Windows.Forms.ToolStripMenuItem listBoxSymbolsMenuItemActivateTradingApp;
        private System.Windows.Forms.ToolStripMenuItem listBoxSymbolsMenuItemActivateTradingApps;
        private System.Windows.Forms.TabPage tabPageAltrady;
        private Microsoft.Web.WebView2.WinForms.WebView2 webViewAltrady;
        private System.Windows.Forms.ToolStripMenuItem applicationMenuItemAbout;
        private System.Windows.Forms.Timer timerSoundHeartBeat;
        private System.Windows.Forms.ToolStripMenuItem listBoxSymbolsMenuItemShowTrendInformation;
        private System.Windows.Forms.ToolStripMenuItem listViewSignalsMenuItemShowTrendInformation;
        private System.Windows.Forms.ToolStripMenuItem listViewSignalsMenuItemCopySignal;
        private System.Windows.Forms.ToolStripMenuItem listBoxSymbolsMenuItemCopy;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ComboBox comboBoxBarometerInterval;
        private System.Windows.Forms.Label labelBarometerDateValue;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private CryptoSbmScanner.ListViewDoubleBuffered listViewSymbolPrices;
        private System.Windows.Forms.Timer timerCandles;
        private System.Windows.Forms.Label labelVersion;
        private CryptoSbmScanner.ListViewDoubleBuffered listViewInformation;
        private System.Windows.Forms.ToolStripMenuItem ApplicationPlaySounds;
        private System.Windows.Forms.ToolStripMenuItem ApplicationCreateSignals;
        private System.Windows.Forms.Timer timerAddSignal;
    }
}

