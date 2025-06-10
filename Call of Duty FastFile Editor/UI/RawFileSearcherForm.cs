using Call_of_Duty_FastFile_Editor.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.DirectoryServices;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Call_of_Duty_FastFile_Editor.UI
{
    public partial class RawFileSearcherForm : Form
    {
        private List<RawFileNode> _loadedRawFiles;
        public RawFileSearcherForm()
        {
            InitializeComponent();
        }

        public RawFileSearcherForm(List<RawFileNode> selectedFileNodes)
        {
            InitializeComponent();
            _loadedRawFiles = selectedFileNodes;
        }

        public void searchButton_Click(object sender, EventArgs e)
        {
            List<SearchResult> results = SearchRawFileContent(searchTextBox.Text);
            PopulateListView(results);
        }

        private List<SearchResult> SearchRawFileContent(string searchText)
        {
            List<SearchResult> results = new List<SearchResult>();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                MessageBox.Show("Search text cannot be empty.", "Search Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return results;
            }

            foreach (var fileNode in _loadedRawFiles)
            {
                if (!string.IsNullOrEmpty(fileNode.RawFileContent))
                {
                    string content = fileNode.RawFileContent;
                    int startIndex = 0;

                    // Find all occurrences of searchText within the file content
                    while ((startIndex = content.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase)) != -1)
                    {
                        results.Add(new SearchResult
                        {
                            FileName = fileNode.FileName,
                            Offset = startIndex,
                            MatchedText = searchText
                        });

                        // Move startIndex forward to continue searching after this match
                        startIndex += searchText.Length;
                    }
                }
            }

            return results;
        }

        private void PopulateListView(List<SearchResult> results)
        {
            resultsListView.Items.Clear(); // Clear old results

            if (results.Count == 0)
            {
                MessageBox.Show("No matches found.", "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var result in results)
            {
                ListViewItem item = new ListViewItem(result.FileName);
                item.SubItems.Add(result.Offset.ToString());
                item.SubItems.Add(result.MatchedText);
                resultsListView.Items.Add(item);
            }
        }

        private void RawFileSearcherForm_Resize(object sender, EventArgs e)
        {
            int formWidth = this.ClientSize.Width;
            int formHeight = this.ClientSize.Height;

            // Adjust searchTextBox width dynamically
            searchTextBox.Width = formWidth - searchButton.Width - 30;

            // Keep searchButton at the right
            searchButton.Left = formWidth - searchButton.Width - 10;

            // Adjust resultsListView size dynamically
            resultsListView.Width = formWidth - 24;  // 12px padding on both sides
            resultsListView.Height = formHeight - searchTextBox.Height - 40;

            // Resize columns proportionally
            if (resultsListView.Columns.Count > 0)
            {
                resultsListView.Columns[0].Width = (int)(formWidth * 0.4); // 40% width for File Name
                resultsListView.Columns[1].Width = (int)(formWidth * 0.2); // 20% width for AssetPoolRecordOffset
                resultsListView.Columns[2].Width = (int)(formWidth * 0.4); // 40% width for Matched Text
            }
        }
    }
    public class SearchResult
    {
        public string FileName { get; set; }
        public int Offset { get; set; }
        public string MatchedText { get; set; }

        public override string ToString()
        {
            return $"Found '{MatchedText}' in {FileName} at offset {Offset}";
        }
    }
}
