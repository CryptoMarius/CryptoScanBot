using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Signal;

namespace CryptoSbmScanner.SettingsDialog;
public partial class UserControlStrategy : UserControl
{

    private readonly Dictionary<CheckBox, string> ControlList = new();

    public UserControlStrategy()
    {
        InitializeComponent();
    }

    public void InitControls(string caption, CryptoTradeSide tradeSide)
    {
        EditGroupBox.Text = caption;

        foreach (var signalDefinition in SignalHelper.AlgorithmDefinitionIndex.Values)
        {
            bool valid = ((tradeSide == CryptoTradeSide.Long && signalDefinition.AnalyzeLongType != null) ||
                (tradeSide == CryptoTradeSide.Short && signalDefinition.AnalyzeShortType != null));

            CheckBox checkbox = new()
            {
                UseVisualStyleBackColor = true,
                Text = SignalHelper.GetSignalAlgorithmText(signalDefinition.Strategy),
            };
            flowLayoutPanel1.Controls.Add(checkbox);

            if (valid)
                ControlList.Add(checkbox, checkbox.Text);
            else
                checkbox.Enabled = false;
        }
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
