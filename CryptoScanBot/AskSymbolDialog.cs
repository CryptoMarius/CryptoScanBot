using CryptoScanBot.Core.Intern;

namespace CryptoScanBot
{
    public partial class AskSymbolDialog : Form
    {
        public AskSymbolDialog()
        {
            InitializeComponent();


            EditSymbol.Text = GlobalData.Settings.BackTest.BackTestSymbol;

            EditTimeStart.Format = DateTimePickerFormat.Custom;
            EditTimeStart.CustomFormat = "yyyy.MM.dd HH:mm";
            EditTimeStart.Value = GlobalData.Settings.BackTest.BackTestStartTime;

            EditTimeEnd.Format = DateTimePickerFormat.Custom;
            EditTimeEnd.CustomFormat = "yyyy.MM.dd HH:mm";
            EditTimeEnd.Value = GlobalData.Settings.BackTest.BackTestEndTime;


            //// De intervallen in de combox zetten (default=1h)
            //EditInterval.Items.Clear();
            //foreach (CryptoInterval interval in GlobalData.IntervalList)
            //    EditInterval.Items.Add(interval.Name);
            //EditInterval.MaxDropDownItems = EditInterval.Items.Count;
            //EditInterval.SelectedIndex = EditInterval.Items.IndexOf(GlobalData.Settings.BackTest.BackTestInterval);
            //if (EditInterval.SelectedIndex < 0)
            //    EditInterval.SelectedIndex = 0;

            //EditAlgoritm.Items.Clear();
            //foreach (AlgorithmDefinition definition in SignalHelper.AlgorithmDefinitionList)
            //    EditAlgoritm.Items.Add(definition.Name);
            ////foreach (SignalLongStrategy strategy in Enum.GetValues(typeof(SignalLongStrategy)))
            //    //EditAlgoritm.Items.Add(SignalHelper.GetSignalAlgorithmText(strategy));
            //EditAlgoritm.MaxDropDownItems = EditAlgoritm.Items.Count;
            //EditAlgoritm.SelectedIndex = (int)GlobalData.Settings.BackTest.BackTestAlgoritm;
            //if (EditAlgoritm.SelectedIndex < 0)
            //    EditAlgoritm.SelectedIndex = 0;

        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            GlobalData.Settings.BackTest.BackTestSymbol = EditSymbol.Text.Trim();
            GlobalData.Settings.BackTest.BackTestStartTime = EditTimeStart.Value;
            GlobalData.Settings.BackTest.BackTestEndTime = EditTimeEnd.Value;

            //GlobalData.Settings.BackTest.BackTestInterval = EditInterval.Text.Trim();
            //GlobalData.Settings.BackTest.BackTestAlgoritm = (CryptoSignalStrategy)EditAlgoritm.SelectedIndex;

            DialogResult = DialogResult.OK;
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
