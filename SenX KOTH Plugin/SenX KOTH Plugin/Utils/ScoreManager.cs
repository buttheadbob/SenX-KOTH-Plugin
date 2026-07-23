using ProtoBuf;
using System;
using System.Collections.Generic;
using Torch;

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

    [ProtoContract]
    public sealed class PointVerification
    {
        [ProtoMember(1)] public byte FromServerID { get; set; }
        [ProtoMember(2)] public List<PointEarned> WeekEvents { get; set; } = new List<PointEarned>();
        [ProtoMember(3)] public List<PointEarned> MonthEvents { get; set; } = new List<PointEarned>();
        [ProtoMember(4)] public List<PointEarned> YearEvents { get; set; } = new List<PointEarned>();
    }

    [ProtoContract]
    public sealed class RewardConfigSync
    {
        [ProtoMember(1)] public byte FromServerID { get; set; }
        [ProtoMember(2)] public byte[]? ConfigData { get; set; }
    }

    public sealed class ScoreFile : ViewModel
    {
        public List<KeyValuePair<string, int>> WeekScores { get => field; set => SetValue(ref field, value); } = new();
        public List<KeyValuePair<string, int>> MonthScores { get => field; set => SetValue(ref field, value); } = new();
        public List<KeyValuePair<string, int>> YearScores { get => field; set => SetValue(ref field, value); } = new();
    }
}

