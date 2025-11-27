namespace FastFileCompilerGUI;

public class RenameDialog : Form
{
    private TextBox textBoxName = null!;
    private Button btnOK = null!;
    private Button btnCancel = null!;
    private Label labelPrompt = null!;

    public string NewName => textBoxName.Text;

    public RenameDialog(string currentName)
    {
        InitializeComponent();
        textBoxName.Text = currentName;
        textBoxName.SelectAll();
    }

    private void InitializeComponent()
    {
        this.labelPrompt = new Label();
        this.textBoxName = new TextBox();
        this.btnOK = new Button();
        this.btnCancel = new Button();
        this.SuspendLayout();

        // labelPrompt
        this.labelPrompt.AutoSize = true;
        this.labelPrompt.Location = new Point(12, 15);
        this.labelPrompt.Text = "Asset Name:";

        // textBoxName
        this.textBoxName.Location = new Point(12, 35);
        this.textBoxName.Size = new Size(360, 23);
        this.textBoxName.TabIndex = 0;

        // btnOK
        this.btnOK.DialogResult = DialogResult.OK;
        this.btnOK.Location = new Point(216, 70);
        this.btnOK.Size = new Size(75, 28);
        this.btnOK.Text = "OK";
        this.btnOK.UseVisualStyleBackColor = true;

        // btnCancel
        this.btnCancel.DialogResult = DialogResult.Cancel;
        this.btnCancel.Location = new Point(297, 70);
        this.btnCancel.Size = new Size(75, 28);
        this.btnCancel.Text = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = true;

        // RenameDialog
        this.AcceptButton = this.btnOK;
        this.CancelButton = this.btnCancel;
        this.ClientSize = new Size(384, 111);
        this.Controls.Add(this.labelPrompt);
        this.Controls.Add(this.textBoxName);
        this.Controls.Add(this.btnOK);
        this.Controls.Add(this.btnCancel);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Rename Asset";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
