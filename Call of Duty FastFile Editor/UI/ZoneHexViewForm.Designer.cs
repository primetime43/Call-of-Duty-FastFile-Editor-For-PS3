using Be.Windows.Forms;

namespace Call_of_Duty_FastFile_Editor.UI
{
    partial class ZoneHexViewForm
    {
        private System.ComponentModel.IContainer components = null;

        // menus
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem,
                                    saveAsToolStripMenuItem,
                                    closeToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem,
                                    copyHexToolStripMenuItem,
                                    copyAsciiToolStripMenuItem,
                                    selectAllToolStripMenuItem;
        private ToolStripMenuItem goToToolStripMenuItem;
        private ToolStripMenuItem byteOrderMenu,
                                    littleEndianItem,
                                    bigEndianItem;

        // status bar
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel offsetStatusLabel,
                                    selStatusLabel,
                                    valueStatusLabel;

        // main controls
        internal HexBox hexBox;
        private TextBox inspectorTextBox;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // ► MenuStrip
            this.menuStrip1 = new MenuStrip();

            // File
            this.fileToolStripMenuItem = new ToolStripMenuItem("File");
            this.saveAsToolStripMenuItem = new ToolStripMenuItem("Save As…");
            this.closeToolStripMenuItem = new ToolStripMenuItem("Close");
            this.fileToolStripMenuItem.DropDownItems.AddRange(new[]{
                saveAsToolStripMenuItem,
                closeToolStripMenuItem
            });

            // Edit
            this.editToolStripMenuItem = new ToolStripMenuItem("Edit");
            this.copyHexToolStripMenuItem = new ToolStripMenuItem("Copy Hex");
            this.copyAsciiToolStripMenuItem = new ToolStripMenuItem("Copy ASCII");
            this.selectAllToolStripMenuItem = new ToolStripMenuItem("Select All");
            this.editToolStripMenuItem.DropDownItems.AddRange(new[]{
                copyHexToolStripMenuItem,
                copyAsciiToolStripMenuItem,
                selectAllToolStripMenuItem
            });

            // Go To…
            this.goToToolStripMenuItem = new ToolStripMenuItem("Go To…");

            // Byte Order submenu
            this.byteOrderMenu = new ToolStripMenuItem("Byte Order");
            this.littleEndianItem = new ToolStripMenuItem("Little‑endian") { Checked = true, CheckOnClick = true };
            this.bigEndianItem = new ToolStripMenuItem("Big‑endian") { Checked = false, CheckOnClick = true };
            this.byteOrderMenu.DropDownItems.AddRange(new[]{
                littleEndianItem,
                bigEndianItem
            });
            // attach Byte Order into Edit
            this.editToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            this.editToolStripMenuItem.DropDownItems.Add(byteOrderMenu);

            // top‑level
            this.menuStrip1.Items.AddRange(new ToolStripItem[]{
                fileToolStripMenuItem,
                editToolStripMenuItem,
                goToToolStripMenuItem
            });

            // ► StatusStrip
            this.statusStrip1 = new StatusStrip();
            this.offsetStatusLabel = new ToolStripStatusLabel("Offset: 0x0");
            this.selStatusLabel = new ToolStripStatusLabel("Sel: 0 b");
            this.valueStatusLabel = new ToolStripStatusLabel("Value: --");
            this.statusStrip1.Items.AddRange(new ToolStripItem[]{
                offsetStatusLabel,
                new ToolStripSeparator(),
                selStatusLabel,
                new ToolStripSeparator(),
                valueStatusLabel
            });

            // ► HexBox
            this.hexBox = new HexBox
            {
                Dock = DockStyle.Fill
            };

            // ► Inspector panel
            this.inspectorTextBox = new TextBox
            {
                Dock = DockStyle.Right,
                Width = 200,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            // ► Form layout
            this.MainMenuStrip = this.menuStrip1;
            this.Controls.Add(hexBox);
            this.Controls.Add(inspectorTextBox);
            this.Controls.Add(statusStrip1);
            this.Controls.Add(menuStrip1);

            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Text = "Zone File Hex Viewer";
        }
    }
}
