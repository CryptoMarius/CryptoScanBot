namespace CryptoScanBot.Commands;

public class CommandAbout : CommandBase
{
    public override string CommandName() 
        => "About";


    public override void Execute(object sender)
    {
        AboutBox form = new()
        {
            StartPosition = FormStartPosition.CenterParent
        };
        form.ShowDialog();
    }
}
