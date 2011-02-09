using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace JpegBookMaker
{
    public class PicturePanel : UserControl
    {
        public PicturePanel()
        {
            SetStyle(ControlStyles.Selectable, true);
            DoubleBuffered = true;
        }
    }
}
