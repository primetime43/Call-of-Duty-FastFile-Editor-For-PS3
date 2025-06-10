namespace Call_of_Duty_FastFile_Editor.UI
{
    partial class RenameDialog
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
            txtNewFileName = new TextBox();
            btnOk = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // txtNewFileName
            // 
            txtNewFileName.Location = new Point(12, 13);
            txtNewFileName.Margin = new Padding(3, 4, 3, 4);
            txtNewFileName.Name = "txtNewFileName";
            txtNewFileName.Size = new Size(284, 27);
            txtNewFileName.TabIndex = 0;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(43, 47);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(91, 30);
            btnOk.TabIndex = 1;
            btnOk.Text = "Confirm";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(160, 47);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(91, 30);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // RenameDialog
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(315, 84);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(txtNewFileName);
            Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            Name = "RenameDialog";
            Text = "RenameDialog";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtNewFileName;
        private Button btnOk;
        private Button btnCancel;
    }
}