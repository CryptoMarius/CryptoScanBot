namespace CryptoScanner.ZoneVisualisation;

partial class CryptoVisualisation
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        flowLayoutPanel1 = new FlowLayoutPanel();
        groupBox2 = new GroupBox();
        label1 = new Label();
        EditIntervalName = new ComboBox();
        labelInterval2 = new Label();
        EditSymbolQuote = new ComboBox();
        label3 = new Label();
        EditSymbolBase = new ComboBox();
        groupBox5 = new GroupBox();
        EditTrendShowZigZag = new CheckBox();
        EditTrendType = new ComboBox();
        groupBox4 = new GroupBox();
        EditFibTrend = new ComboBox();
        EditFibShow = new CheckBox();
        EditFibZhowZigZag = new CheckBox();
        panel1 = new Panel();
        labelInterval = new Label();
        ButtonPlus = new Button();
        ButtonMinus = new Button();
        PanelPlayBack = new Panel();
        labelMaxTime = new Label();
        ButtonGoRight = new Button();
        ButtonGoLeft = new Button();
        ButtonRefresh = new Button();
        ButtonCalculate = new Button();
        ButtonZoomLast = new Button();
        groupBox1 = new GroupBox();
        EditShowSmaLinesSbm = new CheckBox();
        EditShowNadarayaWatsonEnvelope = new CheckBox();
        EditShowBollingerBand = new CheckBox();
        EditShowDlzZones = new CheckBox();
        EditTransparant = new CheckBox();
        EditShowPivots = new CheckBox();
        EditShowSignals = new CheckBox();
        EditShowDtb = new CheckBox();
        EditShowFvgZones = new CheckBox();
        ButtonOpenTradingApp = new Button();
        plotView = new OxyPlot.WindowsForms.PlotView();
        EditShowNadarayaWatsonEnvelopeRepaining = new CheckBox();
        flowLayoutPanel1.SuspendLayout();
        groupBox2.SuspendLayout();
        groupBox5.SuspendLayout();
        groupBox4.SuspendLayout();
        panel1.SuspendLayout();
        PanelPlayBack.SuspendLayout();
        groupBox1.SuspendLayout();
        SuspendLayout();
        // 
        // flowLayoutPanel1
        // 
        flowLayoutPanel1.AutoSize = true;
        flowLayoutPanel1.Controls.Add(groupBox2);
        flowLayoutPanel1.Controls.Add(groupBox5);
        flowLayoutPanel1.Controls.Add(groupBox4);
        flowLayoutPanel1.Controls.Add(panel1);
        flowLayoutPanel1.Controls.Add(PanelPlayBack);
        flowLayoutPanel1.Controls.Add(ButtonRefresh);
        flowLayoutPanel1.Controls.Add(ButtonCalculate);
        flowLayoutPanel1.Controls.Add(ButtonZoomLast);
        flowLayoutPanel1.Controls.Add(groupBox1);
        flowLayoutPanel1.Controls.Add(ButtonOpenTradingApp);
        flowLayoutPanel1.Dock = DockStyle.Left;
        flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel1.Location = new Point(0, 0);
        flowLayoutPanel1.Name = "flowLayoutPanel1";
        flowLayoutPanel1.Padding = new Padding(3);
        flowLayoutPanel1.Size = new Size(212, 802);
        flowLayoutPanel1.TabIndex = 0;
        // 
        // groupBox2
        // 
        groupBox2.Controls.Add(label1);
        groupBox2.Controls.Add(EditIntervalName);
        groupBox2.Controls.Add(labelInterval2);
        groupBox2.Controls.Add(EditSymbolQuote);
        groupBox2.Controls.Add(label3);
        groupBox2.Controls.Add(EditSymbolBase);
        groupBox2.Location = new Point(6, 6);
        groupBox2.Name = "groupBox2";
        groupBox2.Size = new Size(200, 100);
        groupBox2.TabIndex = 37;
        groupBox2.TabStop = false;
        groupBox2.Text = "Symbol";
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(5, 19);
        label1.Name = "label1";
        label1.Size = new Size(31, 15);
        label1.TabIndex = 3;
        label1.Text = "Base";
        // 
        // EditIntervalName
        // 
        EditIntervalName.FormattingEnabled = true;
        EditIntervalName.Location = new Point(75, 70);
        EditIntervalName.Name = "EditIntervalName";
        EditIntervalName.Size = new Size(100, 23);
        EditIntervalName.TabIndex = 9;
        // 
        // labelInterval2
        // 
        labelInterval2.AutoSize = true;
        labelInterval2.Location = new Point(5, 72);
        labelInterval2.Name = "labelInterval2";
        labelInterval2.Size = new Size(46, 15);
        labelInterval2.TabIndex = 3;
        labelInterval2.Text = "Interval";
        // 
        // EditSymbolQuote
        // 
        EditSymbolQuote.Location = new Point(75, 44);
        EditSymbolQuote.Name = "EditSymbolQuote";
        EditSymbolQuote.Size = new Size(100, 23);
        EditSymbolQuote.TabIndex = 12;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(5, 45);
        label3.Name = "label3";
        label3.Size = new Size(40, 15);
        label3.TabIndex = 13;
        label3.Text = "Quote";
        // 
        // EditSymbolBase
        // 
        EditSymbolBase.Location = new Point(75, 18);
        EditSymbolBase.Name = "EditSymbolBase";
        EditSymbolBase.Size = new Size(100, 23);
        EditSymbolBase.TabIndex = 9;
        // 
        // groupBox5
        // 
        groupBox5.Controls.Add(EditTrendShowZigZag);
        groupBox5.Controls.Add(EditTrendType);
        groupBox5.Location = new Point(6, 112);
        groupBox5.Name = "groupBox5";
        groupBox5.Size = new Size(200, 82);
        groupBox5.TabIndex = 40;
        groupBox5.TabStop = false;
        groupBox5.Text = "Trend";
        // 
        // EditTrendShowZigZag
        // 
        EditTrendShowZigZag.AutoSize = true;
        EditTrendShowZigZag.Location = new Point(9, 51);
        EditTrendShowZigZag.Name = "EditTrendShowZigZag";
        EditTrendShowZigZag.Size = new Size(91, 19);
        EditTrendShowZigZag.TabIndex = 36;
        EditTrendShowZigZag.Text = "Show zigzag";
        EditTrendShowZigZag.UseVisualStyleBackColor = true;
        // 
        // EditTrendType
        // 
        EditTrendType.DropDownStyle = ComboBoxStyle.DropDownList;
        EditTrendType.FormattingEnabled = true;
        EditTrendType.Items.AddRange(new object[] { "Primary trend", "Secondary trend" });
        EditTrendType.Location = new Point(11, 22);
        EditTrendType.Name = "EditTrendType";
        EditTrendType.Size = new Size(121, 23);
        EditTrendType.TabIndex = 35;
        // 
        // groupBox4
        // 
        groupBox4.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupBox4.Controls.Add(EditFibTrend);
        groupBox4.Controls.Add(EditFibShow);
        groupBox4.Controls.Add(EditFibZhowZigZag);
        groupBox4.Location = new Point(6, 200);
        groupBox4.Name = "groupBox4";
        groupBox4.Size = new Size(200, 107);
        groupBox4.TabIndex = 39;
        groupBox4.TabStop = false;
        groupBox4.Text = "FIB";
        // 
        // EditFibTrend
        // 
        EditFibTrend.DropDownStyle = ComboBoxStyle.DropDownList;
        EditFibTrend.FormattingEnabled = true;
        EditFibTrend.Items.AddRange(new object[] { "Primary trend", "Secondary trend" });
        EditFibTrend.Location = new Point(5, 22);
        EditFibTrend.Name = "EditFibTrend";
        EditFibTrend.Size = new Size(121, 23);
        EditFibTrend.TabIndex = 36;
        // 
        // EditFibShow
        // 
        EditFibShow.AutoSize = true;
        EditFibShow.Location = new Point(5, 48);
        EditFibShow.Name = "EditFibShow";
        EditFibShow.Size = new Size(139, 19);
        EditFibShow.TabIndex = 20;
        EditFibShow.Text = "Show fib retracement";
        EditFibShow.UseVisualStyleBackColor = true;
        // 
        // EditFibZhowZigZag
        // 
        EditFibZhowZigZag.AutoSize = true;
        EditFibZhowZigZag.Location = new Point(6, 72);
        EditFibZhowZigZag.Name = "EditFibZhowZigZag";
        EditFibZhowZigZag.Size = new Size(91, 19);
        EditFibZhowZigZag.TabIndex = 24;
        EditFibZhowZigZag.Text = "Show zigzag";
        EditFibZhowZigZag.UseVisualStyleBackColor = true;
        // 
        // panel1
        // 
        panel1.Controls.Add(labelInterval);
        panel1.Controls.Add(ButtonPlus);
        panel1.Controls.Add(ButtonMinus);
        panel1.Location = new Point(6, 313);
        panel1.Name = "panel1";
        panel1.Size = new Size(200, 30);
        panel1.TabIndex = 21;
        // 
        // labelInterval
        // 
        labelInterval.AutoSize = true;
        labelInterval.Location = new Point(83, 9);
        labelInterval.Name = "labelInterval";
        labelInterval.Size = new Size(38, 15);
        labelInterval.TabIndex = 21;
        labelInterval.Text = "label5";
        // 
        // ButtonPlus
        // 
        ButtonPlus.Location = new Point(47, 5);
        ButtonPlus.Name = "ButtonPlus";
        ButtonPlus.Size = new Size(25, 23);
        ButtonPlus.TabIndex = 20;
        ButtonPlus.Text = "+";
        ButtonPlus.UseVisualStyleBackColor = true;
        ButtonPlus.Click += ButtonPlusClick;
        // 
        // ButtonMinus
        // 
        ButtonMinus.Location = new Point(13, 5);
        ButtonMinus.Name = "ButtonMinus";
        ButtonMinus.Size = new Size(25, 23);
        ButtonMinus.TabIndex = 19;
        ButtonMinus.Text = "-";
        ButtonMinus.UseVisualStyleBackColor = true;
        ButtonMinus.Click += ButtonMinusClick;
        // 
        // PanelPlayBack
        // 
        PanelPlayBack.Controls.Add(labelMaxTime);
        PanelPlayBack.Controls.Add(ButtonGoRight);
        PanelPlayBack.Controls.Add(ButtonGoLeft);
        PanelPlayBack.Location = new Point(6, 349);
        PanelPlayBack.Name = "PanelPlayBack";
        PanelPlayBack.Size = new Size(200, 30);
        PanelPlayBack.TabIndex = 22;
        // 
        // labelMaxTime
        // 
        labelMaxTime.AutoSize = true;
        labelMaxTime.Location = new Point(83, 7);
        labelMaxTime.Name = "labelMaxTime";
        labelMaxTime.Size = new Size(38, 15);
        labelMaxTime.TabIndex = 23;
        labelMaxTime.Text = "label5";
        // 
        // ButtonGoRight
        // 
        ButtonGoRight.Location = new Point(49, 3);
        ButtonGoRight.Name = "ButtonGoRight";
        ButtonGoRight.Size = new Size(25, 23);
        ButtonGoRight.TabIndex = 22;
        ButtonGoRight.Text = ">";
        ButtonGoRight.UseVisualStyleBackColor = true;
        ButtonGoRight.Click += ButtonGoRightClick;
        // 
        // ButtonGoLeft
        // 
        ButtonGoLeft.Location = new Point(13, 3);
        ButtonGoLeft.Name = "ButtonGoLeft";
        ButtonGoLeft.Size = new Size(25, 23);
        ButtonGoLeft.TabIndex = 21;
        ButtonGoLeft.Text = "<";
        ButtonGoLeft.UseVisualStyleBackColor = true;
        ButtonGoLeft.Click += ButtonGoLeftClick;
        // 
        // ButtonRefresh
        // 
        ButtonRefresh.Location = new Point(6, 385);
        ButtonRefresh.Name = "ButtonRefresh";
        ButtonRefresh.Size = new Size(121, 23);
        ButtonRefresh.TabIndex = 31;
        ButtonRefresh.Text = "Refresh screen";
        ButtonRefresh.UseVisualStyleBackColor = true;
        // 
        // ButtonCalculate
        // 
        ButtonCalculate.Location = new Point(6, 414);
        ButtonCalculate.Name = "ButtonCalculate";
        ButtonCalculate.Size = new Size(121, 23);
        ButtonCalculate.TabIndex = 8;
        ButtonCalculate.Text = "Calculate zones";
        ButtonCalculate.UseVisualStyleBackColor = true;
        // 
        // ButtonZoomLast
        // 
        ButtonZoomLast.Location = new Point(6, 443);
        ButtonZoomLast.Name = "ButtonZoomLast";
        ButtonZoomLast.Size = new Size(121, 23);
        ButtonZoomLast.TabIndex = 14;
        ButtonZoomLast.Text = "Zoom last";
        ButtonZoomLast.UseVisualStyleBackColor = true;
        // 
        // groupBox1
        // 
        groupBox1.Controls.Add(EditShowNadarayaWatsonEnvelopeRepaining);
        groupBox1.Controls.Add(EditShowSmaLinesSbm);
        groupBox1.Controls.Add(EditShowNadarayaWatsonEnvelope);
        groupBox1.Controls.Add(EditShowBollingerBand);
        groupBox1.Controls.Add(EditShowDlzZones);
        groupBox1.Controls.Add(EditTransparant);
        groupBox1.Controls.Add(EditShowPivots);
        groupBox1.Controls.Add(EditShowSignals);
        groupBox1.Controls.Add(EditShowDtb);
        groupBox1.Controls.Add(EditShowFvgZones);
        groupBox1.Location = new Point(6, 472);
        groupBox1.Name = "groupBox1";
        groupBox1.Size = new Size(200, 245);
        groupBox1.TabIndex = 36;
        groupBox1.TabStop = false;
        groupBox1.Text = "Misc";
        // 
        // EditShowSmaLinesSbm
        // 
        EditShowSmaLinesSbm.AutoSize = true;
        EditShowSmaLinesSbm.Location = new Point(5, 219);
        EditShowSmaLinesSbm.Name = "EditShowSmaLinesSbm";
        EditShowSmaLinesSbm.Size = new Size(110, 19);
        EditShowSmaLinesSbm.TabIndex = 36;
        EditShowSmaLinesSbm.Text = "Show SBM SMA";
        EditShowSmaLinesSbm.UseVisualStyleBackColor = true;
        // 
        // EditShowNadarayaWatsonEnvelope
        // 
        EditShowNadarayaWatsonEnvelope.AutoSize = true;
        EditShowNadarayaWatsonEnvelope.Location = new Point(5, 194);
        EditShowNadarayaWatsonEnvelope.Name = "EditShowNadarayaWatsonEnvelope";
        EditShowNadarayaWatsonEnvelope.Size = new Size(84, 19);
        EditShowNadarayaWatsonEnvelope.TabIndex = 35;
        EditShowNadarayaWatsonEnvelope.Text = "Show NWE";
        EditShowNadarayaWatsonEnvelope.UseVisualStyleBackColor = true;
        // 
        // EditShowBollingerBand
        // 
        EditShowBollingerBand.AutoSize = true;
        EditShowBollingerBand.Location = new Point(6, 169);
        EditShowBollingerBand.Name = "EditShowBollingerBand";
        EditShowBollingerBand.Size = new Size(72, 19);
        EditShowBollingerBand.TabIndex = 34;
        EditShowBollingerBand.Text = "Show BB";
        EditShowBollingerBand.UseVisualStyleBackColor = true;
        // 
        // EditShowDlzZones
        // 
        EditShowDlzZones.AutoSize = true;
        EditShowDlzZones.Location = new Point(5, 96);
        EditShowDlzZones.Name = "EditShowDlzZones";
        EditShowDlzZones.Size = new Size(106, 19);
        EditShowDlzZones.TabIndex = 5;
        EditShowDlzZones.Text = "Show dlz zones";
        EditShowDlzZones.UseVisualStyleBackColor = true;
        // 
        // EditTransparant
        // 
        EditTransparant.AutoSize = true;
        EditTransparant.Location = new Point(5, 22);
        EditTransparant.Name = "EditTransparant";
        EditTransparant.Size = new Size(87, 19);
        EditTransparant.TabIndex = 30;
        EditTransparant.Text = "Transparant";
        EditTransparant.UseVisualStyleBackColor = true;
        // 
        // EditShowPivots
        // 
        EditShowPivots.AutoSize = true;
        EditShowPivots.Location = new Point(5, 47);
        EditShowPivots.Name = "EditShowPivots";
        EditShowPivots.Size = new Size(91, 19);
        EditShowPivots.TabIndex = 31;
        EditShowPivots.Text = "Show points";
        EditShowPivots.UseVisualStyleBackColor = true;
        // 
        // EditShowSignals
        // 
        EditShowSignals.AutoSize = true;
        EditShowSignals.Location = new Point(5, 72);
        EditShowSignals.Name = "EditShowSignals";
        EditShowSignals.Size = new Size(94, 19);
        EditShowSignals.TabIndex = 32;
        EditShowSignals.Text = "Show signals";
        EditShowSignals.UseVisualStyleBackColor = true;
        // 
        // EditShowDtb
        // 
        EditShowDtb.AutoSize = true;
        EditShowDtb.Location = new Point(5, 144);
        EditShowDtb.Name = "EditShowDtb";
        EditShowDtb.Size = new Size(156, 19);
        EditShowDtb.TabIndex = 33;
        EditShowDtb.Text = "Show dtb (experimental)";
        EditShowDtb.UseVisualStyleBackColor = true;
        // 
        // EditShowFvgZones
        // 
        EditShowFvgZones.AutoSize = true;
        EditShowFvgZones.Location = new Point(5, 120);
        EditShowFvgZones.Name = "EditShowFvgZones";
        EditShowFvgZones.Size = new Size(108, 19);
        EditShowFvgZones.TabIndex = 32;
        EditShowFvgZones.Text = "Show fvg zones";
        EditShowFvgZones.UseVisualStyleBackColor = true;
        // 
        // ButtonOpenTradingApp
        // 
        ButtonOpenTradingApp.Location = new Point(6, 723);
        ButtonOpenTradingApp.Name = "ButtonOpenTradingApp";
        ButtonOpenTradingApp.Size = new Size(121, 23);
        ButtonOpenTradingApp.TabIndex = 41;
        ButtonOpenTradingApp.Text = "Open trading app";
        ButtonOpenTradingApp.UseVisualStyleBackColor = true;
        // 
        // plotView
        // 
        plotView.BackColor = Color.Black;
        plotView.Dock = DockStyle.Fill;
        plotView.Location = new Point(212, 0);
        plotView.Name = "plotView";
        plotView.PanCursor = Cursors.Hand;
        plotView.Size = new Size(1129, 802);
        plotView.TabIndex = 1;
        plotView.Text = "plotView1";
        plotView.ZoomHorizontalCursor = Cursors.SizeWE;
        plotView.ZoomRectangleCursor = Cursors.SizeNWSE;
        plotView.ZoomVerticalCursor = Cursors.SizeNS;
        // 
        // EditShowNadarayaWatsonEnvelopeRepaining
        // 
        EditShowNadarayaWatsonEnvelopeRepaining.AutoSize = true;
        EditShowNadarayaWatsonEnvelopeRepaining.Location = new Point(95, 194);
        EditShowNadarayaWatsonEnvelopeRepaining.Name = "EditShowNadarayaWatsonEnvelopeRepaining";
        EditShowNadarayaWatsonEnvelopeRepaining.Size = new Size(66, 19);
        EditShowNadarayaWatsonEnvelopeRepaining.TabIndex = 37;
        EditShowNadarayaWatsonEnvelopeRepaining.Text = "Repaint";
        EditShowNadarayaWatsonEnvelopeRepaining.UseVisualStyleBackColor = true;
        // 
        // CryptoVisualisation
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1341, 802);
        Controls.Add(plotView);
        Controls.Add(flowLayoutPanel1);
        Name = "CryptoVisualisation";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Form1";
        flowLayoutPanel1.ResumeLayout(false);
        groupBox2.ResumeLayout(false);
        groupBox2.PerformLayout();
        groupBox5.ResumeLayout(false);
        groupBox5.PerformLayout();
        groupBox4.ResumeLayout(false);
        groupBox4.PerformLayout();
        panel1.ResumeLayout(false);
        panel1.PerformLayout();
        PanelPlayBack.ResumeLayout(false);
        PanelPlayBack.PerformLayout();
        groupBox1.ResumeLayout(false);
        groupBox1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private FlowLayoutPanel flowLayoutPanel1;
    private ComboBox EditSymbolBase;
    private Label labelInterval2;
    private CheckBox EditShowDlzZones;
    private Button ButtonCalculate;
    private ComboBox EditIntervalName;
    private Label label3;
    private ComboBox EditSymbolQuote;
    private Button ButtonZoomLast;
    private OxyPlot.WindowsForms.PlotView plotView;
    private CheckBox EditFibShow;
    private Panel panel1;
    private Button ButtonPlus;
    private Button ButtonMinus;
    private Panel PanelPlayBack;
    private Button ButtonGoRight;
    private Button ButtonGoLeft;
    private Label labelInterval;
    private Label labelMaxTime;
    private CheckBox EditFibZhowZigZag;
    private Button ButtonRefresh;
    private CheckBox EditShowFvgZones;
    private CheckBox EditShowDtb;
    private ComboBox EditTrendType;
    private GroupBox groupBox2;
    private Label label1;
    private GroupBox groupBox1;
    private CheckBox EditTransparant;
    private CheckBox EditShowPivots;
    private CheckBox EditShowSignals;
    private GroupBox groupBox4;
    private ComboBox EditFibTrend;
    private GroupBox groupBox5;
    private CheckBox EditTrendShowZigZag;
    private CheckBox EditShowNadarayaWatsonEnvelope;
    private CheckBox EditShowBollingerBand;
    private CheckBox EditShowSmaLinesSbm;
    private Button ButtonOpenTradingApp;
    private CheckBox EditShowNadarayaWatsonEnvelopeRepaining;
}
