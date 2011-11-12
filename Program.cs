using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BitOrchestra
{
    /// <summary>
    /// Contains program related functions.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Program main entry-point.
        /// </summary>
        [STAThread]
        public static void Main(string[] Args)
        {
            Application.EnableVisualStyles();
            Application.Run(new MainForm());
        }
    }
}