using System.Runtime.InteropServices;

namespace BlueLockScreen
{
    public static class LockHelper
    {
        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();
    }
}
