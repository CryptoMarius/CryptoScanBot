namespace CryptoScanBot.SettingsDialog;

partial class UserControlColorAndSound
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
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserControlColorAndSound));
        labelCaption = new Label();
        label28 = new Label();
        buttonColor = new Button();
        buttonPlaySound = new Button();
        imageList1 = new ImageList(components);
        PanelColor = new Panel();
        EditSoundFile = new TextBox();
        buttonSelectSound = new Button();
        SuspendLayout();
        // 
        // labelCaption
        // 
        labelCaption.AutoSize = true;
        labelCaption.Location = new Point(17, 5);
        labelCaption.Margin = new Padding(4, 0, 4, 0);
        labelCaption.Name = "labelCaption";
        labelCaption.Size = new Size(70, 15);
        labelCaption.TabIndex = 165;
        labelCaption.Text = "CaptionText";
        // 
        // label28
        // 
        label28.AutoSize = true;
        label28.Location = new Point(408, 6);
        label28.Margin = new Padding(4, 0, 4, 0);
        label28.Name = "label28";
        label28.Size = new Size(57, 15);
        label28.TabIndex = 164;
        label28.Text = "Soundfile";
        // 
        // buttonColor
        // 
        buttonColor.Location = new Point(235, 0);
        buttonColor.Margin = new Padding(4, 3, 4, 3);
        buttonColor.Name = "buttonColor";
        buttonColor.Size = new Size(88, 27);
        buttonColor.TabIndex = 162;
        buttonColor.Text = "Background";
        buttonColor.UseVisualStyleBackColor = true;
        // 
        // buttonPlaySound
        // 
        buttonPlaySound.ImageIndex = 0;
        buttonPlaySound.ImageList = imageList1;
        buttonPlaySound.Location = new Point(748, 4);
        buttonPlaySound.Margin = new Padding(4, 3, 4, 3);
        buttonPlaySound.Name = "buttonPlaySound";
        buttonPlaySound.Size = new Size(23, 23);
        buttonPlaySound.TabIndex = 161;
        buttonPlaySound.UseVisualStyleBackColor = true;
        // 
        // imageList1
        // 
        imageList1.ColorDepth = ColorDepth.Depth32Bit;
        imageList1.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
        imageList1.TransparentColor = Color.Transparent;
        imageList1.Images.SetKeyName(0, "volume.png");
        imageList1.Images.SetKeyName(1, "open-folderblack.png");
        // 
        // PanelColor
        // 
        PanelColor.BackColor = Color.Transparent;
        PanelColor.BorderStyle = BorderStyle.FixedSingle;
        PanelColor.Location = new Point(157, 4);
        PanelColor.Margin = new Padding(4, 3, 4, 3);
        PanelColor.Name = "PanelColor";
        PanelColor.Size = new Size(70, 22);
        PanelColor.TabIndex = 163;
        // 
        // EditSoundFile
        // 
        EditSoundFile.Location = new Point(483, 3);
        EditSoundFile.Margin = new Padding(4, 3, 4, 3);
        EditSoundFile.Name = "EditSoundFile";
        EditSoundFile.Size = new Size(227, 23);
        EditSoundFile.TabIndex = 159;
        // 
        // buttonSelectSound
        // 
        buttonSelectSound.ImageIndex = 1;
        buttonSelectSound.ImageList = imageList1;
        buttonSelectSound.Location = new Point(718, 4);
        buttonSelectSound.Margin = new Padding(4, 3, 4, 3);
        buttonSelectSound.Name = "buttonSelectSound";
        buttonSelectSound.Size = new Size(23, 23);
        buttonSelectSound.TabIndex = 160;
        buttonSelectSound.Text = "...";
        buttonSelectSound.UseVisualStyleBackColor = true;
        // 
        // UserControlColorAndSound
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        Controls.Add(labelCaption);
        Controls.Add(label28);
        Controls.Add(buttonColor);
        Controls.Add(buttonPlaySound);
        Controls.Add(PanelColor);
        Controls.Add(EditSoundFile);
        Controls.Add(buttonSelectSound);
        Name = "UserControlColorAndSound";
        Size = new Size(778, 30);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label labelCaption;
    private Label label28;
    private Button buttonColor;
    private Button buttonPlaySound;
    private Panel PanelColor;
    private TextBox EditSoundFile;
    private Button buttonSelectSound;
    private ImageList imageList1;
}
