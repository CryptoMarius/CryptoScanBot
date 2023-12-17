namespace CryptoSbmScanner.SettingsDialog;

partial class UserControlBarometerInterval
{
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        label1 = new Label();
        EditBarometer = new CheckBox();
        EditBarometerMax = new NumericUpDown();
        EditBarometerMin = new NumericUpDown();
        ((System.ComponentModel.ISupportInitialize)EditBarometerMax).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometerMin).BeginInit();
        SuspendLayout();
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(228, 6);
        label1.Name = "label1";
        label1.Size = new Size(13, 15);
        label1.TabIndex = 270;
        label1.Text = "..";
        // 
        // EditBarometer
        // 
        EditBarometer.AutoSize = true;
        EditBarometer.Location = new Point(17, 5);
        EditBarometer.Margin = new Padding(4, 3, 4, 3);
        EditBarometer.Name = "EditBarometer";
        EditBarometer.Size = new Size(49, 19);
        EditBarometer.TabIndex = 269;
        EditBarometer.Text = "15m";
        EditBarometer.UseVisualStyleBackColor = true;
        // 
        // EditBarometerMax
        // 
        EditBarometerMax.DecimalPlaces = 2;
        EditBarometerMax.Location = new Point(256, 4);
        EditBarometerMax.Margin = new Padding(4, 3, 4, 3);
        EditBarometerMax.Name = "EditBarometerMax";
        EditBarometerMax.Size = new Size(88, 23);
        EditBarometerMax.TabIndex = 268;
        EditBarometerMax.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // EditBarometerMin
        // 
        EditBarometerMin.DecimalPlaces = 2;
        EditBarometerMin.Location = new Point(129, 3);
        EditBarometerMin.Margin = new Padding(4, 3, 4, 3);
        EditBarometerMin.Name = "EditBarometerMin";
        EditBarometerMin.Size = new Size(88, 23);
        EditBarometerMin.TabIndex = 267;
        EditBarometerMin.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // UserControlBarometerInterval
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        Controls.Add(label1);
        Controls.Add(EditBarometer);
        Controls.Add(EditBarometerMax);
        Controls.Add(EditBarometerMin);
        Name = "UserControlBarometerInterval";
        Size = new Size(357, 30);
        ((System.ComponentModel.ISupportInitialize)EditBarometerMax).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditBarometerMin).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private CheckBox EditBarometer;
    private NumericUpDown EditBarometerMax;
    private NumericUpDown EditBarometerMin;
}
