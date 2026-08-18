namespace Gtr.Tests;

using System.Net;
using System.Text;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _json;

    public FakeHttpMessageHandler(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _json = json;
        _status = status;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        var response = new HttpResponseMessage(_status)
        {
            Content = new StringContent(_json, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
