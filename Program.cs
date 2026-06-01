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

            // 设置 PerMonitorV2 DPI 感知，实现界面缩放（非等比缩放）
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            SettingsManager.Load();
            Application.Run(new MainForm());
        }
    }
}
