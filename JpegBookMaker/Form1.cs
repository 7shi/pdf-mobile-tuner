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
        private int[,] gamma = new int[11, 256];

        public Form1()
        {
            InitializeComponent();
            AdjustPanel();
            pictureBox1.MouseWheel += pictureBox_MouseWheel;
            pictureBox2.MouseWheel += pictureBox_MouseWheel;
#if DEBUG
            folderBrowserDialog1.SelectedPath = @"D:\pdf2png";
#endif
            var v = new double[11];
            for (int i = 0; i <= 255; i++)
            {
                v[0] = ((double)i) * 2 / 255 - 1;
                v[5] = Math.Sin((((double)i) * 180 / 255 - 90) * Math.PI / 180);
                v[10] = i < 128 ? -1.0 : 1.0;
                for (int j = 1; j <= 4; j++)
                {
                    var d = ((double)j) / 5;
                    v[j] = v[0] * (1 - d) + v[5] * d;
                    var val = 1.0 - Math.Abs(v[j + 4]);
                    v[j + 5] = (1.0 - val * val) * v[10];
                }
                for (int j = 0; j <= 10; j++)
                {
                    gamma[j, i] = (int)(((v[j] + 1) * 255 + 1) / 2);
                }
            }
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
                    if (i == 0) ok = false;
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
                ClearList(false, true);
                fi.Selected = true;
                stop = false;
            }
            listView1.Focus();
        }

        private void ClearList(bool back, bool sel)
        {
            var bc = listView1.BackColor;
            foreach (ListViewItem li in listView1.Items)
            {
                if (back && li.BackColor != bc) li.BackColor = bc;
                if (sel && li.Selected) li.Selected = false;
            }
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
            ClearList(true, false);
            if (li1 != null)
            {
                li1.BackColor = SystemColors.ControlLight;
                path1 = li1.Tag.ToString();
            }
            if (li2 != null)
            {
                li2.BackColor = SystemColors.ControlLight;
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

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            var sz = panel1.ClientSize;
            int w = sz.Width, h = sz.Height;
            for (int x = 0, yy = 0; x < w; x++)
            {
                int y = (255 - gamma[trackBar1.Value, x * 256 / w]) * h / 256;
                if (x > 0) e.Graphics.DrawLine(SystemPens.WindowText, x - 1, yy, x, y);
                yy = y;
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            panel1.Invalidate();
        }
    }
}
