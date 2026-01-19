namespace CryptoScanner.Commands;

public class CommandDatagridHideSelection : CommandBase
{
    public override void Execute(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"Copying cells to clipboard");

        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.datagrid != null)
        {
            dto.datagrid.SelectedItems.Clear();
        }
    }

}
