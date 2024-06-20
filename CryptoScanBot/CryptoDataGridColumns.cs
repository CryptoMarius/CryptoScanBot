using CryptoScanBot.Core.Settings;

namespace CryptoScanBot;

public partial class CryptoDataGridColumns : Form
{
    private CryptoDataGrid Grid;
    private readonly List<CheckBox> List = [];

    public CryptoDataGridColumns()
    {
        InitializeComponent();
    }


    public void AddColumns(CryptoDataGrid grid)
    {
        Grid = grid;

        foreach (DataGridViewColumn column in Grid.Grid.Columns)
        {
            if (!Grid.ColumnList.TryGetValue(column.HeaderText, out ColumnSetting columnSetting))
            {
                columnSetting = new();
                Grid.ColumnList.Add(column.HeaderText, columnSetting);
            }

            CheckBox item = new()
            {
                Tag = column,
                Text = column.HeaderText,
                Size = new Size(100, 22),
                CheckState = CheckState.Unchecked,
                Checked = columnSetting.Visible,
            };
            flowLayoutPanel.Controls.Add(item);
            List.Add(item);
        }
    }

    public void FormOkay()
    {
        int count = 0;
        foreach (var item in List)
        {
            if (!Grid.ColumnList.TryGetValue(item.Text, out ColumnSetting columnSetting))
            {
                columnSetting = new();
                Grid.ColumnList.Add(item.Text, columnSetting);
            }

            columnSetting.Visible = item.Checked;
            if (columnSetting.Visible)
                count++;
        }

        // Restore a column (otherwise the complete header is gone)
        if (count == 0)
        {
            Grid.ColumnList.Values[0].Visible = true;
        }

    }

    private void buttonOk_Click(object? sender, EventArgs? e)
    {
        FormOkay();
        DialogResult = DialogResult.OK;
    }

    private void buttonCancel_Click(object? sender, EventArgs? e)
    {
        DialogResult = DialogResult.Cancel;
    }
}
