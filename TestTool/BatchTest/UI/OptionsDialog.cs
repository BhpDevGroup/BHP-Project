﻿using Bhp.Properties;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Bhp.UI
{
    public partial class OptionsDialog : Form
    {
        public OptionsDialog()
        {
            InitializeComponent();
            textBox1.Lines = Settings.Default.BRC5Watched.OfType<string>().ToArray();
        }

        private void Apply_Click(object sender, EventArgs e)
        {
            Settings.Default.BRC5Watched.Clear();
            Settings.Default.BRC5Watched.AddRange(textBox1.Lines.Where(p => !string.IsNullOrWhiteSpace(p) && UInt160.TryParse(p, out _)).ToArray());
            Settings.Default.Save();
        }
    }
}
