namespace Gtr.Tests;

using System.Net;
using Gtr.Api;

public class NotificationTests
{
    [Fact]
    public async Task GetUnreadNotifications_ReturnsNotifications_WhenSuccessful()
    {
        var json = """
        [
            {
                "id": "1",
                "unread": true,
                "reason": "mention",
                "updated_at": "2026-08-01T10:00:00Z",
                "subject": {
                    "title": "Fix login bug",
                    "url": "https://api.github.com/repos/foo/bar/issues/1",
                    "type": "Issue"
                },
                "repository": {
                    "full_name": "foo/bar",
                    "html_url": "https://github.com/foo/bar"
                },
                "notification_url": "https://api.github.com/notifications/threads/1",
                "details": {
                    "body": "Some issue body"
                }
            }
        ]
        """;
        var handler = new FakeHttpMessageHandler(json);
        var httpClient = new HttpClient(handler);
        var api = new NotificationApi(httpClient);

        var result = await api.GetUnreadNotifications();

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("1", result[0].Id);
        Assert.True(result[0].Unread);
        Assert.Equal("mention", result[0].Reason);
        Assert.Equal("Fix login bug", result[0].Subject.Title);
        Assert.Equal("foo/bar", result[0].Repository.FullName);
    }

    [Fact]
    public async Task GetUnreadNotifications_Throws_WhenRequestFails()
    {
        var handler = new FakeHttpMessageHandler("", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler);
        var api = new NotificationApi(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => api.GetUnreadNotifications());
    }

    [Fact]
    public async Task GetNotificationDetails_ReturnsDetails_WhenSuccessful()
    {
        var json = """
        {
            "body": "Some issue body"
        }
        """;
        var handler = new FakeHttpMessageHandler(json);
        var httpClient = new HttpClient(handler);
        var api = new NotificationApi(httpClient);

        var result = await api.GetNotificationDetails("https://api.github.com/repos/foo/bar/issues/1");

        Assert.NotNull(result);
        Assert.Equal("Some issue body", result.Body);
    }

    [Fact]
    public async Task GetNotificationDetails_Throws_WhenRequestFails()
    {
        var handler = new FakeHttpMessageHandler("", HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler);
        var api = new NotificationApi(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => api.GetNotificationDetails("https://api.github.com/repos/foo/bar/issues/1"));
    }

    [Fact]
    public async Task MarkAsRead_CompletesSuccessfully_WhenRequestSucceeds()
    {
        var handler = new FakeHttpMessageHandler("", HttpStatusCode.ResetContent);
        var httpClient = new HttpClient(handler);
        var api = new NotificationApi(httpClient);

        await api.MarkAsRead("123");
        // No exception thrown = success (EnsureSuccessStatusCode passed)
    }

    [Fact]
    public async Task MarkAsRead_Throws_WhenRequestFails()
    {
        var handler = new FakeHttpMessageHandler("", HttpStatusCode.Unauthorized);
        var httpClient = new HttpClient(handler);
        var api = new NotificationApi(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => api.MarkAsRead("123"));
    }
}
