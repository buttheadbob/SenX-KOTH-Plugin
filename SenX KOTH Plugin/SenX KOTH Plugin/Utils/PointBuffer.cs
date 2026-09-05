using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using NLog;
using SenX_KOTH_Plugin.Models;

namespace SenX_KOTH_Plugin.Utils;

internal static class PointBuffer
{
    private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => PointBuffer");
    private static readonly object Sync = new();

    private static readonly Dictionary<long, (string Name, string Tag, int Points)> BankAccum = new();
    private static readonly Dictionary<string, ulong> WeekScoreAccum = new();
    private static readonly List<PointEarned> WeekEventAccum = new();

    private static Timer? _timer;
    private static bool _dirty;
    private static bool _flushing;

    private static string ScorePath => Path.Combine(SenX_KOTH_PluginMain.DataPath, "ScoreData.json");
    private static string EventPath => Path.Combine(SenX_KOTH_PluginMain.LocalDataPath, "EventData.json");

    public static void Start()
    {
        _timer = new Timer(60000);
        _timer.Elapsed += (_, _) => Flush();
        _timer.Start();
    }

    public static void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        Flush();
    }

    public static void Record(long factionId, string name, string tag, int points, string zoneName)
    {
        lock (Sync)
        {
            BankAccum.TryGetValue(factionId, out var bank);
            BankAccum[factionId] = (name, tag, bank.Points + points);

            WeekScoreAccum.TryGetValue(name, out var score);
            WeekScoreAccum[name] = score + (ulong)points;

            WeekEventAccum.Add(new PointEarned
            {
                Points = points,
                FactionId = factionId,
                FactionName = name,
                FactionTag = tag,
                EarnedAt = DateTime.UtcNow,
                ZoneName = zoneName
            });

            _dirty = true;
        }
    }

    public static void Flush()
    {
        Dictionary<long, (string Name, string Tag, int Points)> bank;
        Dictionary<string, ulong> scores;
        List<PointEarned> events;

        lock (Sync)
        {
            if (_flushing || !_dirty) return;
            _flushing = true;
            bank = new Dictionary<long, (string Name, string Tag, int Points)>(BankAccum);
            scores = new Dictionary<string, ulong>(WeekScoreAccum);
            events = new List<PointEarned>(WeekEventAccum);
        }

        try
        {
            bool bankOk = FlushBank(bank);
            bool scoreOk = FlushScores(scores);
            bool eventOk = FlushEvents(events);

            lock (Sync)
            {
                if (bankOk)
                    foreach (var kv in bank) BankAccum.Remove(kv.Key);
                if (scoreOk)
                    foreach (var kv in scores) WeekScoreAccum.Remove(kv.Key);
                if (eventOk)
                    WeekEventAccum.RemoveRange(0, events.Count);

                _dirty = BankAccum.Count > 0 || WeekScoreAccum.Count > 0 || WeekEventAccum.Count > 0;
            }
        }
        catch (Exception ex)
        {
            KoTHLog.Error(Log, ex, "PointBuffer flush failed; will retry next cycle.");
        }
        finally
        {
            lock (Sync) _flushing = false;
        }
    }

    private static bool FlushBank(Dictionary<long, (string Name, string Tag, int Points)> bank)
    {
        return SharedFile.ReadModifyWrite<BanksData>(BankService.BankPath, data =>
        {
            var d = data ?? new BanksData();
            foreach (var kv in bank)
            {
                var entry = d.Banks.FirstOrDefault(b => b.FactionId == kv.Key);
                if (entry == null)
                {
                    entry = new FactionBankEntry { FactionId = kv.Key, FactionName = kv.Value.Name, FactionTag = kv.Value.Tag };
                    d.Banks.Add(entry);
                }
                entry.FactionName = kv.Value.Name;
                entry.FactionTag = kv.Value.Tag;
                entry.Points += kv.Value.Points;
            }
            return d;
        }, out _);
    }

    private static bool FlushScores(Dictionary<string, ulong> scores)
    {
        return SharedFile.ReadModifyWrite<ScoreFile>(ScorePath, data =>
        {
            var d = data ?? new ScoreFile();
            foreach (var kv in scores)
            {
                var idx = d.WeekScores.FindIndex(x => x.Key == kv.Key);
                if (idx >= 0)
                    d.WeekScores[idx] = new KeyValuePair<string, ulong>(kv.Key, d.WeekScores[idx].Value + kv.Value);
                else
                    d.WeekScores.Add(new KeyValuePair<string, ulong>(kv.Key, kv.Value));
            }
            return d;
        }, out _);
    }

    private static bool FlushEvents(List<PointEarned> events)
    {
        return SharedFile.ReadModifyWrite<EventData>(EventPath, data =>
        {
            var d = data ?? new EventData();
            d.WeekEvents.AddRange(events);
            return d;
        }, out _);
    }
}
