namespace CryptoScanBot.SettingsDialog;

partial class UserControlZigZagSettings
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
        groupBox16 = new GroupBox();
        EditUseHighLow = new CheckBox();
        EditUsePrimary = new CheckBox();
        groupBox16.SuspendLayout();
        SuspendLayout();
        // 
        // groupBox16
        // 
        groupBox16.AutoSize = true;
        groupBox16.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        groupBox16.Controls.Add(EditUseHighLow);
        groupBox16.Controls.Add(EditUsePrimary);
        groupBox16.Dock = DockStyle.Fill;
        groupBox16.Location = new Point(0, 0);
        groupBox16.Name = "groupBox16";
        groupBox16.Size = new Size(145, 96);
        groupBox16.TabIndex = 252;
        groupBox16.TabStop = false;
        groupBox16.Text = "Primary/secondary trend";
        // 
        // EditUseHighLow
        // 
        EditUseHighLow.AutoSize = true;
        EditUseHighLow.Location = new Point(19, 55);
        EditUseHighLow.Margin = new Padding(4, 3, 4, 3);
        EditUseHighLow.Name = "EditUseHighLow";
        EditUseHighLow.Size = new Size(101, 19);
        EditUseHighLow.TabIndex = 292;
        EditUseHighLow.Text = "Use High/Low";
        EditUseHighLow.UseVisualStyleBackColor = true;
        // 
        // EditUsePrimary
        // 
        EditUsePrimary.AutoSize = true;
        EditUsePrimary.Location = new Point(18, 27);
        EditUsePrimary.Margin = new Padding(4, 3, 4, 3);
        EditUsePrimary.Name = "EditUsePrimary";
        EditUsePrimary.Size = new Size(120, 19);
        EditUsePrimary.TabIndex = 291;
        EditUsePrimary.Text = "Use primary trend";
        EditUsePrimary.UseVisualStyleBackColor = true;
        // 
        // UserControlZigZagSettings
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(groupBox16);
        Name = "UserControlZigZagSettings";
        Size = new Size(145, 96);
        groupBox16.ResumeLayout(false);
        groupBox16.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox groupBox16;
    private CheckBox EditUseHighLow;
    private CheckBox EditUsePrimary;
}
