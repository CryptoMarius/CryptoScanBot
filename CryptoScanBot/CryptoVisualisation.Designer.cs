namespace CryptoShowTrend;

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
        EditUseHighLow = new CheckBox();
        label4 = new Label();
        EditDeviation = new NumericUpDown();
        EditShowLiqBoxes = new CheckBox();
        EditZoomLiqBoxes = new CheckBox();
        EditShowZigZag = new CheckBox();
        label9 = new Label();
        EditCandleCount = new NumericUpDown();
        ButtonCalculate = new Button();
        ButtonZoomLast = new Button();
        flowLayoutPanel1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)EditDeviation).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditCandleCount).BeginInit();
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
        flowLayoutPanel1.Controls.Add(EditUseHighLow);
        flowLayoutPanel1.Controls.Add(label4);
        flowLayoutPanel1.Controls.Add(EditDeviation);
        flowLayoutPanel1.Controls.Add(EditShowLiqBoxes);
        flowLayoutPanel1.Controls.Add(EditZoomLiqBoxes);
        flowLayoutPanel1.Controls.Add(EditShowZigZag);
        flowLayoutPanel1.Controls.Add(label9);
        flowLayoutPanel1.Controls.Add(EditCandleCount);
        flowLayoutPanel1.Controls.Add(ButtonCalculate);
        flowLayoutPanel1.Controls.Add(ButtonZoomLast);
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
        // EditUseHighLow
        // 
        EditUseHighLow.AutoSize = true;
        EditUseHighLow.Location = new Point(6, 138);
        EditUseHighLow.Name = "EditUseHighLow";
        EditUseHighLow.Size = new Size(101, 19);
        EditUseHighLow.TabIndex = 6;
        EditUseHighLow.Text = "Use High/Low";
        EditUseHighLow.UseVisualStyleBackColor = true;
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new Point(6, 160);
        label4.Name = "label4";
        label4.Size = new Size(57, 15);
        label4.TabIndex = 16;
        label4.Text = "Deviation";
        // 
        // EditDeviation
        // 
        EditDeviation.DecimalPlaces = 2;
        EditDeviation.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        EditDeviation.Location = new Point(6, 178);
        EditDeviation.Name = "EditDeviation";
        EditDeviation.Size = new Size(120, 23);
        EditDeviation.TabIndex = 1;
        // 
        // EditShowLiqBoxes
        // 
        EditShowLiqBoxes.AutoSize = true;
        EditShowLiqBoxes.Location = new Point(6, 207);
        EditShowLiqBoxes.Name = "EditShowLiqBoxes";
        EditShowLiqBoxes.Size = new Size(111, 19);
        EditShowLiqBoxes.TabIndex = 5;
        EditShowLiqBoxes.Text = "Show Liq. boxes";
        EditShowLiqBoxes.UseVisualStyleBackColor = true;
        // 
        // EditZoomLiqBoxes
        // 
        EditZoomLiqBoxes.AutoSize = true;
        EditZoomLiqBoxes.Location = new Point(6, 232);
        EditZoomLiqBoxes.Name = "EditZoomLiqBoxes";
        EditZoomLiqBoxes.Size = new Size(111, 19);
        EditZoomLiqBoxes.TabIndex = 4;
        EditZoomLiqBoxes.Text = "Zoom liq. boxes";
        EditZoomLiqBoxes.UseVisualStyleBackColor = true;
        // 
        // EditShowZigZag
        // 
        EditShowZigZag.AutoSize = true;
        EditShowZigZag.Location = new Point(6, 257);
        EditShowZigZag.Name = "EditShowZigZag";
        EditShowZigZag.Size = new Size(95, 19);
        EditShowZigZag.TabIndex = 7;
        EditShowZigZag.Text = "Show ZigZag";
        EditShowZigZag.UseVisualStyleBackColor = true;
        // 
        // label9
        // 
        label9.AutoSize = true;
        label9.Location = new Point(6, 279);
        label9.Name = "label9";
        label9.Size = new Size(49, 15);
        label9.TabIndex = 11;
        label9.Text = "Candles";
        // 
        // EditCandleCount
        // 
        EditCandleCount.Location = new Point(6, 297);
        EditCandleCount.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
        EditCandleCount.Name = "EditCandleCount";
        EditCandleCount.Size = new Size(120, 23);
        EditCandleCount.TabIndex = 10;
        // 
        // ButtonCalculate
        // 
        ButtonCalculate.Location = new Point(6, 326);
        ButtonCalculate.Name = "ButtonCalculate";
        ButtonCalculate.Size = new Size(75, 23);
        ButtonCalculate.TabIndex = 8;
        ButtonCalculate.Text = "Calculate";
        ButtonCalculate.UseVisualStyleBackColor = true;
        // 
        // ButtonZoomLast
        // 
        ButtonZoomLast.Location = new Point(6, 355);
        ButtonZoomLast.Name = "ButtonZoomLast";
        ButtonZoomLast.Size = new Size(75, 23);
        ButtonZoomLast.TabIndex = 14;
        ButtonZoomLast.Text = "Zoom last";
        ButtonZoomLast.UseVisualStyleBackColor = true;
        // 
        // CryptoVisualisation
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1184, 761);
        Controls.Add(flowLayoutPanel1);
        Name = "CryptoVisualisation";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Form1";
        flowLayoutPanel1.ResumeLayout(false);
        flowLayoutPanel1.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)EditDeviation).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditCandleCount).EndInit();
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
    private CheckBox EditUseHighLow;
    private CheckBox EditShowZigZag;
    private Button ButtonCalculate;
    private ComboBox EditIntervalName;
    private Label label9;
    private NumericUpDown EditCandleCount;
    private Label label3;
    private ComboBox EditSymbolQuote;
    private Button Button1;
    private Label label4;
    private NumericUpDown EditDeviation;
    private Button ButtonZoomLast;
}
