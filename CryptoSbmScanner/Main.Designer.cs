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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            panelLeft = new Panel();
            panel4 = new Panel();
            listBoxSymbols = new ListBox();
            listBoxSymbolsMenuStrip = new ContextMenuStrip(components);
            listBoxSymbolsMenuItemActivateTradingApp = new ToolStripMenuItem();
            listBoxSymbolsMenuItemActivateTradingviewInternal = new ToolStripMenuItem();
            listBoxSymbolsMenuItemActivateTradingviewExternal = new ToolStripMenuItem();
            listBoxSymbolsMenuItemShowTrendInformation = new ToolStripMenuItem();
            listBoxSymbolsMenuItemCopy = new ToolStripMenuItem();
            listBoxSymbolsMenuItemCandleDump = new ToolStripMenuItem();
            symbolsDumpToolStripMenuItem = new ToolStripMenuItem();
            panelLeftTop = new Panel();
            label1 = new Label();
            applicationMenuStrip = new MenuStrip();
            MenuMain = new ToolStripMenuItem();
            ApplicationPlaySounds = new ToolStripMenuItem();
            ApplicationCreateSignals = new ToolStripMenuItem();
            ApplicationTradingBot = new ToolStripMenuItem();
            ToolStripMenuItemSettings = new ToolStripMenuItem();
            ToolStripMenuItemRefresh = new ToolStripMenuItem();
            clearMenusToolStripMenuItem = new ToolStripMenuItem();
            applicationMenuItemAbout = new ToolStripMenuItem();
            backtestToolStripMenuItem = new ToolStripMenuItem();
            symbolFilter = new TextBox();
            panelClient = new Panel();
            tabControl = new TabControl();
            tabPageDashBoard = new TabPage();
            dashBoardControl1 = new DashBoardControl();
            tabPageSignals = new TabPage();
            tabPageBrowser = new TabPage();
            webViewTradingView = new Microsoft.Web.WebView2.WinForms.WebView2();
            tabPagePositionsOpen = new TabPage();
            tabPagePositionsClosed = new TabPage();
            tabPageLog = new TabPage();
            TextBoxLog = new TextBox();
            tabPagewebViewDummy = new TabPage();
            webViewDummy = new Microsoft.Web.WebView2.WinForms.WebView2();
            listViewSignalsMenuStrip = new ContextMenuStrip(components);
            listViewSignalsMenuItemActivateTradingApp = new ToolStripMenuItem();
            listViewSignalsMenuItemActivateTradingViewInternal = new ToolStripMenuItem();
            listViewSignalsMenuItemActivateTradingViewExternal = new ToolStripMenuItem();
            listViewSignalsMenuItemShowTrendInformation = new ToolStripMenuItem();
            listViewSignalsMenuItemCopySignal = new ToolStripMenuItem();
            listViewSignalsMenuItemCandleDump = new ToolStripMenuItem();
            panelClient1 = new Panel();
            dashBoardInformation1 = new TradingView.DashBoardInformation();
            contextMenuStripPositionsClosed = new ContextMenuStrip(components);
            contextMenuStripPositionsOpenRecalculate = new ToolStripMenuItem();
            debugDumpExcelToolStripMenuItem = new ToolStripMenuItem();
            contextMenuStripPositionsOpen = new ContextMenuStrip(components);
            debugDumpExcelToolStripMenuItem1 = new ToolStripMenuItem();
            listViewIgnalsColumns = new ContextMenuStrip(components);
            testToolStripMenuItem = new ToolStripMenuItem();
            test2ToolStripMenuItem = new ToolStripMenuItem();
            panelLeft.SuspendLayout();
            panel4.SuspendLayout();
            listBoxSymbolsMenuStrip.SuspendLayout();
            panelLeftTop.SuspendLayout();
            applicationMenuStrip.SuspendLayout();
            panelClient.SuspendLayout();
            tabControl.SuspendLayout();
            tabPageDashBoard.SuspendLayout();
            tabPageBrowser.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webViewTradingView).BeginInit();
            tabPageLog.SuspendLayout();
            tabPagewebViewDummy.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webViewDummy).BeginInit();
            listViewSignalsMenuStrip.SuspendLayout();
            panelClient1.SuspendLayout();
            contextMenuStripPositionsClosed.SuspendLayout();
            contextMenuStripPositionsOpen.SuspendLayout();
            listViewIgnalsColumns.SuspendLayout();
            SuspendLayout();
            // 
            // panelLeft
            // 
            panelLeft.Controls.Add(panel4);
            panelLeft.Controls.Add(panelLeftTop);
            panelLeft.Dock = DockStyle.Left;
            panelLeft.Location = new Point(0, 0);
            panelLeft.Margin = new Padding(2);
            panelLeft.Name = "panelLeft";
            panelLeft.Size = new Size(190, 741);
            panelLeft.TabIndex = 12;
            // 
            // panel4
            // 
            panel4.Controls.Add(listBoxSymbols);
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new Point(0, 103);
            panel4.Margin = new Padding(2);
            panel4.Name = "panel4";
            panel4.Size = new Size(190, 638);
            panel4.TabIndex = 2;
            // 
            // listBoxSymbols
            // 
            listBoxSymbols.ContextMenuStrip = listBoxSymbolsMenuStrip;
            listBoxSymbols.Dock = DockStyle.Fill;
            listBoxSymbols.FormattingEnabled = true;
            listBoxSymbols.ItemHeight = 15;
            listBoxSymbols.Location = new Point(0, 0);
            listBoxSymbols.Margin = new Padding(2);
            listBoxSymbols.Name = "listBoxSymbols";
            listBoxSymbols.Size = new Size(190, 638);
            listBoxSymbols.Sorted = true;
            listBoxSymbols.TabIndex = 0;
            // 
            // listBoxSymbolsMenuStrip
            // 
            listBoxSymbolsMenuStrip.ImageScalingSize = new Size(20, 20);
            listBoxSymbolsMenuStrip.Items.AddRange(new ToolStripItem[] { listBoxSymbolsMenuItemActivateTradingApp, listBoxSymbolsMenuItemActivateTradingviewInternal, listBoxSymbolsMenuItemActivateTradingviewExternal, listBoxSymbolsMenuItemShowTrendInformation, listBoxSymbolsMenuItemCopy, listBoxSymbolsMenuItemCandleDump, symbolsDumpToolStripMenuItem });
            listBoxSymbolsMenuStrip.Name = "contextMenuStrip1";
            listBoxSymbolsMenuStrip.Size = new Size(183, 180);
            // 
            // listBoxSymbolsMenuItemActivateTradingApp
            // 
            listBoxSymbolsMenuItemActivateTradingApp.Name = "listBoxSymbolsMenuItemActivateTradingApp";
            listBoxSymbolsMenuItemActivateTradingApp.Size = new Size(182, 22);
            listBoxSymbolsMenuItemActivateTradingApp.Text = "Activate trading app";
            listBoxSymbolsMenuItemActivateTradingApp.Click += ListBoxSymbolsMenuItemActivateTradingApp_Click;
            // 
            // listBoxSymbolsMenuItemActivateTradingviewInternal
            // 
            listBoxSymbolsMenuItemActivateTradingviewInternal.Name = "listBoxSymbolsMenuItemActivateTradingviewInternal";
            listBoxSymbolsMenuItemActivateTradingviewInternal.Size = new Size(182, 22);
            listBoxSymbolsMenuItemActivateTradingviewInternal.Text = "Tradingview browser";
            listBoxSymbolsMenuItemActivateTradingviewInternal.Click += ListBoxSymbolsMenuItemActivateTradingviewInternal_Click;
            // 
            // listBoxSymbolsMenuItemActivateTradingviewExternal
            // 
            listBoxSymbolsMenuItemActivateTradingviewExternal.Name = "listBoxSymbolsMenuItemActivateTradingviewExternal";
            listBoxSymbolsMenuItemActivateTradingviewExternal.Size = new Size(182, 22);
            listBoxSymbolsMenuItemActivateTradingviewExternal.Text = "Tradingview external";
            listBoxSymbolsMenuItemActivateTradingviewExternal.Click += ListBoxSymbolsMenuItemActivateTradingviewExternal_Click;
            // 
            // listBoxSymbolsMenuItemShowTrendInformation
            // 
            listBoxSymbolsMenuItemShowTrendInformation.Name = "listBoxSymbolsMenuItemShowTrendInformation";
            listBoxSymbolsMenuItemShowTrendInformation.Size = new Size(182, 22);
            listBoxSymbolsMenuItemShowTrendInformation.Text = "Trend informatie";
            listBoxSymbolsMenuItemShowTrendInformation.Click += MenuSymbolsShowTrendInformation_Click;
            // 
            // listBoxSymbolsMenuItemCopy
            // 
            listBoxSymbolsMenuItemCopy.Name = "listBoxSymbolsMenuItemCopy";
            listBoxSymbolsMenuItemCopy.Size = new Size(182, 22);
            listBoxSymbolsMenuItemCopy.Text = "Copy";
            listBoxSymbolsMenuItemCopy.Click += ListBoxSymbolsMenuItemCopy_Click;
            // 
            // listBoxSymbolsMenuItemCandleDump
            // 
            listBoxSymbolsMenuItemCandleDump.Name = "listBoxSymbolsMenuItemCandleDump";
            listBoxSymbolsMenuItemCandleDump.Size = new Size(182, 22);
            listBoxSymbolsMenuItemCandleDump.Text = "Candle dump";
            listBoxSymbolsMenuItemCandleDump.Click += ListBoxSymbolsMenuItemCandleDump_Click;
            // 
            // symbolsDumpToolStripMenuItem
            // 
            symbolsDumpToolStripMenuItem.Name = "symbolsDumpToolStripMenuItem";
            symbolsDumpToolStripMenuItem.Size = new Size(182, 22);
            symbolsDumpToolStripMenuItem.Text = "Symbols dump";
            symbolsDumpToolStripMenuItem.Click += symbolsDumpToolStripMenuItem_Click;
            // 
            // panelLeftTop
            // 
            panelLeftTop.Controls.Add(label1);
            panelLeftTop.Controls.Add(applicationMenuStrip);
            panelLeftTop.Controls.Add(symbolFilter);
            panelLeftTop.Dock = DockStyle.Top;
            panelLeftTop.Location = new Point(0, 0);
            panelLeftTop.Margin = new Padding(2);
            panelLeftTop.Name = "panelLeftTop";
            panelLeftTop.Size = new Size(190, 103);
            panelLeftTop.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 44);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(36, 15);
            label1.TabIndex = 71;
            label1.Text = "Filter:";
            // 
            // applicationMenuStrip
            // 
            applicationMenuStrip.ImageScalingSize = new Size(20, 20);
            applicationMenuStrip.Items.AddRange(new ToolStripItem[] { MenuMain });
            applicationMenuStrip.Location = new Point(0, 0);
            applicationMenuStrip.Name = "applicationMenuStrip";
            applicationMenuStrip.Padding = new Padding(5, 2, 0, 2);
            applicationMenuStrip.RenderMode = ToolStripRenderMode.Professional;
            applicationMenuStrip.Size = new Size(190, 24);
            applicationMenuStrip.TabIndex = 16;
            applicationMenuStrip.Text = "menuStrip1";
            // 
            // MenuMain
            // 
            MenuMain.DropDownItems.AddRange(new ToolStripItem[] { ApplicationPlaySounds, ApplicationCreateSignals, ApplicationTradingBot, ToolStripMenuItemSettings, ToolStripMenuItemRefresh, clearMenusToolStripMenuItem, applicationMenuItemAbout, backtestToolStripMenuItem });
            MenuMain.Name = "MenuMain";
            MenuMain.Size = new Size(50, 20);
            MenuMain.Text = "Menu";
            // 
            // ApplicationPlaySounds
            // 
            ApplicationPlaySounds.Checked = true;
            ApplicationPlaySounds.CheckState = CheckState.Checked;
            ApplicationPlaySounds.Name = "ApplicationPlaySounds";
            ApplicationPlaySounds.Size = new Size(168, 22);
            ApplicationPlaySounds.Text = "Geluiden afspelen";
            ApplicationPlaySounds.Click += ApplicationPlaySounds_Click;
            // 
            // ApplicationCreateSignals
            // 
            ApplicationCreateSignals.Checked = true;
            ApplicationCreateSignals.CheckState = CheckState.Checked;
            ApplicationCreateSignals.Name = "ApplicationCreateSignals";
            ApplicationCreateSignals.Size = new Size(168, 22);
            ApplicationCreateSignals.Text = "Signalen maken";
            ApplicationCreateSignals.Click += ApplicationCreateSignals_Click;
            // 
            // ApplicationTradingBot
            // 
            ApplicationTradingBot.Name = "ApplicationTradingBot";
            ApplicationTradingBot.Size = new Size(168, 22);
            ApplicationTradingBot.Text = "Trading bot actief";
            ApplicationTradingBot.Click += ApplicationTradingBot_Click;
            // 
            // ToolStripMenuItemSettings
            // 
            ToolStripMenuItemSettings.Name = "ToolStripMenuItemSettings";
            ToolStripMenuItemSettings.Size = new Size(168, 22);
            ToolStripMenuItemSettings.Text = "Instellingen";
            ToolStripMenuItemSettings.Click += ToolStripMenuItemSettings_Click;
            // 
            // ToolStripMenuItemRefresh
            // 
            ToolStripMenuItemRefresh.Name = "ToolStripMenuItemRefresh";
            ToolStripMenuItemRefresh.Size = new Size(168, 22);
            ToolStripMenuItemRefresh.Text = "Verversen";
            ToolStripMenuItemRefresh.Click += ToolStripMenuItemRefresh_Click_1;
            // 
            // clearMenusToolStripMenuItem
            // 
            clearMenusToolStripMenuItem.Name = "clearMenusToolStripMenuItem";
            clearMenusToolStripMenuItem.Size = new Size(168, 22);
            clearMenusToolStripMenuItem.Text = "Clear";
            clearMenusToolStripMenuItem.Click += MainMenuClearAll_Click;
            // 
            // applicationMenuItemAbout
            // 
            applicationMenuItemAbout.Name = "applicationMenuItemAbout";
            applicationMenuItemAbout.Size = new Size(168, 22);
            applicationMenuItemAbout.Text = "About";
            applicationMenuItemAbout.Click += ApplicationMenuItemAbout_Click;
            // 
            // backtestToolStripMenuItem
            // 
            backtestToolStripMenuItem.Name = "backtestToolStripMenuItem";
            backtestToolStripMenuItem.Size = new Size(168, 22);
            backtestToolStripMenuItem.Text = "Backtest";
            backtestToolStripMenuItem.Click += BacktestToolStripMenuItem_Click;
            // 
            // symbolFilter
            // 
            symbolFilter.Location = new Point(14, 61);
            symbolFilter.Margin = new Padding(2);
            symbolFilter.Name = "symbolFilter";
            symbolFilter.Size = new Size(164, 23);
            symbolFilter.TabIndex = 0;
            symbolFilter.KeyDown += SymbolFilter_KeyDown;
            // 
            // panelClient
            // 
            panelClient.Controls.Add(tabControl);
            panelClient.Dock = DockStyle.Fill;
            panelClient.Location = new Point(0, 103);
            panelClient.Margin = new Padding(2);
            panelClient.Name = "panelClient";
            panelClient.Size = new Size(1542, 638);
            panelClient.TabIndex = 13;
            // 
            // tabControl
            // 
            tabControl.Appearance = TabAppearance.FlatButtons;
            tabControl.Controls.Add(tabPageDashBoard);
            tabControl.Controls.Add(tabPageSignals);
            tabControl.Controls.Add(tabPageBrowser);
            tabControl.Controls.Add(tabPagePositionsOpen);
            tabControl.Controls.Add(tabPagePositionsClosed);
            tabControl.Controls.Add(tabPageLog);
            tabControl.Controls.Add(tabPagewebViewDummy);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Margin = new Padding(2);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1542, 638);
            tabControl.TabIndex = 12;
            // 
            // tabPageDashBoard
            // 
            tabPageDashBoard.Controls.Add(dashBoardControl1);
            tabPageDashBoard.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            tabPageDashBoard.Location = new Point(4, 27);
            tabPageDashBoard.Name = "tabPageDashBoard";
            tabPageDashBoard.Padding = new Padding(3);
            tabPageDashBoard.Size = new Size(1534, 607);
            tabPageDashBoard.TabIndex = 9;
            tabPageDashBoard.Text = "Dashboard";
            tabPageDashBoard.UseVisualStyleBackColor = true;
            // 
            // dashBoardControl1
            // 
            dashBoardControl1.Dock = DockStyle.Fill;
            dashBoardControl1.Location = new Point(3, 3);
            dashBoardControl1.Name = "dashBoardControl1";
            dashBoardControl1.Size = new Size(1528, 601);
            dashBoardControl1.TabIndex = 0;
            // 
            // tabPageSignals
            // 
            tabPageSignals.Location = new Point(4, 27);
            tabPageSignals.Margin = new Padding(4, 3, 4, 3);
            tabPageSignals.Name = "tabPageSignals";
            tabPageSignals.Padding = new Padding(4, 3, 4, 3);
            tabPageSignals.Size = new Size(1534, 607);
            tabPageSignals.TabIndex = 4;
            tabPageSignals.Text = "Signals";
            tabPageSignals.UseVisualStyleBackColor = true;
            // 
            // tabPageBrowser
            // 
            tabPageBrowser.Controls.Add(webViewTradingView);
            tabPageBrowser.Location = new Point(4, 27);
            tabPageBrowser.Margin = new Padding(4, 3, 4, 3);
            tabPageBrowser.Name = "tabPageBrowser";
            tabPageBrowser.Padding = new Padding(4, 3, 4, 3);
            tabPageBrowser.Size = new Size(1534, 607);
            tabPageBrowser.TabIndex = 5;
            tabPageBrowser.Text = "Tradingview";
            tabPageBrowser.UseVisualStyleBackColor = true;
            // 
            // webViewTradingView
            // 
            webViewTradingView.AllowExternalDrop = true;
            webViewTradingView.CreationProperties = null;
            webViewTradingView.DefaultBackgroundColor = Color.White;
            webViewTradingView.Dock = DockStyle.Fill;
            webViewTradingView.Location = new Point(4, 3);
            webViewTradingView.Margin = new Padding(4, 3, 4, 3);
            webViewTradingView.Name = "webViewTradingView";
            webViewTradingView.Size = new Size(1526, 601);
            webViewTradingView.TabIndex = 0;
            webViewTradingView.ZoomFactor = 1D;
            // 
            // tabPagePositionsOpen
            // 
            tabPagePositionsOpen.Location = new Point(4, 27);
            tabPagePositionsOpen.Name = "tabPagePositionsOpen";
            tabPagePositionsOpen.Padding = new Padding(3);
            tabPagePositionsOpen.Size = new Size(1534, 607);
            tabPagePositionsOpen.TabIndex = 7;
            tabPagePositionsOpen.Text = "Open positions";
            tabPagePositionsOpen.UseVisualStyleBackColor = true;
            // 
            // tabPagePositionsClosed
            // 
            tabPagePositionsClosed.Location = new Point(4, 27);
            tabPagePositionsClosed.Name = "tabPagePositionsClosed";
            tabPagePositionsClosed.Padding = new Padding(3);
            tabPagePositionsClosed.Size = new Size(1534, 607);
            tabPagePositionsClosed.TabIndex = 8;
            tabPagePositionsClosed.Text = "Closed positions";
            tabPagePositionsClosed.UseVisualStyleBackColor = true;
            // 
            // tabPageLog
            // 
            tabPageLog.Controls.Add(TextBoxLog);
            tabPageLog.Location = new Point(4, 27);
            tabPageLog.Margin = new Padding(2);
            tabPageLog.Name = "tabPageLog";
            tabPageLog.Padding = new Padding(2);
            tabPageLog.Size = new Size(1534, 607);
            tabPageLog.TabIndex = 1;
            tabPageLog.Text = "Log";
            tabPageLog.UseVisualStyleBackColor = true;
            // 
            // TextBoxLog
            // 
            TextBoxLog.BorderStyle = BorderStyle.None;
            TextBoxLog.Dock = DockStyle.Fill;
            TextBoxLog.Location = new Point(2, 2);
            TextBoxLog.Margin = new Padding(2);
            TextBoxLog.Multiline = true;
            TextBoxLog.Name = "TextBoxLog";
            TextBoxLog.ScrollBars = ScrollBars.Both;
            TextBoxLog.Size = new Size(1530, 603);
            TextBoxLog.TabIndex = 1;
            // 
            // tabPagewebViewDummy
            // 
            tabPagewebViewDummy.Controls.Add(webViewDummy);
            tabPagewebViewDummy.Location = new Point(4, 27);
            tabPagewebViewDummy.Margin = new Padding(4, 3, 4, 3);
            tabPagewebViewDummy.Name = "tabPagewebViewDummy";
            tabPagewebViewDummy.Padding = new Padding(4, 3, 4, 3);
            tabPagewebViewDummy.Size = new Size(1534, 607);
            tabPagewebViewDummy.TabIndex = 6;
            tabPagewebViewDummy.Text = "WebView (dummy)";
            tabPagewebViewDummy.UseVisualStyleBackColor = true;
            // 
            // webViewDummy
            // 
            webViewDummy.AllowExternalDrop = true;
            webViewDummy.CreationProperties = null;
            webViewDummy.DefaultBackgroundColor = Color.White;
            webViewDummy.Dock = DockStyle.Fill;
            webViewDummy.Location = new Point(4, 3);
            webViewDummy.Margin = new Padding(4, 3, 4, 3);
            webViewDummy.Name = "webViewDummy";
            webViewDummy.Size = new Size(1526, 601);
            webViewDummy.TabIndex = 1;
            webViewDummy.ZoomFactor = 1D;
            // 
            // listViewSignalsMenuStrip
            // 
            listViewSignalsMenuStrip.Items.AddRange(new ToolStripItem[] { listViewSignalsMenuItemActivateTradingApp, listViewSignalsMenuItemActivateTradingViewInternal, listViewSignalsMenuItemActivateTradingViewExternal, listViewSignalsMenuItemShowTrendInformation, listViewSignalsMenuItemCopySignal, listViewSignalsMenuItemCandleDump });
            listViewSignalsMenuStrip.Name = "contextMenuStripSignals";
            listViewSignalsMenuStrip.Size = new Size(184, 136);
            // 
            // listViewSignalsMenuItemActivateTradingApp
            // 
            listViewSignalsMenuItemActivateTradingApp.Name = "listViewSignalsMenuItemActivateTradingApp";
            listViewSignalsMenuItemActivateTradingApp.Size = new Size(183, 22);
            listViewSignalsMenuItemActivateTradingApp.Text = "Activate trading app";
            listViewSignalsMenuItemActivateTradingApp.Click += ListViewSignalsMenuItemActivateTradingApp_Click;
            // 
            // listViewSignalsMenuItemActivateTradingViewInternal
            // 
            listViewSignalsMenuItemActivateTradingViewInternal.Name = "listViewSignalsMenuItemActivateTradingViewInternal";
            listViewSignalsMenuItemActivateTradingViewInternal.Size = new Size(183, 22);
            listViewSignalsMenuItemActivateTradingViewInternal.Text = "TradingView browser";
            listViewSignalsMenuItemActivateTradingViewInternal.Click += ListViewSignalsMenuItemActivateTradingViewInternal_Click;
            // 
            // listViewSignalsMenuItemActivateTradingViewExternal
            // 
            listViewSignalsMenuItemActivateTradingViewExternal.Name = "listViewSignalsMenuItemActivateTradingViewExternal";
            listViewSignalsMenuItemActivateTradingViewExternal.Size = new Size(183, 22);
            listViewSignalsMenuItemActivateTradingViewExternal.Text = "Tradingview extern";
            listViewSignalsMenuItemActivateTradingViewExternal.Click += ListViewSignalsMenuItemActivateTradingviewExternal_Click;
            // 
            // listViewSignalsMenuItemShowTrendInformation
            // 
            listViewSignalsMenuItemShowTrendInformation.Name = "listViewSignalsMenuItemShowTrendInformation";
            listViewSignalsMenuItemShowTrendInformation.Size = new Size(183, 22);
            listViewSignalsMenuItemShowTrendInformation.Text = "Trend informatie";
            listViewSignalsMenuItemShowTrendInformation.Click += MenuSignalsShowTrendInformation_Click;
            // 
            // listViewSignalsMenuItemCopySignal
            // 
            listViewSignalsMenuItemCopySignal.Name = "listViewSignalsMenuItemCopySignal";
            listViewSignalsMenuItemCopySignal.Size = new Size(183, 22);
            listViewSignalsMenuItemCopySignal.Text = "Copy";
            listViewSignalsMenuItemCopySignal.Click += ListViewSignalsMenuItemCopySignal_Click;
            // 
            // listViewSignalsMenuItemCandleDump
            // 
            listViewSignalsMenuItemCandleDump.Name = "listViewSignalsMenuItemCandleDump";
            listViewSignalsMenuItemCandleDump.Size = new Size(183, 22);
            listViewSignalsMenuItemCandleDump.Text = "Candle Debug";
            listViewSignalsMenuItemCandleDump.Click += ListViewSignalsMenuItemCandleDump_Click;
            // 
            // panelClient1
            // 
            panelClient1.Controls.Add(panelClient);
            panelClient1.Controls.Add(dashBoardInformation1);
            panelClient1.Dock = DockStyle.Fill;
            panelClient1.Location = new Point(190, 0);
            panelClient1.Margin = new Padding(2);
            panelClient1.Name = "panelClient1";
            panelClient1.Size = new Size(1542, 741);
            panelClient1.TabIndex = 12;
            // 
            // dashBoardInformation1
            // 
            dashBoardInformation1.Dock = DockStyle.Top;
            dashBoardInformation1.Location = new Point(0, 0);
            dashBoardInformation1.Name = "dashBoardInformation1";
            dashBoardInformation1.Size = new Size(1542, 103);
            dashBoardInformation1.TabIndex = 15;
            // 
            // contextMenuStripPositionsClosed
            // 
            contextMenuStripPositionsClosed.Items.AddRange(new ToolStripItem[] { contextMenuStripPositionsOpenRecalculate, debugDumpExcelToolStripMenuItem });
            contextMenuStripPositionsClosed.Name = "contextMenuStrip1";
            contextMenuStripPositionsClosed.Size = new Size(175, 48);
            // 
            // contextMenuStripPositionsOpenRecalculate
            // 
            contextMenuStripPositionsOpenRecalculate.Name = "contextMenuStripPositionsOpenRecalculate";
            contextMenuStripPositionsOpenRecalculate.Size = new Size(174, 22);
            contextMenuStripPositionsOpenRecalculate.Text = "Herberekenen";
            contextMenuStripPositionsOpenRecalculate.Click += ContextMenuStripPositionsOpenRecalculateAsync_Click;
            // 
            // debugDumpExcelToolStripMenuItem
            // 
            debugDumpExcelToolStripMenuItem.Name = "debugDumpExcelToolStripMenuItem";
            debugDumpExcelToolStripMenuItem.Size = new Size(174, 22);
            debugDumpExcelToolStripMenuItem.Text = "Debug dump Excel";
            debugDumpExcelToolStripMenuItem.Click += DebugPositionClosedDumpExcelToolStripMenuItemAsync_Click;
            // 
            // contextMenuStripPositionsOpen
            // 
            contextMenuStripPositionsOpen.Items.AddRange(new ToolStripItem[] { debugDumpExcelToolStripMenuItem1 });
            contextMenuStripPositionsOpen.Name = "contextMenuStripPositionsOpen";
            contextMenuStripPositionsOpen.Size = new Size(175, 26);
            // 
            // debugDumpExcelToolStripMenuItem1
            // 
            debugDumpExcelToolStripMenuItem1.Name = "debugDumpExcelToolStripMenuItem1";
            debugDumpExcelToolStripMenuItem1.Size = new Size(174, 22);
            debugDumpExcelToolStripMenuItem1.Text = "Debug dump Excel";
            debugDumpExcelToolStripMenuItem1.Click += DebugPositionOpenDumpExcelToolStripMenuItemAsync_Click;
            // 
            // listViewIgnalsColumns
            // 
            listViewIgnalsColumns.Items.AddRange(new ToolStripItem[] { testToolStripMenuItem, test2ToolStripMenuItem });
            listViewIgnalsColumns.Name = "listViewIgnalsColumns";
            listViewIgnalsColumns.Size = new Size(101, 48);
            // 
            // testToolStripMenuItem
            // 
            testToolStripMenuItem.Name = "testToolStripMenuItem";
            testToolStripMenuItem.Size = new Size(100, 22);
            testToolStripMenuItem.Text = "Test";
            // 
            // test2ToolStripMenuItem
            // 
            test2ToolStripMenuItem.Name = "test2ToolStripMenuItem";
            test2ToolStripMenuItem.Size = new Size(100, 22);
            test2ToolStripMenuItem.Text = "Test2";
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1732, 741);
            Controls.Add(panelClient1);
            Controls.Add(panelLeft);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2);
            Name = "FrmMain";
            panelLeft.ResumeLayout(false);
            panel4.ResumeLayout(false);
            listBoxSymbolsMenuStrip.ResumeLayout(false);
            panelLeftTop.ResumeLayout(false);
            panelLeftTop.PerformLayout();
            applicationMenuStrip.ResumeLayout(false);
            applicationMenuStrip.PerformLayout();
            panelClient.ResumeLayout(false);
            tabControl.ResumeLayout(false);
            tabPageDashBoard.ResumeLayout(false);
            tabPageBrowser.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webViewTradingView).EndInit();
            tabPageLog.ResumeLayout(false);
            tabPageLog.PerformLayout();
            tabPagewebViewDummy.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webViewDummy).EndInit();
            listViewSignalsMenuStrip.ResumeLayout(false);
            panelClient1.ResumeLayout(false);
            contextMenuStripPositionsClosed.ResumeLayout(false);
            contextMenuStripPositionsOpen.ResumeLayout(false);
            listViewIgnalsColumns.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Panel panelLeft;
        private Panel panelClient;
        private ListBox listBoxSymbols;
        private Panel panelLeftTop;
        private Panel panel4;
        private Panel panelClient1;
        private ContextMenuStrip listBoxSymbolsMenuStrip;
        private TextBox symbolFilter;
        private MenuStrip applicationMenuStrip;
        private ToolStripMenuItem MenuMain;
        private ToolStripMenuItem ToolStripMenuItemSettings;
        private ToolStripMenuItem ToolStripMenuItemRefresh;
        private ToolStripMenuItem clearMenusToolStripMenuItem;
        private ContextMenuStrip listViewSignalsMenuStrip;
        private ToolStripMenuItem listViewSignalsMenuItemActivateTradingApp;
        private ToolStripMenuItem listViewSignalsMenuItemActivateTradingViewInternal;
        private ToolStripMenuItem listViewSignalsMenuItemActivateTradingViewExternal;
        private ToolStripMenuItem listBoxSymbolsMenuItemActivateTradingviewInternal;
        private ToolStripMenuItem listBoxSymbolsMenuItemActivateTradingviewExternal;
        private ToolStripMenuItem listBoxSymbolsMenuItemActivateTradingApp;
        private ToolStripMenuItem applicationMenuItemAbout;
        private ToolStripMenuItem listBoxSymbolsMenuItemShowTrendInformation;
        private ToolStripMenuItem listViewSignalsMenuItemShowTrendInformation;
        private ToolStripMenuItem listViewSignalsMenuItemCopySignal;
        private ToolStripMenuItem listBoxSymbolsMenuItemCopy;
        private ContextMenuStrip contextMenuStripPositionsClosed;
        private ToolStripMenuItem ApplicationPlaySounds;
        private ToolStripMenuItem ApplicationCreateSignals;
        private ToolStripMenuItem backtestToolStripMenuItem;
        private ToolStripMenuItem ApplicationTradingBot;
        private Label label1;
        private TabControl tabControl;
        private TabPage tabPageSignals;
        private TabPage tabPageBrowser;
        private Microsoft.Web.WebView2.WinForms.WebView2 webViewTradingView;
        private TabPage tabPageLog;
        private TextBox TextBoxLog;
        private TabPage tabPagewebViewDummy;
        private Microsoft.Web.WebView2.WinForms.WebView2 webViewDummy;
        private TabPage tabPagePositionsOpen;
        private TabPage tabPagePositionsClosed;
        private ToolStripMenuItem contextMenuStripPositionsOpenRecalculate;
        private ContextMenuStrip contextMenuStripPositionsOpen;
        private TabPage tabPageDashBoard;
        private DashBoardControl dashBoardControl1;
        private ToolStripMenuItem listBoxSymbolsMenuItemCandleDump;
        private ToolStripMenuItem listViewSignalsMenuItemCandleDump;
        private ToolStripMenuItem debugDumpExcelToolStripMenuItem;
        private ToolStripMenuItem debugDumpExcelToolStripMenuItem1;
        private TradingView.DashBoardInformation dashBoardInformation1;
        private ContextMenuStrip listViewIgnalsColumns;
        private ToolStripMenuItem testToolStripMenuItem;
        private ToolStripMenuItem test2ToolStripMenuItem;
        private ToolStripMenuItem symbolsDumpToolStripMenuItem;
    }
}

