using System.Net;
using System.Net.Sockets;
using Xunit;

namespace ConformanceTests;

public class LoopbackHttpListenerTests
{
    [Fact]
    public async Task Get_callback_returns_query_response()
    {
        var redirectUri = CreateRedirectUri("callback");
        await using var listener = new LoopbackHttpListener(redirectUri);
        using var httpClient = new HttpClient();

        var response = await httpClient.GetAsync(new Uri(redirectUri, "?code=code-value&state=state-value"));
        var callback = await listener.WaitForCallbackAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("?code=code-value&state=state-value", callback);
    }

    [Fact]
    public async Task Form_post_callback_returns_encoded_body()
    {
        var redirectUri = CreateRedirectUri("form-post");
        await using var listener = new LoopbackHttpListener(redirectUri);
        using var httpClient = new HttpClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = "code-value",
            ["state"] = "state-value"
        });

        var response = await httpClient.PostAsync(redirectUri, content);
        var callback = await listener.WaitForCallbackAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("code=code-value", callback, StringComparison.Ordinal);
        Assert.Contains("state=state-value", callback, StringComparison.Ordinal);
    }

    private static Uri CreateRedirectUri(string path)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return new Uri($"http://127.0.0.1:{port}/{path}");
    }
}
