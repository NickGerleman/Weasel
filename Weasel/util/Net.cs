using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Wsl
{
    /// <summary>
    /// Network Utilities
    /// </summary>
    public static partial class Util
    {
        /// <summary>
        /// Whether the client has an available internet connection. This will
        /// make an HttpRequest and is thus potentially very slow.
        /// </summary>
        /// <param name="httpClient">An HttpClient to use for connection tests</param>
        public static async Task<bool> HasInternetConnectionAsync(HttpClient httpClient)
        {
            // There's no nice cross platform way to check internet
            // connectivity. We could add a bunch of P/Invoke shims for Windows
            // and OS X but we would need this for Linux anyway.
            using (var response = await httpClient.GetAsync("https://www.google.com/", HttpCompletionOption.ResponseHeadersRead))
                return response.StatusCode == HttpStatusCode.OK;

        }
    }
}
