using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SenX_KOTH_Plugin.Bot
{
    internal sealed class DiscordRestClient : IDisposable
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "https://discord.com/api/v10";

        public DiscordRestClient(string token)
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("Authorization", "Bot " + token);
            _http.DefaultRequestHeaders.Add("User-Agent", "SenxDiscordBot/1.0");
        }

        public async Task<string> SendEmbedAsync(ulong channelId, DiscordMessage message)
        {
            var json = JsonConvert.SerializeObject(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{BaseUrl}/channels/{channelId}/messages", content);
            resp.EnsureSuccessStatusCode();
            var response = await resp.Content.ReadAsStringAsync();
            var msg = JsonConvert.DeserializeObject<DiscordMessage>(response);
            return msg?.Id ?? "";
        }

        public async Task EditMessageAsync(ulong channelId, ulong messageId, DiscordMessage message)
        {
            var json = JsonConvert.SerializeObject(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"{BaseUrl}/channels/{channelId}/messages/{messageId}")
            {
                Content = content
            };
            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
        }

        public async Task<string> CreateTextChannelAsync(ulong guildId, string name, ulong parentCategoryId)
        {
            var req = new CreateChannelRequest
            {
                Name = name,
                Type = 0,
                ParentId = parentCategoryId == 0 ? null : parentCategoryId.ToString(),
                PermissionOverwrites = BuildEveryoneOverwrite(guildId)
            };
            var json = JsonConvert.SerializeObject(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{BaseUrl}/guilds/{guildId}/channels", content);
            resp.EnsureSuccessStatusCode();
            var response = await resp.Content.ReadAsStringAsync();
            var ch = JsonConvert.DeserializeObject<DiscordChannel>(response);
            return ch?.Id ?? "";
        }

        private static List<PermissionOverwrite> BuildEveryoneOverwrite(ulong guildId)
        {
            const long VIEW_CHANNEL = 0x400;
            const long READ_MESSAGE_HISTORY = 0x40000;
            const long SEND_MESSAGES = 0x800;
            const long ADD_REACTIONS = 0x40;
            const long SEND_MESSAGES_IN_THREADS = 0x40000000;
            const long CREATE_PUBLIC_THREADS = 0x800000000;
            const long CREATE_PRIVATE_THREADS = 0x1000000000;
            const long USE_APPLICATION_COMMANDS = 0x8000000;

            return new List<PermissionOverwrite>
            {
                new PermissionOverwrite
                {
                    Id = guildId.ToString(),
                    Type = 0,
                    Allow = (VIEW_CHANNEL | READ_MESSAGE_HISTORY).ToString(),
                    Deny = (SEND_MESSAGES | ADD_REACTIONS | SEND_MESSAGES_IN_THREADS |
                            CREATE_PUBLIC_THREADS | CREATE_PRIVATE_THREADS |
                            USE_APPLICATION_COMMANDS).ToString()
                }
            };
        }

        public async Task<bool> ChannelExistsAsync(ulong channelId)
        {
            try
            {
                var resp = await _http.GetAsync($"{BaseUrl}/channels/{channelId}");
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<DiscordChannel?> GetChannelAsync(ulong channelId)
        {
            try
            {
                var resp = await _http.GetAsync($"{BaseUrl}/channels/{channelId}");
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DiscordChannel>(json);
            }
            catch
            {
                return null;
            }
        }

        public async Task ModifyChannelAsync(ulong channelId, string name)
        {
            var json = JsonConvert.SerializeObject(new { name });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"{BaseUrl}/channels/{channelId}")
            {
                Content = content
            };
            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
        }

        public async Task ModifyChannelPermissionsAsync(ulong channelId, ulong guildId)
        {
            var overwrites = BuildEveryoneOverwrite(guildId);
            var json = JsonConvert.SerializeObject(new { permission_overwrites = overwrites });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"{BaseUrl}/channels/{channelId}")
            {
                Content = content
            };
            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
        }

        public async Task DeleteChannelAsync(ulong channelId)
        {
            var resp = await _http.DeleteAsync($"{BaseUrl}/channels/{channelId}");
            resp.EnsureSuccessStatusCode();
        }

        public async Task<bool> GuildAccessibleAsync(ulong guildId)
        {
            try
            {
                var resp = await _http.GetAsync($"{BaseUrl}/guilds/{guildId}");
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
