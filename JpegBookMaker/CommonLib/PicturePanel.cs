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

        public event EventHandler BoxBoundsChanged;

        private Rectangle boxBounds;
        public Rectangle BoxBounds
        {
            get { return boxBounds; }
            set
            {
                boxBounds = value;
                Invalidate();
                if (BoxBoundsChanged != null)
                    BoxBoundsChanged(this, EventArgs.Empty);
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

        private void setCache()
        {
            if (bmplv == null) return;

            if (cache != null) cache.Dispose();
            var sz = ClientSize;
            cache = Utils.ResizeImage(bmplv, sz);
            if (cache == null) return;

            Utils.AdjustContrast(cache, contrast);
            if (grayScale) Utils.GrayScale(cache);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (cache == null) setCache();
            if (cache != null)
            {
                var sz = ClientSize;
                var x = (sz.Width - cache.Width) / 2;
                var y = (sz.Height - cache.Height) / 2;
                e.Graphics.DrawImage(cache, x, y);
                if (!boxBounds.Size.IsEmpty)
                {
                    var bx = boxBounds.X * cache.Width / bitmap.Width;
                    var by = boxBounds.Y * cache.Height / bitmap.Height;
                    var bw = boxBounds.Width * cache.Width / bitmap.Width;
                    var bh = boxBounds.Height * cache.Height / bitmap.Height;
                    using (var pen = new Pen(Color.Red, 2))
                        e.Graphics.DrawRectangle(pen, x + bx, y + by, bw, bh);
                }
            }
            base.OnPaint(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            setCache();
            Invalidate();
        }
    }
}
