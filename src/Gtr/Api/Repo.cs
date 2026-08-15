namespace Gtr.Api;

using System.Text;
using System.Net.Http.Json;

using static Gtr.Http.GitHubClient;
using Gtr.Models;

public class Repo
{
    private readonly HttpClient _http;
    public Repo(HttpClient http) => _http = http;

    private const string RepoBaseUrl = "https://api.github.com/repos";
    private const string OpenPrsUrl = "https://api.github.com/search/issues?q=is:pr+is:open+author:@me";
    private const string OpenReviewsUrl = "https://api.github.com/search/issues?q=is:pr+is:open+review-requested:@me";
    private const string OpenIssuesUrl = "https://api.github.com/search/issues?q=is:issue+is:open+assignee:@me";
    private const string GithubGraphqlUrl = "https://api.github.com/graphql";
    public enum PrStatus
    {
        Draft,
        Ready
    }

    public async Task<OpenPrs?> GetOpenPrs()
    {
        var res = await _http.GetAsync(OpenPrsUrl);
        res.EnsureSuccessStatusCode();
        var OpenPrs = await res.Content.ReadFromJsonAsync<OpenPrs>(SnakeCaseOptions);
        return OpenPrs;
    }

    public async Task<OpenReviews?> GetOpenReviews()
    {
        var res = await _http.GetAsync(OpenReviewsUrl);
        res.EnsureSuccessStatusCode();
        var openReviews = await res.Content.ReadFromJsonAsync<OpenReviews>(SnakeCaseOptions);
        return openReviews;
    }


    public async Task<ChangeInfo?> GetFilesChangedInfo(string prUrl)
    {
        var res = await _http.GetAsync(prUrl);
        res.EnsureSuccessStatusCode();
        var changeInfo = await res.Content.ReadFromJsonAsync<ChangeInfo>(SnakeCaseOptions);
        return changeInfo;
    }


    public async Task<OpenIssues?> GetOpenIssues()
    {
        var res = await _http.GetAsync(OpenIssuesUrl);
        res.EnsureSuccessStatusCode();
        var openIssues = await res.Content.ReadFromJsonAsync<OpenIssues>(SnakeCaseOptions);
        return openIssues;
    }

    public async Task ChangePrStatus(string id, PrStatus changeTo)
    {
        string payload;
        if (changeTo == PrStatus.Ready)
        {
            payload = $$"""{"query": "mutation {markPullRequestReadyForReview(input: {pullRequestId: \"{{id}}\"}) {pullRequest { id isDraft } } }"}""";
        }
        else
        {
            payload = $$"""{"query": "mutation {convertPullRequestToDraft(input: {pullRequestId: \"{{id}}\"}) {pullRequest { id isDraft } } }"}""";
        }
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var res = await _http.PostAsync(GithubGraphqlUrl, content);
        res.EnsureSuccessStatusCode();
    }

    public async Task ClosePr(string owner, string repo, int pullNumber)
    {
        var res = await _http.PatchAsJsonAsync($"{RepoBaseUrl}/{owner}/{repo}/pulls/{pullNumber}", new { state = "closed" });
        res.EnsureSuccessStatusCode();
    }

    // Returns General discussion comments
    public async Task<Comment[]?> ViewPrComments(string owner, string repo, int pullNumber)
    {
        var res = await _http.GetAsync($"{RepoBaseUrl}/{owner}/{repo}/issues/{pullNumber}/comments");
        res.EnsureSuccessStatusCode();
        var comments = await res.Content.ReadFromJsonAsync<Comment[]>(SnakeCaseOptions);
        return comments;
    }
}
