using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace JpegBookMaker
{
    public partial class BookPanelHelp : Form
    {
        public BookPanelHelp()
        {
            InitializeComponent();
        }

        public new void Show()
        {
            base.Show();
            textBox1.Focus();
            textBox1.DeselectAll();
            textBox1.SelectionStart = 0;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = true;
            Hide();
        }
    }
}
