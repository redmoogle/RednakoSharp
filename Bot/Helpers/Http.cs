using Newtonsoft.Json.Linq;

namespace RednakoSharp.Helpers
{
    internal sealed class Http
    {
        /// <summary>
        ///  HttpClient for API requests
        /// </summary>
        private static readonly HttpClient Client = new();

        /// <summary>
        /// Performs a get request and returns a awaitable JObject
        /// </summary>
        /// <param name="url">HTTP URL</param>
        /// <returns>JObject</returns>
        public static async Task<JObject> HttpAPIRequest(string url)
        {
            HttpResponseMessage response = await Client.GetAsync(url);
            JObject json = JObject.Parse(await response.Content.ReadAsStringAsync());
            return json;
        }
    }
}
