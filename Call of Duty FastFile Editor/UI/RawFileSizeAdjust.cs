using System;
using System.Windows.Forms;

namespace Call_of_Duty_FastFile_Editor.UI
{
    public partial class RawFileSizeAdjust : Form
    {
        /// <summary>
        /// Gets or sets the current file size. Should be set before showing the form.
        /// </summary>
        public int CurrentFileSize { get; set; }

        /// <summary>
        /// Gets the new file size entered by the user.
        /// </summary>
        public int NewFileSize { get; private set; }

        public RawFileSizeAdjust()
        {
            InitializeComponent();
        }

        private void RawFileSizeAdjust_Load(object sender, EventArgs e)
        {
            // Update the current size label and set the minimum for the numeric control.
            lblCurrentSize.Text = $"Current file size: {CurrentFileSize} bytes";
            nudNewSize.Minimum = CurrentFileSize + 1;
            nudNewSize.Value = CurrentFileSize + 1; // Default to just 1 byte larger.
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (nudNewSize.Value <= CurrentFileSize)
            {
                MessageBox.Show("The new file size must be greater than the current file size.",
                    "Invalid Size", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            NewFileSize = (int)nudNewSize.Value;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
