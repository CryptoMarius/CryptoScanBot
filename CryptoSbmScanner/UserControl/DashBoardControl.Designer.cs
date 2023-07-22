namespace CryptoSbmScanner;

partial class DashBoardControl
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
        button1 = new Button();
        label1 = new Label();
        label2 = new Label();
        label5 = new Label();
        label6 = new Label();
        label7 = new Label();
        labelReturned = new Label();
        labelCommission = new Label();
        labelNettoPnlValue = new Label();
        labelInvested = new Label();
        labelPositions = new Label();
        label8 = new Label();
        SuspendLayout();
        // 
        // button1
        // 
        button1.Location = new Point(537, 3);
        button1.Name = "button1";
        button1.Size = new Size(75, 23);
        button1.TabIndex = 0;
        button1.Text = "Ververs";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(46, 83);
        label1.Name = "label1";
        label1.Size = new Size(75, 15);
        label1.TabIndex = 1;
        label1.Text = "Geinvesteerd";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(27, 30);
        label2.Name = "label2";
        label2.Size = new Size(71, 15);
        label2.TabIndex = 2;
        label2.Text = "Openstaand";
        // 
        // label5
        // 
        label5.AutoSize = true;
        label5.Location = new Point(46, 160);
        label5.Name = "label5";
        label5.Size = new Size(62, 15);
        label5.TabIndex = 5;
        label5.Text = "Netto PNL";
        // 
        // label6
        // 
        label6.AutoSize = true;
        label6.Location = new Point(46, 135);
        label6.Name = "label6";
        label6.Size = new Size(66, 15);
        label6.TabIndex = 6;
        label6.Text = "Commissie";
        // 
        // label7
        // 
        label7.AutoSize = true;
        label7.Location = new Point(46, 108);
        label7.Name = "label7";
        label7.Size = new Size(83, 15);
        label7.TabIndex = 7;
        label7.Text = "Geretourneerd";
        // 
        // labelReturned
        // 
        labelReturned.Location = new Point(153, 108);
        labelReturned.Name = "labelReturned";
        labelReturned.Size = new Size(83, 15);
        labelReturned.TabIndex = 12;
        labelReturned.Text = "Geretourneerd";
        labelReturned.TextAlign = ContentAlignment.MiddleRight;
        // 
        // labelCommission
        // 
        labelCommission.Location = new Point(170, 135);
        labelCommission.Name = "labelCommission";
        labelCommission.Size = new Size(66, 15);
        labelCommission.TabIndex = 11;
        labelCommission.Text = "Commissie";
        labelCommission.TextAlign = ContentAlignment.MiddleRight;
        // 
        // labelNettoPnlValue
        // 
        labelNettoPnlValue.Location = new Point(174, 160);
        labelNettoPnlValue.Name = "labelNettoPnlValue";
        labelNettoPnlValue.Size = new Size(62, 15);
        labelNettoPnlValue.TabIndex = 10;
        labelNettoPnlValue.Text = "Netto PNL";
        labelNettoPnlValue.TextAlign = ContentAlignment.MiddleRight;
        // 
        // labelInvested
        // 
        labelInvested.Location = new Point(161, 83);
        labelInvested.Name = "labelInvested";
        labelInvested.Size = new Size(75, 15);
        labelInvested.TabIndex = 8;
        labelInvested.Text = "Geinvesteerd";
        labelInvested.TextAlign = ContentAlignment.MiddleRight;
        // 
        // labelPositions
        // 
        labelPositions.Location = new Point(161, 58);
        labelPositions.Name = "labelPositions";
        labelPositions.Size = new Size(75, 15);
        labelPositions.TabIndex = 14;
        labelPositions.Text = "Geinvesteerd";
        labelPositions.TextAlign = ContentAlignment.MiddleRight;
        // 
        // label8
        // 
        label8.AutoSize = true;
        label8.Location = new Point(46, 58);
        label8.Name = "label8";
        label8.Size = new Size(120, 15);
        label8.TabIndex = 13;
        label8.Text = "Openstaande posities";
        // 
        // DashBoardControl
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(labelPositions);
        Controls.Add(label8);
        Controls.Add(labelReturned);
        Controls.Add(labelCommission);
        Controls.Add(labelNettoPnlValue);
        Controls.Add(labelInvested);
        Controls.Add(label7);
        Controls.Add(label6);
        Controls.Add(label5);
        Controls.Add(label2);
        Controls.Add(label1);
        Controls.Add(button1);
        Name = "DashBoardControl";
        Size = new Size(615, 403);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Button button1;
    private Label label1;
    private Label label2;
    private Label label5;
    private Label label6;
    private Label label7;
    private Label labelReturned;
    private Label labelCommission;
    private Label labelNettoPnlValue;
    private Label labelInvested;
    private Label labelPositions;
    private Label label8;
}
