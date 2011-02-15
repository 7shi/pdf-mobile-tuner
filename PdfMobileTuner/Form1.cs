using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CommonLib;
using JpegBookMaker;

namespace PdfMobileTuner
{
    public partial class Form1 : Form
    {
        private SaveDialog saveDialog = new SaveDialog();
        private BookPanelHelp help;

        public Form1()
        {
            InitializeComponent();
            saveDialog.BookPanel = bookPanel1;
#if DEBUG
            folderBrowserDialog1.SelectedPath = @"E:\temp";
#endif
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
                openPDF(openFileDialog1.FileName);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void openDirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog(this) != DialogResult.OK) return;

            var path = folderBrowserDialog1.SelectedPath;
            toolStripStatusLabel1.Text = path;
            bookPanel1.Open(path);
            rightBindingToolStripMenuItem.Checked = bookPanel1.RightBinding;
            saveDirToolStripMenuItem.Enabled = true;
        }

        private void saveDirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveDialog.ShowDialog(this);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bookPanel1.SelectAll();
        }

        private void rightBindingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menu = rightBindingToolStripMenuItem;
            bookPanel1.RightBinding = menu.Checked = !menu.Checked;
        }

        private void viewHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (help == null)
            {
                help = new BookPanelHelp();
                help.Owner = this;
                var cs = ClientSize;
                help.Location = PointToScreen(new Point(cs.Width - help.Width, 0));
            }
            help.Show();
        }

        private void bookPanel1_Resize(object sender, EventArgs e)
        {
            setStatusLabel();
        }

        private void bookPanel1_BoxResize(object sender, EventArgs e)
        {
            setStatusLabel();
        }

        private void setStatusLabel()
        {
            var sz = bookPanel1.DisplayBoxSize;
            if (sz.Width == 0)
            {
                toolStripStatusLabel2.Text = "";
                toolStripStatusLabel3.Text = "";
            }
            else
            {
                double w = sz.Width, h = sz.Height;
                toolStripStatusLabel2.Text = w + "x" + h;
                toolStripStatusLabel3.Text = (h / w).ToString("0.00");
            }
        }

        private void openPDF(string pdf)
        {
            toolStripStatusLabel1.Text = Path.GetFileNameWithoutExtension(pdf);
            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Visible = true;
            menuStrip1.Enabled = false;
            analyzerPanel1.OpenPDF(pdf);
        }

        private void analyzerPanel1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void analyzerPanel1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripProgressBar1.Visible = false;
            menuStrip1.Enabled = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (analyzerPanel1.IsBusy)
            {
                MessageBox.Show(
                    this, "処理中のため閉じることができません。", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                e.Cancel = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            analyzerPanel1.ClosePDF();
        }
    }
}
