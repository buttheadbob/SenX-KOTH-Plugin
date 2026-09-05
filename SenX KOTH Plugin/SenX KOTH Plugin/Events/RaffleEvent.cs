using System.Timers;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin.Events
{
    internal sealed class RaffleEvent : IKothEvent
    {
        private readonly SenX_KOTH_PluginConfig _config;
        private Timer? _timer;

        public string Name => "RaffleEvent";
        public bool ShouldRun => _config.RaffleEnabled;
        public bool IsRunning { get; private set; }

        public RaffleEvent(SenX_KOTH_PluginConfig config) => _config = config;

        public void Start()
        {
            _timer = new Timer(60000);
            _timer.Elapsed += Tick;
            _timer.Start();
            IsRunning = true;
        }

        public void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            IsRunning = false;
        }

        public void Update() { }
        public void IntegrityCheck() { }
        public void Save() { }

        private void Tick(object? sender, ElapsedEventArgs e)
        {
            BankService.CheckRaffleDraw(_config);
        }
    }
}
