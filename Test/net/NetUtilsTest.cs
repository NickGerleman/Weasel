using Rs.Mock;
using Rs.Net;
using System.Net;
using Xunit;

namespace Rs.Test
{
    public class NetUtilsTest
    {

        [Fact]
        public void HasConnectivity()
        {
            // Mock client responds to Google by default
            var mockClient = MockHttpClient.Build("http://abc.xyz");
            var task = NetUtils.HasInternetConnectionAsync(mockClient);
            task.Wait();
            Assert.True(task.Result);
        }


        [Fact]
        public void HasNoConnectivity()
        {
            var mockClient = MockHttpClient.Build("http://abc.xyz");
            mockClient.SetResponse("https://www.google.com/", new MockHttpResponse { StatusCode = HttpStatusCode.RequestTimeout });

            var task = NetUtils.HasInternetConnectionAsync(mockClient);
            task.Wait();
            Assert.False(task.Result);
        }

    }
}
