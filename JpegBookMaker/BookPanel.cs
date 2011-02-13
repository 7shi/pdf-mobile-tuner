using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
                if (rightBinding == value) return;
                rightBinding = value;
                adjustPanel();
                var r = panel1.BoxBounds;
                ignore = true;
                panel1.BoxBounds = panel2.BoxBounds;
                panel2.BoxBounds = r;
                ignore = false;
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
            var sz = rightPanel.ClientSize;
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
        public string ImagePath { get; private set; }

        public void Open(string path)
        {
            ImagePath = path;
            ClearPanel();
            RightBinding = false;
            stop = true;
            SetBitmap(null, null);
            lastFocused = null;
            listView1.Items.Clear();
            listView1.BeginUpdate();
            listView1.Items.Add(new ListViewItem("(空)"));
            var files = new List<string>(Directory.GetFiles(path));
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
            listView1.EndUpdate();
            if (listView1.Items.Count > 0)
            {
                var fi = listView1.Items[0];
                listView1.FocusedItem = fi;
                ShowPage(fi);
            }
            setBoxes();
            stop = false;
        }

        private void setBoxes()
        {
            var bmp = panel2.Bitmap;
            if (bmp == null)
            {
                panel1.BoxBounds = panel2.BoxBounds = Rectangle.Empty;
                return;
            }
            int w = bmp.Width, h = bmp.Height;
            int bx = w / 40, by = h / 40, bw = w - bx * 3, bh = h - by * 2;
            BoxSize = new Size(bw, bh);
            ignore = true;
            panel1.BoxBounds = new Rectangle(bx, by, bw, bh);
            panel2.BoxBounds = new Rectangle(bx * 2, by, bw, bh);
            ignore = false;
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
                bmp1 = new Bitmap(Path.Combine(ImagePath, li1.Tag as string));
            if (li2 != null && li2.Tag is string)
                bmp2 = new Bitmap(Path.Combine(ImagePath, li2.Tag as string));
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
            var sz = curvePanel.ClientSize;
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
            curvePanel.Invalidate();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            setLevel();
            panel1.Refresh();
            panel2.Refresh();
            curvePanel.Refresh();

            Cursor.Current = cur;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            setContrast();
            panel1.Refresh();
            panel2.Refresh();
            curvePanel.Refresh();

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

        public event EventHandler BoxResize;

        private Size boxSize;
        public Size BoxSize
        {
            get { return boxSize; }
            set
            {
                if (boxSize == value) return;
                boxSize = value;
                if (BoxResize != null)
                    BoxResize(this, EventArgs.Empty);
            }
        }

        private bool ignore;

        private void panel1_BoxResize(object sender, EventArgs e)
        {
            if (ignore) return;
            var st = panel1.State;
            var b1 = panel1.BoxBounds;
            var b2 = panel2.BoxBounds;
            if (st == 2 || st == 5 || st == 8) b2.X += b2.Width - b1.Width;
            b2.Y = b1.Y;
            BoxSize = b2.Size = b1.Size;
            ignore = true;
            panel2.BoxBounds = b2;
            ignore = false;
            panel1.Refresh();
            panel2.Refresh();
        }

        private void panel2_BoxResize(object sender, EventArgs e)
        {
            if (ignore) return;
            var st = panel2.State;
            var b1 = panel1.BoxBounds;
            var b2 = panel2.BoxBounds;
            if (st == 2 || st == 5 || st == 8) b1.X += b1.Width - b2.Width;
            b1.Y = b2.Y;
            BoxSize = b1.Size = b2.Size;
            ignore = true;
            panel1.BoxBounds = b1;
            ignore = false;
            panel1.Refresh();
            panel2.Refresh();
        }

        public Size DisplayBoxSize
        {
            get
            {
                var bmp = panel1.Bitmap;
                if (bmp == null)
                    bmp = panel2.Bitmap;
                if (bmp == null)
                    return Size.Empty;
                var sz2 = Utils.GetSize(bmp.Size, panel1.ClientSize);
                return new Size(
                    boxSize.Width * sz2.Width / bmp.Width,
                    boxSize.Height * sz2.Height / bmp.Height);
            }
        }

        public void Save(BackgroundWorker bw, int ow, int oh, bool r)
        {
            var dir = ImagePath;
            var outdir = Path.Combine(dir, "output");
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);
            int count = 0, lv = 0, ct = 0;
            Invoke(new Action(() =>
            {
                count = listView1.Items.Count;
                lv = trackBar1.Value;
                ct = trackBar2.Value * 16;
            }));
            int p = 0;
            for (int i = 0, no = 1; i < count; i++)
            {
                if (bw.CancellationPending) break;
                int pp = i * 100 / count;
                if (p != pp) bw.ReportProgress(p = pp);

                string name = "";
                bool gray = false;
                Bitmap src = null;
                Invoke(new Action(() =>
                {
                    var li = listView1.Items[i];
                    name = li.Tag as string;
                    if (name != null)
                    {
                        gray = li.Checked;
                        if (li == panel1.Tag)
                            src = new Bitmap(panel1.Bitmap);
                        else if (li == panel2.Tag)
                            src = new Bitmap(panel2.Bitmap);
                    }
                }));
                if (name == null) continue;
                if (src == null)
                    src = new Bitmap(Path.Combine(ImagePath, name));
                Utils.AdjustLevels(src, gray ? lv : 5);
                var box = (i & 1) == 0 ? panel1.BoxBounds : panel2.BoxBounds;
                using (var bmp = GetBitmap(src, box, ow, oh))
                {
                    Utils.AdjustContrast(bmp, gray ? ct : 128);
                    if (gray) Utils.GrayScale(bmp);
                    if (r) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    var jpg = Path.Combine(outdir, string.Format("{0:0000}.jpg", no++));
                    bmp.Save(jpg, ImageFormat.Jpeg);
                }
                src.Dispose();
            }
        }

        private Bitmap GetBitmap(Bitmap src, Rectangle box, int ow, int oh)
        {
            int w = box.Width, h = box.Height;
            if (ow > 0 && oh > 0)
            {
                var sz = Utils.GetSize(box.Size, new Size(ow, oh));
                w = sz.Width;
                h = sz.Height;
            }
            else if (ow > 0)
            {
                h = h * ow / w;
                w = ow;
            }
            else if (oh > 0)
            {
                w = w * oh / h;
                h = oh;
            }
            var ret = new Bitmap(w, h);
            using (var g = Graphics.FromImage(ret))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                var dest = new Rectangle(0, 0, w, h);
                g.DrawImage(src, dest, box.X, box.Y, box.Width, box.Height, GraphicsUnit.Pixel, new ImageAttributes());
            }
            return ret;
        }
    }
}
