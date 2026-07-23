using System;
using System.Linq;
using System.Timers;
using NLog;
using SenX_KOTH_Plugin.Events;
using SenX_KOTH_Plugin.Models;
using SenX_KOTH_Plugin.Utils;

namespace SenX_KOTH_Plugin.Services;

internal static class Supervisor
{
    private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => Supervisor");
    private static Timer? _timer;

    public static void Init()
    {
        Log.Info("Supervisor starting — 60s check interval");
        _timer = new(60000);
        _timer.Elapsed += RunChecks;
        _timer.Start();
    }

    private static void RunChecks(object? sender, ElapsedEventArgs e)
    {
        var config = SenX_KOTH_PluginMain.Instance?.Config;
            var zonePersist = SenX_KOTH_PluginMain.ZonePersist;
            var bankData = SenX_KOTH_PluginMain.BankPersist?.Data;
            var eventData = SenX_KOTH_PluginMain.EventPersist?.Data;

            if (config != null && zonePersist != null && bankData != null && eventData != null)
            {
                var zones = zonePersist.Data.Zones;

                foreach (var zone in zones)
                {
                    var existing = SenX_KOTH_PluginMain.Events.FirstOrDefault(ev => ev.Name == zone.Name);
                    if (existing == null)
                    {
                        var evt = new ZonePointEvent(zone, config, bankData, eventData);
                        SenX_KOTH_PluginMain.Events.Add(evt);
                        Log.Info("Created zone event: " + zone.Name + " — " + evt.ShouldRunStatus);
                    }
                }

                var toRemove = SenX_KOTH_PluginMain.Events.OfType<ZonePointEvent>()
                    .Where(zp => !zones.Any(z => string.Equals(z.Name, zp.Name, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                foreach (var evt in toRemove)
                {
                    evt.Stop();
                    SenX_KOTH_PluginMain.Events.Remove(evt);
                    Log.Info("Removed zone event: " + evt.Name);
                }
            }

            var events = SenX_KOTH_PluginMain.Events.ToList();
            Log.Info("Supervisor check — {0} events", events.Count);

            foreach (IKothEvent evt in events)
            {
                try
                {
                    var state = evt.IsRunning ? "RUNNING" : "STOPPED";
                    Log.Info("  [" + state + "] " + evt.Name + " — ShouldRun=" + evt.ShouldRun);

                    if (evt.ShouldRun && !evt.IsRunning)
                    {
                        evt.Start();
                        Log.Info("  -> STARTED: " + evt.Name);
                    }
                    else if (evt is { ShouldRun: false, IsRunning: true })
                    {
                        evt.Stop();
                        Log.Info("  -> STOPPED: " + evt.Name);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Supervisor error for event: " + evt.Name);
                }
            }
        }

    public static void ShutDown()
    {
        Log.Info("Supervisor shutting down");
        foreach (IKothEvent evt in SenX_KOTH_PluginMain.Events)
        {
            try { evt.Stop(); }
            catch (Exception ex) { Log.Error(ex, "Supervisor shutdown error: " + evt.Name); }
        }
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}
