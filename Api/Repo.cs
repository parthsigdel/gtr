namespace Gtr.Api;

using System.Net.Http.Json;

using static Gtr.Http.GitHubClient;
using Gtr.Models;

public static class Repo
{
    private const string OpenPrsUrl = "https://api.github.com/search/issues?q=is:pr+is:open+author:@me";
    private const string OpenReviewsUrl = "https://api.github.com/search/issues?q=is:pr+is:open+review-requested:@me";
    private const string OpenIssuesUrl = "https://api.github.com/search/issues?q=is:issue+is:open+assignee:@me";

    public static async Task<OpenPrs?> GetOpenPrs()
    {
        var res = await GhClient.GetAsync(OpenPrsUrl);
        res.EnsureSuccessStatusCode();
        var OpenPrs = await res.Content.ReadFromJsonAsync<OpenPrs>(SnakeCaseOptions);
        return OpenPrs;
    }


    public static async Task<OpenReviews?> GetOpenReviews()
    {
        var res = await GhClient.GetAsync(OpenReviewsUrl);
        res.EnsureSuccessStatusCode();
        var openReviews = await res.Content.ReadFromJsonAsync<OpenReviews>(SnakeCaseOptions);
        return openReviews;
    }


    public static async Task<ChangeInfo?> GetFilesChangedInfo(string prUrl)
    {
        var res = await GhClient.GetAsync(prUrl);
        res.EnsureSuccessStatusCode();
        var changeInfo = await res.Content.ReadFromJsonAsync<ChangeInfo>(SnakeCaseOptions);
        return changeInfo;
    }


    public static async Task<OpenIssues?> GetOpenIssues()
    {
        var res = await GhClient.GetAsync(OpenIssuesUrl);
        res.EnsureSuccessStatusCode();
        var openIssues = await res.Content.ReadFromJsonAsync<OpenIssues>(SnakeCaseOptions);
        return openIssues;
    }
}
