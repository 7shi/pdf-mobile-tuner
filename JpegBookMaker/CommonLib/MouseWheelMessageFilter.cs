using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CommonLib
{
    public class MouseWheelMessageFilter : IMessageFilter
    {
        #region Win32

        public const int WM_MOUSEWHEEL = 0x020A;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x, y;
        }

        [DllImport("User32.dll")]
        public static extern IntPtr WindowFromPoint(POINT p);

        [DllImport("User32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        #endregion

        public bool PreFilterMessage(ref Message m)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT || m.Msg != WM_MOUSEWHEEL)
                return false;
            var p = Cursor.Position;
            var pp = new POINT();
            pp.x = p.X;
            pp.y = p.Y;
            var hWnd = WindowFromPoint(pp);
            if (hWnd == m.HWnd || hWnd == IntPtr.Zero) return false;
            SendMessage(hWnd, m.Msg, m.WParam, m.LParam);
            return true;
        }
    }
}
