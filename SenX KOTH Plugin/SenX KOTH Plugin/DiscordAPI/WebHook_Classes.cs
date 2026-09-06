using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SenX_KOTH_Plugin.DiscordAPI
{
    public sealed class DiscordMessage
    {
        public DiscordMessage()
        {
            Embeds = new List<DiscordEmbed>();
        }

        [JsonProperty(PropertyName = "content")]
        public string? Content { get; set; }

        [JsonProperty(PropertyName = "tts")]
        public bool TTS { get; set; }

        [JsonProperty(PropertyName = "username")]
        public string? Username { get; set; }

        [JsonProperty(PropertyName = "avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonProperty(PropertyName = "embeds")]
        public List<DiscordEmbed> Embeds { get; set; }

        [JsonProperty(PropertyName = "allowed_mentions")]
        public AllowedMentions? AllowedMentions { get; set; }
    }

    public class DiscordEmbed
    {
        public DiscordEmbed()
        {
            Fields = new List<EmbedField>();
        }

        [JsonProperty(PropertyName = "title")]
        public string? Title { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string? Description { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string? Url { get; set; }

        public DateTime? Timestamp
        {
            get
            {
                if (string.IsNullOrEmpty(StringTimestamp))
                    return null;
                return DateTime.Parse(StringTimestamp);
            }
            set => StringTimestamp = value?.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz");
        }

        [JsonProperty(PropertyName = "timestamp")]
        public string? StringTimestamp { get; private set; }

        [JsonProperty(PropertyName = "color")]
        public int? Color { get; set; }

        [JsonProperty(PropertyName = "footer")]
        public EmbedFooter? Footer { get; set; }

        [JsonProperty(PropertyName = "image")]
        public EmbedMedia? Image { get; set; }

        [JsonProperty(PropertyName = "thumbnail")]
        public EmbedMedia? Thumbnail { get; set; }

        [JsonProperty(PropertyName = "video")]
        public EmbedMedia? Video { get; set; }

        [JsonProperty(PropertyName = "provider")]
        public EmbedProvider? Provider { get; set; }

        [JsonProperty(PropertyName = "author")]
        public EmbedAuthor? Author { get; set; }

        [JsonProperty(PropertyName = "fields")]
        public List<EmbedField> Fields { get; set; }
    }

    public class EmbedFooter
    {
        [JsonProperty(PropertyName = "text")]
        public string? Text { get; set; }

        [JsonProperty(PropertyName = "icon_url")]
        public string? IconUrl { get; set; }

        [JsonProperty(PropertyName = "proxy_icon_url")]
        public string? ProxyIconUrl { get; set; }
    }

    public class EmbedMedia
    {
        [JsonProperty(PropertyName = "url")]
        public string? Url { get; set; }

        [JsonProperty(PropertyName = "proxy_url")]
        public string? ProxyUrl { get; set; }

        [JsonProperty(PropertyName = "height")]
        public int? Height { get; set; }

        [JsonProperty(PropertyName = "width")]
        public int? Width { get; set; }
    }

    public class EmbedProvider
    {
        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string? Url { get; set; }
    }

    public class EmbedAuthor
    {
        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string? Url { get; set; }

        [JsonProperty(PropertyName = "icon_url")]
        public string? IconUrl { get; set; }

        [JsonProperty(PropertyName = "proxy_icon_url")]
        public string? ProxyIconUrl { get; set; }
    }

    public class EmbedField
    {
        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string? Value { get; set; }

        [JsonProperty(PropertyName = "inline")]
        public bool? InLine { get; set; }
    }

    public class AllowedMentions
    {
        [JsonProperty(PropertyName = "parse")]
        public List<string>? Parse { get; set; }

        [JsonProperty(PropertyName = "roles")]
        public List<ulong>? Roles { get; set; }

        [JsonProperty(PropertyName = "users")]
        public List<ulong>? Users { get; set; }
    }
}
