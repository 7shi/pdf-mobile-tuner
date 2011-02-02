using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace JpegBookMaker
{
    public partial class Form1 : Form
    {
        private byte[][] gamma = new byte[11][];

        public Form1()
        {
            InitializeComponent();
            AdjustPanel();
            pictureBox1.MouseWheel += pictureBox_MouseWheel;
            pictureBox2.MouseWheel += pictureBox_MouseWheel;
#if DEBUG
            folderBrowserDialog1.SelectedPath = @"D:\pdf2jpg";
#endif
            var v = new double[11];
            for (int i = 0; i <= 10; i++)
                gamma[i] = new byte[256];
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
                    gamma[j][i] = (byte)(((v[j] + 1) * 255 + 1) / 2);
                }
            }
        }

        private void pictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            var fi = listView1.FocusedItem;
            if (fi == null && listView1.Items.Count > 0)
                fi = listView1.Items[0];
            if (fi == null) return;

            int index = fi.Index, idx = index;
            int count = listView1.Items.Count;
            bool ok = true;
            for (int i = 0; i < 2; )
            {
                if (e.Delta > 0) idx--; else idx++;
                if (idx < 0 || idx >= count)
                {
                    if (i == 0) ok = false;
                    break;
                }
                if (listView1.Items[index].Checked)
                {
                    index = idx;
                    i++;
                }
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

        private void ClearPanel()
        {
            var b1 = pictureBox1.Image;
            var b2 = pictureBox2.Image;
            pictureBox1.Image = null;
            pictureBox2.Image = null;
            if (b1 != null) b1.Dispose();
            if (b2 != null) b2.Dispose();
            if (bmp1 != null) bmp1.Dispose();
            if (bmp2 != null) bmp2.Dispose();
            bmp1 = bmp2 = null;
            bmpPath1 = bmpPath2 = null;
        }

        bool stop = false;

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog(this) != DialogResult.OK) return;

            ClearPanel();
            stop = true;
            SetBitmap(null, null);
            listView1.Items.Clear();
            listView1.BeginUpdate();
            listView1.Items.Add(new ListViewItem("(空)") { Checked = true });
            var dir = folderBrowserDialog1.SelectedPath;
            toolStripStatusLabel1.Text = dir;
            var files = Directory.GetFiles(dir, "*.jpg");
            foreach (var file in files)
            {
                var fn = Path.GetFileNameWithoutExtension(file);
                listView1.Items.Add(new ListViewItem(fn) { Tag = file, Checked = true });
            }
            listView1.Items.Add(new ListViewItem("(空)") { Checked = true });
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
            ListViewItem li1 = null, li2 = null;
            foreach (ListViewItem li3 in listView1.Items)
            {
                if (!li3.Checked) continue;
                if (li1 == null)
                    li1 = li3;
                else
                {
                    li2 = li3;
                    if (li3.Index >= li.Index) break;
                    li1 = li2 = null;
                }
            }
            string path1 = null, path2 = null;
            ClearList(true, false);
            if (li1 != null)
            {
                li1.BackColor = SystemColors.ControlLight;
                path1 = li1.Tag as string;
            }
            if (li2 != null)
            {
                li2.BackColor = SystemColors.ControlLight;
                path2 = li2.Tag as string;
            }
            SetBitmap(path1, path2);
            stop = stp;
        }

        Bitmap bmp1, bmp2;
        string bmpPath1, bmpPath2;

        private void SetBitmap(string path1, string path2)
        {
            if (bmpPath1 == path1 && bmpPath2 == path2) return;

            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            bmpPath1 = path1;
            bmpPath2 = path2;
            if (bmp1 != null) bmp1.Dispose();
            if (bmp2 != null) bmp2.Dispose();
            bmp1 = path1 != null ? new Bitmap(path1) : null;
            bmp2 = path2 != null ? new Bitmap(path2) : null;
            SetBitmap();

            Cursor.Current = cur;
        }

        private void SetBitmap()
        {
            var b1 = pictureBox1.Image;
            var b2 = pictureBox2.Image;
            pictureBox1.Image = MakeBitmap(bmp1);
            pictureBox2.Image = MakeBitmap(bmp2);
            if (b1 != null) b1.Dispose();
            if (b2 != null) b2.Dispose();
        }

        private Bitmap MakeBitmap(Bitmap bmp)
        {
            if (bmp == null) return null;

            int w = bmp.Width, h = bmp.Height;
            var r = new Rectangle(0, 0, w, h);

            var data1 = bmp.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var buf = new byte[w * h * 3];
            Marshal.Copy(data1.Scan0, buf, 0, buf.Length);
            bmp.UnlockBits(data1);

            if (trackBar1.Value > 0)
            {
                var g = gamma[trackBar1.Value];
                for (int i = 0; i < buf.Length; i++)
                    buf[i] = g[buf[i]];
            }

            var ret = new Bitmap(w, h);
            var data2 = ret.LockBits(r, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            Marshal.Copy(buf, 0, data2.Scan0, buf.Length);
            ret.UnlockBits(data2);

            return ret;
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
            var g = gamma[trackBar1.Value];
            var pts = new PointF[w];
            for (int x = 0; x < w; x++)
                pts[x] = new PointF(x, ((float)((255 - g[x * 256 / w]) * h)) / 256);
            var sm = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawLines(SystemPens.WindowText, pts);
            e.Graphics.SmoothingMode = sm;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            panel1.Refresh();
            SetBitmap();

            Cursor.Current = cur;
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            panel1.Invalidate();
        }
    }
}
