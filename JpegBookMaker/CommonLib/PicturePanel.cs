using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CommonLib;

namespace CommonLib
{
    public class PicturePanel : UserControl
    {
        private Bitmap bitmap, bmplv, cache;
        public Bitmap Bitmap
        {
            get { return bitmap; }
            set
            {
                bitmap = value;
                setBitmap();
            }
        }

        private int level;
        public int Level
        {
            get { return level; }
            set
            {
                if (level == value) return;
                level = value;
                setBitmap();
            }
        }

        private int contrast = 128;
        public int Contrast
        {
            get { return contrast; }
            set
            {
                if (contrast == value) return;
                contrast = value;
                if (cache != null) cache.Dispose();
                cache = null;
                Invalidate();
            }
        }

        private bool grayScale;
        public bool GrayScale
        {
            get { return grayScale; }
            set
            {
                if (grayScale == value) return;
                grayScale = value;
                if (cache != null) cache.Dispose();
                cache = null;
                Invalidate();
            }
        }

        private Rectangle boxBounds;
        public Rectangle BoxBounds
        {
            get { return boxBounds; }
            set
            {
                boxBounds = value;
                Invalidate();
            }
        }

        public PicturePanel()
        {
            SetStyle(ControlStyles.Selectable, true);
            DoubleBuffered = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (bmplv != null) bmplv.Dispose();
                if (cache != null) cache.Dispose();
                bmplv = cache = null;
            }
            base.Dispose(disposing);
        }

        private void setBitmap()
        {
            if (bmplv != null) bmplv.Dispose();
            if (cache != null) cache.Dispose();
            bmplv = cache = null;
            if (bitmap != null)
            {
                bmplv = new Bitmap(bitmap);
                Utils.AdjustLevels(bmplv, level);
            }
            Invalidate();
        }

        private int cx, cy, bx, by, bw, bh;

        private void setCache()
        {
            if (bmplv == null) return;

            if (cache != null) cache.Dispose();
            var sz = ClientSize;
            cache = Utils.ResizeImage(bmplv, sz);
            if (cache == null) return;

            Utils.AdjustContrast(cache, contrast);
            if (grayScale) Utils.GrayScale(cache);

            cx = (sz.Width - cache.Width) / 2;
            cy = (sz.Height - cache.Height) / 2;
            bx = cx + boxBounds.X * cache.Width / bitmap.Width;
            by = cy + boxBounds.Y * cache.Height / bitmap.Height;
            bw = boxBounds.Width * cache.Width / bitmap.Width;
            bh = boxBounds.Height * cache.Height / bitmap.Height;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (cache == null) setCache();
            if (cache != null)
            {
                e.Graphics.DrawImage(cache, cx, cy);
                if (!boxBounds.Size.IsEmpty)
                    using (var pen = new Pen(Color.Red, 2))
                        e.Graphics.DrawRectangle(pen, bx, by, bw, bh);
            }
            base.OnPaint(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            setCache();
            Invalidate();
        }

        public int State { get; private set; }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            State = 0;
            var cur = Cursors.Default;
            if (!boxBounds.Size.IsEmpty && cache != null)
            {
                const int th = 3;
                int bx2 = bx + bw, by2 = by + bh;
                if (bx - th <= e.X && e.X <= bx2 + th && by - th <= e.Y && e.Y <= by2 + th)
                {
                    if (bx - th <= e.X && e.X <= bx + th)
                    {
                        State = 1;
                        cur = Cursors.SizeWE;
                    }
                    else if (bx2 - th <= e.X && e.X <= bx2 + th)
                    {
                        State = 2;
                        cur = Cursors.SizeWE;
                    }
                    else if (by - th <= e.Y && e.Y <= by + th)
                    {
                        State = 3;
                        cur = Cursors.SizeNS;
                    }
                    else if (by2 - th <= e.Y && e.Y <= by2 + th)
                    {
                        State = 4;
                        cur = Cursors.SizeNS;
                    }
                }
            }
            if (Cursor != cur) Cursor = cur;
        }
    }
}
