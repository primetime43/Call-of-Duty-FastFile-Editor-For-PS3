namespace Call_of_Duty_FastFile_Editor.UI
{
    partial class RawFileSearcherForm
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
            searchTextBox = new TextBox();
            searchButton = new Button();
            resultsListView = new ListView();
            SuspendLayout();

            // 
            // searchTextBox
            // 
            searchTextBox.Location = new Point(13, 13);
            searchTextBox.Margin = new Padding(4);
            searchTextBox.Name = "searchTextBox";
            searchTextBox.PlaceholderText = "Enter the text you'd like to search for...";
            searchTextBox.Size = new Size(319, 29);
            searchTextBox.TabIndex = 0;
            searchTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; // Resizes with form

            // 
            // searchButton
            // 
            searchButton.Location = new Point(352, 13);
            searchButton.Name = "searchButton";
            searchButton.Size = new Size(128, 29);
            searchButton.TabIndex = 2;
            searchButton.Text = "Search";
            searchButton.UseVisualStyleBackColor = true;
            searchButton.Click += searchButton_Click;
            searchButton.Anchor = AnchorStyles.Top | AnchorStyles.Right; // Stays on top-right

            // 
            // resultsListView
            // 
            resultsListView.Location = new Point(12, 49);
            resultsListView.Name = "resultsListView";
            resultsListView.Size = new Size(468, 210);
            resultsListView.TabIndex = 3;
            resultsListView.UseCompatibleStateImageBehavior = false;
            resultsListView.View = View.Details;
            resultsListView.FullRowSelect = true;
            resultsListView.GridLines = true;
            resultsListView.Columns.Add("File Name", 200);
            resultsListView.Columns.Add("Character Position AssetPoolRecordOffset", 100);
            resultsListView.Columns.Add("Matched Text", 250);
            resultsListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; // Expands when resized

            // 
            // RawFileSearcherForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(492, 271);
            Controls.Add(resultsListView);
            Controls.Add(searchButton);
            Controls.Add(searchTextBox);
            Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Margin = new Padding(4);
            Name = "RawFileSearcherForm";
            Text = "Raw File Text to Search";
            Resize += RawFileSearcherForm_Resize; // Attach the resize event
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox searchTextBox;
        private Button searchButton;
        private ListView resultsListView;
    }
}