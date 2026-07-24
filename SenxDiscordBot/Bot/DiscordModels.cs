using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenX_KOTH_Plugin.Bot
{
    public sealed class DiscordEmbed
    {
        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("color")]
        public uint? Color { get; set; }

        [JsonProperty("timestamp")]
        public string? Timestamp { get; set; }

        [JsonProperty("footer")]
        public DiscordEmbedFooter? Footer { get; set; }

        [JsonProperty("fields")]
        public List<DiscordEmbedField>? Fields { get; set; }
    }

    public sealed class DiscordEmbedFooter
    {
        [JsonProperty("text")]
        public string? Text { get; set; }
    }

    public sealed class DiscordEmbedField
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("inline")]
        public bool? Inline { get; set; }
    }

    public sealed class DiscordMessage
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("content")]
        public string? Content { get; set; }

        [JsonProperty("embeds")]
        public List<DiscordEmbed>? Embeds { get; set; }
    }

    public sealed class DiscordChannel
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("type")]
        public int? Type { get; set; }

        [JsonProperty("permission_overwrites")]
        public List<PermissionOverwrite>? PermissionOverwrites { get; set; }
    }

    public sealed class PermissionOverwrite
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("allow")]
        public string Allow { get; set; } = "";

        [JsonProperty("deny")]
        public string Deny { get; set; } = "";
    }

    public sealed class CreateChannelRequest
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("parent_id")]
        public string? ParentId { get; set; }

        [JsonProperty("permission_overwrites")]
        public List<PermissionOverwrite>? PermissionOverwrites { get; set; }
    }

    internal sealed class GatewayPayload
    {
        [JsonProperty("op")]
        public int Op { get; set; }

        [JsonProperty("d")]
        public object? D { get; set; }

        [JsonProperty("s")]
        public int? S { get; set; }

        [JsonProperty("t")]
        public string? T { get; set; }
    }

    internal sealed class IdentifyPayload
    {
        [JsonProperty("token")]
        public string Token { get; set; } = "";

        [JsonProperty("intents")]
        public int Intents { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, string> Properties { get; set; } = new()
        {
            ["os"] = "windows",
            ["browser"] = "senx_koth",
            ["device"] = "senx_koth"
        };

        [JsonProperty("presence")]
        public GatewayPresence Presence { get; set; } = new();
    }

    internal sealed class GatewayPresence
    {
        [JsonProperty("since")]
        public long? Since { get; set; }

        [JsonProperty("activities")]
        public List<object> Activities { get; set; } = new();

        [JsonProperty("status")]
        public string Status { get; set; } = "online";

        [JsonProperty("afk")]
        public bool Afk { get; set; }
    }

    internal sealed class HeartbeatPayload
    {
        [JsonProperty("op")]
        public int Op => 1;

        [JsonProperty("d")]
        public int? Sequence;

        public HeartbeatPayload(int? seq) => Sequence = seq;
    }

    internal sealed class ResumePayload
    {
        [JsonProperty("token")]
        public string Token { get; set; } = "";

        [JsonProperty("session_id")]
        public string SessionId { get; set; } = "";

        [JsonProperty("seq")]
        public int? Seq { get; set; }
    }

    internal sealed class HelloData
    {
        [JsonProperty("heartbeat_interval")]
        public int HeartbeatInterval { get; set; }
    }

    internal sealed class ReadyData
    {
        [JsonProperty("session_id")]
        public string? SessionId { get; set; }
    }
}
