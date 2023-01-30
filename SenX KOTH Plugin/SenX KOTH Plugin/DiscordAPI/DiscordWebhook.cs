using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace SenX_KOTH_Plugin.DiscordAPI
{
    // This is based off N4T4NM work => https://github.com/N4T4NM/CSharpDiscordWebhook
    public sealed class DiscordWebHook
    {
        /// <summary>
        /// WebHook url
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// Send WebHook message
        /// </summary>
        public async Task SendAsync(DiscordMessage message)
        {
            var httpClient = new HttpClient();

            string bound = "------------------------" + DateTime.Now.Ticks.ToString("x");
            var httpContent = new MultipartFormDataContent(bound);

            var jsonContent = new StringContent(JsonConvert.SerializeObject(message));
            jsonContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            httpContent.Add(jsonContent, "payload_json");

            var response = await httpClient.PostAsync(Uri, httpContent);
            if (!response.IsSuccessStatusCode)
            {
                throw new DiscordException(await response.Content.ReadAsStringAsync());
            }
        }
    }
}
