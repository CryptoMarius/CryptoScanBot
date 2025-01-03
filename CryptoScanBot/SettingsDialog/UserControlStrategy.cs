﻿using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Signal;

namespace CryptoScanBot.SettingsDialog;
public partial class UserControlStrategy : UserControl
{

    private readonly Dictionary<CheckBox, string> ControlList = new();

    public UserControlStrategy()
    {
        InitializeComponent();
    }

    public void InitControls(bool isForTrading, CryptoTradeSide tradeSide)
    {
        foreach (var signalDefinition in RegisterAlgorithms.AlgorithmDefinitionList.Values)
        {
            if (signalDefinition.Strategy >= CryptoSignalStrategy.DominantLevel)
                continue;
            if (isForTrading && signalDefinition.Strategy == CryptoSignalStrategy.Jump)
                continue;

            bool validStrategy = ((tradeSide == CryptoTradeSide.Long && signalDefinition.AnalyzeLongType != null) ||
                (tradeSide == CryptoTradeSide.Short && signalDefinition.AnalyzeShortType != null));

            CheckBox checkbox = new()
            {
                AutoSize = true,
                UseVisualStyleBackColor = true,
                Text = RegisterAlgorithms.GetAlgorithm(signalDefinition.Strategy),
            };
            flowLayoutPanel1.Controls.Add(checkbox);

            if (validStrategy)
                ControlList.Add(checkbox, checkbox.Text);
            else
                checkbox.Enabled = false;
        }
    }

    public void LoadConfig(List<string> list)
    {
        foreach (var item in ControlList)
            item.Key.Checked = list.Contains(item.Value);
    }

    public void SaveConfig(List<string> list)
    {
        list.Clear();
        foreach (var item in ControlList)
        {
            if (item.Key.Checked)
                list.Add(item.Value);
        }
    }
}
