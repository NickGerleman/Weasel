using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

using HeaderList = System.Collections.Generic.List<System.Tuple<string, string>>;

namespace Rs.Mock
{

    /// <summary>
    /// Fields of an HttpResponseMessage to mock
    /// </summary>
    public class MockHttpResponse
    {
        public MediaTypeHeaderValue ContentType { get; set; } = new MediaTypeHeaderValue("application/json");
        public string Content { get; set; } = "";
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public HeaderList Headers { get; set; } = new HeaderList();
    }


    /// <summary>
    /// HttpMessageHandler with predefined responses.
    /// </summary>
    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        private List<Tuple<Regex, HttpResponseMessage>> mMockResponses;
        private List<Tuple<Regex, HeaderList>> mExpectedHeaders;
        private readonly string mBaseUrl;


        /// <summary>
        /// Create the handler
        /// </summary>
        /// <param name="baseUrl">The base URL of all endpoints</param>
        public MockHttpMessageHandler(string baseUrl)
        {
            mBaseUrl = baseUrl;
            mMockResponses = new List<Tuple<Regex, HttpResponseMessage>>();
            mExpectedHeaders = new List<Tuple<Regex, HeaderList>>();
        }


        /// <summary>
        /// Set the expectation that a header is present in a request. Calls made
        /// with an existing endpoint will replace the previous expectation.
        /// </summary>
        /// <param name="endpoint">The endpoint the request is made to</param>
        /// <param name="header">The header key</param>
        /// <param name="value">The expected header value</param>
        public void ExpectHeader(string endpoint, HttpRequestHeader header, string value)
        {
            ExpectHeaderPattern(Regex.Escape(endpoint), header, value);
        }


        /// <summary>
        /// Set the expectation that a header is present in a request. Calls made
        /// with an existing endpoint will replace the previous expectation. All
        /// expectations are checked, even after a match.
        /// </summary>
        /// <param name="endpointRegex">A regex to match multiple endpoints</param>
        /// <param name="header">The header key</param>
        /// <param name="value">The expected header value</param>
        public void ExpectHeaderPattern(string endpointRegex, HttpRequestHeader header, string value)
        {
            var absoluteUrlPattern = new Regex($"^{Regex.Escape(mBaseUrl)}{endpointRegex}$");
        
            var expectedHeaders = GetOrMakeValue(absoluteUrlPattern, mExpectedHeaders, () => new HeaderList());
            expectedHeaders.Add(Tuple.Create(header.ToString(), value));
        }


        /// <summary>
        /// Set a predefined response to a request. Calls made with an existing
        /// endpoint will replace the previous response.
        /// </summary>
        /// <param name="endpoint">The endpoint being requested</param>
        /// <param name="response">A predefined response</param>
        public void SetResponse(string endpoint, MockHttpResponse response)
        {
            SetResponsePattern(Regex.Escape(endpoint), response);
        }


        /// <summary>
        /// Set a predefined response to a request. Calls made with an existing
        /// endpoint will replace the previous response.
        /// </summary>
        /// <param name="endpointRegex">Pattern for the requested endpoint</param>
        /// <param name="givenResponse">A predefined response</param>
        public void SetResponsePattern(string endpointRegex, MockHttpResponse givenResponse)
        {
            if (!Regex.IsMatch(endpointRegex, @"https?:\/\/"))
                endpointRegex = Regex.Escape(mBaseUrl) + endpointRegex;
            var absoluteUrlPattern = new Regex(endpointRegex);
        
            var httpResponse = GetOrMakeValue(absoluteUrlPattern, mMockResponses, () => new HttpResponseMessage());
            httpResponse.StatusCode = givenResponse.StatusCode;
            httpResponse.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(givenResponse.Content));
            httpResponse.Content.Headers.ContentType = givenResponse.ContentType;

