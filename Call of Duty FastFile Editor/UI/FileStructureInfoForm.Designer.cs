namespace Call_of_Duty_FastFile_Editor.UI
{
    partial class FileStructureInfoForm
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
            selectedFileNameLabel = new Label();
            codeStartPositionLabel = new Label();
            startOfFileHeaderPositionLabel = new Label();
            maxFileSizeLabel = new Label();
            fileSizePositionLabel = new Label();
            endOfFIlePositionLabel = new Label();
            selectedFileNameTextBox = new TextBox();
            codeStartPositionTextBox = new TextBox();
            startOfFileHeaderPositionTextBox = new TextBox();
            fileSizePositionTextBox = new TextBox();
            maxFileSizeTextBox = new TextBox();
            endOfFIlePositionTextBox = new TextBox();
            decimalRadioButton = new RadioButton();
            hexadecimalRadioButton = new RadioButton();
            SuspendLayout();
            // 
            // selectedFileNameLabel
            // 
            selectedFileNameLabel.AutoSize = true;
            selectedFileNameLabel.Location = new Point(10, 16);
            selectedFileNameLabel.Name = "selectedFileNameLabel";
            selectedFileNameLabel.Size = new Size(140, 20);
            selectedFileNameLabel.TabIndex = 0;
            selectedFileNameLabel.Text = "Selected File Name:";
            // 
            // codeStartPositionLabel
            // 
            codeStartPositionLabel.AutoSize = true;
            codeStartPositionLabel.Location = new Point(10, 56);
            codeStartPositionLabel.Name = "codeStartPositionLabel";
            codeStartPositionLabel.Size = new Size(138, 20);
            codeStartPositionLabel.TabIndex = 1;
            codeStartPositionLabel.Text = "Code Start Position:";
            // 
            // startOfFileHeaderPositionLabel
            // 
            startOfFileHeaderPositionLabel.AutoSize = true;
            startOfFileHeaderPositionLabel.Location = new Point(10, 103);
            startOfFileHeaderPositionLabel.Name = "startOfFileHeaderPositionLabel";
            startOfFileHeaderPositionLabel.Size = new Size(197, 20);
            startOfFileHeaderPositionLabel.TabIndex = 2;
            startOfFileHeaderPositionLabel.Text = "Start of File Header Position:";
            // 
            // maxFileSizeLabel
            // 
            maxFileSizeLabel.AutoSize = true;
            maxFileSizeLabel.Location = new Point(12, 195);
            maxFileSizeLabel.Name = "maxFileSizeLabel";
            maxFileSizeLabel.Size = new Size(98, 20);
            maxFileSizeLabel.TabIndex = 3;
            maxFileSizeLabel.Text = "Max File Size:";
            // 
            // fileSizePositionLabel
            // 
            fileSizePositionLabel.AutoSize = true;
            fileSizePositionLabel.Location = new Point(10, 150);
            fileSizePositionLabel.Name = "fileSizePositionLabel";
            fileSizePositionLabel.Size = new Size(122, 20);
            fileSizePositionLabel.TabIndex = 4;
            fileSizePositionLabel.Text = "File Size Position:";
            // 
            // endOfFIlePositionLabel
            // 
            endOfFIlePositionLabel.AutoSize = true;
            endOfFIlePositionLabel.Location = new Point(10, 243);
            endOfFIlePositionLabel.Name = "endOfFIlePositionLabel";
            endOfFIlePositionLabel.Size = new Size(140, 20);
            endOfFIlePositionLabel.TabIndex = 5;
            endOfFIlePositionLabel.Text = "End Of File Position:";
            // 
            // selectedFileNameTextBox
            // 
            selectedFileNameTextBox.Location = new Point(156, 9);
            selectedFileNameTextBox.Name = "selectedFileNameTextBox";
            selectedFileNameTextBox.ReadOnly = true;
            selectedFileNameTextBox.Size = new Size(293, 27);
            selectedFileNameTextBox.TabIndex = 6;
            // 
            // codeStartPositionTextBox
            // 
            codeStartPositionTextBox.Location = new Point(285, 49);
            codeStartPositionTextBox.Name = "codeStartPositionTextBox";
            codeStartPositionTextBox.ReadOnly = true;
            codeStartPositionTextBox.Size = new Size(164, 27);
            codeStartPositionTextBox.TabIndex = 7;
            // 
            // startOfFileHeaderPositionTextBox
            // 
            startOfFileHeaderPositionTextBox.Location = new Point(285, 96);
            startOfFileHeaderPositionTextBox.Name = "startOfFileHeaderPositionTextBox";
            startOfFileHeaderPositionTextBox.ReadOnly = true;
            startOfFileHeaderPositionTextBox.Size = new Size(164, 27);
            startOfFileHeaderPositionTextBox.TabIndex = 8;
            // 
            // fileSizePositionTextBox
            // 
            fileSizePositionTextBox.Location = new Point(285, 143);
            fileSizePositionTextBox.Name = "fileSizePositionTextBox";
            fileSizePositionTextBox.ReadOnly = true;
            fileSizePositionTextBox.Size = new Size(164, 27);
            fileSizePositionTextBox.TabIndex = 9;
            // 
            // maxFileSizeTextBox
            // 
            maxFileSizeTextBox.Location = new Point(285, 188);
            maxFileSizeTextBox.Name = "maxFileSizeTextBox";
            maxFileSizeTextBox.ReadOnly = true;
            maxFileSizeTextBox.Size = new Size(164, 27);
            maxFileSizeTextBox.TabIndex = 10;
            // 
            // endOfFIlePositionTextBox
            // 
            endOfFIlePositionTextBox.Location = new Point(285, 236);
            endOfFIlePositionTextBox.Name = "endOfFIlePositionTextBox";
            endOfFIlePositionTextBox.ReadOnly = true;
            endOfFIlePositionTextBox.Size = new Size(164, 27);
            endOfFIlePositionTextBox.TabIndex = 11;
            // 
            // decimalRadioButton
            // 
            decimalRadioButton.AutoSize = true;
            decimalRadioButton.Location = new Point(12, 310);
            decimalRadioButton.Name = "decimalRadioButton";
            decimalRadioButton.Size = new Size(82, 24);
            decimalRadioButton.TabIndex = 12;
            decimalRadioButton.TabStop = true;
            decimalRadioButton.Text = "Decimal";
            decimalRadioButton.UseVisualStyleBackColor = true;
            decimalRadioButton.CheckedChanged += decimalRadioButton_CheckedChanged;
            // 
            // hexadecimalRadioButton
            // 
            hexadecimalRadioButton.AutoSize = true;
            hexadecimalRadioButton.Location = new Point(100, 310);
            hexadecimalRadioButton.Name = "hexadecimalRadioButton";
            hexadecimalRadioButton.Size = new Size(114, 24);
            hexadecimalRadioButton.TabIndex = 13;
            hexadecimalRadioButton.TabStop = true;
            hexadecimalRadioButton.Text = "Hexadecimal";
            hexadecimalRadioButton.UseVisualStyleBackColor = true;
            hexadecimalRadioButton.CheckedChanged += hexadecimalRadioButton_CheckedChanged;
            // 
            // FileStructureInfoForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(490, 346);
            Controls.Add(hexadecimalRadioButton);
            Controls.Add(decimalRadioButton);
            Controls.Add(endOfFIlePositionTextBox);
            Controls.Add(maxFileSizeTextBox);
            Controls.Add(fileSizePositionTextBox);
            Controls.Add(startOfFileHeaderPositionTextBox);
            Controls.Add(codeStartPositionTextBox);
            Controls.Add(selectedFileNameTextBox);
            Controls.Add(endOfFIlePositionLabel);
            Controls.Add(fileSizePositionLabel);
            Controls.Add(maxFileSizeLabel);
            Controls.Add(startOfFileHeaderPositionLabel);
            Controls.Add(codeStartPositionLabel);
            Controls.Add(selectedFileNameLabel);
            Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            Name = "FileStructureInfoForm";
            Text = "File Structure Info";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label selectedFileNameLabel;
        private Label codeStartPositionLabel;
        private Label startOfFileHeaderPositionLabel;
        private Label maxFileSizeLabel;
        private Label fileSizePositionLabel;
        private Label endOfFIlePositionLabel;
        private TextBox selectedFileNameTextBox;
        private TextBox codeStartPositionTextBox;
        private TextBox startOfFileHeaderPositionTextBox;
        private TextBox fileSizePositionTextBox;
        private TextBox maxFileSizeTextBox;
        private TextBox endOfFIlePositionTextBox;
        private RadioButton decimalRadioButton;
        private RadioButton hexadecimalRadioButton;
    }
}