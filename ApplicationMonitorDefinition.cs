namespace SysHiberSwitch
{
    internal sealed class ApplicationMonitorDefinition
    {
        public ApplicationMonitorDefinition(string displayName, string processName, bool useGpuActivity)
        {
            DisplayName = displayName;
            ProcessName = processName;
            UseGpuActivity = useGpuActivity;
        }

        public string DisplayName { get; private set; }

        public string ProcessName { get; private set; }

        public bool UseGpuActivity { get; private set; }
    }
}
