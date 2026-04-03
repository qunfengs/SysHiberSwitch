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

            var photoshopDefinition = new ApplicationMonitorDefinition("Photoshop", "Photoshop", false);
            var cinema4DDefinition = new ApplicationMonitorDefinition("Cinema 4D", "Cinema 4D", true);

            using (var appState = new AppState())
            using (var photoshopMonitor = new ApplicationIdleMonitor(photoshopDefinition))
            using (var cinema4DMonitor = new ApplicationIdleMonitor(cinema4DDefinition))
            {
                var autoStartManager = new AutoStartManager();
                autoStartManager.EnsureInitialized(Application.ExecutablePath);

                EventHandler syncPolicy = delegate
                {
                    appState.SetKeepAwakeEnabled(
                        KeepAwakePolicy.ShouldKeepAwake(photoshopMonitor, cinema4DMonitor));
                };

                photoshopMonitor.StateChanged += syncPolicy;
                cinema4DMonitor.StateChanged += syncPolicy;

                syncPolicy(null, EventArgs.Empty);
                photoshopMonitor.Start();
                cinema4DMonitor.Start();

                Application.Run(new FloatingForm(
                    appState,
                    photoshopMonitor,
                    cinema4DMonitor,
                    autoStartManager));
            }
        }
    }
}
