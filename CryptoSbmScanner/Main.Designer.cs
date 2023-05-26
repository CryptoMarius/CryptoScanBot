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
            listBoxSymbolsMenuItemActivateTradingApps = new ToolStripMenuItem();
            listBoxSymbolsMenuItemActivateTradingviewInternal = new ToolStripMenuItem();
            listBoxSymbolsMenuItemActivateTradingviewExternal = new ToolStripMenuItem();
            listBoxSymbolsMenuItemShowTrendInformation = new ToolStripMenuItem();
            listBoxSymbolsMenuItemCopy = new ToolStripMenuItem();
            panel3 = new Panel();
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
            listViewSignalsMenuStrip = new ContextMenuStrip(components);
            listViewSignalsMenuItemActivateTradingApp = new ToolStripMenuItem();
            listViewSignalsMenuItemActivateTradingApps = new ToolStripMenuItem();
            listViewSignalsMenuItemActivateTradingViewInternal = new ToolStripMenuItem();
            listViewSignalsMenuItemActivateTradingViewExternal = new ToolStripMenuItem();
            listViewSignalsMenuItemShowTrendInformation = new ToolStripMenuItem();
            listViewSignalsMenuItemClearSignals = new ToolStripMenuItem();
            listViewSignalsMenuItemCopySignal = new ToolStripMenuItem();
            panelTop = new Panel();
            labelBarometerDateValue = new Label();
            comboBoxBarometerInterval = new ComboBox();
            pictureBox1 = new PictureBox();
            comboBoxBarometerQuote = new ComboBox();
            panelClient1 = new Panel();
            contextMenuStrip1 = new ContextMenuStrip(components);
            tabPagePositions = new TabPage();
            tabPageAltrady = new TabPage();
            webViewAltrady = new Microsoft.Web.WebView2.WinForms.WebView2();
            tabPageLog = new TabPage();
            TextBoxLog = new TextBox();
            tabPageBrowser = new TabPage();
            webViewTradingView = new Microsoft.Web.WebView2.WinForms.WebView2();
            tabPageSignals = new TabPage();
            tabControl = new TabControl();
            panelLeft.SuspendLayout();
            panel4.SuspendLayout();
            listBoxSymbolsMenuStrip.SuspendLayout();
            panel3.SuspendLayout();
            applicationMenuStrip.SuspendLayout();
            panelClient.SuspendLayout();
            listViewSignalsMenuStrip.SuspendLayout();
            panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panelClient1.SuspendLayout();
            tabPageAltrady.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webViewAltrady).BeginInit();
            tabPageLog.SuspendLayout();
            tabPageBrowser.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webViewTradingView).BeginInit();
            tabControl.SuspendLayout();
            SuspendLayout();
            // 
            // panelLeft
            // 
            panelLeft.Controls.Add(panel4);
            panelLeft.Controls.Add(panel3);
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
            panel4.Location = new Point(0, 93);
            panel4.Margin = new Padding(2);
            panel4.Name = "panel4";
            panel4.Size = new Size(190, 648);
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
            listBoxSymbols.Size = new Size(190, 648);
            listBoxSymbols.Sorted = true;
            listBoxSymbols.TabIndex = 0;
            // 
            // listBoxSymbolsMenuStrip
            // 
            listBoxSymbolsMenuStrip.ImageScalingSize = new Size(20, 20);
            listBoxSymbolsMenuStrip.Items.AddRange(new ToolStripItem[] { listBoxSymbolsMenuItemActivateTradingApp, listBoxSymbolsMenuItemActivateTradingApps, listBoxSymbolsMenuItemActivateTradingviewInternal, listBoxSymbolsMenuItemActivateTradingviewExternal, listBoxSymbolsMenuItemShowTrendInformation, listBoxSymbolsMenuItemCopy });
            listBoxSymbolsMenuStrip.Name = "contextMenuStrip1";
            listBoxSymbolsMenuStrip.Size = new Size(215, 136);
            // 
            // listBoxSymbolsMenuItemActivateTradingApp
            // 
            listBoxSymbolsMenuItemActivateTradingApp.Name = "listBoxSymbolsMenuItemActivateTradingApp";
            listBoxSymbolsMenuItemActivateTradingApp.Size = new Size(214, 22);
            listBoxSymbolsMenuItemActivateTradingApp.Text = "Activate trading app";
            listBoxSymbolsMenuItemActivateTradingApp.Click += ListBoxSymbolsMenuItemActivateTradingApp_Click;
            // 
            // listBoxSymbolsMenuItemActivateTradingApps
            // 
            listBoxSymbolsMenuItemActivateTradingApps.Name = "listBoxSymbolsMenuItemActivateTradingApps";
            listBoxSymbolsMenuItemActivateTradingApps.Size = new Size(214, 22);
            listBoxSymbolsMenuItemActivateTradingApps.Text = "Trading app + TradingView";
            listBoxSymbolsMenuItemActivateTradingApps.Click += ListBoxSymbolsMenuItemActivateTradingApps_Click;
            // 
            // listBoxSymbolsMenuItemActivateTradingviewInternal
            // 
            listBoxSymbolsMenuItemActivateTradingviewInternal.Name = "listBoxSymbolsMenuItemActivateTradingviewInternal";
            listBoxSymbolsMenuItemActivateTradingviewInternal.Size = new Size(214, 22);
            listBoxSymbolsMenuItemActivateTradingviewInternal.Text = "Tradingview browser";
            listBoxSymbolsMenuItemActivateTradingviewInternal.Click += ListBoxSymbolsMenuItemActivateTradingviewInternal_Click;
            // 
            // listBoxSymbolsMenuItemActivateTradingviewExternal
            // 
            listBoxSymbolsMenuItemActivateTradingviewExternal.Name = "listBoxSymbolsMenuItemActivateTradingviewExternal";
            listBoxSymbolsMenuItemActivateTradingviewExternal.Size = new Size(214, 22);
            listBoxSymbolsMenuItemActivateTradingviewExternal.Text = "Tradingview external";
            listBoxSymbolsMenuItemActivateTradingviewExternal.Click += ListBoxSymbolsMenuItemActivateTradingviewExternal_Click;
            // 
            // listBoxSymbolsMenuItemShowTrendInformation
            // 
            listBoxSymbolsMenuItemShowTrendInformation.Name = "listBoxSymbolsMenuItemShowTrendInformation";
            listBoxSymbolsMenuItemShowTrendInformation.Size = new Size(214, 22);
            listBoxSymbolsMenuItemShowTrendInformation.Text = "Trend informatie";
            listBoxSymbolsMenuItemShowTrendInformation.Click += MenuSymbolsShowTrendInformation_Click;
            // 
            // listBoxSymbolsMenuItemCopy
            // 
            listBoxSymbolsMenuItemCopy.Name = "listBoxSymbolsMenuItemCopy";
            listBoxSymbolsMenuItemCopy.Size = new Size(214, 22);
            listBoxSymbolsMenuItemCopy.Text = "Copy";
            listBoxSymbolsMenuItemCopy.Click += ListBoxSymbolsMenuItemCopy_Click;
            // 
            // panel3
            // 
            panel3.Controls.Add(label1);
            panel3.Controls.Add(applicationMenuStrip);
            panel3.Controls.Add(symbolFilter);
            panel3.Dock = DockStyle.Top;
            panel3.Location = new Point(0, 0);
            panel3.Margin = new Padding(2);
            panel3.Name = "panel3";
            panel3.Size = new Size(190, 93);
            panel3.TabIndex = 1;
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
            panelClient.Location = new Point(0, 93);
            panelClient.Margin = new Padding(2);
            panelClient.Name = "panelClient";
            panelClient.Size = new Size(1542, 648);
            panelClient.TabIndex = 13;
            // 
            // listViewSignalsMenuStrip
            // 
            listViewSignalsMenuStrip.Items.AddRange(new ToolStripItem[] { listViewSignalsMenuItemActivateTradingApp, listViewSignalsMenuItemActivateTradingApps, listViewSignalsMenuItemActivateTradingViewInternal, listViewSignalsMenuItemActivateTradingViewExternal, listViewSignalsMenuItemShowTrendInformation, listViewSignalsMenuItemClearSignals, listViewSignalsMenuItemCopySignal });
            listViewSignalsMenuStrip.Name = "contextMenuStripSignals";
            listViewSignalsMenuStrip.Size = new Size(215, 158);
            // 
            // listViewSignalsMenuItemActivateTradingApp
            // 
            listViewSignalsMenuItemActivateTradingApp.Name = "listViewSignalsMenuItemActivateTradingApp";
            listViewSignalsMenuItemActivateTradingApp.Size = new Size(214, 22);
            listViewSignalsMenuItemActivateTradingApp.Text = "Activate trading app";
            listViewSignalsMenuItemActivateTradingApp.Click += ListViewSignalsMenuItemActivateTradingApp_Click;
            // 
            // listViewSignalsMenuItemActivateTradingApps
            // 
            listViewSignalsMenuItemActivateTradingApps.Name = "listViewSignalsMenuItemActivateTradingApps";
            listViewSignalsMenuItemActivateTradingApps.Size = new Size(214, 22);
            listViewSignalsMenuItemActivateTradingApps.Text = "Trading app + TradingView";
            listViewSignalsMenuItemActivateTradingApps.Click += ListViewSignalsMenuItemActivateTradingApps_Click;
            // 
            // listViewSignalsMenuItemActivateTradingViewInternal
            // 
            listViewSignalsMenuItemActivateTradingViewInternal.Name = "listViewSignalsMenuItemActivateTradingViewInternal";
            listViewSignalsMenuItemActivateTradingViewInternal.Size = new Size(214, 22);
            listViewSignalsMenuItemActivateTradingViewInternal.Text = "TradingView browser";
            listViewSignalsMenuItemActivateTradingViewInternal.Click += ListViewSignalsMenuItemActivateTradingViewInternal_Click;
            // 
            // listViewSignalsMenuItemActivateTradingViewExternal
            // 
            listViewSignalsMenuItemActivateTradingViewExternal.Name = "listViewSignalsMenuItemActivateTradingViewExternal";
            listViewSignalsMenuItemActivateTradingViewExternal.Size = new Size(214, 22);
            listViewSignalsMenuItemActivateTradingViewExternal.Text = "Tradingview extern";
            listViewSignalsMenuItemActivateTradingViewExternal.Click += ListViewSignalsMenuItemActivateTradingviewExternal_Click;
            // 
            // listViewSignalsMenuItemShowTrendInformation
            // 
            listViewSignalsMenuItemShowTrendInformation.Name = "listViewSignalsMenuItemShowTrendInformation";
            listViewSignalsMenuItemShowTrendInformation.Size = new Size(214, 22);
            listViewSignalsMenuItemShowTrendInformation.Text = "Trend informatie";
            listViewSignalsMenuItemShowTrendInformation.Click += MenuSignalsShowTrendInformation_Click;
            // 
            // listViewSignalsMenuItemClearSignals
            // 
            listViewSignalsMenuItemClearSignals.Name = "listViewSignalsMenuItemClearSignals";
            listViewSignalsMenuItemClearSignals.Size = new Size(214, 22);
            listViewSignalsMenuItemClearSignals.Text = "Clear";
            listViewSignalsMenuItemClearSignals.Click += ListViewSignalsMenuItemClearSignals_Click;
            // 
            // listViewSignalsMenuItemCopySignal
            // 
            listViewSignalsMenuItemCopySignal.Name = "listViewSignalsMenuItemCopySignal";
            listViewSignalsMenuItemCopySignal.Size = new Size(214, 22);
            listViewSignalsMenuItemCopySignal.Text = "Copy";
            listViewSignalsMenuItemCopySignal.Click += ListViewSignalsMenuItemCopySignal_Click;
            // 
            // panelTop
            // 
            panelTop.Controls.Add(labelBarometerDateValue);
            panelTop.Controls.Add(comboBoxBarometerInterval);
            panelTop.Controls.Add(pictureBox1);
            panelTop.Controls.Add(comboBoxBarometerQuote);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Margin = new Padding(2);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(1542, 93);
            panelTop.TabIndex = 14;
            // 
            // labelBarometerDateValue
            // 
            labelBarometerDateValue.AutoSize = true;
            labelBarometerDateValue.Location = new Point(8, 61);
            labelBarometerDateValue.Margin = new Padding(4, 0, 4, 0);
            labelBarometerDateValue.Name = "labelBarometerDateValue";
            labelBarometerDateValue.Size = new Size(62, 15);
            labelBarometerDateValue.TabIndex = 70;
            labelBarometerDateValue.Text = "Barometer";
            // 
            // comboBoxBarometerInterval
            // 
            comboBoxBarometerInterval.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxBarometerInterval.FormattingEnabled = true;
            comboBoxBarometerInterval.Location = new Point(10, 31);
            comboBoxBarometerInterval.Margin = new Padding(2);
            comboBoxBarometerInterval.Name = "comboBoxBarometerInterval";
            comboBoxBarometerInterval.Size = new Size(79, 23);
            comboBoxBarometerInterval.TabIndex = 69;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            pictureBox1.Location = new Point(95, 0);
            pictureBox1.Margin = new Padding(4, 3, 4, 3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(419, 93);
            pictureBox1.TabIndex = 67;
            pictureBox1.TabStop = false;
            // 
            // comboBoxBarometerQuote
            // 
            comboBoxBarometerQuote.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxBarometerQuote.FormattingEnabled = true;
            comboBoxBarometerQuote.Location = new Point(10, 3);
            comboBoxBarometerQuote.Margin = new Padding(2);
            comboBoxBarometerQuote.Name = "comboBoxBarometerQuote";
            comboBoxBarometerQuote.Size = new Size(79, 23);
            comboBoxBarometerQuote.TabIndex = 41;
            // 
            // panelClient1
            // 
            panelClient1.Controls.Add(panelClient);
            panelClient1.Controls.Add(panelTop);
            panelClient1.Dock = DockStyle.Fill;
            panelClient1.Location = new Point(190, 0);
            panelClient1.Margin = new Padding(2);
            panelClient1.Name = "panelClient1";
            panelClient1.Size = new Size(1542, 741);
            panelClient1.TabIndex = 12;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(61, 4);
            // 
            // tabPagePositions
            // 
            tabPagePositions.Location = new Point(4, 24);
            tabPagePositions.Name = "tabPagePositions";
            tabPagePositions.Padding = new Padding(3);
            tabPagePositions.Size = new Size(1534, 620);
            tabPagePositions.TabIndex = 7;
            tabPagePositions.Text = "Positions";
            tabPagePositions.UseVisualStyleBackColor = true;
            // 
            // tabPageAltrady
            // 
            tabPageAltrady.Controls.Add(webViewAltrady);
            tabPageAltrady.Location = new Point(4, 24);
            tabPageAltrady.Margin = new Padding(4, 3, 4, 3);
            tabPageAltrady.Name = "tabPageAltrady";
            tabPageAltrady.Padding = new Padding(4, 3, 4, 3);
            tabPageAltrady.Size = new Size(1534, 620);
            tabPageAltrady.TabIndex = 6;
            tabPageAltrady.Text = "Altrady (dummy)";
            tabPageAltrady.UseVisualStyleBackColor = true;
            // 
            // webViewAltrady
            // 
            webViewAltrady.AllowExternalDrop = true;
            webViewAltrady.CreationProperties = null;
            webViewAltrady.DefaultBackgroundColor = Color.White;
            webViewAltrady.Dock = DockStyle.Fill;
            webViewAltrady.Location = new Point(4, 3);
            webViewAltrady.Margin = new Padding(4, 3, 4, 3);
            webViewAltrady.Name = "webViewAltrady";
            webViewAltrady.Size = new Size(1526, 614);
            webViewAltrady.TabIndex = 1;
            webViewAltrady.ZoomFactor = 1D;
            // 
            // tabPageLog
            // 
            tabPageLog.Controls.Add(TextBoxLog);
            tabPageLog.Location = new Point(4, 24);
            tabPageLog.Margin = new Padding(2);
            tabPageLog.Name = "tabPageLog";
            tabPageLog.Padding = new Padding(2);
            tabPageLog.Size = new Size(1534, 620);
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
            TextBoxLog.Size = new Size(1530, 616);
            TextBoxLog.TabIndex = 1;
            // 
            // tabPageBrowser
            // 
            tabPageBrowser.Controls.Add(webViewTradingView);
            tabPageBrowser.Location = new Point(4, 24);
            tabPageBrowser.Margin = new Padding(4, 3, 4, 3);
            tabPageBrowser.Name = "tabPageBrowser";
            tabPageBrowser.Padding = new Padding(4, 3, 4, 3);
            tabPageBrowser.Size = new Size(1534, 620);
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
            webViewTradingView.Size = new Size(1526, 614);
            webViewTradingView.TabIndex = 0;
            webViewTradingView.ZoomFactor = 1D;
            // 
            // tabPageSignals
            // 
            tabPageSignals.Location = new Point(4, 24);
            tabPageSignals.Margin = new Padding(4, 3, 4, 3);
            tabPageSignals.Name = "tabPageSignals";
            tabPageSignals.Padding = new Padding(4, 3, 4, 3);
            tabPageSignals.Size = new Size(1534, 620);
            tabPageSignals.TabIndex = 4;
            tabPageSignals.Text = "Signals";
            tabPageSignals.UseVisualStyleBackColor = true;
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabPageSignals);
            tabControl.Controls.Add(tabPageBrowser);
            tabControl.Controls.Add(tabPagePositions);
            tabControl.Controls.Add(tabPageLog);
            tabControl.Controls.Add(tabPageAltrady);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Margin = new Padding(2);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1542, 648);
            tabControl.TabIndex = 12;
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
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            applicationMenuStrip.ResumeLayout(false);
            applicationMenuStrip.PerformLayout();
            panelClient.ResumeLayout(false);
            listViewSignalsMenuStrip.ResumeLayout(false);
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panelClient1.ResumeLayout(false);
            tabPageAltrady.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webViewAltrady).EndInit();
            tabPageLog.ResumeLayout(false);
            tabPageLog.PerformLayout();
            tabPageBrowser.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webViewTradingView).EndInit();
            tabControl.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Panel panelLeft;
        private Panel panelClient;
        private ListBox listBoxSymbols;
        private Panel panelTop;
        private Panel panel3;
        private Panel panel4;
        private Panel panelClient1;
        private ContextMenuStrip listBoxSymbolsMenuStrip;
        private TextBox symbolFilter;
        private ComboBox comboBoxBarometerQuote;
        private MenuStrip applicationMenuStrip;
        private ToolStripMenuItem MenuMain;
        private ToolStripMenuItem ToolStripMenuItemSettings;
        private ToolStripMenuItem ToolStripMenuItemRefresh;
        private ToolStripMenuItem clearMenusToolStripMenuItem;
        private ContextMenuStrip listViewSignalsMenuStrip;
        private ToolStripMenuItem listViewSignalsMenuItemClearSignals;
        private ToolStripMenuItem listViewSignalsMenuItemActivateTradingApp;
        private ToolStripMenuItem listViewSignalsMenuItemActivateTradingViewInternal;
        private ToolStripMenuItem listViewSignalsMenuItemActivateTradingApps;
        private ToolStripMenuItem listViewSignalsMenuItemActivateTradingViewExternal;
        private ToolStripMenuItem listBoxSymbolsMenuItemActivateTradingviewInternal;
        private ToolStripMenuItem listBoxSymbolsMenuItemActivateTradingviewExternal;
        private ToolStripMenuItem listBoxSymbolsMenuItemActivateTradingApp;
        private ToolStripMenuItem listBoxSymbolsMenuItemActivateTradingApps;
        private ToolStripMenuItem applicationMenuItemAbout;
        private ToolStripMenuItem listBoxSymbolsMenuItemShowTrendInformation;
        private ToolStripMenuItem listViewSignalsMenuItemShowTrendInformation;
        private ToolStripMenuItem listViewSignalsMenuItemCopySignal;
        private ToolStripMenuItem listBoxSymbolsMenuItemCopy;
        private PictureBox pictureBox1;
        private ComboBox comboBoxBarometerInterval;
        private Label labelBarometerDateValue;
        private ContextMenuStrip contextMenuStrip1;
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
        private TabPage tabPageAltrady;
        private Microsoft.Web.WebView2.WinForms.WebView2 webViewAltrady;
        private TabPage tabPagePositions;
    }
}

