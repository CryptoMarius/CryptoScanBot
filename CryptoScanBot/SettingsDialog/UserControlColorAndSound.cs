﻿using CryptoScanBot.Core.Core;

namespace CryptoScanBot.SettingsDialog;
public partial class UserControlColorAndSound : UserControl
{
    public UserControlColorAndSound()
    {
        InitializeComponent();

        buttonColor.Click += ButtonSelectColor;
        buttonSelectSound.Click += ButtonSelectSound;
        buttonPlaySound.Click += ButtonPlaySound;
    }

    private void ButtonSelectColor(object? sender, EventArgs? e)
    {
        ColorDialog dlg = new()
        {
            Color = PanelColor.BackColor,
            CustomColors = GlobalData.SettingsUser.CustomColors
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            PanelColor.BackColor = dlg.Color;
            GlobalData.SettingsUser.CustomColors = dlg.CustomColors;
        }
    }

    private void ButtonSelectSound(object? sender, EventArgs? e)
    {
        OpenFileDialog openFileDialog = new()
        {
            Filter = "wav bestanden|*.wav"
        };

        string path = Path.GetDirectoryName(EditSoundFile.Text);
        if (path == null || path == "")
        {
            //This will give us the full name path of the executable file:
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = Path.GetDirectoryName(strExeFilePath) + @"\Sounds\";
        }

        openFileDialog.InitialDirectory = path;
        openFileDialog.FileName = Path.GetFileName(EditSoundFile.Text);

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            string fileName = openFileDialog.FileName;
            if (File.Exists(fileName))
                EditSoundFile.Text = fileName;
            else
                MessageBox.Show("Selected file doesn't exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    //private void SetGrayed(object? sender, EventArgs? e)
    //{
    //    //// Stobb
    //    //EditSoundStobbOverbought.Enabled = EditPlaySoundStobbSignal.Checked;
    //    //buttonPlaySoundStobbOverbought.Enabled = EditPlaySoundStobbSignal.Checked;
    //    //buttonSelectSoundStobbOverbought.Enabled = EditPlaySoundStobbSignal.Checked;
    //    //buttonPlaySoundStobbOverbought.Enabled = EditPlaySoundStobbSignal.Checked;

    //    //EditSoundStobbOversold.Enabled = EditPlaySoundStobbSignal.Checked;
    //    //buttonPlaySoundStobbOversold.Enabled = EditPlaySoundStobbSignal.Checked;
    //    //buttonSelectSoundStobbOversold.Enabled = EditPlaySoundStobbSignal.Checked;
    //    //buttonPlaySoundStobbOversold.Enabled = EditPlaySoundStobbSignal.Checked;

    //}

    private void ButtonPlaySound(object? sender, EventArgs? e)
    {
        GlobalData.PlaySomeMusic(EditSoundFile.Text, true);
    }

    public void LoadConfig(string caption, Color color, string soundFile)
    {
        labelCaption.Text = caption;
        PanelColor.BackColor = color;
        EditSoundFile.Text = soundFile;
    }

    public Color GetColor()
    {
        return PanelColor.BackColor;
    }

    public string GetSoundFile()
    {
        return EditSoundFile.Text;
    }
}
