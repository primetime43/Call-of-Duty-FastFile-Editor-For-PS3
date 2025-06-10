using System;
using System.Linq;
using System.Windows.Forms;
using Be.Windows.Forms;

namespace Call_of_Duty_FastFile_Editor.UI
{
    public partial class ZoneHexViewForm : Form
    {
        public ZoneHexViewForm(byte[] data)
        {
            InitializeComponent();

            // default to big‑endian
            bigEndianItem.Checked = true;
            littleEndianItem.Checked = false;

            // configure HexBox
            var dp = new DynamicByteProvider(data);
            hexBox.ByteProvider = dp;
            hexBox.ReadOnly = true;
            hexBox.StringViewVisible = true;   // show ASCII column
            hexBox.LineInfoVisible = true;   // show offsets
            hexBox.VScrollBarVisible = true;   // ensure vertical scroll
            hexBox.BytesPerLine = 16;     // 16 bytes per row
            hexBox.GroupSize = 4;      // group every 4 bytes

            // update status on click or key
            hexBox.MouseClick += (s, e) => RefreshStatus();
            hexBox.KeyUp += (s, e) => RefreshStatus();

            // File menu actions
            saveAsToolStripMenuItem.Click += SaveAs;
            closeToolStripMenuItem.Click += (s, e) => Close();

            // Edit menu actions
            copyHexToolStripMenuItem.Click += (s, e) => hexBox.CopyHex();
            copyAsciiToolStripMenuItem.Click += (s, e) => hexBox.Copy();
            selectAllToolStripMenuItem.Click += (s, e) => hexBox.SelectAll();

            // Go To…
            goToToolStripMenuItem.Click += (s, e) => ShowGotoDialog();

            // Byte Order toggle
            littleEndianItem.Click += (s, e) =>
            {
                littleEndianItem.Checked = true;
                bigEndianItem.Checked = false;
                RefreshStatus();
            };
            bigEndianItem.Click += (s, e) =>
            {
                bigEndianItem.Checked = true;
                littleEndianItem.Checked = false;
                RefreshStatus();
            };

            RefreshStatus();
        }

        private bool BigEndianSelected => bigEndianItem.Checked;

        private void RefreshStatus()
        {
            long pos = hexBox.SelectionStart;
            long len = hexBox.SelectionLength;

            offsetStatusLabel.Text = $"Offset: 0x{pos:X}";
            selStatusLabel.Text = $"Sel: {len} bytes";

            // single‐byte value
            if (len == 1)
            {
                byte b = ((DynamicByteProvider)hexBox.ByteProvider).Bytes[(int)pos];
                valueStatusLabel.Text = $"Value: 0x{b:X2}";
            }
            else
            {
                valueStatusLabel.Text = "Value: --";
            }

            // 4‐byte inspector
            if (len == 4)
            {
                var buf = ((DynamicByteProvider)hexBox.ByteProvider)
                              .Bytes
                              .Skip((int)pos)
                              .Take(4)
                              .ToArray();

                if (BigEndianSelected)
                    Array.Reverse(buf);

                inspectorTextBox.Text =
                    $"UInt32: {BitConverter.ToUInt32(buf, 0)}\r\n" +
                    $"Int32 : {BitConverter.ToInt32(buf, 0)}\r\n" +
                    $"Float : {BitConverter.ToSingle(buf, 0):F6}";
            }
            else
            {
                inspectorTextBox.Clear();
            }
        }

        private void SaveAs(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog { Filter = "Binary|*.*", Title = "Save Zone As" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var bytes = ((DynamicByteProvider)hexBox.ByteProvider).Bytes.ToArray();
                    System.IO.File.WriteAllBytes(sfd.FileName, bytes);
                }
            }
        }

        private void ShowGotoDialog()
        {
            // ask for a hex offset (allow “0x” prefix)
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Go to offset (hex)", "Go To", "0");

            if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                input = input.Substring(2);

            if (long.TryParse(input,
                              System.Globalization.NumberStyles.HexNumber,
                              null,
                              out var off))
            {
                // clamp to valid range
                var max = ((DynamicByteProvider)hexBox.ByteProvider).Length - 1;
                if (off < 0) off = 0;
                if (off > max) off = max;

                // move the caret, select that single byte, scroll it into view
                hexBox.SelectionStart = off;
                hexBox.SelectionLength = 1;
                hexBox.ScrollByteIntoView(off);

                RefreshStatus();
            }
            else
            {
                MessageBox.Show($"\"{input}\" is not a valid hex number.",
                                "Invalid Offset",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
            }
        }
    }
}
