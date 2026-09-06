using ProtoBuf;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Torch;
using VRage.Game.ModAPI;

namespace SenX_KOTH_Plugin.Utils
{
    [ProtoContract]
    public sealed class PointEarned
    {
        [ProtoMember(1)] public int Points { get; set; }
        [ProtoMember(2)] public long FactionId { get; set; }
        [ProtoMember(3)] public string FactionName { get; set; } = "";
        [ProtoMember(4)] public string FactionTag { get; set; } = "";
        [ProtoMember(5)] public DateTime? LastWipe { get; set; }
        [ProtoMember(6)] public byte FromServerID { get; set; }
        [ProtoMember(7)] public DateTime EarnedAt { get; set; }
        [ProtoMember(8)] public string ZoneName { get; set; } = "";
        [ProtoMember(9)] public long EventId { get; set; }
        [ProtoMember(10)] public WipePeriod WipePeriod { get; set; } = WipePeriod.None;
    }

    public sealed class ScoreFile : ViewModel
    {
        public List<KeyValuePair<long, ulong>> WeekScores { get; set => SetValue(ref field, value); } = [];
        public List<KeyValuePair<long, ulong>> MonthScores { get; set => SetValue(ref field, value); } = [];
        public List<KeyValuePair<long, ulong>> YearScores { get; set => SetValue(ref field, value); } = [];
    }

    internal static class FactionLookup
    {
        public static string GetName(long factionId)
        {
            IMyFaction? faction = null;
            MyAPIGateway.Session.Factions.Factions.TryGetValue(factionId, out faction);
            return faction?.Name ?? ("Faction " + factionId);
        }
    }
}
