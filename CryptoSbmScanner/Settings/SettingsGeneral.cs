using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace CryptoSbmScanner
{
    public enum TradingApp
    {
        Altrady,
        AltradyWeb,
        Hypertrader
    }

    public enum DoubleClickAction
    {
        activateTradingApp,
        activateTradingAppAndTradingViewInternal,
        activateTradingViewBrowerInternal,
        activateTradingViewBrowerExternal
    }

    public enum TrendCalculationMethod
    {
        trendCalculationViaAlgo1,
        trendCalculationViaAlgo2,
        trendCalculationcViaEma
    }

    // TODO indelen in categorien
    [Serializable]
    public class SettingsGeneral
    {
        public bool BlackTheming { get; set; } = false;
        public TradingApp TradingApp { get; set; } = TradingApp.Altrady;
        public string SelectedBarometerQuote { get; set; } = "BUSD";
        public string SelectedBarometerInterval { get; set; } = "1H";

        public string FontName { get; set; } = "Microsoft Sans Serif";
        public float FontSize { get; set; } = 8.25f;

        public DoubleClickAction DoubleClickAction { get; set; } = DoubleClickAction.activateTradingApp;

        public int GetCandleInterval { get; set; } = 60;

        public Rectangle WindowPosition { get; set; } = new Rectangle();
        public FormWindowState WindowState { get; set; } = FormWindowState.Normal;

        public TrendCalculationMethod TrendCalculationMethod { get; set; } = TrendCalculationMethod.trendCalculationViaAlgo1;

        public SettingsGeneral()
        {
        }
    }
}

