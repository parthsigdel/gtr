namespace Gtr.Tests.Auth;

using Gtr.Auth;
using System.Net;
using System.Text;

public class DeviceFlowTests : IDisposable
{
    private readonly TextWriter _originalOut = Console.Out;

    public DeviceFlowTests()
    {
        Console.SetOut(TextWriter.Null);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_ReturnsLoginResponse_OnSuccess()
    {
        var fakeJson = """
            {
                "device_code": "abc123",
                "user_code": "WXYZ-1234",
                "verification_uri": "https://github.com/login/device",
                "expires_in": 900,
                "interval": 5
            }
            """;
        var http = new HttpClient(new FakeHttpMessageHandler(fakeJson));
        var deviceFlow = new DeviceFlow(http);
        var result = await deviceFlow.Login("client-id", "repo", "notifications", "user");
        Assert.NotNull(result);
        Assert.Equal("abc123", result!.DeviceCode);
        Assert.Equal("WXYZ-1234", result.UserCode);
        Assert.Equal(5, result.Interval);
    }

    [Fact]
    public async Task Login_SendsSpaceDelimitedScopes()
    {
        var fakeJson = """{"device_code":"abc123","user_code":"WXYZ-1234","verification_uri":"https://github.com/login/device","expires_in":900,"interval":5}""";
        var handler = new RecordingHttpMessageHandler(fakeJson);
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);

        await deviceFlow.Login("client-id", "repo", "notifications", "user");

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("repo notifications user ", handler.LastRequestBody);
    }

    [Fact]
    public async Task Login_ReturnsNull_OnNonSuccessStatusCode()
    {
        var handler = new RecordingHttpMessageHandler("""{"error":"bad_request"}""", HttpStatusCode.BadRequest);
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);

        var result = await deviceFlow.Login("client-id", "repo");

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_ReturnsNull_OnMalformedJson()
    {
        var handler = new RecordingHttpMessageHandler("not json at all");
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);

