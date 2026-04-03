namespace SysHiberSwitch
{
    internal static class KeepAwakePolicy
    {
        public static bool NeedsProtection(ApplicationDetectionState detectionState)
        {
            switch (detectionState)
            {
                case ApplicationDetectionState.Active:
                case ApplicationDetectionState.IdleCountdown:
                    return true;
                default:
                    return false;
            }
        }

        public static bool ShouldKeepAwake(ApplicationIdleMonitor photoshopMonitor, ApplicationIdleMonitor cinema4DMonitor)
        {
            return NeedsProtection(photoshopMonitor.State) || NeedsProtection(cinema4DMonitor.State);
        }
    }
}
