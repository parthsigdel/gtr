namespace Gtr.Tests;

using System.Net;
using Gtr.Api;

public class RepoTests : IDisposable
{
    private readonly TextWriter _originalOut = Console.Out;

    public RepoTests()
    {
        Console.SetOut(TextWriter.Null);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
    }

    [Fact]
    public async Task GetOpenPrs_ReturnsPrs_WhenSuccessful()
    {
        var json = """
        {
            "total_count": 1,
            "items": [
                {
                    "node_id": "PR_1",
                    "number": 42,
                    "title": "Fix login bug",
                    "html_url": "https://github.com/foo/bar/pull/42",
                    "state": "open",
                    "repository_url": "https://api.github.com/repos/foo/bar",
                    "created_at": "2026-08-01T10:00:00Z",
                    "body": "Fixes the bug",
                    "labels": [{ "name": "bug" }],
                    "draft": false
                }
            ]
        }
        """;
        var api = new Repo(new HttpClient(new FakeHttpMessageHandler(json)));

        var result = await api.GetOpenPrs();

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(42, result.Items[0].Number);
        Assert.False(result.Items[0].Draft);
    }

    [Fact]
    public async Task GetOpenPrs_Throws_WhenRequestFails()
    {
        var api = new Repo(new HttpClient(new FakeHttpMessageHandler("", HttpStatusCode.InternalServerError)));

        await Assert.ThrowsAsync<HttpRequestException>(() => api.GetOpenPrs());
    }

    [Fact]
    public async Task GetOpenReviews_ReturnsReviews_WhenSuccessful()
    {
        var json = """
        {
            "total_count": 1,
            "items": [
                {
                    "number": 7,
                    "title": "Add feature",
                    "html_url": "https://github.com/foo/bar/pull/7",
                    "repository_url": "https://api.github.com/repos/foo/bar",
                    "user": { "login": "octocat", "avatar_url": "https://avatars/octocat" },
                    "created_at": "2026-08-01T10:00:00Z",
                    "body": "Adds a feature",
                    "pull_request": { "url": "https://api.github.com/repos/foo/bar/pulls/7" },
                    "change_info": { "additions": 10, "deletions": 2, "changed_files": 3 }
                }
            ]
        }
        """;
        var api = new Repo(new HttpClient(new FakeHttpMessageHandler(json)));

        var result = await api.GetOpenReviews();

        Assert.NotNull(result);
        Assert.Equal(7, result.Items[0].Number);
        Assert.Equal("octocat", result.Items[0].User.Login);
    }

    [Fact]
    public async Task GetFilesChangedInfo_ReturnsChangeInfo_WhenSuccessful()
    {
        var json = """{ "additions": 10, "deletions": 2, "changed_files": 3 }""";
        var api = new Repo(new HttpClient(new FakeHttpMessageHandler(json)));

        var result = await api.GetFilesChangedInfo("https://api.github.com/repos/foo/bar/pulls/7");

        Assert.NotNull(result);
        Assert.Equal(10, result.Additions);
        Assert.Equal(3, result.ChangedFiles);
    }

    [Fact]
    public async Task GetOpenIssues_ReturnsIssues_WhenSuccessful()
    {
        var json = """
        {
            "total_count": 1,
            "items": [
                {
                    "number": 3,
                    "title": "Crash on startup",
                    "html_url": "https://github.com/foo/bar/issues/3",
                    "repository_url": "https://api.github.com/repos/foo/bar",
                    "state": "open",
                    "user": { "login": "octocat", "avatar_url": "https://avatars/octocat" },
                    "created_at": "2026-08-01T10:00:00Z",
                    "body": "Crashes immediately",
                    "labels": [{ "name": "bug" }],
                    "assignees": [{ "login": "octocat", "avatar_url": "https://avatars/octocat" }]
                }
            ]
        }
        """;
        var api = new Repo(new HttpClient(new FakeHttpMessageHandler(json)));

        var result = await api.GetOpenIssues();

        Assert.NotNull(result);
        Assert.Equal(3, result.Items[0].Number);
        Assert.Single(result.Items[0].Assignees);
    }

    [Fact]
    public async Task ChangePrStatus_CompletesSuccessfully_WhenSuccessful()
    {
        var api = new Repo(new HttpClient(new FakeHttpMessageHandler("{}")));

        await api.ChangePrStatus("PR_1", Repo.PrStatus.Ready);
    }

    [Fact]
    public async Task ClosePr_CompletesSuccessfully_WhenSuccessful()
    {
        var api = new Repo(new HttpClient(new FakeHttpMessageHandler("{}")));

        await api.ClosePr("foo", "bar", 42);
    }

    [Fact]
    public async Task ViewPrComments_ReturnsComments_WhenSuccessful()
    {
        var json = """
        [
            {
                "id": 1,
                "html_url": "https://github.com/foo/bar/pull/42#issuecomment-1",
                "updated_at": "2026-08-01T10:00:00Z",
                "user": { "login": "octocat", "avatar_url": "https://avatars/octocat" },
                "body": "Looks good",
                "author_association": "MEMBER",
                "reactions": {
                    "total_count": 1,
                    "+1": 1,
                    "-1": 0,
                    "laugh": 0,
                    "hooray": 0,
                    "confused": 0,
                    "heart": 0,
                    "rocket": 0,
                    "eyes": 0
                }
            }
        ]
        """;
        var api = new Repo(new HttpClient(new FakeHttpMessageHandler(json)));

        var result = await api.ViewPrComments("foo", "bar", 42);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Looks good", result[0].Body);
    }
}
