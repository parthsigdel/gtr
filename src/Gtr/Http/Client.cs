namespace Gtr.Http;

using System.Text.Json;

public static class GitHubClient
{
    public static HttpClient GhClient = new();

    static GitHubClient()
    {
        GhClient.DefaultRequestHeaders.Add("Accept", "application/json");
        GhClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");
    }

    public static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };
}
