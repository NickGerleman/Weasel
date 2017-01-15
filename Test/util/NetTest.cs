using Wsl.Mock;
using System.Net;
using Xunit;

namespace Wsl.Test
{
    public class NetTest
    {

        [Fact]
        public void HasConnectivity()
        {
            // Mock client responds to Google by default
            var mockClient = MockHttpClient.Build("http://abc.xyz");
            var task = Util.HasInternetConnectionAsync(mockClient);
            task.Wait();
            Assert.True(task.Result);
        }


        [Fact]
        public void HasNoConnectivity()
        {
            var mockClient = MockHttpClient.Build("http://abc.xyz");
            mockClient.SetResponse("https://www.google.com/", new MockHttpResponse { StatusCode = HttpStatusCode.RequestTimeout });

            var task = Util.HasInternetConnectionAsync(mockClient);
            task.Wait();
            Assert.False(task.Result);
        }

    }
}
