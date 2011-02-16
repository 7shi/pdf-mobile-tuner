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
using PdfLib;

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
                setBoxBounds();
            }
        }

        public event EventHandler BoxResize;

        protected virtual void OnBoxResize(EventArgs e)
        {
            if (BoxResize != null) BoxResize(this, e);
        }

        public PicturePanel Panel1 { get { return panel1; } }
        public PicturePanel Panel2 { get { return panel2; } }

        private int defaultLevel, defaultContrast;
        private PageInfo common = new PageInfo();

        public BookPanel()
        {
            InitializeComponent();
            defaultLevel = trackBar1.Value;
            defaultContrast = trackBar2.Value;
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

        private void rightPanel_Resize(object sender, EventArgs e)
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

        private void clearPanel()
        {
            if (panel1.Bitmap != null) panel1.Bitmap.Dispose();
            if (panel2.Bitmap != null) panel2.Bitmap.Dispose();
            panel1.Bitmap = panel2.Bitmap = null;
            panel1.Tag = panel2.Tag = null;
        }

        private bool stop = false;

        private PdfDocument doc;

        public void OpenPDF(PdfDocument doc)
        {
            CloseDir();
            this.doc = doc;
            rightBinding = doc.RightBinding;
            adjustPanel();

            stop = true;
            listView1.BeginUpdate();
            listView1.Items.Add(new ListViewItem("(空)") { Checked = true });
            int n = doc.PageCount;
            for (int i = 1; i <= n; i++)
            {
                var page = doc.GetPage(i);
                var img = PageInfo.GetImage(page);
                if (img != null)
                {
                    var pi = new PageInfo(page);
                    var pn = string.Format("{0:0000}", i);
                    listView1.Items.Add(new ListViewItem(pn) { Tag = pi, Checked = true });
                }
            }
            listView1.EndUpdate();
            stop = false;

            ShowPage(listView1.Items[0]);
            checkBox1.Enabled = true;
            panel1.Focus();
        }

        private string imagePath;

        public void OpenDir(string path)
        {
            CloseDir();
            adjustPanel();
            imagePath = path;

            stop = true;
            listView1.BeginUpdate();
            listView1.Items.Add(new ListViewItem("(空)") { Checked = true });
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
                        var pi = new PageInfo(Path.Combine(path, fn));
                        listView1.Items.Add(new ListViewItem(fn2) { Tag = pi, Checked = true });
                        break;
                }
            }
            listView1.EndUpdate();
            stop = false;

            ShowPage(listView1.Items[0]);
            checkBox1.Enabled = true;
            panel1.Focus();
        }

        public void CloseDir()
        {
            clearPanel();
            doc = null;
            imagePath = null;
            RightBinding = false;
            stop = true;
            ignore = true;
            SetBitmap(null, null);
            lastFocused = null;
            common.Level = defaultLevel;
            common.Contrast = defaultContrast;
            common.Bounds = Rectangle.Empty;
            trackBar1.Value = defaultLevel;
            trackBar2.Value = defaultContrast;
            checkBox1.Enabled = false;
            checkBox1.Checked = false;
            lastFocused = null;
            OnBoxResize(EventArgs.Empty);
            listView1.Items.Clear();
            ignore = false;
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
            common.Bounds = new Rectangle(bx, by, bw, bh);
        }

        private ListViewItem lastFocused;

        private void ShowPage(ListViewItem li)
        {
            if (li == lastFocused) return;
            lastFocused = li;

            if (li == null)
            {
                SetBitmap(null, null);
                panel1.Selected = panel2.Selected = false;
            }
            else
            {
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
                panel1.Selected = li == li1;
                panel2.Selected = li == li2;
                stop = stp;
            }
            OnBoxResize(EventArgs.Empty);
        }

        private void SetBitmap(ListViewItem li1, ListViewItem li2)
        {
            if (panel1.Tag == li1 && panel2.Tag == li2) return;

            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            panel1.Tag = li1;
            panel2.Tag = li2;
            if (panel1.Bitmap != null) panel1.Bitmap.Dispose();
            if (panel2.Bitmap != null) panel2.Bitmap.Dispose();
            panel1.Bitmap = panel2.Bitmap = null;
            Bitmap bmp1 = null, bmp2 = null;
            if (li1 != null && li1.Tag is PageInfo)
                bmp1 = (li1.Tag as PageInfo).GetBitmap();
            if (li2 != null && li2.Tag is PageInfo)
                bmp2 = (li2.Tag as PageInfo).GetBitmap();
            panel1.Bitmap = bmp1;
            panel2.Bitmap = bmp2;
            if (common.Bounds.IsEmpty) setBoxes();
            setBoxBounds();
            setState();

            Cursor.Current = cur;
        }

        private void setBoxBounds()
        {
            ignore = true;
            panel1.BoxBounds = getBounds(panel1);
            panel2.BoxBounds = getBounds(panel2);
            ignore = false;
        }

        private Rectangle getBounds(PicturePanel pp)
        {
            var li = pp.Tag as ListViewItem;
            if (li == null) return Rectangle.Empty;

            if (li.Checked)
            {
                if ((rightBinding && pp == panel1) || (!rightBinding && pp == panel2))
                    return mirror(common.Bounds, pp.Bitmap);
                else
                    return common.Bounds;
            }
            else
            {
                var pi = li.Tag as PageInfo;
                return pi != null ? pi.Bounds : Rectangle.Empty;
            }
        }

        private Rectangle mirror(Rectangle r, Bitmap bmp)
        {
            if (bmp == null)
                return Rectangle.Empty;
            else
                return new Rectangle(bmp.Width - r.Right, r.Y, r.Width, r.Height);
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (stop) return;

            ShowPage(listView1.FocusedItem);
            setControls();
        }

        private void setControls()
        {
            ignore = true;
            var pi = getInfo(lastFocused) ?? common;
            trackBar1.Value = pi.Level;
            trackBar2.Value = pi.Contrast;
            checkBox1.Checked = pi.IsGrayScale;
            tabPage1.Text = pi == common ? "プロパティ（共通）" : "プロパティ（個別）";
            ignore = false;
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (stop || e.Item.Tag == null) return;
            PicturePanel pp;
            if (e.Item == panel1.Tag)
                pp = panel1;
            else if (e.Item == panel2.Tag)
                pp = panel2;
            else
                return;
            if (!e.Item.Checked)
            {
                var pi = e.Item.Tag as PageInfo;
                pi.Bounds = common.Bounds;
                pi.Level = trackBar1.Value;
                pi.Contrast = trackBar2.Value;
                pi.IsGrayScale = checkBox1.Checked;
            }
            setControls();
            ignore = true;
            pp.BoxBounds = getBounds(pp);
            ignore = false;
            setState();
            panel1.Refresh();
            panel2.Refresh();
        }

        private void curvePanel_Paint(object sender, PaintEventArgs e)
        {
            var sz = curvePanel.ClientSize;
            int w = sz.Width, h = sz.Height;
            var lt = Utils.GetLevelsTable(checkBox1.Checked ? trackBar1.Value : 5);
            var ct = Utils.GetContrastTable(checkBox1.Checked ? trackBar2.Value * 16 : 128);
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

        private void curvePanel_Resize(object sender, EventArgs e)
        {
            curvePanel.Invalidate();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (ignore) return;

            var pi = getInfo(lastFocused);
            if (pi == null) return;

            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            pi.Level = trackBar1.Value;
            setLevel();
            panel1.Refresh();
            panel2.Refresh();
            curvePanel.Refresh();

            Cursor.Current = cur;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            if (ignore) return;

            var pi = getInfo(lastFocused);
            if (pi == null) return;

            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            pi.Contrast = trackBar2.Value;
            setContrast();
            panel1.Refresh();
            panel2.Refresh();
            curvePanel.Refresh();

            Cursor.Current = cur;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (ignore) return;

            var cur = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            trackBar1.Enabled = trackBar2.Enabled = checkBox1.Checked;
            var pi = getInfo(lastFocused);
            if (pi != null)
            {
                pi.IsGrayScale = checkBox1.Checked;
                setState();
                panel1.Refresh();
                panel2.Refresh();
                curvePanel.Refresh();
            }

            Cursor.Current = cur;
        }

        private void setState()
        {
            setLevel();
            setContrast();
            var pi1 = getInfo(panel1.Tag as ListViewItem);
            var pi2 = getInfo(panel2.Tag as ListViewItem);
            if (pi1 != null) panel1.GrayScale = pi1.IsGrayScale;
            if (pi2 != null) panel2.GrayScale = pi2.IsGrayScale;
        }

        private void setLevel()
        {
            var pi1 = getInfo(panel1.Tag as ListViewItem);
            var pi2 = getInfo(panel2.Tag as ListViewItem);
            if (pi1 != null) panel1.Level = pi1.IsGrayScale ? pi1.Level : 5;
            if (pi2 != null) panel2.Level = pi2.IsGrayScale ? pi2.Level : 5;
        }

        private void setContrast()
        {
            var pi1 = getInfo(panel1.Tag as ListViewItem);
            var pi2 = getInfo(panel2.Tag as ListViewItem);
            if (pi1 != null) panel1.Contrast = pi1.IsGrayScale ? pi1.Contrast * 16 : 128;
            if (pi2 != null) panel2.Contrast = pi2.IsGrayScale ? pi2.Contrast * 16 : 128;
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

        private bool ignore;

        private void panel1_BoxResize(object sender, EventArgs e)
        {
            doBoxResize(panel1);
        }

        private void panel2_BoxResize(object sender, EventArgs e)
        {
            doBoxResize(panel2);
        }

        private void doBoxResize(PicturePanel p1)
        {
            if (ignore) return;

            var isLeft = isLeftPanel(p1);
            var p2 = p1 == panel1 ? panel2 : panel1;
            var li1 = p1.Tag as ListViewItem;
            var li2 = p2.Tag as ListViewItem;
            if (li1 == null || p1.Bitmap == null) return;

            var flg = li2 != null && li2.Checked && p2.Bitmap != null;
            var r1 = p1.BoxBounds;
            if (li1.Checked)
            {
                ignore = true;
                if (isLeft)
                {
                    common.Bounds = r1;
                    if (flg) p2.BoxBounds = mirror(r1, p2.Bitmap);
                }
                else
                {
                    common.Bounds = mirror(r1, p1.Bitmap);
                    if (flg) p2.BoxBounds = common.Bounds;
                }
                ignore = false;
            }
            else
                (li1.Tag as PageInfo).Bounds = r1;
            p1.Refresh();
            p2.Refresh();
            OnBoxResize(EventArgs.Empty);
        }

        private bool isLeftPanel(PicturePanel pp)
        {
            return pp == panel1 ? !rightBinding : rightBinding;
        }

        private bool isLeftPage(int index)
        {
            int i1 = index & 1;
            return rightBinding ? i1 == 1 : i1 == 0;
        }

        public PicturePanel SelectedPanel
        {
            get
            {
                if (panel1.Selected)
                    return panel1;
                else if (panel2.Selected)
                    return panel2;
                else
                    return null;
            }
        }

        public Size DisplayBoxSize
        {
            get
            {
                var sp = SelectedPanel;
                return sp != null ? sp.DisplayBoxSize : Size.Empty;
            }
        }

        public void Save(BackgroundWorker bw, int ow, int oh, bool r)
        {
            var outdir = Path.Combine(imagePath, "output");
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);
            int no = 1;
            getEachBitmap(bw, ow, oh, r, bmp =>
            {
                var jpg = Path.Combine(outdir, string.Format("{0:0000}.jpg", no++));
                bmp.Save(jpg, ImageFormat.Jpeg);
            });
        }

        public void Save(BackgroundWorker bw, int ow, int oh, bool r, string pdf)
        {
            if (pdf == null)
            {
                Save(bw, ow, oh, r);
                return;
            }
            using (var fs = new FileStream(pdf, FileMode.Create))
            using (var sw = new StreamWriter(fs) { AutoFlush = true })
            {
                var objp = new List<long>();
                var sizes = new Size[countBitmap()];
                var no_r = 2 + sizes.Length;

                sw.WriteLine("%PDF-1.2");

                sw.WriteLine();
                objp.Add(fs.Position);
                sw.WriteLine("{0} 0 obj", objp.Count);
                sw.WriteLine("<<");
                sw.WriteLine("  /Type /Catalog /Pages {0} 0 R", no_r);
                if (rightBinding)
                    sw.WriteLine("  /ViewerPreferences << /Direction /R2L >>");
                sw.WriteLine(">>");
                sw.WriteLine("endobj");

                int n = 0;
                getEachBitmap(bw, ow, oh, r, bmp =>
                {
                    using (var ms = new MemoryStream())
                    {
                        var sz = sizes[n++] = bmp.Size;
                        var name = "/Jpeg" + n;
                        bmp.Save(ms, ImageFormat.Jpeg);
                        ms.Close();
                        var buf = ms.ToArray();

                        sw.WriteLine();
                        objp.Add(fs.Position);
                        sw.WriteLine("{0} 0 obj", objp.Count);
                        sw.WriteLine("<<");
                        sw.WriteLine("  /Type /XObject /Subtype /Image /Name {0}", name);
                        sw.WriteLine("  /Filter /DCTDecode /BitsPerComponent 8 /ColorSpace /DeviceRGB");
                        sw.WriteLine("  /Width {0} /Height {1} /Length {2}", sz.Width, sz.Height, buf.Length);
                        sw.WriteLine(">>");
                        sw.WriteLine("stream");
                        fs.Write(buf, 0, buf.Length);
                        sw.WriteLine();
                        sw.WriteLine("endstream");
                        sw.WriteLine("endobj");
                    }
                });
                bw.ReportProgress(100);

                sw.WriteLine();
                objp.Add(fs.Position);
                sw.WriteLine("{0} 0 obj", objp.Count);
                sw.WriteLine("<<");
                sw.WriteLine("  /Type /Pages /Count {0}", sizes.Length);
                if (r)
                    sw.WriteLine("  /Rotate 90");
                sw.WriteLine("  /Kids");
                sw.Write("  [");
                for (int i = 0; i < sizes.Length; i++)
                {
                    if ((i & 7) == 0)
                    {
                        sw.WriteLine();
                        sw.Write("   ");
                    }
                    sw.Write(" {0} 0 R", no_r + 1 + i * 2);
                }
                sw.WriteLine();
                sw.WriteLine("  ]");
                sw.WriteLine(">>");
                sw.WriteLine("endobj");

                for (int i = 0; i < sizes.Length; i++)
                {
                    var name = "/Jpeg" + (i + 1);
                    var sz = sizes[i];

                    sw.WriteLine();
                    objp.Add(fs.Position);
                    sw.WriteLine("{0} 0 obj", objp.Count);
                    sw.WriteLine("<<");
                    sw.WriteLine("  /Type /Page /Parent {0} 0 R /Contents {1} 0 R", no_r, objp.Count + 1);
                    sw.WriteLine("  /MediaBox [ 0 0 {0} {1} ]", sz.Width, sz.Height);
                    sw.WriteLine("  /Resources");
                    sw.WriteLine("  <<");
                    sw.WriteLine("    /ProcSet [ /PDF /ImageB /ImageC /ImageI ]");
                    sw.WriteLine("    /XObject << {0} {1} 0 R >>", name, 2 + i);
                    sw.WriteLine("  >>");
                    sw.WriteLine(">>");
                    sw.WriteLine("endobj");

                    sw.WriteLine();
                    objp.Add(fs.Position);
                    sw.WriteLine("{0} 0 obj", objp.Count);
                    var st4 = string.Format("q {0} 0 0 {1} 0 0 cm {2} Do Q",
                        sz.Width, sz.Height, name);
                    sw.WriteLine("<< /Length {0} >>", st4.Length);
                    sw.WriteLine("stream");
                    sw.WriteLine(st4);
                    sw.WriteLine("endstream");
                    sw.WriteLine("endobj");
                }

                sw.WriteLine();
                var xref = fs.Position;
                sw.WriteLine("xref");
                var size = objp.Count + 1;
                sw.WriteLine("0 {0}", size);
                sw.WriteLine("{0:0000000000} {1:00000} f", 0, 65535);
                foreach (var p in objp)
                    sw.WriteLine("{0:0000000000} {1:00000} n", p, 0);
                sw.WriteLine("trailer");
                sw.WriteLine("<< /Root 1 0 R /Size {0} >>", size);
                sw.WriteLine("startxref");
                sw.WriteLine("{0}", xref);
                sw.WriteLine("%%EOF");
            }
        }

        private int countBitmap()
        {
            int ret = 0;
            int count = 0;
            Invoke(new Action(() =>
            {
                count = listView1.Items.Count;
            }));
            for (int i = 0; i < count; i++)
            {
                PageInfo lpi = null;
                Invoke(new Action(() =>
                {
                    lpi = listView1.Items[i].Tag as PageInfo;
                }));
                if (lpi != null) ret++;
            }
            return ret;
        }

        private void getEachBitmap(BackgroundWorker bw, int ow, int oh, bool r, Action<Bitmap> delg)
        {
            int count = 0;
            Invoke(new Action(() =>
            {
                count = listView1.Items.Count;
            }));
            int p = 0;
            for (int i = 0; i < count; i++)
            {
                if (bw.CancellationPending) break;
                int pp = i * 100 / count;
                if (p != pp) bw.ReportProgress(p = pp);

                PageInfo pi = null, lpi = null;
                Bitmap src = null;
                Invoke(new Action(() =>
                {
                    var li = listView1.Items[i];
                    if (li == panel1.Tag && panel1.Bitmap != null)
                        src = new Bitmap(panel1.Bitmap);
                    else if (li == panel2.Tag && panel2.Bitmap != null)
                        src = new Bitmap(panel2.Bitmap);
                    pi = getInfo(li);
                    lpi = li.Tag as PageInfo;
                }));
                if (lpi == null) continue;
                if (src == null)
                    using (var tmp = lpi.GetBitmap())
                        src = new Bitmap(tmp);
                Utils.AdjustLevels(src, pi.IsGrayScale ? pi.Level : 5);
                var box = pi == common && !isLeftPage(i) ? mirror(pi.Bounds, src) : pi.Bounds;
                using (var bmp = GetBitmap(src, box, ow, oh))
                {
                    Utils.AdjustContrast(bmp, pi.IsGrayScale ? pi.Contrast * 16 : 128);
                    if (pi.IsGrayScale) Utils.GrayScale(bmp);
                    if (r) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    delg(bmp);
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

        private void setSelection(PicturePanel pp)
        {
            var sel = pp.Tag as ListViewItem;
            if (sel == null) return;

            var stp = stop;
            stop = true;
            foreach (ListViewItem li in listView1.Items)
            {
                if (li.Selected) li.Selected = false;
            }
            listView1.FocusedItem = sel;
            stop = stp;
            sel.Selected = true;
            listView1.Focus();
        }

        private void panel1_Enter(object sender, EventArgs e)
        {
            if (!panel1.IsDragging) setSelection(panel1);
        }

        private void panel2_Enter(object sender, EventArgs e)
        {
            if (!panel2.IsDragging) setSelection(panel2);
        }

        private PageInfo getInfo(ListViewItem li)
        {
            if (li == null)
                return null;
            else if (li.Checked)
                return common;
            else
                return li.Tag as PageInfo;
        }

        private void listView1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape)
            {
                if (panel1.IsDragging)
                {
                    panel1.IsDragging = false;
                    panel1.BoxBounds = panel1.LastBoxBounds;
                }
                else if (panel2.IsDragging)
                {
                    panel2.IsDragging = false;
                    panel2.BoxBounds = panel2.LastBoxBounds;
                }
            }
        }
    }
}
