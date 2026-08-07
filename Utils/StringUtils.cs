namespace Gtr.Utils;

public class StringUtils
{
    public static (string repo, string owner) ParseRepoAndOwnerName(string repoUrl)
    {
        var repoParts = repoUrl.Split('/');
        string repo = repoParts[repoParts.Length - 1];
        string owner = repoParts[repoParts.Length - 2];
        return (repo, owner);
    }

    public static string GetAgoTime(DateTimeOffset createdAt)
    {
        var diff = DateTimeOffset.UtcNow - createdAt;
        string ago = diff.TotalDays >= 1
            ? $"{(int)diff.TotalDays}d ago"
            : diff.TotalHours >= 1
                ? $"{(int)diff.TotalHours}h ago"
                : $"{(int)diff.TotalMinutes}m ago";
        return ago;
    }
}
