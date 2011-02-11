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
    public partial class BookPanel : UserControl
    {
        public PicturePanel Panel1 { get { return panel1; } }
        public PicturePanel Panel2 { get { return panel2; } }

        public BookPanel()
        {
            InitializeComponent();
            adjustPanel();
            setState();
            panel1.MouseWheel += pictureBox_MouseWheel;
            panel2.MouseWheel += pictureBox_MouseWheel;
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
            //listView1.Focus();
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

        private void panel5_Resize(object sender, EventArgs e)
        {
            adjustPanel();
        }

        private void adjustPanel()
        {
            var sz = panel5.ClientSize;
            int w = sz.Width / 2;
            panel1.Bounds = new Rectangle(0, 0, w, sz.Height);
            panel2.Bounds = new Rectangle(w, 0, sz.Width - w, sz.Height);
        }

        private void ClearPanel()
        {
            if (panel1.Bitmap != null) panel1.Bitmap.Dispose();
            if (panel2.Bitmap != null) panel2.Bitmap.Dispose();
            if (bmp1lv != null) bmp1lv.Dispose();
            if (bmp2lv != null) bmp2lv.Dispose();
            panel1.Bitmap = panel2.Bitmap = bmp1lv = bmp2lv = null;
            bmp1fn = bmp2fn = null;
        }

        bool stop = false;

        public void Open(string bmpPath)
        {
            this.bmpPath = bmpPath;
            ClearPanel();
            stop = true;
            SetBitmap(null, null);
            lastFocused = null;
            listView1.Items.Clear();
            listView1.BeginUpdate();
            listView1.Items.Add(new ListViewItem("(空)") { Checked = true });
            var files = new List<string>(Directory.GetFiles(bmpPath));
            files.Sort(new NumberStringComparer());
            foreach (var file in files)
            {
                switch (Path.GetExtension(file).ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".gif":
                    case ".bmp":
                    case ".tif":
                    case ".tiff":
                        var fn = Path.GetFileName(file);
                        var fn2 = Path.GetFileNameWithoutExtension(file);
                        listView1.Items.Add(new ListViewItem(fn2) { Tag = fn, Checked = true });
                        break;
                }
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

        ListViewItem lastFocused;

        private void ShowPage(ListViewItem li)
        {
            if (li == lastFocused) return;
            lastFocused = li;

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

        Bitmap bmp1lv, bmp2lv;
        string bmpPath, bmp1fn, bmp2fn;

        private void SetBitmap(string path1, string path2)
        {
            if (bmp1fn == path1 && bmp2fn == path2) return;

            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            bmp1fn = path1;
            bmp2fn = path2;
            var bmp1 = panel1.Bitmap;
            var bmp2 = panel2.Bitmap;
            if (bmp1 != null) bmp1.Dispose();
            if (bmp2 != null) bmp2.Dispose();
            bmp1 = bmp2 = null;
            if (path1 != null) bmp1 = new Bitmap(Path.Combine(bmpPath, path1));
            if (path2 != null) bmp2 = new Bitmap(Path.Combine(bmpPath, path2));
            panel1.Bitmap = bmp1;
            panel2.Bitmap = bmp2;

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

        private void panel3_Paint(object sender, PaintEventArgs e)
        {
            var sz = panel3.ClientSize;
            int w = sz.Width, h = sz.Height;
            var lt = Utils.GetLevelsTable(trackBar1.Value);
            var ct = Utils.GetContrastTable(trackBar2.Value * 16);
            var pts = new PointF[256];
            for (int i = 0; i < 256; i++)
            {
                var x = ((float)i) * w / 256;
                pts[i] = new PointF(x, ((float)((255 - lt[ct[i]]) * (h - 1))) / 255);
            }
            var sm = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawLines(SystemPens.WindowText, pts);
            e.Graphics.SmoothingMode = sm;
        }

        private void panel3_Resize(object sender, EventArgs e)
        {
            panel3.Invalidate();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            setLevel();
            panel1.Refresh();
            panel2.Refresh();
            panel3.Refresh();

            Cursor.Current = cur;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            setContrast();
            panel1.Refresh();
            panel2.Refresh();
            panel3.Refresh();

            Cursor.Current = cur;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            setState();
            panel1.Refresh();
            panel2.Refresh();

            Cursor.Current = cur;
        }

        private void setState()
        {
            setLevel();
            setContrast();
            panel1.GrayScale = panel2.GrayScale = checkBox1.Checked;
            trackBar1.Enabled = trackBar2.Enabled = checkBox1.Checked;
        }

        private void setLevel()
        {
            panel1.Level = panel2.Level = checkBox1.Checked ? trackBar1.Value : 5;
        }

        private void setContrast()
        {
            panel1.Contrast = panel2.Contrast = checkBox1.Checked ? trackBar2.Value * 16 : 128;
        }
    }
}
