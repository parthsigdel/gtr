namespace Gtr.Models;

using System.Text.Json.Serialization;

public class Models { }

// PRs
public record OpenPrs(int TotalCount, PrItem[] Items);
public record PrItem(string NodeId, int Number, string Title, string HtmlUrl,
        string State, string RepositoryUrl, DateTimeOffset CreatedAt,
        string Body, Label[] Labels, bool Draft);

// Reviews
public record OpenReviews(int TotalCount, ReviewItem[] Items);
public record ReviewItem(int Number, string Title, string HtmlUrl,
                string RepositoryUrl, User User, DateTimeOffset CreatedAt,
                string Body, PullRequestUrl PullRequest,
                ChangeInfo ChangeInfo);
public record PullRequestUrl(string Url);
public record ChangeInfo(int Additions, int Deletions, int ChangedFiles);

// Issues
public record OpenIssues(int TotalCount, IssueItem[] Items);
public record IssueItem(int Number, string Title, string HtmlUrl,
                string RepositoryUrl, string State, User User, DateTimeOffset CreatedAt,
                string Body, Label[] Labels, User[] Assignees);

// Notifications
public record Notification(string Id, bool Unread, string Reason, DateTimeOffset UpdatedAt,
                                Subject Subject, Repository Repository, string NotificationUrl,
                                Details Details);
public record Subject(string Title, string Url, string Type);
public record Repository(string FullName, string HtmlUrl);
public record Details(string Body);

public record Label(string Name);
public record User(string Login, string AvatarUrl); // Login = Username

// Comments: General discussion comments
// public record Comments(Comment[] comments);
public record Comment(
                long Id,
                string HtmlUrl,
                DateTimeOffset UpdatedAt,
                User User,
                string Body,
                string AuthorAssociation,
                Reaction Reactions
);

// Reactions
public record Reaction(
                int TotalCount,
                [property: JsonPropertyName("+1")] int Plus1,
                [property: JsonPropertyName("-1")] int Minus1,
                int Laugh,
                int Hooray,
                int Confused,
                int Heart,
                int Rocket,
                int Eyes
);
