namespace CryptoSbmScanner
{
    partial class AskSymbolDialog
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
            EditInterval = new ComboBox();
            EditSymbol = new TextBox();
            EditTime = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            ButtonCancel = new Button();
            ButtonOk = new Button();
            label4 = new Label();
            EditAlgoritm = new ComboBox();
            SuspendLayout();
            // 
            // EditInterval
            // 
            EditInterval.AccessibleRole = AccessibleRole.IpAddress;
            EditInterval.DropDownStyle = ComboBoxStyle.DropDownList;
            EditInterval.FormattingEnabled = true;
            EditInterval.Location = new Point(98, 56);
            EditInterval.Margin = new Padding(2);
            EditInterval.Name = "EditInterval";
            EditInterval.Size = new Size(151, 23);
            EditInterval.TabIndex = 71;
            // 
            // EditSymbol
            // 
            EditSymbol.CharacterCasing = CharacterCasing.Upper;
            EditSymbol.Location = new Point(98, 28);
            EditSymbol.Name = "EditSymbol";
            EditSymbol.Size = new Size(151, 23);
            EditSymbol.TabIndex = 70;
            // 
            // EditTime
            // 
            EditTime.Location = new Point(98, 84);
            EditTime.Name = "EditTime";
            EditTime.Size = new Size(151, 23);
            EditTime.TabIndex = 72;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(21, 31);
            label1.Name = "label1";
            label1.Size = new Size(47, 15);
            label1.TabIndex = 73;
            label1.Text = "Symbol";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(21, 59);
            label2.Name = "label2";
            label2.Size = new Size(46, 15);
            label2.TabIndex = 74;
            label2.Text = "Interval";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(21, 87);
            label3.Name = "label3";
            label3.Size = new Size(33, 15);
            label3.TabIndex = 75;
            label3.Text = "Time";
            // 
            // ButtonCancel
            // 
            ButtonCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ButtonCancel.Location = new Point(161, 161);
            ButtonCancel.Margin = new Padding(4, 3, 4, 3);
            ButtonCancel.Name = "ButtonCancel";
            ButtonCancel.Size = new Size(88, 27);
            ButtonCancel.TabIndex = 74;
            ButtonCancel.Text = "&Cancel";
            ButtonCancel.UseVisualStyleBackColor = true;
            ButtonCancel.Click += ButtonCancel_Click;
            // 
            // ButtonOk
            // 
            ButtonOk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ButtonOk.Location = new Point(65, 161);
            ButtonOk.Margin = new Padding(4, 3, 4, 3);
            ButtonOk.Name = "ButtonOk";
            ButtonOk.Size = new Size(88, 27);
            ButtonOk.TabIndex = 73;
            ButtonOk.Text = "&Ok";
            ButtonOk.UseVisualStyleBackColor = true;
            ButtonOk.Click += ButtonOk_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(21, 115);
            label4.Name = "label4";
            label4.Size = new Size(60, 15);
            label4.TabIndex = 77;
            label4.Text = "Algoritme";
            // 
            // EditAlgoritm
            // 
            EditAlgoritm.AccessibleRole = AccessibleRole.IpAddress;
            EditAlgoritm.DropDownStyle = ComboBoxStyle.DropDownList;
            EditAlgoritm.FormattingEnabled = true;
            EditAlgoritm.Location = new Point(98, 112);
            EditAlgoritm.Margin = new Padding(2);
            EditAlgoritm.Name = "EditAlgoritm";
            EditAlgoritm.Size = new Size(151, 23);
            EditAlgoritm.TabIndex = 76;
            // 
            // AskSymbolDialog
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(264, 200);
            Controls.Add(label4);
            Controls.Add(EditAlgoritm);
            Controls.Add(ButtonCancel);
            Controls.Add(ButtonOk);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(EditTime);
            Controls.Add(EditSymbol);
            Controls.Add(EditInterval);
            Name = "AskSymbolDialog";
            Text = "Backtest";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ComboBox EditInterval;
        private TextBox EditSymbol;
        private TextBox EditTime;
        private Label label1;
        private Label label2;
        private Label label3;
        private Button ButtonCancel;
        private Button ButtonOk;
        private Label label4;
        private ComboBox EditAlgoritm;
    }
}