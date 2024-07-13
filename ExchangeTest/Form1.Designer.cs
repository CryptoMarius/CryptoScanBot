namespace CryptoScanBot.Experiment;

partial class Form1
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
        textBox1 = new TextBox();
        panel1 = new Panel();
        ButtonAltradyAddTp = new Button();
        buttonAltradyIncreasePosition = new Button();
        ButtonAltradyOpen = new Button();
        button1 = new Button();
        ButtonAltradyCancel = new Button();
        panel1.SuspendLayout();
        SuspendLayout();
        // 
        // textBox1
        // 
        textBox1.Dock = DockStyle.Fill;
        textBox1.Location = new Point(0, 32);
        textBox1.Multiline = true;
        textBox1.Name = "textBox1";
        textBox1.Size = new Size(800, 418);
        textBox1.TabIndex = 0;
        // 
        // panel1
        // 
        panel1.Controls.Add(ButtonAltradyCancel);
        panel1.Controls.Add(ButtonAltradyAddTp);
        panel1.Controls.Add(buttonAltradyIncreasePosition);
        panel1.Controls.Add(ButtonAltradyOpen);
        panel1.Controls.Add(button1);
        panel1.Dock = DockStyle.Top;
        panel1.Location = new Point(0, 0);
        panel1.Name = "panel1";
        panel1.Size = new Size(800, 32);
        panel1.TabIndex = 1;
        // 
        // ButtonAltradyAddTp
        // 
        ButtonAltradyAddTp.Location = new Point(424, 3);
        ButtonAltradyAddTp.Name = "ButtonAltradyAddTp";
        ButtonAltradyAddTp.Size = new Size(152, 23);
        ButtonAltradyAddTp.TabIndex = 3;
        ButtonAltradyAddTp.Text = "Altrady position tp";
        ButtonAltradyAddTp.UseVisualStyleBackColor = true;
        ButtonAltradyAddTp.Click += ButtonAltradyAddTpClick;
        // 
        // buttonAltradyIncreasePosition
        // 
        buttonAltradyIncreasePosition.Location = new Point(266, 4);
        buttonAltradyIncreasePosition.Name = "buttonAltradyIncreasePosition";
        buttonAltradyIncreasePosition.Size = new Size(152, 23);
        buttonAltradyIncreasePosition.TabIndex = 2;
        buttonAltradyIncreasePosition.Text = "Altrady Increase position";
        buttonAltradyIncreasePosition.UseVisualStyleBackColor = true;
        buttonAltradyIncreasePosition.Click += ButtonAltradyIncreasePositionClick;
        // 
        // ButtonAltradyOpen
        // 
        ButtonAltradyOpen.Location = new Point(108, 4);
        ButtonAltradyOpen.Name = "ButtonAltradyOpen";
        ButtonAltradyOpen.Size = new Size(152, 23);
        ButtonAltradyOpen.TabIndex = 1;
        ButtonAltradyOpen.Text = "Altrady Open position";
        ButtonAltradyOpen.UseVisualStyleBackColor = true;
        ButtonAltradyOpen.Click += ButtonAltradyOpenClick;
        // 
        // button1
        // 
        button1.Location = new Point(12, 3);
        button1.Name = "button1";
        button1.Size = new Size(75, 23);
        button1.TabIndex = 0;
        button1.Text = "button1";
        button1.UseVisualStyleBackColor = true;
        button1.Click += Button1_Click;
        // 
        // ButtonAltradyCancel
        // 
        ButtonAltradyCancel.Location = new Point(582, 4);
        ButtonAltradyCancel.Name = "ButtonAltradyCancel";
        ButtonAltradyCancel.Size = new Size(152, 23);
        ButtonAltradyCancel.TabIndex = 4;
        ButtonAltradyCancel.Text = "Altrady position cancel";
        ButtonAltradyCancel.UseVisualStyleBackColor = true;
        ButtonAltradyCancel.Click += ButtonAltradyCancelClick;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Controls.Add(textBox1);
        Controls.Add(panel1);
        Name = "Form1";
        Text = "Form1";
        panel1.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TextBox textBox1;
    private Panel panel1;
    private Button button1;
    private Button ButtonAltradyOpen;
    private Button buttonAltradyIncreasePosition;
    private Button ButtonAltradyAddTp;
    private Button ButtonAltradyCancel;
}
