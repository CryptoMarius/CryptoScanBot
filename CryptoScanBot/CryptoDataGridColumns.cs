using CryptoScanBot.Core.Settings;

namespace CryptoScanBot;

public partial class CryptoDataGridColumns : Form
{
    public required CryptoDataGrid Grid;
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
            if (!Grid.ColumnList.TryGetValue(column.HeaderText, out ColumnSetting? columnSetting))
            {
                columnSetting = new();
                if (column.Tag is bool alwaysVisible)
                    columnSetting.AlwaysVisible = alwaysVisible;
                Grid.ColumnList.Add(column.HeaderText, columnSetting);
            }

            CheckBox item = new()
            {
                Tag = column,
                AutoSize = true,
                Text = column.HeaderText,
                Size = new Size(100, 22),
                CheckState = CheckState.Unchecked,
                Checked = columnSetting.Visible || columnSetting.AlwaysVisible,
            };
            flowLayoutPanel.Controls.Add(item);
            List.Add(item);
        }
    }

    public void FormOkay()
    {
        foreach (var item in List)
        {
            if (!Grid.ColumnList.TryGetValue(item.Text, out ColumnSetting? columnSetting))
            {
                columnSetting = new();
                Grid.ColumnList.Add(item.Text, columnSetting);
            }
            columnSetting.Visible = item.Checked || columnSetting.AlwaysVisible;
        }
    }

    private void ButtonOkClick(object? sender, EventArgs? e)
    {
        FormOkay();
        DialogResult = DialogResult.OK;
    }

    private void ButtonCancelClick(object? sender, EventArgs? e)
    {
        DialogResult = DialogResult.Cancel;
    }
}
