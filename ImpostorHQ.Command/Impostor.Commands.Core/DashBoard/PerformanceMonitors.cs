using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
namespace Impostor.Commands.Core.DashBoard
{
    public class PerformanceMonitors
    {
        /// <summary>
        /// We update this value every 1 second. 
        /// </summary>
        public int CpuUsage{ get; private set; }
        public bool Running { get; private set; }
        public PerformanceMonitors()
        {
            Running = true;
            var cpuThread = new Thread(MonitorCpu);
            cpuThread.Start();
        }

        private void MonitorCpu()
        {
            var cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            Thread.Sleep(1000);
            var firstCall = cpuUsage.NextValue();

            while(Running)
            {
                Thread.Sleep(1000);
                this.CpuUsage = (int) cpuUsage.NextValue();
            }
        }

        public void Shutdown()
        {
            this.Running = false;
        }
    }
}
