using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CommonLib;

namespace JpegBookMaker
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.AddMessageFilter(new MouseWheelMessageFilter());
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
