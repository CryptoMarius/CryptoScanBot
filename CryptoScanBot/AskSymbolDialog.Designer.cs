namespace CryptoScanBot
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
            EditSymbol = new TextBox();
            label1 = new Label();
            label3 = new Label();
            ButtonCancel = new Button();
            ButtonOk = new Button();
            label2 = new Label();
            EditTimeStart = new DateTimePicker();
            EditTimeEnd = new DateTimePicker();
            SuspendLayout();
            // 
            // EditSymbol
            // 
            EditSymbol.CharacterCasing = CharacterCasing.Upper;
            EditSymbol.Location = new Point(98, 28);
            EditSymbol.Name = "EditSymbol";
            EditSymbol.Size = new Size(151, 23);
            EditSymbol.TabIndex = 70;
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
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(21, 60);
            label3.Name = "label3";
            label3.Size = new Size(58, 15);
            label3.TabIndex = 75;
            label3.Text = "Start time";
            // 
            // ButtonCancel
            // 
            ButtonCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ButtonCancel.Location = new Point(161, 126);
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
            ButtonOk.Location = new Point(65, 126);
            ButtonOk.Margin = new Padding(4, 3, 4, 3);
            ButtonOk.Name = "ButtonOk";
            ButtonOk.Size = new Size(88, 27);
            ButtonOk.TabIndex = 73;
            ButtonOk.Text = "&Ok";
            ButtonOk.UseVisualStyleBackColor = true;
            ButtonOk.Click += ButtonOk_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(21, 89);
            label2.Name = "label2";
            label2.Size = new Size(27, 15);
            label2.TabIndex = 77;
            label2.Text = "End";
            // 
            // EditTimeStart
            // 
            EditTimeStart.CustomFormat = "yyyy.MM.dd HH:mm";
            EditTimeStart.Format = DateTimePickerFormat.Custom;
            EditTimeStart.Location = new Point(98, 58);
            EditTimeStart.Name = "EditTimeStart";
            EditTimeStart.Size = new Size(200, 23);
            EditTimeStart.TabIndex = 78;
            // 
            // EditTimeEnd
            // 
            EditTimeEnd.CustomFormat = "yyyy.MM.dd HH:mm";
            EditTimeEnd.Format = DateTimePickerFormat.Custom;
            EditTimeEnd.Location = new Point(98, 88);
            EditTimeEnd.Name = "EditTimeEnd";
            EditTimeEnd.Size = new Size(200, 23);
            EditTimeEnd.TabIndex = 79;
            // 
            // AskSymbolDialog
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(264, 172);
            Controls.Add(EditTimeEnd);
            Controls.Add(EditTimeStart);
            Controls.Add(label2);
            Controls.Add(ButtonCancel);
            Controls.Add(ButtonOk);
            Controls.Add(label3);
            Controls.Add(label1);
            Controls.Add(EditSymbol);
            Name = "AskSymbolDialog";
            Text = "Backtest";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TextBox EditSymbol;
        private Label label1;
        private Label label3;
        private Button ButtonCancel;
        private Button ButtonOk;
        private Label label2;
        private DateTimePicker EditTimeStart;
        private DateTimePicker EditTimeEnd;
    }
}