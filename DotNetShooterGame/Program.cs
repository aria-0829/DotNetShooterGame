using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotNetShooterGame
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            FormClient fc = null;
            FormServer fs = null;
            if (args[0] == "client")
            {
                fc = new FormClient();
                fc.Show();
            }
            else if (args[0] == "server")
            {
                fs = new FormServer();
                fs.Show();
            }
            while (Application.OpenForms.Count > 0)
            {
                Application.DoEvents();
                if (fc != null) fc.UpdateList();
                if (fs != null) fs.UpdateList();
            }
        }
    }
}
