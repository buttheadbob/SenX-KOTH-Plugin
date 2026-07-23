using System.Timers;
using SenX_KOTH_Plugin.Discord;

namespace SenX_KOTH_Plugin.Events
{
    internal sealed class LiveScoreboardEvent : IKothEvent
    {
        private readonly SenX_KOTH_PluginConfig _config;
        private Timer? _timer;

        public string Name => "LiveScoreboardEvent";
        public bool ShouldRun => _config.DiscordBotEnabled;
        public bool IsRunning { get; private set; }

        public LiveScoreboardEvent(SenX_KOTH_PluginConfig config) => _config = config;

        public void Start()
        {
            _timer = new Timer(30000);
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

        private async void Tick(object? sender, ElapsedEventArgs e)
        {
            await DiscordBotService.UpdateLiveScoreboardAsync();
        }
    }
}
