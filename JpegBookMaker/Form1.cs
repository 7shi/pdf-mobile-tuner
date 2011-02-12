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
            setStatusLabel();
        }

        private void bookPanel1_BoxResize(object sender, EventArgs e)
        {
            setStatusLabel();
        }

        private void rightBindingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menu = rightBindingToolStripMenuItem;
            bookPanel1.RightBinding = menu.Checked = !menu.Checked;
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bookPanel1.SelectAll();
        }

        private void setStatusLabel()
        {
            var bmp = bookPanel1.Panel1.Bitmap;
            if (bmp == null)
                bmp = bookPanel1.Panel2.Bitmap;
            if (bmp == null)
            {
                toolStripStatusLabel2.Text = "";
                return;
            }
            var bsz = bookPanel1.BoxSize;
            var sz2 = Utils.GetSize(bmp.Size, bookPanel1.Panel1.ClientSize);
            var w = bsz.Width * sz2.Width / bmp.Width;
            var h = bsz.Height * sz2.Height / bmp.Height;
            toolStripStatusLabel2.Text = w + "x" + h;
        }
    }
}
