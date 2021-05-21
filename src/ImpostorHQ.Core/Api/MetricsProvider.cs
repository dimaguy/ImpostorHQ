using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Impostor.Api.Games.Managers;
using Impostor.Api.Net.Manager;

namespace ImpostorHQ.Core.Api
{
    public class MetricsProvider : IDisposable
    {
        private readonly IClientManager _clientManager;

        private readonly CancellationTokenSource _cts;

        private readonly IGameManager _gameManager;

        private readonly Process _proc;

        public MetricsProvider(IGameManager gameManager, IClientManager clientManager)
        {
            _gameManager = gameManager;
            _clientManager = clientManager;

            _proc = Process.GetCurrentProcess();
            _cts = new CancellationTokenSource();

            _ = CalculateCpuUsage();
        }

        public long MemoryUsageBytes
        {
            get
            {
                _proc.Refresh();
                return _proc.PrivateMemorySize64;
            }
        }

        public int CpuUsagePercent { get; private set; }

        public int PlayerCount => _clientManager.Clients.Count();

        public int GameCount => _gameManager.Games.Count();

        public void Dispose()
        {
            _cts.Cancel();
        }

        private async Task CalculateCpuUsage()
        {
            while (!_cts.IsCancellationRequested)
            {
                var startTime = DateTime.Now;
                var usage = _proc.TotalProcessorTime;

                await Task.Delay(500, _cts.Token);

                var endTime = DateTime.Now;

                var cpuTime = (float) (_proc.TotalProcessorTime - usage).TotalMilliseconds;
                var duration = Environment.ProcessorCount * (endTime - startTime).TotalMilliseconds;

                CpuUsagePercent = (int) Math.Round(cpuTime / duration * 100f, 2);
                _proc.Refresh();
            }
        }
    }
}