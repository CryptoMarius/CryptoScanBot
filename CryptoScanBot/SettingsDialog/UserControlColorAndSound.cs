using CryptoScanBot.Intern;

using Microsoft.IdentityModel.Tokens;

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

    private void ButtonSelectColor(object sender, EventArgs e)
    {
        ColorDialog dlg = new()
        {
            Color = PanelColor.BackColor
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            PanelColor.BackColor = dlg.Color;
        }
    }

    private void ButtonSelectSound(object sender, EventArgs e)
    {
        OpenFileDialog openFileDialog = new()
        {
            Filter = "wav bestanden|*.wav"
        };
        if (!EditSoundFile.Text.IsNullOrEmpty())
            openFileDialog.FileName = Path.GetFileName(EditSoundFile.Text);
        if (!EditSoundFile.Text.IsNullOrEmpty())
            openFileDialog.InitialDirectory = Path.GetDirectoryName(EditSoundFile.Text);

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            string fileName = openFileDialog.FileName;
            if (File.Exists(fileName))
            {
                EditSoundFile.Text = fileName;
            }
            else
            {
                MessageBox.Show("Selected file doesn't exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    //private void SetGrayed(object sender, EventArgs e)
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

    private void ButtonPlaySound(object sender, EventArgs e)
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
