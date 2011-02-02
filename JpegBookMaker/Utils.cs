using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace JpegBookMaker
{
    public static class Utils
    {
        private static byte[][] ltable = new byte[11][];

        static Utils()
        {
            var v = new double[11];
            for (int i = 0; i <= 10; i++)
                ltable[i] = new byte[256];
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
                    ltable[j][i] = (byte)(((v[j] + 1) * 255 + 1) / 2);
                }
            }
        }

        public static byte[] GetLevelsTable(int level)
        {
            return ltable[level];
        }

        public static byte[] GetContrastTable(int center)
        {
            if (center < 0) center = 0;
            if (center > 255) center = 255;

            var table = new byte[256];
            if (center < 128)
            {
                int d = (128 - center) * 2;
                int range = 256 - d;
                for (int i = 0; i < range; i++)
                    table[i] = (byte)((i * 512 / range + 1) / 2);
                for (int i = range; i < 256; i++)
                    table[i] = 255;
            }
            else
            {
                int d = (center - 128) * 2;
                int range = 256 - d;
                for (int i = 0; i < d; i++)
                    table[i] = 0;
                for (int i = d; i < 256; i++)
                    table[i] = (byte)(((i - d) * 512 / range + 1) / 2);
            }
            return table;
        }

        public static void AdjustLevels(Bitmap bmp, int level)
        {
            int w = bmp.Width, h = bmp.Height;
            var r = new Rectangle(0, 0, w, h);

            var data = bmp.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            var buf = new byte[w * h * 3];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);

            if (level > 0)
            {
                var lt = ltable[level];
                for (int i = 0; i < buf.Length; i++)
                    buf[i] = lt[buf[i]];
            }

            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            bmp.UnlockBits(data);
        }

        public static void AdjustContrast(Bitmap bmp, int center)
        {
            if (center == 128) return;

            var table = GetContrastTable(center);
            int w = bmp.Width, h = bmp.Height;
            var r = new Rectangle(0, 0, w, h);
            var data = bmp.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            var buf = new byte[w * h * 3];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            for (int i = 0; i < buf.Length; i++)
                buf[i] = table[buf[i]];
            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            bmp.UnlockBits(data);
        }

        public static Bitmap ResizeImage(Bitmap img, Size sz)
        {
            int w = sz.Width, h = sz.Height;
            if (w < 1 || h < 1) return null;

            int iw = img.Width, ih = img.Height;
            int hh = ih * w / iw;
            if (hh < h)
            {
                iw = w;
                ih = hh;
            }
            else
            {
                iw = iw * h / ih;
                ih = h;
            }

            var ret = new Bitmap(iw, ih);
            using (var g = Graphics.FromImage(ret))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, 0, 0, iw, ih);
            }
            return ret;
        }
    }
}