        var result = await deviceFlow.Login("client-id", "repo");

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_ReturnsNull_OnNetworkFailure()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("network down"));
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);

        var result = await deviceFlow.Login("client-id", "repo");

        Assert.Null(result);
    }

    // ---------- Poll ----------

    [Fact]
    public async Task Poll_ReturnsAccessToken_OnImmediateSuccess()
    {
        var fakeJson = """{"access_token":"ghu_realtoken","token_type":"bearer","scope":"repo notifications user"}""";
        var http = new HttpClient(new FakeHttpMessageHandler(fakeJson));
        var deviceFlow = new DeviceFlow(http);
        using var cts = new CancellationTokenSource();

        var result = await deviceFlow.Poll("client-id", "abc123", interval: 0, expiresIn: 900, cts: cts);

        Assert.NotNull(result);
        Assert.Equal("ghu_realtoken", result!.AccessToken);
        Assert.Null(result.error);
    }

    [Fact]
    public async Task Poll_RetriesOnAuthorizationPending_ThenSucceeds()
    {
        var responses = new Queue<string>(new[]
        {
            """{"error":"authorization_pending"}""",
            """{"error":"authorization_pending"}""",
            """{"access_token":"ghu_realtoken","token_type":"bearer","scope":"repo"}"""
        });
        var handler = new SequencedHttpMessageHandler(responses);
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);
        using var cts = new CancellationTokenSource();

        var result = await deviceFlow.Poll("client-id", "abc123", interval: 0, expiresIn: 900, cts: cts);

        Assert.NotNull(result);
        Assert.Equal("ghu_realtoken", result!.AccessToken);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task Poll_WaitsIntervalSecondsBetweenPendingRetries()
    {
        var responses = new Queue<string>(new[]
        {
            """{"error":"authorization_pending"}""",
            """{"access_token":"ghu_realtoken","token_type":"bearer","scope":"repo"}"""
        });
        var handler = new SequencedHttpMessageHandler(responses);
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);
        using var cts = new CancellationTokenSource();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await deviceFlow.Poll("client-id", "abc123", interval: 1, expiresIn: 900, cts: cts);
        sw.Stop();

        Assert.True(sw.Elapsed >= TimeSpan.FromMilliseconds(900),
            $"expected at least ~1s delay before the second poll, took {sw.Elapsed}");
    }

    [Fact]
    public async Task Poll_ReturnsResponseImmediately_OnAccessDenied()
    {
        var responses = new Queue<string>(new[]
        {
            """{"error":"access_denied"}""",
            """{"access_token":"should_never_be_reached","token_type":"bearer","scope":"repo"}"""
        });
        var handler = new SequencedHttpMessageHandler(responses);
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);
        using var cts = new CancellationTokenSource();

        var result = await deviceFlow.Poll("client-id", "abc123", interval: 0, expiresIn: 900, cts: cts);

        Assert.NotNull(result);
        Assert.Equal("access_denied", result!.error);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Poll_ReturnsNull_OnHttpError()
    {
        var handler = new RecordingHttpMessageHandler("""{"error":"server_error"}""", HttpStatusCode.InternalServerError);
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);
        using var cts = new CancellationTokenSource();

        var result = await deviceFlow.Poll("client-id", "abc123", interval: 0, expiresIn: 900, cts: cts);

        Assert.Null(result);
    }

    [Fact]
    public async Task Poll_ReturnsNull_OnNetworkFailure()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("network down"));
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);
        using var cts = new CancellationTokenSource();

        var result = await deviceFlow.Poll("client-id", "abc123", interval: 0, expiresIn: 900, cts: cts);

        Assert.Null(result);
    }

    [Fact]
    public async Task Poll_OnCancellationDuringDelay_ReturnsLastPendingResponse()
    {
        var handler = new SequencedHttpMessageHandler(new Queue<string>(new[]
        {
            """{"error":"authorization_pending"}"""
        }));
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        var result = await deviceFlow.Poll("client-id", "abc123", interval: 30, expiresIn: 900, cts: cts);

        Assert.NotNull(result);
        Assert.Equal("authorization_pending", result!.error);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Poll_CancellingCtsExternally_StopsFurtherPolling()
    {
        // Confirms cts.Cancel() from outside the loop is what actually stops
        var responses = new Queue<string>(Enumerable.Repeat("""{"error":"authorization_pending"}""", 20));
        var handler = new SequencedHttpMessageHandler(responses);
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);
        using var cts = new CancellationTokenSource();

        var pollTask = deviceFlow.Poll("client-id", "abc123", interval: 5, expiresIn: 900, cts: cts);
        await Task.Delay(50); // let the first request go out and hit the delay
        cts.Cancel();

        var result = await pollTask;

        Assert.NotNull(result);
        Assert.Equal("authorization_pending", result!.error);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Poll_StopsImmediately_WhenExpiresInIsZero()
    {
        var responses = new Queue<string>(Enumerable.Repeat("""{"error":"authorization_pending"}""", 50));
        var handler = new SequencedHttpMessageHandler(responses);
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);
        using var cts = new CancellationTokenSource();

        var result = await deviceFlow.Poll("client-id", "abc123", interval: 0, expiresIn: 0, cts: cts);

        Assert.True(result is null || result.error != "authorization_pending");
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Poll_StopsShortlyAfterExpiresInElapsesMidPolling()
    {
        var responses = new Queue<string>(Enumerable.Repeat("""{"error":"authorization_pending"}""", 50));
        var handler = new SequencedHttpMessageHandler(responses);
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);
        using var cts = new CancellationTokenSource();

        // interval=1s, expiresIn=2s -> should give up after ~2 polls, not run forever
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await deviceFlow.Poll("client-id", "abc123", interval: 1, expiresIn: 2, cts: cts);
        sw.Stop();

        Assert.True(result is null || result.error != "authorization_pending");
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5),
            $"expected Poll to give up shortly after expiresIn elapsed, took {sw.Elapsed}");
    }

    [Fact]
    public async Task Poll_DoesNotMakeOneLastRequest_AfterDeadlinePassed()
    {
        var responses = new Queue<string>(Enumerable.Repeat("""{"error":"authorization_pending"}""", 50));
        var handler = new SequencedHttpMessageHandler(responses);
        var http = new HttpClient(handler);
        var deviceFlow = new DeviceFlow(http);
        using var cts = new CancellationTokenSource();

        await deviceFlow.Poll("client-id", "abc123", interval: 1, expiresIn: 1, cts: cts);

        Assert.Equal(1, handler.CallCount);
    }
}

// ---------- Test doubles ----------

/// <summary>Returns a fixed response body/status for every request.</summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseJson;
    private readonly HttpStatusCode _statusCode;

    public FakeHttpMessageHandler(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseJson = responseJson;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

/// <summary>Like FakeHttpMessageHandler, but records the outgoing request so tests can assert on it.</summary>
public class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseJson;
    private readonly HttpStatusCode _statusCode;

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    public RecordingHttpMessageHandler(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseJson = responseJson;
        _statusCode = statusCode;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
        };
    }
}

/// <summary>Returns a different queued response body on each successive call. Useful for polling sequences.</summary>
public class SequencedHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<string> _responses;
    public int CallCount { get; private set; }

    public SequencedHttpMessageHandler(Queue<string> responses)
    {
        _responses = responses;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        var body = _responses.Count > 0 ? _responses.Dequeue() : """{"error":"authorization_pending"}""";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

/// <summary>Simulates a network-level failure (DNS, connection refused, etc.).</summary>
public class ThrowingHttpMessageHandler : HttpMessageHandler
{
    private readonly Exception _exception;

    public ThrowingHttpMessageHandler(Exception exception)
    {
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw _exception;
    }
}
