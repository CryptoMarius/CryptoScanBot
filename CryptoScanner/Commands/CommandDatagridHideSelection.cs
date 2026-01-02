namespace CryptoScanner.Commands;

public class CommandDatagridHideSelection : CommandBase
{
    public void Execute(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"Copying cells to clipboard");

        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.datagrid != null)
        {
            dto.datagrid.SelectedItems.Clear();
        }
    }

}
