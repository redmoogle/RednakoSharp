using Newtonsoft.Json.Linq;

namespace RednakoSharp.Helpers
{
    public static class HttpHelper
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
        public static async Task<JObject> HttpApiRequest(string url)
        {
            HttpResponseMessage response = await Client.GetAsync(new Uri(url));
            return JObject.Parse(await response.Content.ReadAsStringAsync());
        }

        public static async Task<JObject> HttpApiRequest(Uri url)
        {
            HttpResponseMessage response = await Client.GetAsync(url);
            return JObject.Parse(await response.Content.ReadAsStringAsync());
        }
    }
}
