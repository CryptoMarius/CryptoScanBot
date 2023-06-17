
using CryptoSbmScanner;

namespace CryptoSbmScanner
{
    partial class TestForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            panel1 = new Panel();
            button1 = new Button();
            button6 = new Button();
            ButtonVolatiteit = new Button();
            comboBox1 = new ComboBox();
            ButtonBackTest = new Button();
            PriceAction = new Button();
            button3 = new Button();
            ButtonBitmap = new Button();
            tabControl = new TabControl();
            tabPageLog = new TabPage();
            textBox1 = new TextBox();
            tabPageSignals = new TabPage();
            listView1 = new ListView();
            tabPageBitmap = new TabPage();
            pictureBox1 = new PictureBox();
            tabPage3 = new TabPage();
            contextMenuStripSignals = new ContextMenuStrip(components);
            toolStripMenuItemClear = new ToolStripMenuItem();
            toolStripMenuItemAltrady = new ToolStripMenuItem();
            toolStripMenuItemTradingView = new ToolStripMenuItem();
            toolStripMenuItemHypertrader = new ToolStripMenuItem();
            contextMenuStrip1 = new ContextMenuStrip(components);
            testToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripMenuItem();
            toolStripMenuItem3 = new ToolStripMenuItem();
            toolStripMenuItem4 = new ToolStripMenuItem();
            toolStripMenuItem5 = new ToolStripMenuItem();
            toolStripMenuItem6 = new ToolStripMenuItem();
            toolStripMenuItem7 = new ToolStripMenuItem();
            toolStripMenuItem8 = new ToolStripMenuItem();
            toolStripMenuItem9 = new ToolStripMenuItem();
            toolStripMenuItem10 = new ToolStripMenuItem();
            timerClearEvents = new System.Windows.Forms.Timer(components);
            panel1.SuspendLayout();
            tabControl.SuspendLayout();
            tabPageLog.SuspendLayout();
            tabPageSignals.SuspendLayout();
            tabPageBitmap.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            contextMenuStripSignals.SuspendLayout();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(button1);
            panel1.Controls.Add(button6);
            panel1.Controls.Add(ButtonVolatiteit);
            panel1.Controls.Add(comboBox1);
            panel1.Controls.Add(ButtonBackTest);
            panel1.Controls.Add(PriceAction);
            panel1.Controls.Add(button3);
            panel1.Controls.Add(ButtonBitmap);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(4, 3, 4, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(1423, 59);
            panel1.TabIndex = 10;
            // 
            // button1
            // 
            button1.Location = new Point(956, 19);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 14;
            button1.Text = "Telegram";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // button6
            // 
            button6.Location = new Point(736, 18);
            button6.Margin = new Padding(4, 3, 4, 3);
            button6.Name = "button6";
            button6.Size = new Size(88, 27);
            button6.TabIndex = 13;
            button6.Text = "Trend test";
            button6.UseVisualStyleBackColor = true;
            // 
            // ButtonVolatiteit
            // 
            ButtonVolatiteit.Location = new Point(846, 17);
            ButtonVolatiteit.Margin = new Padding(4, 3, 4, 3);
            ButtonVolatiteit.Name = "ButtonVolatiteit";
            ButtonVolatiteit.Size = new Size(88, 27);
            ButtonVolatiteit.TabIndex = 12;
            ButtonVolatiteit.Text = "Volatiteit";
            ButtonVolatiteit.UseVisualStyleBackColor = true;
            ButtonVolatiteit.Click += ButtonVolatiteit_Click;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(588, 21);
            comboBox1.Margin = new Padding(4, 3, 4, 3);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(140, 23);
            comboBox1.TabIndex = 11;
            // 
            // ButtonBackTest
            // 
            ButtonBackTest.Location = new Point(485, 16);
            ButtonBackTest.Margin = new Padding(4, 3, 4, 3);
            ButtonBackTest.Name = "ButtonBackTest";
            ButtonBackTest.Size = new Size(88, 27);
            ButtonBackTest.TabIndex = 10;
            ButtonBackTest.Text = "Backtest";
            ButtonBackTest.UseVisualStyleBackColor = true;
            ButtonBackTest.Click += ButtonBackTest_Click;
            // 
            // PriceAction
            // 
            PriceAction.Location = new Point(391, 16);
            PriceAction.Margin = new Padding(4, 3, 4, 3);
            PriceAction.Name = "PriceAction";
            PriceAction.Size = new Size(88, 27);
            PriceAction.TabIndex = 9;
            PriceAction.Text = "ZigZag test";
            PriceAction.UseVisualStyleBackColor = true;
            PriceAction.Click += PriceAction_Click;
            // 
            // button3
            // 
            button3.Location = new Point(202, 16);
            button3.Margin = new Padding(4, 3, 4, 3);
            button3.Name = "button3";
            button3.Size = new Size(88, 27);
            button3.TabIndex = 7;
            button3.Text = "Signal.Create";
            button3.UseVisualStyleBackColor = true;
            // 
            // ButtonBitmap
            // 
            ButtonBitmap.Location = new Point(13, 16);
            ButtonBitmap.Margin = new Padding(4, 3, 4, 3);
            ButtonBitmap.Name = "ButtonBitmap";
            ButtonBitmap.Size = new Size(88, 27);
            ButtonBitmap.TabIndex = 5;
            ButtonBitmap.Text = "Bitmap";
            ButtonBitmap.UseVisualStyleBackColor = true;
            ButtonBitmap.Click += ButtonBitmap_Click;
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabPageLog);
            tabControl.Controls.Add(tabPageSignals);
            tabControl.Controls.Add(tabPageBitmap);
            tabControl.Controls.Add(tabPage3);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 59);
            tabControl.Margin = new Padding(4, 3, 4, 3);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1423, 738);
            tabControl.TabIndex = 11;
            // 
            // tabPageLog
            // 
            tabPageLog.Controls.Add(textBox1);
            tabPageLog.Location = new Point(4, 24);
            tabPageLog.Margin = new Padding(4, 3, 4, 3);
            tabPageLog.Name = "tabPageLog";
            tabPageLog.Padding = new Padding(4, 3, 4, 3);
            tabPageLog.Size = new Size(1415, 710);
            tabPageLog.TabIndex = 0;
            tabPageLog.Text = "tabPage1";
            tabPageLog.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            textBox1.Dock = DockStyle.Fill;
            textBox1.Location = new Point(4, 3);
            textBox1.Margin = new Padding(4, 3, 4, 3);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ScrollBars = ScrollBars.Both;
            textBox1.Size = new Size(1407, 704);
            textBox1.TabIndex = 2;
            // 
            // tabPageSignals
            // 
            tabPageSignals.Controls.Add(listView1);
            tabPageSignals.Location = new Point(4, 24);
            tabPageSignals.Margin = new Padding(4, 3, 4, 3);
            tabPageSignals.Name = "tabPageSignals";
            tabPageSignals.Padding = new Padding(4, 3, 4, 3);
            tabPageSignals.Size = new Size(1415, 710);
            tabPageSignals.TabIndex = 1;
            tabPageSignals.Text = "tabPage2";
            tabPageSignals.UseVisualStyleBackColor = true;
            // 
            // listView1
            // 
            listView1.BorderStyle = BorderStyle.None;
            listView1.Dock = DockStyle.Fill;
            listView1.Location = new Point(4, 3);
            listView1.Name = "listView1";
            listView1.Size = new Size(1407, 704);
            listView1.TabIndex = 0;
            listView1.UseCompatibleStateImageBehavior = false;
            // 
            // tabPageBitmap
            // 
            tabPageBitmap.Controls.Add(pictureBox1);
            tabPageBitmap.Location = new Point(4, 24);
            tabPageBitmap.Margin = new Padding(4, 3, 4, 3);
            tabPageBitmap.Name = "tabPageBitmap";
            tabPageBitmap.Padding = new Padding(4, 3, 4, 3);
            tabPageBitmap.Size = new Size(1415, 710);
            tabPageBitmap.TabIndex = 2;
            tabPageBitmap.Text = "tabPage2";
            tabPageBitmap.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(27, 19);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(457, 156);
            pictureBox1.TabIndex = 16;
            pictureBox1.TabStop = false;
            // 
            // tabPage3
            // 
            tabPage3.Location = new Point(4, 24);
            tabPage3.Margin = new Padding(4, 3, 4, 3);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(4, 3, 4, 3);
            tabPage3.Size = new Size(1415, 710);
            tabPage3.TabIndex = 3;
            tabPage3.Text = "tabPage3";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // contextMenuStripSignals
            // 
            contextMenuStripSignals.Items.AddRange(new ToolStripItem[] { toolStripMenuItemClear, toolStripMenuItemAltrady, toolStripMenuItemTradingView, toolStripMenuItemHypertrader });
            contextMenuStripSignals.Name = "contextMenuStripSignals";
            contextMenuStripSignals.Size = new Size(139, 92);
            // 
            // toolStripMenuItemClear
            // 
            toolStripMenuItemClear.Name = "toolStripMenuItemClear";
            toolStripMenuItemClear.Size = new Size(138, 22);
            toolStripMenuItemClear.Text = "Clear";
            // 
            // toolStripMenuItemAltrady
            // 
            toolStripMenuItemAltrady.Name = "toolStripMenuItemAltrady";
            toolStripMenuItemAltrady.Size = new Size(138, 22);
            toolStripMenuItemAltrady.Text = "Altrady";
            // 
            // toolStripMenuItemTradingView
            // 
            toolStripMenuItemTradingView.Name = "toolStripMenuItemTradingView";
            toolStripMenuItemTradingView.Size = new Size(138, 22);
            toolStripMenuItemTradingView.Text = "TradingView";
            // 
            // toolStripMenuItemHypertrader
            // 
            toolStripMenuItemHypertrader.Name = "toolStripMenuItemHypertrader";
            toolStripMenuItemHypertrader.Size = new Size(138, 22);
            toolStripMenuItemHypertrader.Text = "Hypertrader";
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { testToolStripMenuItem, toolStripMenuItem1, toolStripMenuItem2, toolStripMenuItem3, toolStripMenuItem4, toolStripMenuItem5, toolStripMenuItem6, toolStripMenuItem7, toolStripMenuItem8, toolStripMenuItem9, toolStripMenuItem10 });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(187, 246);
            // 
            // testToolStripMenuItem
            // 
            testToolStripMenuItem.Name = "testToolStripMenuItem";
            testToolStripMenuItem.Size = new Size(186, 22);
            testToolStripMenuItem.Text = "Test";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(186, 22);
            toolStripMenuItem1.Text = "toolStripMenuItem1";
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(186, 22);
            toolStripMenuItem2.Text = "toolStripMenuItem2";
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new Size(186, 22);
            toolStripMenuItem3.Text = "toolStripMenuItem3";
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.Size = new Size(186, 22);
            toolStripMenuItem4.Text = "toolStripMenuItem4";
            // 
            // toolStripMenuItem5
            // 
            toolStripMenuItem5.Name = "toolStripMenuItem5";
            toolStripMenuItem5.Size = new Size(186, 22);
            toolStripMenuItem5.Text = "toolStripMenuItem5";
            // 
            // toolStripMenuItem6
            // 
            toolStripMenuItem6.Name = "toolStripMenuItem6";
            toolStripMenuItem6.Size = new Size(186, 22);
            toolStripMenuItem6.Text = "toolStripMenuItem6";
            // 
            // toolStripMenuItem7
            // 
            toolStripMenuItem7.Name = "toolStripMenuItem7";
            toolStripMenuItem7.Size = new Size(186, 22);
            toolStripMenuItem7.Text = "toolStripMenuItem7";
            // 
            // toolStripMenuItem8
            // 
            toolStripMenuItem8.Name = "toolStripMenuItem8";
            toolStripMenuItem8.Size = new Size(186, 22);
            toolStripMenuItem8.Text = "toolStripMenuItem8";
            // 
            // toolStripMenuItem9
            // 
            toolStripMenuItem9.Name = "toolStripMenuItem9";
            toolStripMenuItem9.Size = new Size(186, 22);
            toolStripMenuItem9.Text = "toolStripMenuItem9";
            // 
            // toolStripMenuItem10
            // 
            toolStripMenuItem10.Name = "toolStripMenuItem10";
            toolStripMenuItem10.Size = new Size(186, 22);
            toolStripMenuItem10.Text = "toolStripMenuItem10";
            // 
            // TestForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1423, 797);
            Controls.Add(tabControl);
            Controls.Add(panel1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "TestForm";
            Text = "Form1";
            panel1.ResumeLayout(false);
            tabControl.ResumeLayout(false);
            tabPageLog.ResumeLayout(false);
            tabPageLog.PerformLayout();
            tabPageSignals.ResumeLayout(false);
            tabPageBitmap.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            contextMenuStripSignals.ResumeLayout(false);
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button ButtonBitmap;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPageLog;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TabPage tabPageSignals;
        private System.Windows.Forms.TabPage tabPageBitmap;
        private System.Windows.Forms.Button PriceAction;
        private System.Windows.Forms.Button ButtonBackTest;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button ButtonVolatiteit;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem testToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem7;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem8;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem9;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem10;
        private System.Windows.Forms.Timer timerClearEvents;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripSignals;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemClear;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemAltrady;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemTradingView;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemHypertrader;
        private ListView listView1;
        private PictureBox pictureBox1;
        private Button button1;
    }
}

