using System;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using Rs.Image;
using Rs.Mock;
using Rs.Net;
using Xunit;

using HeaderList = System.Collections.Generic.List<System.Tuple<string, string>>;
using System.Collections.Generic;

namespace Rs.Test
{

    public class ImgurDetectorTest
    {
        private const string BaseUrl = "http://imgur.com";
        private const string ClientId = "abcdefg";
        private const string DefaultId = "abcd";
        private const string DefaultUrl = "http://i.imgur.com/abcd.jpg";

        private static readonly HeaderList DefaultHeaders = new HeaderList
        {
            Tuple.Create("X-RateLimit-ClientRemaining", "12500")
        };


        private static readonly HeaderList LimitedHeaders = new HeaderList
        {
            Tuple.Create("X-RateLimit-ClientRemaining", "0")
        };


        private const string DefaultCreditReponse = 
        @"{
            ""data"": {
                ""UserLimit"": 500,
                ""UserRemaining"": 500,
                ""UserReset"": 1483062277,
                ""ClientLimit"": 12500,
                ""ClientRemaining"": 12500
            },
            ""success"": true,
            ""status"": 200
        }";


        private const string LimitedCreditReponse = 
        @"{
            ""data"": {
                ""UserLimit"": 500,
                ""UserRemaining"": 500,
                ""UserReset"": 1483062277,
                ""ClientLimit"": 12500,
                ""ClientRemaining"": 0
            },
            ""success"": true,
            ""status"": 200
        }";


        private static readonly ImageRecord PngRecord = new ImageRecord
        {
            Url = new Uri("http://i.imgur.com/8tDeuAI.png"),
            Format = ImageFormat.Png
        };


        private readonly ImgurDetector mDetector;
        private readonly MockHttpClient mMockClient;


        public ImgurDetectorTest()
        {
            Logger.SetLoggingEnabled(false);
            mMockClient = DefaultMockClient();
        
            var detectorBuildTask = ImgurDetector.BuildAsync(ClientId, mMockClient);
            detectorBuildTask.Wait();
            mDetector = detectorBuildTask.Result;
        }


        private static MockHttpClient DefaultMockClient() {
            return MockHttpClient.Build("https://api.imgur.com/3")
                   .ExpectHeaderPattern(".*", HttpRequestHeader.Authorization, $"Client-ID {ClientId}")
                   .SetResponse("/credits", new MockHttpResponse {Content = DefaultCreditReponse});
        }


        [Fact]
        public void InitialState()
        {
            Assert.Equal(expected: ImageDetectorState.Good, actual: mDetector.State);
        }


        [Theory]
        [InlineData("http://i.imgur.com/abc.jpg", "/abc")]
        [InlineData("/album/abc", "/a/abc")]
        public void BadNetwork(string mockUrl, string detectionUrl)
        {
            mMockClient.SetResponse(mockUrl, new MockHttpResponse { StatusCode = HttpStatusCode.GatewayTimeout });

            Assert.Throws(typeof(AggregateException), () =>
            {
                mDetector.DetectImagesAsync(new Uri($"{BaseUrl}{detectionUrl}")).Wait();
            });
            Assert.Equal(expected: ImageDetectorState.BadNetwork, actual: mDetector.State);

            Thread.Sleep(4);
            mDetector.CheckStateAsync().Wait();
            Assert.Equal(expected: ImageDetectorState.Good, actual: mDetector.State);
        }


        [Fact]
        public void RateLimitedFromResponse()
        {
            var album = CreateAlbum(new List<string> { CreateImageModel() });
            mMockClient.SetResponse("/album/abc", new MockHttpResponse
            {
                Content = album,
                Headers = LimitedHeaders
            });

            var task = mDetector.DetectImagesAsync(new Uri($"{BaseUrl}/a/abc"));
            task.Wait();

            var imageRecord = new ImageRecord { Format = ImageFormat.Jpg, Url = new Uri(DefaultUrl) };
            Assert.NotEmpty(task.Result);
            Assert.Equal(expected: imageRecord, actual: task.Result[0]);
            Assert.Equal(expected: ImageDetectorState.RateLimited, actual: mDetector.State);
        }


        [Fact]
        public void RateLimitedFromCreditCheck()
        {
            mMockClient.SetResponse("/credits", new MockHttpResponse { Content = LimitedCreditReponse });
            mDetector.CheckStateAsync().Wait();
            Assert.Equal(expected: ImageDetectorState.RateLimited, actual: mDetector.State);

            Thread.Sleep(4);
            mMockClient.SetResponse("/credits", new MockHttpResponse { Content = DefaultCreditReponse });
            mDetector.CheckStateAsync().Wait();
            Assert.Equal(expected: ImageDetectorState.Good, actual: mDetector.State);
        }


        [Fact]
        public void BadStart()
        {
            var httpClient = MockHttpClient.Build("https://api.imgur.com/3")
                .ExpectHeaderPattern(".*", HttpRequestHeader.Authorization, $"Client-ID {ClientId}")
                .SetResponse("/credit", new MockHttpResponse { StatusCode = HttpStatusCode.GatewayTimeout });
        
            var detectorTask = ImgurDetector.BuildAsync(ClientId, httpClient);
            detectorTask.Wait();
            var detector = detectorTask.Result;
            Assert.Equal(expected: ImageDetectorState.BadNetwork, actual: detector.State);

            Thread.Sleep(4);
            httpClient.SetResponse("/credit", new MockHttpResponse { Content = DefaultCreditReponse });
            mDetector.CheckStateAsync().Wait();
            Assert.Equal(expected: ImageDetectorState.Good, actual: mDetector.State);
        }


        [Fact]
        public void NotFound()
        {
            mMockClient.SetResponse("/image/abc", new MockHttpResponse { StatusCode = HttpStatusCode.NotFound });
            var task = mDetector.DetectImagesAsync(new Uri($"{BaseUrl}/abc"));
            task.Wait();
        
            Assert.Empty(task.Result);
            Assert.Equal(expected: ImageDetectorState.Good, actual: mDetector.State);
        }


        [Theory]
        [InlineData("http://imgur.com/abcd")]
        [InlineData("https://imgur.com/abcd")]
        [InlineData("http://imgur.com/a/abcd")]
        [InlineData("http://i.imgur.com/abcd.jpg")]
        [InlineData("http://m.imgur.com/abcd.jpg")]
        public void CanProcess(string url)
        {
            Assert.True(mDetector.CanProcessPage(new Uri(url)));
        }

    
        [Theory]
        [InlineData("http://imgur.com/abcd/a")]
        [InlineData("http://www.imgur.com/abcd/abcd")]
        [InlineData("http://i.imgur.com/abcd/abcd")]
        [InlineData("http://m.imgur.com/abcd/abcd")]
        [InlineData("http://imgur.com/a/")]
        [InlineData("http://imgur.com/a/abcd/a")]
        public void CantProcess(string url)
        {
            Assert.False(mDetector.CanProcessPage(new Uri(url)));
        }


        [Fact]
        public void SingleImage()
        {
            mMockClient.SetResponse("http://i.imgur.com/abc.jpg", new MockHttpResponse
            {
                ContentType = new MediaTypeHeaderValue("image/jpeg")
            });
        
            var task = mDetector.DetectImagesAsync(new Uri($"{BaseUrl}/abc"));
            task.Wait(); 
            Assert.NotEmpty(task.Result);
            var record = task.Result[0];
        
            Assert.Equal(actual: record, expected: new ImageRecord
            {
                Format = ImageFormat.Jpg,
                Url = new Uri("http://i.imgur.com/abc.jpg")
            });
            Assert.Equal(expected: ImageDetectorState.Good, actual: mDetector.State);
        }


        [Fact]
        public void RawUrl()
        {
            mMockClient.SetResponse("http://i.imgur.com/abc.jpg", new MockHttpResponse
            {
                ContentType = new MediaTypeHeaderValue("image/jpeg")
            });
        
            var task = mDetector.DetectImagesAsync(new Uri($"http://i.imgur.com/abc.jpg"));
            task.Wait();
            Assert.NotEmpty(task.Result);
            var record = task.Result[0];

            Assert.Equal(actual: record, expected: new ImageRecord
            {
                Format = ImageFormat.Jpg,
                Url = new Uri("http://i.imgur.com/abc.jpg")
            });
            Assert.Equal(expected: ImageDetectorState.Good, actual: mDetector.State);
        }


        [Fact]
        public void Album()
        {
            var aUrl = "http://i.imgur.com/aaa.jpg";
            var bUrl = "http://i.imgur.com/bbb.jpg";

            var albumContent = CreateAlbum(new List<string>
            {
                CreateImageModel("aaa", "image/jpg", aUrl),
                CreateImageModel("bbb", "image/png", bUrl)
            });

            mMockClient.SetResponse("/album/abcd", new MockHttpResponse { Content = albumContent });
            var task = mDetector.DetectImagesAsync(new Uri($"{BaseUrl}/a/abcd"));
            task.Wait();

            Assert.Equal(expected: 2, actual: task.Result.Count);

            Assert.Equal(expected: ImageFormat.Jpg, actual: task.Result[0].Format);
            Assert.Equal(expected: aUrl, actual: task.Result[0].Url.ToString());

            Assert.Equal(expected: ImageFormat.Png, actual: task.Result[1].Format);
            Assert.Equal(expected: bUrl, actual: task.Result[1].Url.ToString());
        }


        [Theory]
        [InlineData("image/jpeg", ImageFormat.Jpg)]
        [InlineData("image/gif", ImageFormat.Gif)]
        [InlineData("image/png", ImageFormat.Png)]
        public void FileTypesFromApi(string mimeType, ImageFormat format)
        {
            var albumContent = CreateAlbum(new List<string>
            {
                CreateImageModel(mimeType: mimeType)
            });

            mMockClient.SetResponse("/album/abc", new MockHttpResponse
            {
                Content = albumContent,
                Headers = DefaultHeaders
            });
            var task = mDetector.DetectImagesAsync(new Uri($"{BaseUrl}/a/abc"));
            task.Wait();
        
            Assert.NotEmpty(task.Result);
            var record = task.Result[0];
            Assert.Equal(expected: format, actual: record.Format);
        }


        [Theory]
        [InlineData("image/jpeg", ImageFormat.Jpg)]
        [InlineData("image/gif", ImageFormat.Gif)]
        [InlineData("image/png", ImageFormat.Png)]
        public void FileTypesFromRawImage(string mimeType, ImageFormat format)
        {
            mMockClient.SetResponse("http://i.imgur.com/abc.jpg", new MockHttpResponse
            {
                ContentType = new MediaTypeHeaderValue(mimeType)
            });
            var task = mDetector.DetectImagesAsync(new Uri($"{BaseUrl}/abc"));
            task.Wait();

            Assert.NotEmpty(task.Result);
            var record = task.Result[0];
            Assert.Equal(expected: format, actual: record.Format);
        }


        private static string CreateImageResponse(string mimeType = "image/jpeg", string link = DefaultUrl)
        {
            return $@"{{
                ""data"": {CreateImageModel(mimeType, link)},
                ""success"": true,
                ""status"": 200
            }}";
        }


        private static string CreateImageModel(string id = "abcd", string mimeType = "image/jpg", string link = DefaultUrl)
        {
            return $@"{{
                ""id"": ""{id}"",
                ""title"": null,
                ""description"": null,
                ""datetime"": 1386991994,
                ""type"": ""{mimeType}"",
                ""animated"": false,
                ""width"": 960,
                ""height"": 720,
                ""size"": 70904,
                ""views"": 2831009,
                ""bandwidth"": 200729862136,
                ""vote"": null,
                ""favorite"": false,
                ""nsfw"": false,
                ""section"": ""funny"",
                ""account_url"": null,
                ""account_id"": null,
                ""is_ad"": false,
                ""in_gallery"": true,
                ""link"": ""{link}""
            }}";
        }


        private static string CreateAlbum(List<string> imageModels, string url = "http://imgur.com/a/lDRB2")
        {
            return $@"{{
                ""data"": {{
                    ""id"": ""lDRB2"",
                    ""title"": ""Imgur Office"",
                    ""description"": null,
                    ""datetime"": 1357856292,
                    ""cover"": ""24nLu"",
                    ""account_url"": ""Alan"",
                    ""account_id"": 4,
                    ""privacy"": ""public"",
                    ""layout"": ""blog"",
                    ""views"": 13780,
                    ""link"": ""{url}"",
                    ""images_count"": {imageModels.Count},
                    ""images"": [{String.Join(",", imageModels)}]
                }},
                ""success"": true,
                ""status"": 200
            }}";
        }

    }

}
