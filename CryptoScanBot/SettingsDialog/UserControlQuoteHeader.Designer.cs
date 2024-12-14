namespace CryptoScanBot.SettingsDialog;

partial class UserControlQuoteHeader
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
        label2 = new Label();
        label3 = new Label();
        label4 = new Label();
        label5 = new Label();
        label6 = new Label();
        SuspendLayout();
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(17, 5);
        label1.Name = "label1";
        label1.Size = new Size(40, 15);
        label1.TabIndex = 0;
        label1.Text = "Active";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(119, 5);
        label2.Name = "label2";
        label2.Size = new Size(74, 15);
        label2.TabIndex = 1;
        label2.Text = "Min. volume";
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(371, 5);
        label3.Name = "label3";
        label3.Size = new Size(51, 15);
        label3.TabIndex = 2;
        label3.Text = "Amount";
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new Point(245, 5);
        label4.Name = "label4";
        label4.Size = new Size(60, 15);
        label4.TabIndex = 3;
        label4.Text = "Min. price";
        // 
        // label5
        // 
        label5.AutoSize = true;
        label5.Location = new Point(497, 5);
        label5.Name = "label5";
        label5.Size = new Size(66, 15);
        label5.TabIndex = 4;
        label5.Text = "Percentage";
        // 
        // label6
        // 
        label6.AutoSize = true;
        label6.Location = new Point(581, 5);
        label6.Name = "label6";
        label6.Size = new Size(36, 15);
        label6.TabIndex = 5;
        label6.Text = "Color";
        // 
        // UserControlQuoteHeader
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(label6);
        Controls.Add(label5);
        Controls.Add(label4);
        Controls.Add(label3);
        Controls.Add(label2);
        Controls.Add(label1);
        Margin = new Padding(0);
        Name = "UserControlQuoteHeader";
        Padding = new Padding(0, 0, 0, 3);
        Size = new Size(620, 23);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private Label label2;
    private Label label3;
    private Label label4;
    private Label label5;
    private Label label6;
}
