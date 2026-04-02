using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SysHiberSwitch
{
    internal sealed class AppState : IDisposable
    {
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);

        private readonly Timer keepAwakeTimer;
        private bool enabled;

        public AppState()
        {
            keepAwakeTimer = new Timer();
            keepAwakeTimer.Interval = 30000;
            keepAwakeTimer.Tick += KeepAwakeTimerOnTick;
        }

        public bool Enabled
        {
            get { return enabled; }
        }

        public event EventHandler StateChanged;

        public void Enable()
        {
            enabled = true;
            keepAwakeTimer.Start();
            RefreshExecutionState();
            OnStateChanged();
        }

        public void Disable()
        {
            enabled = false;
            keepAwakeTimer.Stop();
            SetThreadExecutionState(ES_CONTINUOUS);
            OnStateChanged();
        }

        public void Dispose()
        {
            keepAwakeTimer.Stop();
            keepAwakeTimer.Dispose();
            SetThreadExecutionState(ES_CONTINUOUS);
        }

        private void KeepAwakeTimerOnTick(object sender, EventArgs e)
        {
            RefreshExecutionState();
        }

        private void RefreshExecutionState()
        {
            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
        }

        private void OnStateChanged()
        {
            var handler = StateChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
