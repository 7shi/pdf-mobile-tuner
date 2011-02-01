using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
            AdjustPanel();
            pictureBox1.MouseWheel += pictureBox_MouseWheel;
            pictureBox2.MouseWheel += pictureBox_MouseWheel;
        }

        private void pictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            var fi = listView1.FocusedItem;
            if (fi == null && listView1.Items.Count > 0)
                fi = listView1.Items[0];
            if (fi == null) return;

            int index = fi.Index;
            int count = listView1.Items.Count;
            bool ok = true;
            for (int i = 0; i < 2; )
            {
                if (e.Delta > 0) index--; else index++;
                if (index < 0 || index >= count)
                {
                    ok = false;
                    break;
                }
                if (listView1.Items[index].Checked) i++;
            }
            if (ok)
            {
                stop = true;
                fi = listView1.Items[index];
                listView1.FocusedItem = fi;
                listView1.EnsureVisible(index);
                ShowPage(fi);
                stop = false;
            }
            listView1.Focus();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void splitContainer1_Panel2_Resize(object sender, EventArgs e)
        {
            AdjustPanel();
        }

        private void AdjustPanel()
        {
            var sz = splitContainer1.Panel2.ClientSize;
            int w = sz.Width / 2;
            pictureBox1.Bounds = new Rectangle(0, 0, w, sz.Height);
            pictureBox2.Bounds = new Rectangle(w, 0, sz.Width - w, sz.Height);
        }

        bool stop = false;

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog(this) != DialogResult.OK) return;

            stop = true;
            SetBitmap(null, null);
            listView1.Items.Clear();
            listView1.BeginUpdate();
            var dir = folderBrowserDialog1.SelectedPath;
            toolStripStatusLabel1.Text = dir;
            var files = Directory.GetFiles(dir, "*.jpg");
            foreach (var file in files)
            {
                var fn = Path.GetFileNameWithoutExtension(file);
                listView1.Items.Add(new ListViewItem(fn) { Tag = file, Checked = true });
            }
            listView1.EndUpdate();
            if (listView1.Items.Count > 0)
            {
                var fi = listView1.Items[0];
                listView1.FocusedItem = fi;
                ShowPage(fi);
            }
            stop = false;
        }

        private void ShowPage(ListViewItem li)
        {
            if (li == null)
            {
                SetBitmap(null, null);
                return;
            }

            var stp = stop;
            stop = true;
            var first = true;
            ListViewItem li1 = null, li2 = null;
            foreach (ListViewItem li3 in listView1.Items)
            {
                if (!li3.Checked) continue;
                if (!first && li1 == null)
                    li1 = li3;
                else
                {
                    li2 = li3;
                    if (li3.Index >= li.Index) break;
                    li1 = li2 = null;
                    first = false;
                }
            }
            string path1 = null, path2 = null;
            foreach (ListViewItem li4 in listView1.Items)
                if (li4.Selected) li4.Selected = false;
            if (li1 != null)
            {
                li1.Selected = true;
                path1 = li1.Tag.ToString();
            }
            if (li2 != null)
            {
                li2.Selected = true;
                path2 = li2.Tag.ToString();
            }
            SetBitmap(path1, path2);
            stop = stp;
        }

        string bmpPath1, bmpPath2;

        private void SetBitmap(string path1, string path2)
        {
            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            var b1 = pictureBox1.Image;
            var b2 = pictureBox2.Image;
            Image bmp1 = null, bmp2 = null;
            if (path1 == bmpPath1)
                bmp1 = b1;
            else if (path1 == bmpPath2)
                bmp1 = b2;
            else if (path1 != null)
                bmp1 = new Bitmap(path1);
            if (path2 == bmpPath2)
                bmp2 = b2;
            else if (path2 == bmpPath1)
                bmp2 = b1;
            else if (path2 != null)
                bmp2 = new Bitmap(path2);
            bmpPath1 = path1;
            bmpPath2 = path2;
            if (b1 != bmp1) pictureBox1.Image = bmp1;
            if (b2 != bmp2) pictureBox2.Image = bmp2;
            if (b1 != null && b1 != bmp1 && b1 != bmp2) b1.Dispose();
            if (b2 != null && b2 != bmp1 && b2 != bmp2) b2.Dispose();

            Cursor.Current = cur;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!stop) ShowPage(listView1.FocusedItem);
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (!stop) ShowPage(listView1.FocusedItem);
        }
    }
}
