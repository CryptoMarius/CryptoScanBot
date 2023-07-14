using CryptoSbmScanner.Intern;

namespace CryptoSbmScanner;

static class ApplicationTools
{

    public static void SaveWindowLocation(this Form form, bool saveInstant = true)
    {
        GlobalData.Settings.General.WindowPosition = form.DesktopBounds;

        // only save the WindowState if Normal or Maximized
        GlobalData.Settings.General.WindowState = form.WindowState switch
        {
            FormWindowState.Normal or FormWindowState.Maximized => form.WindowState,
            _ => FormWindowState.Normal,
        };

        if (saveInstant)
            GlobalData.SaveSettings();
    }


    public static void InitTimerInterval(this System.Timers.Timer timer, double seconds)
    {
        int msec = (int)(seconds * 1000);

        timer.Enabled = false;
        // Pas op, een interval van 0 mag niet
        if (seconds > 0)
            timer.Interval = msec;
        timer.Enabled = msec > 0;
    }


    public static void InitTimerInterval(this System.Windows.Forms.Timer timer, double seconds)
    {
        int msec = (int)(seconds * 1000);

        timer.Enabled = false;
        // Pas op, een interval van 0 mag niet
        if (seconds > 0)
            timer.Interval = msec;
        timer.Enabled = msec > 0;
    }
}