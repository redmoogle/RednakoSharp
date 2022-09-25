using Newtonsoft.Json.Linq;

namespace RednakoSharp.Helpers
{
    internal sealed class Http
    {
        /// <summary>
        ///  HttpClient for API requests
        /// </summary>
        private static readonly HttpClient Client = new();
        public static async Task<JObject> HttpAPIRequest(string url)
        {
            HttpClient request = Client;
            HttpResponseMessage response = await request.GetAsync(url);
            JObject json = JObject.Parse(await response.Content.ReadAsStringAsync());
            return json;
        }
    }
}
