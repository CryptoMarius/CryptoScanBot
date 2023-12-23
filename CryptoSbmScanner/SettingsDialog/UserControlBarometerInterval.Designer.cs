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
        EditIsActive = new CheckBox();
        EditMaximal = new NumericUpDown();
        EditMinimal = new NumericUpDown();
        ((System.ComponentModel.ISupportInitialize)EditMaximal).BeginInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimal).BeginInit();
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
        // EditIsActive
        // 
        EditIsActive.AutoSize = true;
        EditIsActive.Location = new Point(10, 5);
        EditIsActive.Margin = new Padding(4, 3, 4, 3);
        EditIsActive.Name = "EditIsActive";
        EditIsActive.Size = new Size(49, 19);
        EditIsActive.TabIndex = 269;
        EditIsActive.Text = "15m";
        EditIsActive.UseVisualStyleBackColor = true;
        // 
        // EditMaximal
        // 
        EditMaximal.DecimalPlaces = 2;
        EditMaximal.Location = new Point(256, 4);
        EditMaximal.Margin = new Padding(4, 3, 4, 3);
        EditMaximal.Name = "EditMaximal";
        EditMaximal.Size = new Size(88, 23);
        EditMaximal.TabIndex = 268;
        EditMaximal.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // EditMinimal
        // 
        EditMinimal.DecimalPlaces = 2;
        EditMinimal.Location = new Point(129, 3);
        EditMinimal.Margin = new Padding(4, 3, 4, 3);
        EditMinimal.Name = "EditMinimal";
        EditMinimal.Size = new Size(88, 23);
        EditMinimal.TabIndex = 267;
        EditMinimal.Value = new decimal(new int[] { 25, 0, 0, 0 });
        // 
        // UserControlBarometerInterval
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        Controls.Add(label1);
        Controls.Add(EditIsActive);
        Controls.Add(EditMaximal);
        Controls.Add(EditMinimal);
        Name = "UserControlBarometerInterval";
        Size = new Size(357, 30);
        ((System.ComponentModel.ISupportInitialize)EditMaximal).EndInit();
        ((System.ComponentModel.ISupportInitialize)EditMinimal).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private CheckBox EditIsActive;
    private NumericUpDown EditMaximal;
    private NumericUpDown EditMinimal;
}
