using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Wsl.Image;

using static Wsl.LoggerFor<Wsl.Detection.ImgurDetector>;


namespace Wsl.Detection
{

    /// <summary>
    /// ImageDetector that is able to detect images from URLs pointing to
    /// Imgur images or albums. Gallery and meme endpoints are unsuported as
    /// they are exceedingly rare (and the APIs for them are awful).
    /// </summary>
    public class ImgurDetector : AbstractImageDetector
    {

        private readonly string mClientId;
        private readonly HttpClient mHttpClient;


        public override string ServiceName => "Imgur";


        /// <summary>
        /// Construct the ImgurDetector without checking state
        /// </summary>
        /// <param name="clientId">The imgur API Client ID</param>
        /// <param name="httpClient">An HttpClient to use</param>
        private ImgurDetector(string clientId, HttpClient httpClient)
        {
            mClientId = clientId;
            mHttpClient = httpClient;
        }


        /// <summary>
        /// Build and check the state of the detector
        /// </summary>
        /// <param name="clientId">The imgur API Client ID</param>
        /// <param name="httpClient">An HttpClient to use</param>
        public static async Task<ImgurDetector> BuildAsync(string clientId, HttpClient httpClient)
        {
            var detector = new ImgurDetector(clientId, httpClient);
            await detector.CheckStateAsync();
            return detector;
        }


        public override bool CanProcessPage(Uri url)
        {
            var correctHost = url.Host == "imgur.com"
                           || url.Host == "www.imgur.com"
                           || url.Host == "i.imgur.com"
                           || url.Host == "m.imgur.com";
        
            var pathSegments = url.PathAndQuery.Split('/');
            if (!correctHost || pathSegments.Last() == "")
                return false;

            return pathSegments.Length == 2
                || (pathSegments.Length == 3 && pathSegments[1] == "a");
        }


        protected async override Task<List<ImageRecord>> DetectImagesAsyncCore(Uri url)
        {
            var pathSegments = url.PathAndQuery.Split('/');
            var id = pathSegments.Last().Split('.')[0];

            if (pathSegments[1] == "a")
                return await DetectAlbumImagesAsync(id);
            else
                return await DetectSingleImageAsync(id);
        }


        protected async override Task<ImageDetectorState> CheckStateAsyncCore()
        {
            using (var response = await RequestAsync("/credits"))
            {
                if (!response.IsSuccessStatusCode)
                    return ImageDetectorState.BadNetwork;

                var resString = await response.Content.ReadAsStringAsync();
                dynamic resObject = JObject.Parse(resString);
                int remainingRequests = resObject.data.ClientRemaining;

                if (remainingRequests > 0)
                    return ImageDetectorState.Good;
                else
                    return ImageDetectorState.RateLimited;
            }
        }


        /// <summary>
        /// Detect images for an album
        /// </summary>
        /// <param name="albumId">The id of the album</param>
        /// <param name="httpClient">An HttpClient to use</param>
        private async Task<List<ImageRecord>> DetectAlbumImagesAsync(string albumId)
        {
            using (var response = await RequestAsync($"/album/{albumId}"))
            {
                if (!CheckStateFromResponse(response))
                    return new List<ImageRecord>();

                var resString = await response.Content.ReadAsStringAsync();
                dynamic resObject = JObject.Parse(resString);
                dynamic imageModels = resObject.data.images;
                var records = new List<ImageRecord>();

                foreach (dynamic imageModel in imageModels)
                    records.Add(new ImageRecord(new Uri((string)imageModel.link), (string)imageModel.type));

                return records;
            }
        }


        /// <summary>
        /// Detect images for a single image Url
        /// </summary>
        /// <param name="imageId">The id of the image</param>
        /// <param name="httpClient">An HttpClient to use</param>
        private async Task<List<ImageRecord>> DetectSingleImageAsync(string imageId)
        {
            // Avoid using the API so that we conserve requests and don't get rate
            // limited. We can say jpg and it will gice us the correct format in
            // the header
            var url = $"http://i.imgur.com/{imageId}.jpg";

            using (var response = await mHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                 if (!CheckStateFromResponse(response))
                    return new List<ImageRecord>();

                // Modify the url to represent the actual file type
                var mediaType = response.Content.Headers.ContentType.MediaType;
                if (mediaType == "image/gif")
                    url.Replace(".jpg", ".gif");
                else if (mediaType == "image/png")
                    url.Replace(".jpg", ".png");

                var record = new ImageRecord(new Uri(url), mediaType);
                return new List<ImageRecord> {record};
            }
        }


        /// <summary>
        /// Make a request to the imgur API
        /// </summary>
        /// <param name="endpoint">
        /// The endpoint to make the request to. Eg "/image/id
        /// </param>
        /// <param name="httpClient">
        /// The client used to make the request
        /// </param>
        private async Task<HttpResponseMessage> RequestAsync(string relativePath)
        {
            var apiRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.imgur.com/3{relativePath}");
            apiRequest.Headers.Authorization = new AuthenticationHeaderValue($"Client-ID", mClientId);
        
            return await mHttpClient.SendAsync(apiRequest);
        }


        /// <summary>
        /// Set our state based on the response from the imgur API
        /// </summary>
        /// <param name="res">The response from the API</param>
        /// <returns>Whether the response is actionable</returns>
        private bool CheckStateFromResponse(HttpResponseMessage res)
        {
            if (res.StatusCode == HttpStatusCode.NotFound)
            {
                mState = ImageDetectorState.Good;
                return false;
            }
            else if (!res.IsSuccessStatusCode)
            {
                mState = ImageDetectorState.BadNetwork;
                return false;
            }
            else if (!res.Headers.Contains("X-RateLimit-ClientRemaining"))
            {
                mState = ImageDetectorState.Good;
                return true;
            }

            var rateLimitStr = res.Headers
                                  .GetValues("X-RateLimit-ClientRemaining")
                                  .First();

            if (rateLimitStr == "0")
                mState = ImageDetectorState.RateLimited;
            else
                mState = ImageDetectorState.Good;

            return true;
        }

    }

}
