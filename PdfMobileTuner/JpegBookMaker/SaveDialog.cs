﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CommonLib;

namespace JpegBookMaker
{
    public partial class SaveDialog : Form
    {
        public BookPanel BookPanel { get; set; }
        public string PDF { get; set; }

        private static bool first = true, chk1, chk2, chk3;
        private static int num1, num2;

        public SaveDialog()
        {
            InitializeComponent();
            if (first)
            {
                setValues();
                first = false;
            }
            else
            {
                checkBox1.Checked = chk1;
                checkBox2.Checked = chk2;
                checkBox3.Checked = chk3;
                numericUpDown1.Value = num1;
                numericUpDown2.Value = num2;
            }
        }

        private void setValues()
        {
            chk1 = checkBox1.Checked;
            chk2 = checkBox2.Checked;
            chk3 = checkBox3.Checked;
            num1 = (int)numericUpDown1.Value;
            num2 = (int)numericUpDown2.Value;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (backgroundWorker1.IsBusy)
                backgroundWorker1.CancelAsync();
            groupBox1.Enabled = true;
            button1.Enabled = true;
            progressBar1.Value = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            groupBox1.Enabled = false;
            button1.Enabled = false;
            backgroundWorker1.RunWorkerAsync();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown1.Enabled = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown2.Enabled = checkBox2.Checked;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int w = 0, h = 0;
            bool r = false;
            Invoke(new Action(() =>
            {
                w = checkBox1.Checked ? (int)numericUpDown1.Value : 0;
                h = checkBox2.Checked ? (int)numericUpDown2.Value : 0;
                r = checkBox3.Checked;
            }));
            BookPanel.Save(backgroundWorker1, w, h, r, PDF);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                MessageBox.Show(this, e.Error.ToString(), Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            setValues();
            Close();
        }
    }
}
