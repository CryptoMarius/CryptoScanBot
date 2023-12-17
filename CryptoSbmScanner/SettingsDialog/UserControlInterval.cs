using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.SettingsDialog;
public partial class UserControlInterval : UserControl
{

    // Gewenste trend op interval
    private readonly Dictionary<CheckBox, string> ControlList = new();

    public UserControlInterval()
    {
        InitializeComponent();
    }

    public void InitControls(string caption)
    {
        EditGroupBox.Text = caption;

        ControlList.Add(EditInterval1m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m].Name);
        ControlList.Add(EditInterval2m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2m].Name);
        ControlList.Add(EditInterval3m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval3m].Name);
        ControlList.Add(EditInterval5m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m].Name);
        ControlList.Add(EditInterval10m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m].Name);
        ControlList.Add(EditInterval15m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m].Name);
        ControlList.Add(EditInterval30m, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval30m].Name);
        ControlList.Add(EditInterval1h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h].Name);
        ControlList.Add(EditInterval2h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2h].Name);
        ControlList.Add(EditInterval4h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval4h].Name);
        ControlList.Add(EditInterval6h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval6h].Name);
        ControlList.Add(EditInterval8h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval8h].Name);
        ControlList.Add(EditInterval12h, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval12h].Name);
        ControlList.Add(EditInterval1d, GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d].Name);
    }

    public void LoadConfig(List<string> list)
    {
        foreach (var item in ControlList)
        {
            item.Key.Checked = list.Contains(item.Value);
        }
    }

    public List<string> SaveConfig()
    {
        List<string> list = new();
        foreach (var item in ControlList)
        {
            if (item.Key.Checked)
                list.Add(item.Value);
        }

        return list;
    }
}