            httpResponse.Headers.Clear();
            foreach(var header in givenResponse.Headers)
                httpResponse.Headers.Add(header.Item1, header.Item2);
        }


    #pragma warning disable 1998
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string requestUrl = request.RequestUri.AbsoluteUri;

            foreach (var headerExpectation in mExpectedHeaders)
            {
                if (headerExpectation.Item1.IsMatch(requestUrl))
                    AssertHasAllHeaders(request, headerExpectation.Item2);
            }

            foreach (var response in mMockResponses)
            {
                if (response.Item1.IsMatch(requestUrl))
                    return response.Item2;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    #pragma warning restore 1998


        /// <summary>
        /// Assert that all required heders are present in a request
        /// </summary>
        /// <param name="request">The request made to the handler</param>
        /// <param name="requiredHeaders">The required headers</param>
        private void AssertHasAllHeaders(HttpRequestMessage request, HeaderList requiredHeaders)
        {
            HttpRequestHeaders actualHeaders = request.Headers;

            foreach (var expectedHeader in requiredHeaders)
            {
                Assert.True(actualHeaders.Contains(expectedHeader.Item1), $"Header {expectedHeader.Item1} not found");
                var actualValues = actualHeaders.GetValues(expectedHeader.Item1);
                Assert.Contains(expectedHeader.Item2, actualValues);
            }
        }


        /// <summary>
        /// Get a value from a Url map or create it if not present.
        /// </summary>
        /// <param name="key">The url to look for</param>
        /// <param name="urlList">The list to look through</param>
        /// <param name="constructorFn">A function to create the value</param>
        /// <returns></returns>
        private static T GetOrMakeValue<T>(Regex key, List<Tuple<Regex, T>> urlList, Func<T> constructorFn)
        {
            foreach (var mapping in urlList)
            {
                if (mapping.Item1.ToString().Equals(key.ToString()))
                    return mapping.Item2;
            }

            T newValue = constructorFn();
            urlList.Add(Tuple.Create(key, newValue));
            return newValue;
        }
    }


    /// <summary>
    /// HttpClient that serves predefined responses
    /// </summary>
    public class MockHttpClient : HttpClient
    {
        private MockHttpMessageHandler mMessageHandler;


        /// <summary>
        /// Construct the client from a mock handler.
        /// </summary>
        /// <param name="handler">A mock HttpMessageHandler</param>
        private MockHttpClient(MockHttpMessageHandler handler) : base(handler) {}


        /// <summary>
        /// Build the MockHttpClient
        /// </summary>
        /// <param name="baseUrl">The base Url for mock endpoints</param>
        public static MockHttpClient Build(string baseUrl)
        {
            var handler = new MockHttpMessageHandler(baseUrl);
            var client = new MockHttpClient(handler);
            client.mMessageHandler = handler;

            return client;
        }


        /// <summary>
        /// Set the expectation that a header is present in a request. Calls made
        /// with an existing endpoint will replace the previous expectation.
        /// </summary>
        /// <param name="endpoint">The endpoint the request is made to</param>
        /// <param name="header">The header key</param>
        /// <param name="value">The expected header value</param>
        public MockHttpClient ExpectHeader(string endpoint, HttpRequestHeader header, string value)
        {
            mMessageHandler.ExpectHeader(endpoint, header, value);
            return this;
        }


        /// <summary>
        /// Set the expectation that a header is present in a request. Calls made
        /// with an existing endpoint will replace the previous expectation. All
        /// expectations are checked, even after a match.
        /// </summary>
        /// <param name="endpointRegex">A regex to match multiple endpoints</param>
        /// <param name="header">The header key</param>
        /// <param name="value">The expected header value</param>
        public MockHttpClient ExpectHeaderPattern(string endpointRegex, HttpRequestHeader header, string value)
        {
           mMessageHandler.ExpectHeaderPattern(endpointRegex, header, value);
           return this;
        }


        /// <summary>
        /// /// Set a predefined response to a request. Calls made with an existing
        /// endpoint will replace the previous response.
        /// </summary>
        /// <param name="endpoint">The endpoint being requested</param>
        /// <param name="response">A predefined response</param>
        public MockHttpClient SetResponse(string endpoint, MockHttpResponse response)
        {
            mMessageHandler.SetResponse(endpoint, response);
            return this;
        }


        /// <summary>
        /// Set a predefined response to a request. Calls made with an existing
        /// endpoint will replace the previous response.
        /// </summary>
        /// <param name="endpointRegex">Pattern for the requested endpoint</param>
        /// <param name="response">A predefined response</param>
        public MockHttpClient SetResponsePattern(string urlRegex, MockHttpResponse response)
        {
           mMessageHandler.SetResponsePattern(urlRegex, response);
           return this;
        }

    }

}
