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

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog(this) != DialogResult.OK) return;

            var path = folderBrowserDialog1.SelectedPath;
            toolStripStatusLabel1.Text = path;
            bookPanel1.Open(path);
            rightBindingToolStripMenuItem.Checked = bookPanel1.RightBinding;
            saveToolStripMenuItem.Enabled = true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveDialog.ShowDialog(this);
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
    }
}
