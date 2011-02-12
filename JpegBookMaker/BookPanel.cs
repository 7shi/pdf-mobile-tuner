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
        private bool rightBinding;
        public bool RightBinding
        {
            get { return rightBinding; }
            set
            {
                rightBinding = value;
                adjustPanel();
            }
        }

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
            if (e.Delta > 0)
                idx = Math.Max(idx - 2, 0);
            else
                idx = Math.Min(idx + 2, listView1.Items.Count - 1);
            if (index != idx)
            {
                stop = true;
                fi = listView1.Items[idx];
                listView1.FocusedItem = fi;
                listView1.EnsureVisible(idx);
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

        private void panel5_Resize(object sender, EventArgs e)
        {
            adjustPanel();
        }

        private void adjustPanel()
        {
            var sz = panel5.ClientSize;
            int w = sz.Width / 2;
            var p1 = !rightBinding ? panel1 : panel2;
            var p2 = !rightBinding ? panel2 : panel1;
            p1.Bounds = new Rectangle(0, 0, w, sz.Height);
            p2.Bounds = new Rectangle(w, 0, sz.Width - w, sz.Height);
        }

        private void ClearPanel()
        {
            if (panel1.Bitmap != null) panel1.Bitmap.Dispose();
            if (panel2.Bitmap != null) panel2.Bitmap.Dispose();
            panel1.Bitmap = panel2.Bitmap = null;
            panel1.Tag = panel2.Tag = null;
        }

        private bool stop = false;
        private string bmpPath;

        public void Open(string bmpPath)
        {
            this.bmpPath = bmpPath;
            ClearPanel();
            RightBinding = false;
            stop = true;
            SetBitmap(null, null);
            lastFocused = null;
            listView1.Items.Clear();
            listView1.BeginUpdate();
            listView1.Items.Add(new ListViewItem("(空)"));
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
                        listView1.Items.Add(new ListViewItem(fn2) { Tag = fn });
                        break;
                }
            }
            listView1.Items.Add(new ListViewItem("(空)"));
            listView1.EndUpdate();
            if (listView1.Items.Count > 0)
            {
                var fi = listView1.Items[0];
                listView1.FocusedItem = fi;
                ShowPage(fi);
            }
            stop = false;
        }

        private ListViewItem lastFocused;

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
                if (li1 == null)
                    li1 = li3;
                else
                {
                    li2 = li3;
                    if (li3.Index >= li.Index) break;
                    li1 = li2 = null;
                }
            }
            ClearList(true, false);
            if (li1 != null)
                li1.BackColor = SystemColors.ControlLight;
            if (li2 != null)
                li2.BackColor = SystemColors.ControlLight;
            SetBitmap(li1, li2);
            stop = stp;
        }

        private void SetBitmap(ListViewItem li1, ListViewItem li2)
        {
            if (panel1.Tag == li1 && panel2.Tag == li2) return;

            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            panel1.Tag = li1;
            panel2.Tag = li2;
            var bmp1 = panel1.Bitmap;
            var bmp2 = panel2.Bitmap;
            if (bmp1 != null) bmp1.Dispose();
            if (bmp2 != null) bmp2.Dispose();
            bmp1 = bmp2 = null;
            if (li1 != null && li1.Tag is string)
                bmp1 = new Bitmap(Path.Combine(bmpPath, li1.Tag as string));
            if (li2 != null && li2.Tag is string)
                bmp2 = new Bitmap(Path.Combine(bmpPath, li2.Tag as string));
            panel1.Bitmap = bmp1;
            panel2.Bitmap = bmp2;
            setState();

            Cursor.Current = cur;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!stop) ShowPage(listView1.FocusedItem);
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (stop) return;
            if (e.Item == panel1.Tag || e.Item == panel2.Tag)
            {
                setState();
                panel1.Refresh();
                panel2.Refresh();
            }
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
            var li1 = panel1.Tag as ListViewItem;
            var li2 = panel2.Tag as ListViewItem;
            if (li1 != null) panel1.GrayScale = li1.Checked;
            if (li2 != null) panel2.GrayScale = li2.Checked;
        }

        private void setLevel()
        {
            var li1 = panel1.Tag as ListViewItem;
            var li2 = panel2.Tag as ListViewItem;
            if (li1 != null) panel1.Level = li1.Checked ? trackBar1.Value : 5;
            if (li2 != null) panel2.Level = li2.Checked ? trackBar1.Value : 5;
        }

        private void setContrast()
        {
            var li1 = panel1.Tag as ListViewItem;
            var li2 = panel2.Tag as ListViewItem;
            if (li1 != null) panel1.Contrast = li1.Checked ? trackBar2.Value * 16 : 128;
            if (li2 != null) panel2.Contrast = li2.Checked ? trackBar2.Value * 16 : 128;
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            contextMenuStrip1.Enabled = listView1.SelectedItems.Count > 0;
            int count = listView1.Items.Count;
            var sels = listView1.SelectedItems;
            int min = count - 1, max = 0;
            foreach (ListViewItem li in sels)
            {
                var idx = li.Index;
                if (min > idx) min = idx;
                if (max < idx) max = idx;
            }
            upToolStripMenuItem.Enabled = min > 0;
            downToolStripMenuItem.Enabled = max < count - 1;
        }

        private void upToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var sels = listView1.SelectedItems;
            if (sels.Count == 0) return;

            int min = sels[0].Index;
            if (min < 1) return;

            moveItems(min - 1);
        }

        private void downToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var sels = listView1.SelectedItems;
            if (sels.Count == 0) return;

            int min = sels[0].Index;
            int max = sels[sels.Count - 1].Index;
            if (max >= listView1.Items.Count - 1) return;

            moveItems(min + 1);
        }

        private void moveItems(int p)
        {
            var sels = listView1.SelectedItems;
            var fi = listView1.FocusedItem;

            stop = true;
            listView1.BeginUpdate();
            var list = new List<ListViewItem>();
            foreach (ListViewItem li in listView1.Items)
                list.Add(li);
            foreach (ListViewItem li in sels)
                list.Remove(li);
            foreach (ListViewItem li in sels)
                list.Insert(p++, li);
            listView1.Items.Clear();
            foreach (var li in list)
                listView1.Items.Add(li);
            listView1.FocusedItem = fi;
            lastFocused = null;
            ShowPage(fi);
            listView1.EndUpdate();
            stop = false;
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var sels = listView1.SelectedItems;
            if (sels.Count == 0) return;

            int count = listView1.Items.Count;
            int min = sels[0].Index;

            stop = true;
            listView1.BeginUpdate();
            foreach (ListViewItem li in sels)
                listView1.Items.Remove(li);
            var fidx = Math.Min(min, listView1.Items.Count - 1);
            if (fidx >= 0)
            {
                var fi = listView1.Items[fidx];
                listView1.FocusedItem = fi;
                ShowPage(fi);
            }
            else
                ShowPage(null);
            listView1.EndUpdate();
            stop = false;
        }

        public void SelectAll()
        {
            stop = true;
            listView1.BeginUpdate();
            foreach (ListViewItem li in listView1.Items)
            {
                if (!li.Selected) li.Selected = true;
            }
            listView1.EndUpdate();
            stop = false;
        }
    }
}
