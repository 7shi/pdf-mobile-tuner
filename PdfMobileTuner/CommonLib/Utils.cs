using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace CommonLib
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

            var data = bmp.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
            var buf = new byte[w * h * 4];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);

            if (level > 0)
            {
                var lt = ltable[level];
                for (int i = 0; i < buf.Length; i += 4)
                {
                    buf[i] = lt[buf[i]];
                    buf[i + 1] = lt[buf[i + 1]];
                    buf[i + 2] = lt[buf[i + 2]];
                }
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
            var data = bmp.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
            var buf = new byte[w * h * 4];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            for (int i = 0; i < buf.Length; i += 4)
            {
                buf[i] = table[buf[i]];
                buf[i + 1] = table[buf[i + 1]];
                buf[i + 2] = table[buf[i + 2]];
            }
            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            bmp.UnlockBits(data);
        }

        public static Size GetSize(Size source, Size border)
        {
            int w = border.Width, h = border.Height;
            if (w < 1 || h < 1) return Size.Empty;

            int iw = source.Width, ih = source.Height;
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
            return new Size(iw, ih);
        }

        public static Bitmap ResizeImage(Bitmap img, Size sz)
        {
            var sz2 = GetSize(img.Size, sz);
            if (sz2.IsEmpty) return null;

            var ret = new Bitmap(sz2.Width, sz2.Height);
            using (var g = Graphics.FromImage(ret))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, 0, 0, sz2.Width, sz2.Height);
            }
            return ret;
        }

        public static void GrayScale(Bitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            var r = new Rectangle(0, 0, w, h);

            var data = bmp.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
            var buf = new byte[w * h * 4];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);

            for (int i = 0; i < buf.Length; i += 4)
            {
                buf[i] = buf[i + 1] = buf[i + 2] =
                    (byte)((buf[i] * 117 + buf[i + 1] * 601 + buf[i + 2] * 306 + 512) >> 10);
            }

            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            bmp.UnlockBits(data);
        }

        public static string URLEncode(string text)
        {
            var sb = new StringBuilder();
            foreach (char ch in Encoding.UTF8.GetBytes(text))
            {
                if (ch < 128 && char.IsLetterOrDigit(ch))
                    sb.Append(ch);
                else
                {
                    switch (ch)
                    {
                        case ' ':
                            sb.Append('+');
                            break;
                        case '-':
                        case '_':
                        case '.':
                            sb.Append(ch);
                            break;
                        default:
                            sb.Append(string.Format("%{0:X2}", (int)ch));
                            break;
                    }
                }
            }
            return sb.ToString();
        }
    }

    public delegate void Action();
}
