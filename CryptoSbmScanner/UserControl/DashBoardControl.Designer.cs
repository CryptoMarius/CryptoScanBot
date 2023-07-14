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
        label1 = new Label();
        label2 = new Label();
        SuspendLayout();
        // 
        // button1
        // 
        button1.Location = new Point(95, 105);
        button1.Name = "button1";
        button1.Size = new Size(75, 23);
        button1.TabIndex = 0;
        button1.Text = "button1";
        button1.UseVisualStyleBackColor = true;
        // 
        // checkBox1
        // 
        checkBox1.AutoSize = true;
        checkBox1.Location = new Point(124, 152);
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
        listBox1.Location = new Point(144, 187);
        listBox1.Name = "listBox1";
        listBox1.Size = new Size(116, 169);
        listBox1.TabIndex = 2;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        label1.Location = new Point(21, 25);
        label1.Name = "label1";
        label1.Size = new Size(267, 15);
        label1.TabIndex = 3;
        label1.Text = "Hier komt het toekomstige dashboard te staan";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(55, 70);
        label2.Name = "label2";
        label2.Size = new Size(129, 15);
        label2.TabIndex = 4;
        label2.Text = "Wat test componentjes";
        // 
        // DashBoardControl
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(label2);
        Controls.Add(label1);
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
    private Label label1;
    private Label label2;
}
