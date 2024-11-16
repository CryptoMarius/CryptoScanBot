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
        label1 = new Label();
        EditSymbolBase = new ComboBox();
        label3 = new Label();
        EditSymbolQuote = new ComboBox();
        label2 = new Label();
        EditIntervalName = new ComboBox();
        label4 = new Label();
        EditDeviation = new NumericUpDown();
        EditShowLiqBoxes = new CheckBox();
        EditZoomLiqBoxes = new CheckBox();
        EditShowZigZag = new CheckBox();
        EditShowFib = new CheckBox();
        ButtonCalculate = new Button();
        ButtonZoomLast = new Button();
        panel1 = new Panel();
        labelInterval = new Label();
        ButtonPlus = new Button();
        ButtonMinus = new Button();
        panel2 = new Panel();
        labelMaxTime = new Label();
        ButtonGoRight = new Button();
        ButtonGoLeft = new Button();
        plotView = new OxyPlot.WindowsForms.PlotView();
        flowLayoutPanel1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditDeviation).BeginInit();
        panel1.SuspendLayout();
        panel2.SuspendLayout();
        SuspendLayout();
        // 
        // flowLayoutPanel1
        // 
        flowLayoutPanel1.AutoSize = true;
        flowLayoutPanel1.Controls.Add(label1);
        flowLayoutPanel1.Controls.Add(EditSymbolBase);
        flowLayoutPanel1.Controls.Add(label3);
        flowLayoutPanel1.Controls.Add(EditSymbolQuote);
        flowLayoutPanel1.Controls.Add(label2);
        flowLayoutPanel1.Controls.Add(EditIntervalName);
        flowLayoutPanel1.Controls.Add(label4);
        flowLayoutPanel1.Controls.Add(EditDeviation);
        flowLayoutPanel1.Controls.Add(EditShowLiqBoxes);
        flowLayoutPanel1.Controls.Add(EditZoomLiqBoxes);
        flowLayoutPanel1.Controls.Add(EditShowZigZag);
        flowLayoutPanel1.Controls.Add(EditShowFib);
        flowLayoutPanel1.Controls.Add(ButtonCalculate);
        flowLayoutPanel1.Controls.Add(ButtonZoomLast);
        flowLayoutPanel1.Controls.Add(panel1);
        flowLayoutPanel1.Controls.Add(panel2);
        flowLayoutPanel1.Dock = DockStyle.Left;
        flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
        flowLayoutPanel1.Location = new Point(0, 0);
        flowLayoutPanel1.Name = "flowLayoutPanel1";
        flowLayoutPanel1.Padding = new Padding(3);
        flowLayoutPanel1.Size = new Size(133, 761);
        flowLayoutPanel1.TabIndex = 0;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(6, 3);
        label1.Name = "label1";
        label1.Size = new Size(74, 15);
        label1.TabIndex = 2;
        label1.Text = "Symbol base";
        // 
        // EditSymbolBase
        // 
        EditSymbolBase.Location = new Point(6, 21);
        EditSymbolBase.Name = "EditSymbolBase";
        EditSymbolBase.Size = new Size(121, 23);
        EditSymbolBase.TabIndex = 9;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(6, 47);
        label3.Name = "label3";
        label3.Size = new Size(81, 15);
        label3.TabIndex = 13;
        label3.Text = "Symbol quote";
        // 
        // EditSymbolQuote
        // 
        EditSymbolQuote.Location = new Point(6, 65);
        EditSymbolQuote.Name = "EditSymbolQuote";
        EditSymbolQuote.Size = new Size(100, 23);
        EditSymbolQuote.TabIndex = 12;
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(6, 91);
        label2.Name = "label2";
        label2.Size = new Size(46, 15);
        label2.TabIndex = 3;
        label2.Text = "Interval";
        // 
        // EditIntervalName
        // 
        EditIntervalName.FormattingEnabled = true;
        EditIntervalName.Location = new Point(6, 109);
        EditIntervalName.Name = "EditIntervalName";
        EditIntervalName.Size = new Size(121, 23);
        EditIntervalName.TabIndex = 9;
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new Point(6, 135);
        label4.Name = "label4";
        label4.Size = new Size(57, 15);
        label4.TabIndex = 16;
        label4.Text = "Deviation";
        // 
        // EditDeviation
        // 
        EditDeviation.DecimalPlaces = 2;
        EditDeviation.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditDeviation.Location = new Point(6, 153);
        EditDeviation.Name = "EditDeviation";
        EditDeviation.Size = new Size(120, 23);
        EditDeviation.TabIndex = 1;
        // 
        // EditShowLiqBoxes
        // 
        EditShowLiqBoxes.AutoSize = true;
        EditShowLiqBoxes.Location = new Point(6, 182);
        EditShowLiqBoxes.Name = "EditShowLiqBoxes";
        EditShowLiqBoxes.Size = new Size(111, 19);
        EditShowLiqBoxes.TabIndex = 5;
        EditShowLiqBoxes.Text = "Show Liq. boxes";
        EditShowLiqBoxes.UseVisualStyleBackColor = true;
        // 
        // EditZoomLiqBoxes
        // 
        EditZoomLiqBoxes.AutoSize = true;
        EditZoomLiqBoxes.Location = new Point(6, 207);
        EditZoomLiqBoxes.Name = "EditZoomLiqBoxes";
        EditZoomLiqBoxes.Size = new Size(111, 19);
        EditZoomLiqBoxes.TabIndex = 4;
        EditZoomLiqBoxes.Text = "Zoom liq. boxes";
        EditZoomLiqBoxes.UseVisualStyleBackColor = true;
        // 
        // EditShowZigZag
        // 
        EditShowZigZag.AutoSize = true;
        EditShowZigZag.Location = new Point(6, 232);
        EditShowZigZag.Name = "EditShowZigZag";
        EditShowZigZag.Size = new Size(95, 19);
        EditShowZigZag.TabIndex = 7;
        EditShowZigZag.Text = "Show ZigZag";
        EditShowZigZag.UseVisualStyleBackColor = true;
        // 
        // EditShowFib
        // 
        EditShowFib.AutoSize = true;
        EditShowFib.Location = new Point(6, 257);
        EditShowFib.Name = "EditShowFib";
        EditShowFib.Size = new Size(72, 19);
        EditShowFib.TabIndex = 20;
        EditShowFib.Text = "Show fib";
        EditShowFib.UseVisualStyleBackColor = true;
        // 
        // ButtonCalculate
        // 
        ButtonCalculate.Location = new Point(6, 282);
        ButtonCalculate.Name = "ButtonCalculate";
        ButtonCalculate.Size = new Size(75, 23);
        ButtonCalculate.TabIndex = 8;
        ButtonCalculate.Text = "Calculate";
        ButtonCalculate.UseVisualStyleBackColor = true;
        // 
        // ButtonZoomLast
        // 
        ButtonZoomLast.Location = new Point(6, 311);
        ButtonZoomLast.Name = "ButtonZoomLast";
        ButtonZoomLast.Size = new Size(75, 23);
        ButtonZoomLast.TabIndex = 14;
        ButtonZoomLast.Text = "Zoom last";
        ButtonZoomLast.UseVisualStyleBackColor = true;
        // 
        // panel1
        // 
        panel1.Controls.Add(labelInterval);
        panel1.Controls.Add(ButtonPlus);
        panel1.Controls.Add(ButtonMinus);
        panel1.Location = new Point(6, 340);
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
        // panel2
        // 
        panel2.Controls.Add(labelMaxTime);
        panel2.Controls.Add(ButtonGoRight);
        panel2.Controls.Add(ButtonGoLeft);
        panel2.Location = new Point(6, 412);
        panel2.Name = "panel2";
        panel2.Size = new Size(95, 65);
        panel2.TabIndex = 22;
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
        // plotView
        // 
        plotView.BackColor = Color.Black;
        plotView.Dock = DockStyle.Fill;
        plotView.Location = new Point(133, 0);
        plotView.Name = "plotView";
        plotView.PanCursor = Cursors.Hand;
        plotView.Size = new Size(1051, 761);
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
        ClientSize = new Size(1184, 761);
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
        panel2.ResumeLayout(false);
        panel2.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private FlowLayoutPanel flowLayoutPanel1;
    private Label label1;
    private ComboBox EditSymbolBase;
    private Label label2;
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
    private Panel panel2;
    private Button ButtonGoRight;
    private Button ButtonGoLeft;
    private Label labelInterval;
    private Label labelMaxTime;
}
