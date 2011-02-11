using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace JpegBookMaker
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
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

            var bmpPath = folderBrowserDialog1.SelectedPath;
            toolStripStatusLabel1.Text = bmpPath;
            bookPanel1.Open(bmpPath);
            rightBindingToolStripMenuItem.Checked = bookPanel1.RightBinding;
        }

        private void bookPanel1_Resize(object sender, EventArgs e)
        {
            var sz = bookPanel1.Panel1;
            toolStripStatusLabel2.Text = sz.Width + "x" + sz.Height;
        }

        private void rightBindingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menu = rightBindingToolStripMenuItem;
            bookPanel1.RightBinding = menu.Checked = !menu.Checked;
        }
    }
}
