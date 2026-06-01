using System;
using System.Windows.Forms;

namespace BlueLockScreen
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SettingsManager.Load();
            Application.Run(new MainForm());
        }
    }
}
