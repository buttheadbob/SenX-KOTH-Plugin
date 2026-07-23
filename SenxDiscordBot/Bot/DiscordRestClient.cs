using System;
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
                ParentId = parentCategoryId == 0 ? null : parentCategoryId.ToString()
            };
            var json = JsonConvert.SerializeObject(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{BaseUrl}/guilds/{guildId}/channels", content);
            resp.EnsureSuccessStatusCode();
            var response = await resp.Content.ReadAsStringAsync();
            var ch = JsonConvert.DeserializeObject<DiscordChannel>(response);
            return ch?.Id ?? "";
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

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
