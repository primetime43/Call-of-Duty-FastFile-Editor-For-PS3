namespace Call_of_Duty_FastFile_Editor.UI
{
    partial class RawFileSizeAdjust
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblInstruction;
        private System.Windows.Forms.Label lblCurrentSize;
        private System.Windows.Forms.NumericUpDown nudNewSize;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblInstruction = new System.Windows.Forms.Label();
            this.lblCurrentSize = new System.Windows.Forms.Label();
            this.nudNewSize = new System.Windows.Forms.NumericUpDown();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudNewSize)).BeginInit();
            this.SuspendLayout();
            // 
            // lblInstruction
            // 
            this.lblInstruction.AutoSize = true;
            this.lblInstruction.Location = new System.Drawing.Point(12, 20);
            this.lblInstruction.Name = "lblInstruction";
            this.lblInstruction.Size = new System.Drawing.Size(420, 15);
            this.lblInstruction.TabIndex = 0;
            this.lblInstruction.Text = "Enter the new file size in bytes (must be greater than the current file size):";
            // 
            // lblCurrentSize
            // 
            this.lblCurrentSize.AutoSize = true;
            this.lblCurrentSize.Location = new System.Drawing.Point(12, 50);
            this.lblCurrentSize.Name = "lblCurrentSize";
            this.lblCurrentSize.Size = new System.Drawing.Size(120, 15);
            this.lblCurrentSize.TabIndex = 1;
            this.lblCurrentSize.Text = "Current file size: 0 bytes";
            // 
            // nudNewSize
            // 
            this.nudNewSize.Location = new System.Drawing.Point(12, 80);
            this.nudNewSize.Maximum = new decimal(new int[] {
            int.MaxValue,
            0,
            0,
            0});
            this.nudNewSize.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudNewSize.Name = "nudNewSize";
            this.nudNewSize.Size = new System.Drawing.Size(200, 23);
            this.nudNewSize.TabIndex = 2;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(130, 130);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(220, 130);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // RawFileSizeAdjust
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 180);
            this.Controls.Add(this.lblInstruction);
            this.Controls.Add(this.lblCurrentSize);
            this.Controls.Add(this.nudNewSize);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RawFileSizeAdjust";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Raw File Adjusting";
            this.Load += new System.EventHandler(this.RawFileSizeAdjust_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudNewSize)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
