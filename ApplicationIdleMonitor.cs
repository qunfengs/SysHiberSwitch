using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;

namespace SysHiberSwitch
{
    internal sealed class ApplicationIdleMonitor : IDisposable
    {
        private const int IdleCountdownSeconds = 60;
        private const double ActiveCpuThreshold = 0.02D;
        private const float ActiveGpuThreshold = 5.0F;

        private readonly Timer timer;
        private readonly ApplicationMonitorDefinition definition;
        private TimeSpan previousTotalProcessorTime;
        private DateTime previousSampleTime;
        private bool hasSample;
        private int accumulatedIdleSeconds;

        public ApplicationIdleMonitor(ApplicationMonitorDefinition definition)
        {
            this.definition = definition;
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += TimerOnTick;
            State = ApplicationDetectionState.NotRunning;
            IdleCountdownSecondsRemaining = IdleCountdownSeconds;
        }

        public string DisplayName
        {
            get { return definition.DisplayName; }
        }

        public string ProcessName
        {
            get { return definition.ProcessName; }
        }

        public ApplicationDetectionState State { get; private set; }

        public int IdleCountdownSecondsRemaining { get; private set; }

        public event EventHandler StateChanged;

        public void Start()
        {
            timer.Start();
            Evaluate();
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Dispose();
        }

        private void TimerOnTick(object sender, EventArgs e)
        {
            Evaluate();
        }

        private void Evaluate()
        {
            var snapshot = TryGetActivitySnapshot();
            if (snapshot == null)
            {
                ResetSampling();
                IdleCountdownSecondsRemaining = IdleCountdownSeconds;
                SetState(ApplicationDetectionState.NotRunning, true);
                return;
            }

            var now = DateTime.UtcNow;
            if (!hasSample)
            {
                hasSample = true;
                previousSampleTime = now;
                previousTotalProcessorTime = snapshot.CpuTotalProcessorTime;
                accumulatedIdleSeconds = 0;
                IdleCountdownSecondsRemaining = IdleCountdownSeconds;
                SetState(ApplicationDetectionState.Active, true);
                return;
            }

            var cpuDelta = snapshot.CpuTotalProcessorTime - previousTotalProcessorTime;
            var timeDelta = now - previousSampleTime;
            previousSampleTime = now;
            previousTotalProcessorTime = snapshot.CpuTotalProcessorTime;

            if (timeDelta <= TimeSpan.Zero)
            {
                return;
            }

            var cpuUsage = cpuDelta.TotalMilliseconds / (Environment.ProcessorCount * timeDelta.TotalMilliseconds);
            var gpuUsage = definition.UseGpuActivity ? TryGetGpuUsage(snapshot.ProcessIds) : 0.0F;

            if (cpuUsage >= ActiveCpuThreshold || gpuUsage >= ActiveGpuThreshold)
            {
                accumulatedIdleSeconds = 0;
                IdleCountdownSecondsRemaining = IdleCountdownSeconds;
                SetState(ApplicationDetectionState.Active, true);
                return;
            }

            accumulatedIdleSeconds += (int)Math.Max(1, Math.Round(timeDelta.TotalSeconds));
            IdleCountdownSecondsRemaining = Math.Max(0, IdleCountdownSeconds - accumulatedIdleSeconds + 1);

            if (IdleCountdownSecondsRemaining > 0)
            {
                SetState(ApplicationDetectionState.IdleCountdown, true);
            }
            else
            {
                SetState(ApplicationDetectionState.IdleExpired, true);
            }
        }

        private ActivitySnapshot TryGetActivitySnapshot()
        {
            var processes = Process.GetProcessesByName(definition.ProcessName);
            var totalCpu = TimeSpan.Zero;
            var usableProcessCount = 0;
            var processIds = new int[processes.Length];

            foreach (var process in processes)
            {
                try
                {
                    if (process.HasExited || process.MainWindowHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    usableProcessCount++;
                    processIds[usableProcessCount - 1] = process.Id;
                    totalCpu += process.TotalProcessorTime;
                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }

            if (usableProcessCount == 0)
            {
                return null;
            }

            var activeProcessIds = new int[usableProcessCount];
            Array.Copy(processIds, activeProcessIds, usableProcessCount);
            return new ActivitySnapshot(totalCpu, activeProcessIds);
        }

        private static float TryGetGpuUsage(int[] processIds)
        {
            if (processIds == null || processIds.Length == 0)
            {
                return 0.0F;
            }

            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var totalUsage = 0.0F;
                var instanceNames = category.GetInstanceNames();

                foreach (var instanceName in instanceNames)
                {
                    if (!IsProcessInstance(instanceName, processIds))
                    {
                        continue;
                    }

                    using (var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instanceName, true))
                    {
                        totalUsage += counter.NextValue();
                    }
                }

                return totalUsage;
            }
            catch
            {
                return 0.0F;
            }
        }

        private static bool IsProcessInstance(string instanceName, int[] processIds)
        {
            if (string.IsNullOrEmpty(instanceName))
            {
                return false;
            }

            foreach (var processId in processIds)
            {
                if (instanceName.IndexOf("pid_" + processId.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetSampling()
        {
            hasSample = false;
            accumulatedIdleSeconds = 0;
            previousSampleTime = DateTime.MinValue;
            previousTotalProcessorTime = TimeSpan.Zero;
        }

        private void SetState(ApplicationDetectionState state, bool notifyAlways)
        {
            var changed = State != state;
            State = state;

            if (changed || notifyAlways)
            {
                var handler = StateChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        private sealed class ActivitySnapshot
        {
            public ActivitySnapshot(TimeSpan cpuTotalProcessorTime, int[] processIds)
            {
                CpuTotalProcessorTime = cpuTotalProcessorTime;
                ProcessIds = processIds;
            }

            public TimeSpan CpuTotalProcessorTime { get; private set; }

            public int[] ProcessIds { get; private set; }
        }
    }
}
