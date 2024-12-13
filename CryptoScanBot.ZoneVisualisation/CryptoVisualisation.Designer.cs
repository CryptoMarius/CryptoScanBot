namespace CryptoScanBot.ZoneVisualisation;

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
        EditTransparant = new CheckBox();
        EditShowPivots = new CheckBox();
        EditShowSignals = new CheckBox();
        label1 = new Label();
        EditSymbolBase = new ComboBox();
        label3 = new Label();
        EditSymbolQuote = new ComboBox();
        labelInterval2 = new Label();
        EditIntervalName = new ComboBox();
        label4 = new Label();
        EditDeviation = new NumericUpDown();
        EditUseOptimizing = new CheckBox();
        EditShowLiqBoxes = new CheckBox();
        EditZoomLiqBoxes = new CheckBox();
        EditShowZigZag = new CheckBox();
        EditShowSecondary = new CheckBox();
        ButtonRefresh = new Button();
        ButtonCalculate = new Button();
        ButtonZoomLast = new Button();
        EditShowFib = new CheckBox();
        EditShowFibZigZag = new CheckBox();
        panel1 = new Panel();
        labelInterval = new Label();
        ButtonPlus = new Button();
        ButtonMinus = new Button();
        PanelPlayBack = new Panel();
        labelMaxTime = new Label();
        ButtonGoRight = new Button();
        ButtonGoLeft = new Button();
        EditShowPositions = new CheckBox();
        EditUseBatchProcess = new CheckBox();
        plotView = new OxyPlot.WindowsForms.PlotView();
        flowLayoutPanel1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditDeviation).BeginInit();
        panel1.SuspendLayout();
        PanelPlayBack.SuspendLayout();
        SuspendLayout();
        // 
        // flowLayoutPanel1
        // 
        flowLayoutPanel1.AutoSize = true;
        flowLayoutPanel1.Controls.Add(EditTransparant);
        flowLayoutPanel1.Controls.Add(EditShowPivots);
        flowLayoutPanel1.Controls.Add(EditShowSignals);
        flowLayoutPanel1.Controls.Add(label1);
        flowLayoutPanel1.Controls.Add(EditSymbolBase);
        flowLayoutPanel1.Controls.Add(label3);
        flowLayoutPanel1.Controls.Add(EditSymbolQuote);
        flowLayoutPanel1.Controls.Add(labelInterval2);
        flowLayoutPanel1.Controls.Add(EditIntervalName);
        flowLayoutPanel1.Controls.Add(label4);
        flowLayoutPanel1.Controls.Add(EditDeviation);
        flowLayoutPanel1.Controls.Add(EditUseOptimizing);
        flowLayoutPanel1.Controls.Add(EditShowLiqBoxes);
        flowLayoutPanel1.Controls.Add(EditZoomLiqBoxes);
        flowLayoutPanel1.Controls.Add(EditShowZigZag);
        flowLayoutPanel1.Controls.Add(EditShowSecondary);
        flowLayoutPanel1.Controls.Add(ButtonRefresh);
        flowLayoutPanel1.Controls.Add(ButtonCalculate);
        flowLayoutPanel1.Controls.Add(ButtonZoomLast);
        flowLayoutPanel1.Controls.Add(EditShowFib);
        flowLayoutPanel1.Controls.Add(EditShowFibZigZag);
        flowLayoutPanel1.Controls.Add(panel1);
        flowLayoutPanel1.Controls.Add(PanelPlayBack);
        flowLayoutPanel1.Controls.Add(EditShowPositions);
        flowLayoutPanel1.Controls.Add(EditUseBatchProcess);
        flowLayoutPanel1.Dock = DockStyle.Left;
        flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel1.Location = new Point(0, 0);
        flowLayoutPanel1.Name = "flowLayoutPanel1";
        flowLayoutPanel1.Padding = new Padding(3);
        flowLayoutPanel1.Size = new Size(151, 802);
        flowLayoutPanel1.TabIndex = 0;
        // 
        // EditTransparant
        // 
        EditTransparant.AutoSize = true;
        EditTransparant.Location = new Point(6, 6);
        EditTransparant.Name = "EditTransparant";
        EditTransparant.Size = new Size(87, 19);
        EditTransparant.TabIndex = 23;
        EditTransparant.Text = "Transparant";
        EditTransparant.UseVisualStyleBackColor = true;
        // 
        // EditShowPivots
        // 
        EditShowPivots.AutoSize = true;
        EditShowPivots.Location = new Point(6, 31);
        EditShowPivots.Name = "EditShowPivots";
        EditShowPivots.Size = new Size(91, 19);
        EditShowPivots.TabIndex = 27;
        EditShowPivots.Text = "Show points";
        EditShowPivots.UseVisualStyleBackColor = true;
        // 
        // EditShowSignals
        // 
        EditShowSignals.AutoSize = true;
        EditShowSignals.Location = new Point(6, 56);
        EditShowSignals.Name = "EditShowSignals";
        EditShowSignals.Size = new Size(94, 19);
        EditShowSignals.TabIndex = 29;
        EditShowSignals.Text = "Show signals";
        EditShowSignals.UseVisualStyleBackColor = true;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(6, 78);
        label1.Name = "label1";
        label1.Size = new Size(74, 15);
        label1.TabIndex = 2;
        label1.Text = "Symbol base";
        // 
        // EditSymbolBase
        // 
        EditSymbolBase.Location = new Point(6, 96);
        EditSymbolBase.Name = "EditSymbolBase";
        EditSymbolBase.Size = new Size(121, 23);
        EditSymbolBase.TabIndex = 9;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(6, 122);
        label3.Name = "label3";
        label3.Size = new Size(81, 15);
        label3.TabIndex = 13;
        label3.Text = "Symbol quote";
        // 
        // EditSymbolQuote
        // 
        EditSymbolQuote.Location = new Point(6, 140);
        EditSymbolQuote.Name = "EditSymbolQuote";
        EditSymbolQuote.Size = new Size(120, 23);
        EditSymbolQuote.TabIndex = 12;
        // 
        // labelInterval2
        // 
        labelInterval2.AutoSize = true;
        labelInterval2.Location = new Point(6, 166);
        labelInterval2.Name = "labelInterval2";
        labelInterval2.Size = new Size(46, 15);
        labelInterval2.TabIndex = 3;
        labelInterval2.Text = "Interval";
        // 
        // EditIntervalName
        // 
        EditIntervalName.FormattingEnabled = true;
        EditIntervalName.Location = new Point(6, 184);
        EditIntervalName.Name = "EditIntervalName";
        EditIntervalName.Size = new Size(121, 23);
        EditIntervalName.TabIndex = 9;
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new Point(6, 210);
        label4.Name = "label4";
        label4.Size = new Size(57, 15);
        label4.TabIndex = 16;
        label4.Text = "Deviation";
        // 
        // EditDeviation
        // 
        EditDeviation.DecimalPlaces = 2;
        EditDeviation.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditDeviation.Location = new Point(6, 228);
        EditDeviation.Name = "EditDeviation";
        EditDeviation.Size = new Size(120, 23);
        EditDeviation.TabIndex = 1;
        // 
        // EditUseOptimizing
        // 
        EditUseOptimizing.AutoSize = true;
        EditUseOptimizing.Location = new Point(6, 257);
        EditUseOptimizing.Name = "EditUseOptimizing";
        EditUseOptimizing.Size = new Size(92, 19);
        EditUseOptimizing.TabIndex = 26;
        EditUseOptimizing.Text = "Optimize list";
        EditUseOptimizing.UseVisualStyleBackColor = true;
        // 
        // EditShowLiqBoxes
        // 
        EditShowLiqBoxes.AutoSize = true;
        EditShowLiqBoxes.Location = new Point(6, 282);
        EditShowLiqBoxes.Name = "EditShowLiqBoxes";
        EditShowLiqBoxes.Size = new Size(111, 19);
        EditShowLiqBoxes.TabIndex = 5;
        EditShowLiqBoxes.Text = "Show Liq. boxes";
        EditShowLiqBoxes.UseVisualStyleBackColor = true;
        // 
        // EditZoomLiqBoxes
        // 
        EditZoomLiqBoxes.AutoSize = true;
        EditZoomLiqBoxes.Location = new Point(6, 307);
        EditZoomLiqBoxes.Name = "EditZoomLiqBoxes";
        EditZoomLiqBoxes.Size = new Size(111, 19);
        EditZoomLiqBoxes.TabIndex = 4;
        EditZoomLiqBoxes.Text = "Zoom liq. boxes";
        EditZoomLiqBoxes.UseVisualStyleBackColor = true;
        // 
        // EditShowZigZag
        // 
        EditShowZigZag.AutoSize = true;
        EditShowZigZag.Location = new Point(6, 332);
        EditShowZigZag.Name = "EditShowZigZag";
        EditShowZigZag.Size = new Size(101, 19);
        EditShowZigZag.TabIndex = 7;
        EditShowZigZag.Text = "Show liq. lines";
        EditShowZigZag.UseVisualStyleBackColor = true;
        // 
        // EditShowSecondary
        // 
        EditShowSecondary.AutoSize = true;
        EditShowSecondary.Location = new Point(6, 357);
        EditShowSecondary.Name = "EditShowSecondary";
        EditShowSecondary.Size = new Size(115, 19);
        EditShowSecondary.TabIndex = 25;
        EditShowSecondary.Text = "Show secondairy";
        EditShowSecondary.UseVisualStyleBackColor = true;
        // 
        // ButtonRefresh
        // 
        ButtonRefresh.Location = new Point(6, 382);
        ButtonRefresh.Name = "ButtonRefresh";
        ButtonRefresh.Size = new Size(121, 23);
        ButtonRefresh.TabIndex = 31;
        ButtonRefresh.Text = "Refresh screen";
        ButtonRefresh.UseVisualStyleBackColor = true;
        // 
        // ButtonCalculate
        // 
        ButtonCalculate.Location = new Point(6, 411);
        ButtonCalculate.Name = "ButtonCalculate";
        ButtonCalculate.Size = new Size(121, 23);
        ButtonCalculate.TabIndex = 8;
        ButtonCalculate.Text = "Calculate zones";
        ButtonCalculate.UseVisualStyleBackColor = true;
        // 
        // ButtonZoomLast
        // 
        ButtonZoomLast.Location = new Point(6, 440);
        ButtonZoomLast.Name = "ButtonZoomLast";
        ButtonZoomLast.Size = new Size(121, 23);
        ButtonZoomLast.TabIndex = 14;
        ButtonZoomLast.Text = "Zoom last";
        ButtonZoomLast.UseVisualStyleBackColor = true;
        // 
        // EditShowFib
        // 
        EditShowFib.AutoSize = true;
        EditShowFib.Location = new Point(6, 469);
        EditShowFib.Name = "EditShowFib";
        EditShowFib.Size = new Size(139, 19);
        EditShowFib.TabIndex = 20;
        EditShowFib.Text = "Show fib retracement";
        EditShowFib.UseVisualStyleBackColor = true;
        // 
        // EditShowFibZigZag
        // 
        EditShowFibZigZag.AutoSize = true;
        EditShowFibZigZag.Location = new Point(6, 494);
        EditShowFibZigZag.Name = "EditShowFibZigZag";
        EditShowFibZigZag.Size = new Size(99, 19);
        EditShowFibZigZag.TabIndex = 24;
        EditShowFibZigZag.Text = "Show fib lines";
        EditShowFibZigZag.UseVisualStyleBackColor = true;
        // 
        // panel1
        // 
        panel1.Controls.Add(labelInterval);
        panel1.Controls.Add(ButtonPlus);
        panel1.Controls.Add(ButtonMinus);
        panel1.Location = new Point(6, 519);
        panel1.Name = "panel1";
        panel1.Size = new Size(95, 66);
        panel1.TabIndex = 21;
        // 
        // labelInterval
        // 
        labelInterval.AutoSize = true;
        labelInterval.Location = new Point(11, 43);
        labelInterval.Name = "labelInterval";
        labelInterval.Size = new Size(38, 15);
        labelInterval.TabIndex = 21;
        labelInterval.Text = "label5";
        // 
        // ButtonPlus
        // 
        ButtonPlus.Location = new Point(47, 10);
        ButtonPlus.Name = "ButtonPlus";
        ButtonPlus.Size = new Size(25, 23);
        ButtonPlus.TabIndex = 20;
        ButtonPlus.Text = "+";
        ButtonPlus.UseVisualStyleBackColor = true;
        ButtonPlus.Click += ButtonPlusClick;
        // 
        // ButtonMinus
        // 
        ButtonMinus.Location = new Point(13, 10);
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
        PanelPlayBack.Location = new Point(6, 591);
        PanelPlayBack.Name = "PanelPlayBack";
        PanelPlayBack.Size = new Size(95, 65);
        PanelPlayBack.TabIndex = 22;
        // 
        // labelMaxTime
        // 
        labelMaxTime.AutoSize = true;
        labelMaxTime.Location = new Point(13, 40);
        labelMaxTime.Name = "labelMaxTime";
        labelMaxTime.Size = new Size(38, 15);
        labelMaxTime.TabIndex = 23;
        labelMaxTime.Text = "label5";
        // 
        // ButtonGoRight
        // 
        ButtonGoRight.Location = new Point(49, 8);
        ButtonGoRight.Name = "ButtonGoRight";
        ButtonGoRight.Size = new Size(25, 23);
        ButtonGoRight.TabIndex = 22;
        ButtonGoRight.Text = ">";
        ButtonGoRight.UseVisualStyleBackColor = true;
        ButtonGoRight.Click += ButtonGoRightClick;
        // 
        // ButtonGoLeft
        // 
        ButtonGoLeft.Location = new Point(13, 8);
        ButtonGoLeft.Name = "ButtonGoLeft";
        ButtonGoLeft.Size = new Size(25, 23);
        ButtonGoLeft.TabIndex = 21;
        ButtonGoLeft.Text = "<";
        ButtonGoLeft.UseVisualStyleBackColor = true;
        ButtonGoLeft.Click += ButtonGoLeftClick;
        // 
        // EditShowPositions
        // 
        EditShowPositions.AutoSize = true;
        EditShowPositions.Location = new Point(6, 662);
        EditShowPositions.Name = "EditShowPositions";
        EditShowPositions.Size = new Size(106, 19);
        EditShowPositions.TabIndex = 30;
        EditShowPositions.Text = "Show positions";
        EditShowPositions.UseVisualStyleBackColor = true;
        // 
        // EditUseBatchProcess
        // 
        EditUseBatchProcess.AutoSize = true;
        EditUseBatchProcess.Location = new Point(6, 687);
        EditUseBatchProcess.Name = "EditUseBatchProcess";
        EditUseBatchProcess.Size = new Size(99, 19);
        EditUseBatchProcess.TabIndex = 28;
        EditUseBatchProcess.Text = "Batch process";
        EditUseBatchProcess.UseVisualStyleBackColor = true;
        // 
        // plotView
        // 
        plotView.BackColor = Color.Black;
        plotView.Dock = DockStyle.Fill;
        plotView.Location = new Point(151, 0);
        plotView.Name = "plotView";
        plotView.PanCursor = Cursors.Hand;
        plotView.Size = new Size(1190, 802);
        plotView.TabIndex = 1;
        plotView.Text = "plotView1";
        plotView.ZoomHorizontalCursor = Cursors.SizeWE;
        plotView.ZoomRectangleCursor = Cursors.SizeNWSE;
        plotView.ZoomVerticalCursor = Cursors.SizeNS;
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
        flowLayoutPanel1.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditDeviation).EndInit();
        panel1.ResumeLayout(false);
        panel1.PerformLayout();
        PanelPlayBack.ResumeLayout(false);
        PanelPlayBack.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private FlowLayoutPanel flowLayoutPanel1;
    private Label label1;
    private ComboBox EditSymbolBase;
    private Label labelInterval2;
    private CheckBox EditZoomLiqBoxes;
    private CheckBox EditShowLiqBoxes;
    private CheckBox EditShowZigZag;
    private Button ButtonCalculate;
    private ComboBox EditIntervalName;
    private Label label3;
    private ComboBox EditSymbolQuote;
    private Label label4;
    private NumericUpDown EditDeviation;
    private Button ButtonZoomLast;
    private OxyPlot.WindowsForms.PlotView plotView;
    private CheckBox EditShowFib;
    private Panel panel1;
    private Button ButtonPlus;
    private Button ButtonMinus;
    private Panel PanelPlayBack;
    private Button ButtonGoRight;
    private Button ButtonGoLeft;
    private Label labelInterval;
    private Label labelMaxTime;
    private CheckBox EditTransparant;
    private CheckBox EditShowFibZigZag;
    private CheckBox EditShowSecondary;
    private CheckBox EditUseOptimizing;
    private CheckBox EditShowPivots;
    private CheckBox EditUseBatchProcess;
    private CheckBox EditShowSignals;
    private CheckBox EditShowPositions;
    private Button ButtonRefresh;
}
