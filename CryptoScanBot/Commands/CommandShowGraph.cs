﻿using CryptoScanBot.Core.Model;

using CryptoShowTrend;

namespace CryptoScanBot.Commands;

public class CommandShowGraph : CommandBase
{
    public override void Execute(object sender)
    {
        if (sender is CryptoSymbol symbol)
        {
            CryptoVisualisation dialog = new()
            {
                StartPosition = FormStartPosition.CenterParent
            };
            //ChangeTheme(Main.theme, dialog);
            dialog.BlaAsync(symbol);

            //if (dialog.ShowDialog() != DialogResult.OK)
            //    return;

            dialog.Show();
        }
    }

}
