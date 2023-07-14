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
        checkBox1 = new CheckBox();
        listBox1 = new ListBox();
        SuspendLayout();
        // 
        // button1
        // 
        button1.Location = new Point(31, 233);
        button1.Name = "button1";
        button1.Size = new Size(75, 23);
        button1.TabIndex = 0;
        button1.Text = "button1";
        button1.UseVisualStyleBackColor = true;
        // 
        // checkBox1
        // 
        checkBox1.AutoSize = true;
        checkBox1.Location = new Point(313, 107);
        checkBox1.Name = "checkBox1";
        checkBox1.Size = new Size(83, 19);
        checkBox1.TabIndex = 1;
        checkBox1.Text = "checkBox1";
        checkBox1.UseVisualStyleBackColor = true;
        // 
        // listBox1
        // 
        listBox1.FormattingEnabled = true;
        listBox1.ItemHeight = 15;
        listBox1.Location = new Point(31, 35);
        listBox1.Name = "listBox1";
        listBox1.Size = new Size(116, 169);
        listBox1.TabIndex = 2;
        // 
        // DashBoardControl
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(listBox1);
        Controls.Add(checkBox1);
        Controls.Add(button1);
        Name = "DashBoardControl";
        Size = new Size(615, 403);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Button button1;
    private CheckBox checkBox1;
    private ListBox listBox1;
}
