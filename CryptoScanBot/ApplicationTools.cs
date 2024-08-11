using CryptoScanBot.Core.Intern;

namespace CryptoScanBot;

static class ApplicationTools
{

    public static void SaveWindowLocation(Form form)
    {
        GlobalData.SettingsUser.WindowPosition = form.DesktopBounds;

        // only save the WindowState if Normal or Maximized
        GlobalData.SettingsUser.WindowState = form.WindowState switch
        {
            FormWindowState.Normal => 0,
            FormWindowState.Maximized => 2,
            _ => 0,
        };
    }

    public static void WindowLocationRestore(Form form)
    {
        // this is the default
        form.WindowState = FormWindowState.Normal;
        form.StartPosition = FormStartPosition.WindowsDefaultBounds;

        
        // check if the saved bounds are nonzero and visible on any screen
        if (GlobalData.SettingsUser.WindowPosition != Rectangle.Empty && IsVisibleOnAnyScreen(GlobalData.SettingsUser.WindowPosition))
        {
            // first set the bounds
            form.StartPosition = FormStartPosition.Manual;
            form.DesktopBounds = GlobalData.SettingsUser.WindowPosition;

            // afterwards set the window state to the saved value (which could be Maximized)
            form.WindowState = (FormWindowState)GlobalData.SettingsUser.WindowState;
        }
        else
        {
            // this resets the upper left corner of the window to windows standards
            form.StartPosition = FormStartPosition.WindowsDefaultLocation;

            // we can still apply the saved size
            // msorens: added gatekeeper, otherwise first time appears as just a title bar!
            if (GlobalData.SettingsUser.WindowPosition != Rectangle.Empty)
            {
                form.Size = GlobalData.SettingsUser.WindowPosition.Size;
            }
        }

        // Sometime the height or width becomes zero, not sure why..
        if (form.DesktopBounds.Size.Width == 0 || form.DesktopBounds.Size.Height == 0)
        {
            form.StartPosition = FormStartPosition.CenterScreen;
            form.DesktopBounds = new Rectangle(150, 150, 1000, 800);
        }
    }


    private static bool IsVisibleOnAnyScreen(Rectangle rect)
    {
        return Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(rect));
    }
    

}