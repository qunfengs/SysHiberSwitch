using System;
using System.Windows.Forms;

namespace SysHiberSwitch
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var appState = new AppState())
            {
                appState.SetEnabled(true);
                Application.Run(new FloatingForm(appState));
            }
        }
    }
}
