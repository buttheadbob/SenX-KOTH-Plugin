using System;
using System.Timers;
using SenX_KOTH_Plugin.Discord;
using SenX_KOTH_Plugin.Nexus;

namespace SenX_KOTH_Plugin.Events
{
    internal sealed class LiveScoreboardEvent : IKothEvent
    {
        private readonly SenX_KOTH_PluginConfig _config;
        private Timer? _timer;

        public string Name => "LiveScoreboardEvent";
        public bool ShouldRun => _config.DiscordBotEnabled && NexusManager.IsAuthorityLocal();
        public bool IsRunning { get; private set; }

        public LiveScoreboardEvent(SenX_KOTH_PluginConfig config) => _config = config;

        public void Start()
        {
            var interval = Math.Max(10, _config.DiscordUpdateIntervalSeconds) * 1000;
            _timer = new Timer(interval);
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
            await DiscordBotService.UpdateZoneChannelsAsync();
        }
    }
}
