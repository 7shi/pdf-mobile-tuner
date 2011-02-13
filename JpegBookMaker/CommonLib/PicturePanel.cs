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

        public event EventHandler BoxResize;

        private Rectangle boxBounds;
        public Rectangle BoxBounds
        {
            get { return boxBounds; }
            set
            {
                if (boxBounds == value) return;
                boxBounds = value;
                Invalidate();
                if (BoxResize != null) BoxResize(this, EventArgs.Empty);
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
            var cs = ClientSize;
            cache = Utils.ResizeImage(bmplv, cs);
            if (cache == null) return;

            Utils.AdjustContrast(cache, contrast);
            if (grayScale) Utils.GrayScale(cache);
        }

        private void setBox()
        {
            var cs = ClientSize;
            cx = (cs.Width - cache.Width) / 2;
            cy = (cs.Height - cache.Height) / 2;
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
                setBox();
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

        private void setCursor(int x, int y)
        {
            State = 0;
            var cur = Cursors.Default;
            if (!boxBounds.Size.IsEmpty && cache != null)
            {
                const int th = 3;
                int bx2 = bx + bw, by2 = by + bh;
                if (bx - th <= x && x <= bx2 + th && by - th <= y && y <= by2 + th)
                {
                    // 435
                    // 1 2
                    // 768
                    if (bx - th <= x && x <= bx + th)
                        State = 1;
                    else if (bx2 - th <= x && x <= bx2 + th)
                        State = 2;
                    if (by - th <= y && y <= by + th)
                        State += 3;
                    else if (by2 - th <= y && y <= by2 + th)
                        State += 6;
                    switch (State)
                    {
                        case 1:
                        case 2:
                            cur = Cursors.SizeWE;
                            break;
                        case 3:
                        case 6:
                            cur = Cursors.SizeNS;
                            break;
                        case 4:
                        case 8:
                            cur = Cursors.SizeNWSE;
                            break;
                        case 5:
                        case 7:
                            cur = Cursors.SizeNESW;
                            break;
                    }
                }
            }
            if (Cursor != cur) Cursor = cur;
        }

        public Point ClickLocation { get; private set; }
        public Rectangle LastBoxBounds { get; private set; }

        private bool isDragging;
        public bool IsDragging
        {
            get { return isDragging; }
            set
            {
                if (isDragging == value) return;
                isDragging = value;
                Capture = value;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (!Focused) Focus();
            if (e.Button == MouseButtons.Left && State != 0)
            {
                IsDragging = true;
                ClickLocation = e.Location;
                LastBoxBounds = boxBounds;
            }
            else if (e.Button == MouseButtons.Right && IsDragging)
            {
                IsDragging = false;
                BoxBounds = LastBoxBounds;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (IsDragging)
            {
                const int min = 100;
                int dx = (e.X - ClickLocation.X) * bitmap.Width / cache.Width;
                int dy = (e.Y - ClickLocation.Y) * bitmap.Height / cache.Height;
                var b = LastBoxBounds;
                // 435
                // 1 2
                // 768
                if (State == 1 || State == 4 || State == 7)
                {
                    dx = Math.Min(Math.Max(dx, -b.X), b.Width - min);
                    b.X += dx;
                    b.Width -= dx;
                }
                else if (State == 2 || State == 5 || State == 8)
                {
                    dx = Math.Min(Math.Max(dx, -b.Width + min), bitmap.Width - b.Right);
                    b.Width += dx;
                }
                if (State == 3 || State == 4 || State == 5)
                {
                    dy = Math.Min(Math.Max(dy, -b.Y), b.Height - min);
                    b.Y += dy;
                    b.Height -= dy;
                }
                else if (State == 6 || State == 7 || State == 8)
                {
                    dy = Math.Min(Math.Max(dy, -b.Height + min), bitmap.Height - b.Bottom);
                    b.Height += dy;
                }
                BoxBounds = b;
            }
            else
                setCursor(e.X, e.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && IsDragging)
                IsDragging = false;
        }

        private Color back;

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            back = BackColor;
            BackColor = SystemColors.Highlight;
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            BackColor = back;
        }
    }
}
