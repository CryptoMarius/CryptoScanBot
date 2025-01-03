﻿namespace CryptoScanBot
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
            panelLeft = new Panel();
            tabControlSymbols = new TabControl();
            tabPageSymbols = new TabPage();
            dataGridViewSymbols = new DataGridView();
            panelLeftTop = new Panel();
            label1 = new Label();
            symbolFilter = new TextBox();
            panelClient = new Panel();
            tabControl = new TabControl();
            tabPagewebViewDummy = new TabPage();
            webViewDummy = new Microsoft.Web.WebView2.WinForms.WebView2();
            tabPageSignals = new TabPage();
            dataGridViewSignals = new DataGridView();
            tabPageBrowser = new TabPage();
            webViewTradingView = new Microsoft.Web.WebView2.WinForms.WebView2();
            tabPagePositionsOpen = new TabPage();
            dataGridViewPositionOpen = new DataGridView();
            tabPagePositionsClosed = new TabPage();
            dataGridViewPositionClosed = new DataGridView();
            tabPageDashBoard = new TabPage();
            dashBoardControl1 = new DashBoardControl();
            tabPageLog = new TabPage();
            TextBoxLog = new TextBox();
            tabPageLiveData = new TabPage();
            dataGridLiveDataView = new DataGridView();
            panelClient1 = new Panel();
            dashBoardInformation1 = new TradingView.DashBoardInformation();
            MenuMain = new ToolStripMenuItem();
            applicationMenuStrip = new MenuStrip();
            splitContainer1 = new SplitContainer();
            panelLeft.SuspendLayout();
            tabControlSymbols.SuspendLayout();
            tabPageSymbols.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewSymbols).BeginInit();
            panelLeftTop.SuspendLayout();
            panelClient.SuspendLayout();
            tabControl.SuspendLayout();
            tabPagewebViewDummy.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webViewDummy).BeginInit();
            tabPageSignals.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewSignals).BeginInit();
            tabPageBrowser.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webViewTradingView).BeginInit();
            tabPagePositionsOpen.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewPositionOpen).BeginInit();
            tabPagePositionsClosed.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewPositionClosed).BeginInit();
            tabPageDashBoard.SuspendLayout();
            tabPageLog.SuspendLayout();
            tabPageLiveData.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridLiveDataView).BeginInit();
            panelClient1.SuspendLayout();
            applicationMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // panelLeft
            // 
            panelLeft.AutoSize = true;
            panelLeft.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panelLeft.Controls.Add(tabControlSymbols);
            panelLeft.Controls.Add(panelLeftTop);
            panelLeft.Dock = DockStyle.Fill;
            panelLeft.Location = new Point(0, 0);
            panelLeft.Margin = new Padding(2);
            panelLeft.Name = "panelLeft";
            panelLeft.Size = new Size(225, 723);
            panelLeft.TabIndex = 12;
            // 
            // tabControlSymbols
            // 
            tabControlSymbols.Appearance = TabAppearance.FlatButtons;
            tabControlSymbols.Controls.Add(tabPageSymbols);
            tabControlSymbols.Dock = DockStyle.Fill;
            tabControlSymbols.Location = new Point(0, 103);
            tabControlSymbols.Name = "tabControlSymbols";
            tabControlSymbols.SelectedIndex = 0;
            tabControlSymbols.Size = new Size(225, 620);
            tabControlSymbols.TabIndex = 2;
            // 
            // tabPageSymbols
            // 
            tabPageSymbols.Controls.Add(dataGridViewSymbols);
            tabPageSymbols.Location = new Point(4, 27);
            tabPageSymbols.Name = "tabPageSymbols";
            tabPageSymbols.Size = new Size(217, 589);
            tabPageSymbols.TabIndex = 0;
            tabPageSymbols.Text = "Symbols";
            tabPageSymbols.UseVisualStyleBackColor = true;
            // 
            // dataGridViewSymbols
            // 
            dataGridViewSymbols.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewSymbols.Dock = DockStyle.Fill;
            dataGridViewSymbols.Location = new Point(0, 0);
            dataGridViewSymbols.Name = "dataGridViewSymbols";
            dataGridViewSymbols.Size = new Size(217, 589);
            dataGridViewSymbols.TabIndex = 0;
            // 
            // panelLeftTop
            // 
            panelLeftTop.Controls.Add(label1);
            panelLeftTop.Controls.Add(symbolFilter);
            panelLeftTop.Dock = DockStyle.Top;
            panelLeftTop.Location = new Point(0, 0);
            panelLeftTop.Margin = new Padding(2);
            panelLeftTop.Name = "panelLeftTop";
            panelLeftTop.Size = new Size(225, 103);
            panelLeftTop.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 56);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(36, 15);
            label1.TabIndex = 71;
            label1.Text = "Filter:";
            // 
            // symbolFilter
            // 
            symbolFilter.Location = new Point(10, 75);
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
            panelClient.Location = new Point(0, 102);
            panelClient.Margin = new Padding(2);
            panelClient.Name = "panelClient";
            panelClient.Size = new Size(1321, 621);
            panelClient.TabIndex = 13;
            // 
            // tabControl
            // 
            tabControl.Appearance = TabAppearance.FlatButtons;
            tabControl.Controls.Add(tabPagewebViewDummy);
            tabControl.Controls.Add(tabPageSignals);
            tabControl.Controls.Add(tabPageBrowser);
            tabControl.Controls.Add(tabPageLiveData);
            tabControl.Controls.Add(tabPagePositionsOpen);
            tabControl.Controls.Add(tabPagePositionsClosed);
            tabControl.Controls.Add(tabPageDashBoard);
            tabControl.Controls.Add(tabPageLog);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Margin = new Padding(2);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1321, 621);
            tabControl.TabIndex = 12;
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            tabControl.TabIndexChanged += TimerClearMemo_Tick;
            // 
            // tabPagewebViewDummy
            // 
            tabPagewebViewDummy.Controls.Add(webViewDummy);
            tabPagewebViewDummy.Location = new Point(4, 27);
            tabPagewebViewDummy.Margin = new Padding(4, 3, 4, 3);
            tabPagewebViewDummy.Name = "tabPagewebViewDummy";
            tabPagewebViewDummy.Padding = new Padding(4, 3, 4, 3);
            tabPagewebViewDummy.Size = new Size(1313, 590);
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
            webViewDummy.Size = new Size(1305, 584);
            webViewDummy.TabIndex = 1;
            webViewDummy.ZoomFactor = 1D;
            // 
            // tabPageSignals
            // 
            tabPageSignals.Controls.Add(dataGridViewSignals);
            tabPageSignals.Location = new Point(4, 27);
            tabPageSignals.Margin = new Padding(4, 3, 4, 3);
            tabPageSignals.Name = "tabPageSignals";
            tabPageSignals.Padding = new Padding(4, 3, 4, 3);
            tabPageSignals.Size = new Size(1313, 590);
            tabPageSignals.TabIndex = 4;
            tabPageSignals.Text = "Signals";
            tabPageSignals.UseVisualStyleBackColor = true;
            // 
            // dataGridViewSignals
            // 
            dataGridViewSignals.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewSignals.Dock = DockStyle.Fill;
            dataGridViewSignals.Location = new Point(4, 3);
            dataGridViewSignals.Name = "dataGridViewSignals";
            dataGridViewSignals.Size = new Size(1305, 584);
            dataGridViewSignals.TabIndex = 1;
            // 
            // tabPageBrowser
            // 
            tabPageBrowser.Controls.Add(webViewTradingView);
            tabPageBrowser.Location = new Point(4, 27);
            tabPageBrowser.Margin = new Padding(4, 3, 4, 3);
            tabPageBrowser.Name = "tabPageBrowser";
            tabPageBrowser.Padding = new Padding(4, 3, 4, 3);
            tabPageBrowser.Size = new Size(1313, 590);
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
            webViewTradingView.Size = new Size(1305, 584);
            webViewTradingView.TabIndex = 0;
            webViewTradingView.ZoomFactor = 1D;
            // 
            // tabPagePositionsOpen
            // 
            tabPagePositionsOpen.Controls.Add(dataGridViewPositionOpen);
            tabPagePositionsOpen.Location = new Point(4, 27);
            tabPagePositionsOpen.Name = "tabPagePositionsOpen";
            tabPagePositionsOpen.Padding = new Padding(3);
            tabPagePositionsOpen.Size = new Size(1313, 590);
            tabPagePositionsOpen.TabIndex = 7;
            tabPagePositionsOpen.Text = "Open positions";
            tabPagePositionsOpen.UseVisualStyleBackColor = true;
            // 
            // dataGridViewPositionOpen
            // 
            dataGridViewPositionOpen.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewPositionOpen.Dock = DockStyle.Fill;
            dataGridViewPositionOpen.Location = new Point(3, 3);
            dataGridViewPositionOpen.Name = "dataGridViewPositionOpen";
            dataGridViewPositionOpen.Size = new Size(1307, 584);
            dataGridViewPositionOpen.TabIndex = 1;
            // 
            // tabPagePositionsClosed
            // 
            tabPagePositionsClosed.Controls.Add(dataGridViewPositionClosed);
            tabPagePositionsClosed.Location = new Point(4, 27);
            tabPagePositionsClosed.Name = "tabPagePositionsClosed";
            tabPagePositionsClosed.Padding = new Padding(3);
            tabPagePositionsClosed.Size = new Size(1313, 590);
            tabPagePositionsClosed.TabIndex = 8;
            tabPagePositionsClosed.Text = "Closed positions";
            tabPagePositionsClosed.UseVisualStyleBackColor = true;
            // 
            // dataGridViewPositionClosed
            // 
            dataGridViewPositionClosed.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewPositionClosed.Dock = DockStyle.Fill;
            dataGridViewPositionClosed.Location = new Point(3, 3);
            dataGridViewPositionClosed.Name = "dataGridViewPositionClosed";
            dataGridViewPositionClosed.Size = new Size(1307, 584);
            dataGridViewPositionClosed.TabIndex = 1;
            // 
            // tabPageDashBoard
            // 
            tabPageDashBoard.Controls.Add(dashBoardControl1);
            tabPageDashBoard.Font = new Font("Segoe UI", 9F);
            tabPageDashBoard.Location = new Point(4, 27);
            tabPageDashBoard.Name = "tabPageDashBoard";
            tabPageDashBoard.Padding = new Padding(3);
            tabPageDashBoard.Size = new Size(1313, 590);
            tabPageDashBoard.TabIndex = 9;
            tabPageDashBoard.Text = "Dashboard";
            tabPageDashBoard.UseVisualStyleBackColor = true;
            // 
            // dashBoardControl1
            // 
            dashBoardControl1.Dock = DockStyle.Fill;
            dashBoardControl1.Location = new Point(3, 3);
            dashBoardControl1.Name = "dashBoardControl1";
            dashBoardControl1.Size = new Size(1307, 584);
            dashBoardControl1.TabIndex = 0;
            // 
            // tabPageLog
            // 
            tabPageLog.Controls.Add(TextBoxLog);
            tabPageLog.Location = new Point(4, 27);
            tabPageLog.Margin = new Padding(2);
            tabPageLog.Name = "tabPageLog";
            tabPageLog.Padding = new Padding(2);
            tabPageLog.Size = new Size(1313, 590);
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
            TextBoxLog.Size = new Size(1309, 586);
            TextBoxLog.TabIndex = 1;
            // 
            // tabPageLiveData
            // 
            tabPageLiveData.Controls.Add(dataGridLiveDataView);
            tabPageLiveData.Location = new Point(4, 27);
            tabPageLiveData.Name = "tabPageLiveData";
            tabPageLiveData.Padding = new Padding(3);
            tabPageLiveData.Size = new Size(1313, 590);
            tabPageLiveData.TabIndex = 10;
            tabPageLiveData.Text = "Live data";
            tabPageLiveData.UseVisualStyleBackColor = true;
            // 
            // dataGridLiveDataView
            // 
            dataGridLiveDataView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridLiveDataView.Dock = DockStyle.Fill;
            dataGridLiveDataView.Location = new Point(3, 3);
            dataGridLiveDataView.Name = "dataGridLiveDataView";
            dataGridLiveDataView.Size = new Size(1307, 584);
            dataGridLiveDataView.TabIndex = 2;
            // 
            // panelClient1
            // 
            panelClient1.Controls.Add(panelClient);
            panelClient1.Controls.Add(dashBoardInformation1);
            panelClient1.Dock = DockStyle.Fill;
            panelClient1.Location = new Point(0, 0);
            panelClient1.Margin = new Padding(2);
            panelClient1.Name = "panelClient1";
            panelClient1.Size = new Size(1321, 723);
            panelClient1.TabIndex = 12;
            // 
            // dashBoardInformation1
            // 
            dashBoardInformation1.AutoSize = true;
            dashBoardInformation1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dashBoardInformation1.Dock = DockStyle.Top;
            dashBoardInformation1.Location = new Point(0, 0);
            dashBoardInformation1.MinimumSize = new Size(0, 102);
            dashBoardInformation1.Name = "dashBoardInformation1";
            dashBoardInformation1.Size = new Size(1321, 102);
            dashBoardInformation1.TabIndex = 15;
            // 
            // MenuMain
            // 
            MenuMain.Name = "MenuMain";
            MenuMain.Size = new Size(50, 20);
            MenuMain.Text = "Menu";
            // 
            // applicationMenuStrip
            // 
            applicationMenuStrip.ImageScalingSize = new Size(20, 20);
            applicationMenuStrip.Items.AddRange(new ToolStripItem[] { MenuMain });
            applicationMenuStrip.Location = new Point(0, 0);
            applicationMenuStrip.Name = "applicationMenuStrip";
            applicationMenuStrip.Padding = new Padding(5, 2, 0, 2);
            applicationMenuStrip.RenderMode = ToolStripRenderMode.Professional;
            applicationMenuStrip.Size = new Size(1550, 24);
            applicationMenuStrip.TabIndex = 16;
            applicationMenuStrip.Text = "menuStrip1";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 24);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(panelLeft);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(panelClient1);
            splitContainer1.Size = new Size(1550, 723);
            splitContainer1.SplitterDistance = 225;
            splitContainer1.TabIndex = 17;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1550, 747);
            Controls.Add(splitContainer1);
            Controls.Add(applicationMenuStrip);
            Margin = new Padding(2);
            Name = "FrmMain";
            FormClosing += FrmMain_FormClosing;
            panelLeft.ResumeLayout(false);
            tabControlSymbols.ResumeLayout(false);
            tabPageSymbols.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewSymbols).EndInit();
            panelLeftTop.ResumeLayout(false);
            panelLeftTop.PerformLayout();
            panelClient.ResumeLayout(false);
            tabControl.ResumeLayout(false);
            tabPagewebViewDummy.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webViewDummy).EndInit();
            tabPageSignals.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewSignals).EndInit();
            tabPageBrowser.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)webViewTradingView).EndInit();
            tabPagePositionsOpen.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewPositionOpen).EndInit();
            tabPagePositionsClosed.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewPositionClosed).EndInit();
            tabPageDashBoard.ResumeLayout(false);
            tabPageLog.ResumeLayout(false);
            tabPageLog.PerformLayout();
            tabPageLiveData.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridLiveDataView).EndInit();
            panelClient1.ResumeLayout(false);
            panelClient1.PerformLayout();
            applicationMenuStrip.ResumeLayout(false);
            applicationMenuStrip.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Panel panelLeft;
        private Panel panelClient;
        private Panel panelLeftTop;
        private Panel panelClient1;
        private TextBox symbolFilter;
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
        private TabPage tabPageDashBoard;
        private DashBoardControl dashBoardControl1;
        private TradingView.DashBoardInformation dashBoardInformation1;
        private TabControl tabControlSymbols;
        private TabPage tabPageSymbols;
        private ToolStripMenuItem MenuMain;
        private MenuStrip applicationMenuStrip;
        private DataGridView dataGridViewSymbols;
        private DataGridView dataGridViewSignals;
        private DataGridView dataGridViewPositionOpen;
        private DataGridView dataGridViewPositionClosed;
        private SplitContainer splitContainer1;
        private TabPage tabPageLiveData;
        private DataGridView dataGridLiveDataView;
    }
}

