using CryptoSbmScanner.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoSbmScanner.Settings
{

    internal class SettingsBaseCoin
    {
        public CryptoQuoteData QuoteData;
        public CheckBox FetchCandles;
        public NumericUpDown MinVolume;
        public NumericUpDown MinPrice;
        public CheckBox CreateSignals;
        public Panel PanelColor;
        public Button ButtonColor;

        public SettingsBaseCoin(CryptoQuoteData quoteData, int yPos, Control.ControlCollection controls)
        {
            QuoteData = quoteData;

            FetchCandles = new()
            {
                AutoSize = true,
                Location = new Point(18, yPos + 3),
                Margin = new Padding(4, 3, 4, 3),
                //Name = "EditFetchCandlesBTC",
                Size = new Size(45, 23),
                Text = quoteData.Name,
                UseVisualStyleBackColor = true
            };
            MinVolume = new()
            {
                Location = new Point(118, yPos),
                Margin = new Padding(4, 3, 4, 3),
                Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
                //Name = "EditMinVolumeBTC";
                Size = new Size(140, 23),
                ThousandsSeparator = true
            };
            MinPrice = new()
            {
                DecimalPlaces = 8,
                Increment = new decimal(new int[] { 1, 0, 0, 393216 }),
                Location = new Point(265, yPos),
                Margin = new Padding(4, 3, 4, 3),
                Maximum = new decimal(new int[] { 276447231, 23283, 0, 0 }),
                //Name = "EditMinPriceBTC";
                Size = new Size(140, 23),
                ThousandsSeparator = true
            };
            CreateSignals = new()
            {
                AutoSize = true,
                Location = new Point(425, yPos + 3),
                Margin = new Padding(4, 3, 4, 3),
                //Name = "EditCreateSignalsBTC",
                Size = new Size(15, 23),
                UseVisualStyleBackColor = true
            };
            PanelColor = new()
            {
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(465, yPos),
                Margin = new Padding(4, 3, 4, 3),
                //Name = "panelColorBTC",
                Size = new Size(70, 23)
            };
            ButtonColor = new()
            {
                Location = new Point(542, yPos),
                Margin = new Padding(4, 3, 4, 3),
                //Name = "buttonColorBTC",
                Size = new Size(88, 23),
                Text = "Achtergrond",
                UseVisualStyleBackColor = true,
                //Tag = PanelColor,
            };

            ButtonColor.Click += PickColor_Click;

            controls.Add(FetchCandles);
            controls.Add(MinVolume);
            controls.Add(MinPrice);
            controls.Add(CreateSignals);
            controls.Add(PanelColor);
            controls.Add(ButtonColor);
        }

        private void PickColor_Click(object sender, EventArgs e)
        {
            ColorDialog dlg = new()
            {
                Color = PanelColor.BackColor
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                PanelColor.BackColor = dlg.Color;
            }
        }

        public void SetControlValues()
        {
            FetchCandles.Checked = QuoteData.FetchCandles;
            MinVolume.Value = QuoteData.MinimalVolume;
            MinPrice.Value = QuoteData.MinimalPrice;
            CreateSignals.Checked = QuoteData.CreateSignals;
            PanelColor.BackColor = QuoteData.DisplayColor;
        }

        public void GetControlValues()
        {
            QuoteData.FetchCandles = FetchCandles.Checked;
            QuoteData.MinimalVolume = MinVolume.Value;
            QuoteData.MinimalPrice = MinPrice.Value;
            QuoteData.CreateSignals = CreateSignals.Checked;
            QuoteData.DisplayColor = PanelColor.BackColor;
        }
    }

}
