using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace JpegBookMaker
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
                grayScale = value;
                if (cache != null) cache.Dispose();
                cache = null;
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
            base.OnPaint(e);
            if (cache == null) setCache();
            if (cache != null)
            {
                var sz = ClientSize;
                e.Graphics.DrawImage(cache,
                    (sz.Width - cache.Width) / 2,
                    (sz.Height - cache.Height) / 2);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            setCache();
            Invalidate();
        }
    }
}
