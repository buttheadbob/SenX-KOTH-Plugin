using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;

// ReSharper disable InvalidXmlDocComment

// ReSharper disable once CheckNamespace
namespace SenX_KOTH_Plugin.DiscordAPI
{
    // This is based off N4T4NM work => https://github.com/N4T4NM/CSharpDiscordWebhook

    

    public sealed class DiscordMessage
    {
        readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public DiscordMessage()
        {
            Embeds = new List<DiscordEmbed>();
        }

        [JsonProperty(PropertyName = "content")]
        /// <summary>
        /// Message content
        /// </summary>
        public string Content { get; set; }

        [JsonProperty(PropertyName = "tts")]
        /// <summary>
        /// Read message to everyone on the channel
        /// </summary>
        public bool TTS { get; set; }

        [JsonProperty(PropertyName = "username")]
        /// <summary>
        /// Webhook profile username to be shown
        /// </summary>
        public string Username { get; set; }

        [JsonProperty(PropertyName = "avatar_url")]
        /// <summary>
        /// Webhook profile avater to be shown
        /// </summary>
        public string AvatarUrl { get; set; }

        [JsonProperty(PropertyName = "embeds")]
        /// <summary>
        /// List of embeds
        /// </summary>
        public List<DiscordEmbed> Embeds { get; set; }
        
        [JsonProperty(PropertyName = "allowed_mentions")]
        /// <summary>
        /// Allowed mentions for this message
        /// </summary>
        public AllowedMentions AllowedMentions { get; set; }
    }

    public class DiscordEmbed
    {
        public DiscordEmbed()
        {
            Fields = new List<EmbedField>();
        }

        [JsonProperty(PropertyName = "title")]
        /// <summary>
        /// Embed title
        /// </summary>
        public string Title { get; set; }

        [JsonProperty(PropertyName = "description")]
        /// <summary>
        /// Embed description
        /// </summary>
        public string Description { get; set; }

        [JsonProperty(PropertyName = "url")]
        /// <summary>
        /// Embed url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Embed timestamp
        /// </summary>
        public DateTime? Timestamp
        {
            get => DateTime.Parse(StringTimestamp);
            set => StringTimestamp = value?.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz");
        }

        [JsonProperty(PropertyName = "timestamp")]
        public string StringTimestamp { get; private set; }
        
        [JsonProperty(PropertyName = "color")]
        public int? Color { get; set; }

        [JsonProperty(PropertyName = "footer")]
        /// <summary>
        /// Embed footer
        /// </summary>
        public EmbedFooter Footer { get; set; }

        [JsonProperty(PropertyName = "image")]
        /// <summary>
        /// Embed image
        /// </summary>
        public EmbedMedia Image { get; set; }

        [JsonProperty(PropertyName = "thumbnail")]
        /// <summary>
        /// Embed thumbnail
        /// </summary>
        public EmbedMedia Thumbnail { get; set; }

        [JsonProperty(PropertyName = "video")]
        /// <summary>
        /// Embed video
        /// </summary>
        public EmbedMedia Video { get; set; }

        [JsonProperty(PropertyName = "provider")]
        /// <summary>
        /// Embed provider
        /// </summary>
        public EmbedProvider Provider { get; set; }

        [JsonProperty(PropertyName = "author")]
        /// <summary>
        /// Embed author
        /// </summary>
        public EmbedAuthor Author { get; set; }

        [JsonProperty(PropertyName = "fields")]
        /// <summary>
        /// Embed fields list
        /// </summary>
        public List<EmbedField> Fields { get; set; }
    }

    public class EmbedFooter
    {
        [JsonProperty(PropertyName = "text")]
        /// <summary>
        /// Footer text
        /// </summary>
        public string Text { get; set; }

        [JsonProperty(PropertyName = "icon_url")]
        /// <summary>
        /// Footer icon
        /// </summary>
        public string IconUrl { get; set; }

        [JsonProperty(PropertyName = "proxy_icon_url")]
        /// <summary>
        /// Footer icon proxy
        /// </summary>
        public string ProxyIconUrl { get; set; }
    }

    public class EmbedMedia
    {
        [JsonProperty(PropertyName = "url")]
        /// <summary>
        /// Media url
        /// </summary>
        public string Url { get; set; }

        [JsonProperty(PropertyName = "proxy_url")]
        /// <summary>
        /// Media proxy url
        /// </summary>
        public string ProxyUrl { get; set; }

        [JsonProperty(PropertyName = "height")]
        /// <summary>
        /// Media height
        /// </summary>
        public int? Height { get; set; }

        [JsonProperty(PropertyName = "width")]
        /// <summary>
        /// Media width
        /// </summary>
        public int? Width { get; set; }
    }

    public class EmbedProvider
    {
        [JsonProperty(PropertyName = "name")]
        /// <summary>
        /// Provider name
        /// </summary>
        public string Name { get; set; }

        [JsonProperty(PropertyName = "url")]
        /// <summary>
        /// Provider url
        /// </summary>
        public string Url { get; set; }
    }

    public class EmbedAuthor
    {
        [JsonProperty(PropertyName = "name")]
        /// <summary>
        /// Author name
        /// </summary>
        public string Name { get; set; }

        [JsonProperty(PropertyName = "url")]
        /// <summary>
        /// Author url
        /// </summary>
        public string Url { get; set; }

        [JsonProperty(PropertyName = "icon_url")]
        /// <summary>
        /// Author icon
        /// </summary>
        public string IconUrl { get; set; }

        [JsonProperty(PropertyName = "proxy_icon_url")]
        /// <summary>
        /// Author icon proxy
        /// </summary>
        public string ProxyIconUrl { get; set; }
    }

    public class EmbedField
    {
        [JsonProperty(PropertyName = "name")]
        /// <summary>
        /// Field name
        /// </summary>
        public string Name { get; set; }

        [JsonProperty(PropertyName = "value")]
        /// <summary>
        /// Field value
        /// </summary>
        public string Value { get; set; }

        [JsonProperty(PropertyName = "inline")]
        /// <summary>
        /// Field align
        /// </summary>
        public bool? InLine { get; set; }
    }

    public class AllowedMentions
    {
        [JsonProperty(PropertyName = "parse")]
        /// <summary>
        /// List of allowd mention types to parse from the content
        /// </summary>
        public List<string> Parse { get; set; }
        [JsonProperty(PropertyName = "roles")]
        /// <summary>
        /// List of role_ids to mention
        /// </summary>
        public List<ulong> Roles { get; set; }
        [JsonProperty(PropertyName = "users")]
        /// <summary>
        /// List of user_ids to mention
        /// </summary>
        public List<ulong> Users { get; set; }
    }
    
}
