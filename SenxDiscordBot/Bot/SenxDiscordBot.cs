using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SenX_KOTH_Plugin.Bot
{
    public sealed class DiscordBotClient : IDisposable
    {
        private readonly GatewayConnection _gateway;
        private readonly DiscordRestClient _rest;

        internal DiscordBotClient(string token)
        {
            _rest = new DiscordRestClient(token);
            _gateway = new GatewayConnection(token);
        }

        public async Task StartAsync() => await _gateway.StartAsync();

        public bool IsConnected => _gateway.IsConnected;

        public async Task<ulong> SendEmbedAsync(ulong channelId, string title, string description, uint color)
        {
            var msg = new DiscordMessage
            {
                Embeds = new List<DiscordEmbed>
                {
                    new DiscordEmbed
                    {
                        Title = title,
                        Description = description,
                        Color = color
                    }
                }
            };
            var result = await _rest.SendEmbedAsync(channelId, msg);
            return ulong.TryParse(result, out var id) ? id : 0;
        }

        public async Task EditMessageAsync(ulong channelId, ulong messageId, string content)
        {
            var msg = new DiscordMessage { Content = content };
            await _rest.EditMessageAsync(channelId, messageId, msg);
        }

        public async Task EditMessageAsync(ulong channelId, ulong messageId, DiscordMessage message)
        {
            await _rest.EditMessageAsync(channelId, messageId, message);
        }

        public async Task<ulong> CreateTextChannelAsync(ulong guildId, string name, ulong parentCategoryId = 0)
        {
            var result = await _rest.CreateTextChannelAsync(guildId, name, parentCategoryId);
            return ulong.TryParse(result, out var id) ? id : 0;
        }

        public async Task<bool> ChannelExistsAsync(ulong channelId)
        {
            return await _rest.ChannelExistsAsync(channelId);
        }

        public async Task<DiscordChannel?> GetChannelAsync(ulong channelId)
        {
            return await _rest.GetChannelAsync(channelId);
        }

        public async Task ModifyChannelAsync(ulong channelId, string name)
        {
            await _rest.ModifyChannelAsync(channelId, name);
        }

        public async Task ModifyChannelPermissionsAsync(ulong channelId, ulong guildId)
        {
            await _rest.ModifyChannelPermissionsAsync(channelId, guildId);
        }

        public async Task DeleteChannelAsync(ulong channelId)
        {
            await _rest.DeleteChannelAsync(channelId);
        }

        public async Task<bool> GuildAccessibleAsync(ulong guildId)
        {
            return await _rest.GuildAccessibleAsync(guildId);
        }

        public void Dispose()
        {
            _gateway.Dispose();
            _rest.Dispose();
        }
    }

    public sealed class BotBuilder
    {
        private string _token = "";

        internal BotBuilder() { }

        public BotBuilder WithToken(string token)
        {
            _token = token;
            return this;
        }

        public DiscordBotClient Build()
        {
            if (string.IsNullOrEmpty(_token))
                throw new InvalidOperationException("Token is required.");
            return new DiscordBotClient(_token);
        }
    }

    public static class SenxDiscordBot
    {
        public static BotBuilder CreateBuilder() => new BotBuilder();
    }
}
