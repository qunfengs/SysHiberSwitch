using System;
using System.IO;
using Microsoft.Win32;

namespace SysHiberSwitch
{
    internal sealed class AutoStartManager
    {
        private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string ValueName = "SysHiberSwitch";
        private static readonly string StateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SysHiberSwitch");
        private static readonly string InitializedFilePath = Path.Combine(StateDirectory, "autostart-initialized.txt");

        public bool EnsureInitialized(string executablePath)
        {
            if (!IsInitialized())
            {
                SetEnabled(true, executablePath);
                MarkInitialized();
                return true;
            }

            var enabled = GetEnabled();
            if (enabled)
            {
                SetEnabled(true, executablePath);
            }

            return enabled;
        }

        public bool GetEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
            {
                if (key == null)
                {
                    return false;
                }

                var value = key.GetValue(ValueName) as string;
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        public void SetEnabled(bool enabled, string executablePath)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
            {
                if (key == null)
                {
                    return;
                }

                if (enabled)
                {
                    key.SetValue(ValueName, "\"" + executablePath + "\"");
                }
                else
                {
                    key.DeleteValue(ValueName, false);
                }
            }
        }

        private static bool IsInitialized()
        {
            return File.Exists(InitializedFilePath);
        }

        private static void MarkInitialized()
        {
            Directory.CreateDirectory(StateDirectory);
            File.WriteAllText(InitializedFilePath, "initialized");
        }
    }
}
