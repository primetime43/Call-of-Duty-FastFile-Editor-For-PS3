using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Call_of_Duty_FastFile_Editor.UI
{
    public partial class RenameDialog : Form
    {
        public string NewFileName { get; private set; } = string.Empty;

        public RenameDialog(string currentName)
        {
            InitializeComponent();
            txtNewFileName.Text = currentName;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            NewFileName = txtNewFileName.Text.Trim();
            if (string.IsNullOrEmpty(NewFileName))
            {
                MessageBox.Show("File name cannot be empty.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
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
