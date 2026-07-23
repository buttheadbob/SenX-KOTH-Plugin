using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace SenX_KOTH_Plugin.DiscordAPI
{
    public sealed class DiscordWebHook
    {
        private static readonly HttpClient _http = new();

        public Uri? Uri { get; set; }

        public async Task SendAsync(DiscordMessage message)
        {
            if (Uri == null)
                return;

            string bound = "------------------------" + DateTime.Now.Ticks.ToString("x");
            MultipartFormDataContent httpContent = new MultipartFormDataContent(bound);

            StringContent jsonContent = new StringContent(JsonConvert.SerializeObject(message));
            jsonContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            httpContent.Add(jsonContent, "payload_json");

            HttpResponseMessage response = await _http.PostAsync(Uri, httpContent);
            if (!response.IsSuccessStatusCode)
            {
                throw new DiscordException(await response.Content.ReadAsStringAsync());
            }
        }
    }
}
