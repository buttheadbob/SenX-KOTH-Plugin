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
    public sealed class ProtoEmbed
    {
        [ProtoMember(1)] public string Title { get; set; } = "";
        [ProtoMember(2)] public string Description { get; set; } = "";
        [ProtoMember(3)] public uint Color { get; set; }
    }

    [ProtoContract]
    public sealed class DiscordZoneRelay
    {
        [ProtoMember(1)] public byte FromServerID { get; set; }
        [ProtoMember(2)] public string ZoneName { get; set; } = "";
        [ProtoMember(3)] public ProtoEmbed? Embed { get; set; }
        [ProtoMember(4)] public ulong KnownChannelId { get; set; }
    }

    [ProtoContract]
    public sealed class DiscordRewardRelay
    {
        [ProtoMember(1)] public byte FromServerID { get; set; }
        [ProtoMember(2)] public ProtoEmbed? Embed { get; set; }
    }

    [ProtoContract]
    public sealed class DiscordChannelResponse
    {
        [ProtoMember(1)] public byte FromServerID { get; set; }
        [ProtoMember(2)] public byte TargetServerID { get; set; }
        [ProtoMember(3)] public string ZoneName { get; set; } = "";
        [ProtoMember(4)] public ulong ChannelId { get; set; }
    }

    [ProtoContract]
    public sealed class AuthorityAnnouncement { }

    [ProtoContract]
    public sealed class SyncRequest { }

    [ProtoContract]
    public sealed class PointCreditEntry
    {
        [ProtoMember(1)] public Guid RequestId { get; set; }
        [ProtoMember(2)] public long FactionId { get; set; }
        [ProtoMember(3)] public string FactionName { get; set; } = "";
        [ProtoMember(4)] public string FactionTag { get; set; } = "";
        [ProtoMember(5)] public int Points { get; set; }
        [ProtoMember(6)] public string ZoneName { get; set; } = "";
        [ProtoMember(7)] public DateTime EarnedAt { get; set; }
    }

    [ProtoContract]
    public sealed class SyncResponse
    {
        [ProtoMember(1)] public List<PointCreditEntry> Entries { get; set; } = new();
    }

    [ProtoContract]
    public sealed class TicketPurchaseRequest
    {
        [ProtoMember(1)] public Guid RequestId { get; set; }
        [ProtoMember(2)] public long PlayerIdentityId { get; set; }
        [ProtoMember(3)] public int Count { get; set; }
    }

    [ProtoContract]
    public sealed class BankBalanceRequest
    {
        [ProtoMember(1)] public Guid RequestId { get; set; }
        [ProtoMember(2)] public long PlayerIdentityId { get; set; }
    }

    [ProtoContract]
    public sealed class AuthorityResponse
    {
        [ProtoMember(1)] public Guid RequestId { get; set; }
        [ProtoMember(2)] public bool Approved { get; set; }
        [ProtoMember(3)] public string Message { get; set; } = "";
        [ProtoMember(4)] public int Balance { get; set; }
        [ProtoMember(5)] public int TicketCount { get; set; }
        [ProtoMember(6)] public string FactionTag { get; set; } = "";
        [ProtoMember(7)] public string FactionName { get; set; } = "";
    }

    public sealed class ScoreFile : ViewModel
    {
        public List<KeyValuePair<string, ulong>> WeekScores { get => field; set => SetValue(ref field, value); } = new();
        public List<KeyValuePair<string, ulong>> MonthScores { get => field; set => SetValue(ref field, value); } = new();
        public List<KeyValuePair<string, ulong>> YearScores { get => field; set => SetValue(ref field, value); } = new();
    }
}
