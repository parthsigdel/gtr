namespace Gtr.UI;

using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;

using Gtr.Models;

public static class Display
{
    // Design tokens 
    public const string ColorPrs = "deepskyblue1";
    public const string ColorReviews = "gold3";
    public const string ColorIssues = "orangered1";
    public const string ColorNotifications = "mediumpurple1";
    public const string ColorMuted = "grey58";
    public const string ColorDim = "grey42";
    public const string ColorText = "grey93";

    public static List<Grid> PrDescriptions(PrItem[] prs)
    {
        var grids = new List<Grid>();
        foreach (var pr in prs)
        {
            var grid = new Grid();
            grid.AddColumn();

            grid.AddRow(DetailPanel(
            color: ColorPrs,
            breadcrumb: $"[[p]] PRs  >  #{pr.Number}",
            title: pr.Title,
            meta: $"{pr.State} · {ParseOwnerAndRepoName(pr.RepositoryUrl)} · opened {GetAgoTime(pr.CreatedAt)} · Draft: {pr.Draft}",
            body: pr.Body,
            tags: Labels(pr.Labels),
            footer: $"o open in browser  c {FormatChangePrStatus(pr.Draft)}  {showClosePrOption(pr.State)}  b back   q quit"
            ));
            grids.Add(grid);
        }
        return grids;
    }



    public static List<Grid> ReviewDescription(ReviewItem[] reviews)
    {
        var grids = new List<Grid>();
        foreach (var review in reviews)
        {
            var grid = new Grid();
            grid.AddColumn();

            grid.AddRow(DetailPanel(
                color: ColorReviews,
                breadcrumb: $"[[r]] Reviews  >  #{review.Number}",
                title: review.Title,
                meta: $"review requested · {ParseRepoName(review.RepositoryUrl)}/{review.User.Login}  · requested {GetAgoTime(review.CreatedAt)} ",
                body: review.Body,
                tags: $"[green]+{review.ChangeInfo.Additions}[/] [red]−{review.ChangeInfo.Deletions}[/] · [yellow]{review.ChangeInfo.ChangedFiles} files changed[/]",
                footer: "o open in browser   b back   q quit"
                        ));
            grids.Add(grid);
        }
        return grids;
    }

    public static List<Grid> IssueDescription(IssueItem[] issues)
    {
        var grids = new List<Grid>();
        foreach (var issue in issues)
        {
            var grid = new Grid();
            grid.AddColumn();
            grid.AddRow(DetailPanel(
                color: ColorIssues,
                breadcrumb: $"[[i]] Issues  >  #{issue.Number}",
                title: issue.Title,
                meta: $"{issue.State} · {ParseRepoName(issue.RepositoryUrl)}/{issue.User.Login}  · requested {GetAgoTime(issue.CreatedAt)}",
                body: issue.Body,
                tags: $"{Assignees(issue.Assignees)} · {Labels(issue.Labels)}",
                footer: "o open in browser   b back   q quit"
            ));
            grids.Add(grid);
        }
        return grids;
    }

    public static List<Grid> NotificationDescription(Notification[] notifications)
    {
        var grids = new List<Grid>();
        foreach (var n in notifications)
        {
            var grid = new Grid();
            grid.AddColumn();
            grid.AddRow(DetailPanel(
                color: ColorNotifications,
                breadcrumb: $"[[n]] Notifications #{n.Subject.Url.Split('/').Last()}",
                title: n.Subject.Title,
                meta: $"{n.Subject.Type} ·  {n.Repository.FullName} · {GetAgoTime(n.UpdatedAt)}",
                body: n.Details.Body,
                tags: $" {HumanizedReason(n.Reason)}",
                footer: "o open in browser   m mark as read   b back   q quit"
            ));
            grids.Add(grid);
        }
        return grids;
    }

    public static Panel DetailPanel(string color, string breadcrumb, string title, string meta, string body, string? tags, string footer)
    {
        var lines = new List<IRenderable>
    {
        new Markup($"[{ColorDim}]{breadcrumb}[/]"),
        new Markup($"[bold {ColorText}]{title}[/]"),
        new Markup($"[{color}]{meta}[/]"),
        new Rule().RuleStyle(ColorDim),
        new Markup($"[{ColorText}]{body}[/]"),
    };

        if (!string.IsNullOrEmpty(tags))
        {
            lines.Add(new Rule().RuleStyle(ColorDim));
            lines.Add(new Markup($"[{ColorMuted}]{tags}[/]"));
        }

        lines.Add(new Rule().RuleStyle(ColorDim));
        lines.Add(new Markup($"[{ColorDim}]{footer}[/]"));

        var body_rows = new Rows(lines);

        return new Panel(body_rows)
            .Border(BoxBorder.Rounded)
            .BorderColor(Spectre.Console.Color.FromName(color) ?? Spectre.Console.Color.Grey)
            .Padding(1, 0);
    }

    public static Grid Tabs(char currTab, int prCount, int reviewsCount, int issuesCount, int notificationsCount)
    {
        var grid1 = new Grid();
        grid1.AddColumn();
        grid1.AddColumn();
        grid1.AddColumn();
        grid1.AddColumn();

        var prPanel = TabCell("p", "PRs", prCount, currTab == 'p', ColorPrs);
        var reviewPanel = TabCell("r", "Reviews", reviewsCount, currTab == 'r', ColorReviews);
        var issuesPanel = TabCell("i", "Issues", issuesCount, currTab == 'i', ColorIssues);
        var notificationsPanel = TabCell("n", "Notifications", notificationsCount, currTab == 'n', ColorNotifications);

        grid1.AddRow(prPanel, reviewPanel, issuesPanel, notificationsPanel).Width(90);

        return grid1;
    }

    public static Panel TabCell(string key, string label, int count, bool active, string color)
    {
        var labelMarkup = active
            ? new Markup($"[bold {color}][[{key}]] {label} ({count})[/]")
            : new Markup($"[{ColorMuted}][[{key}]] {label} ({count})[/]");

        var underline = new Rule().RuleStyle(active ? color : ColorDim);

        return new Panel(new Rows(labelMarkup, underline))
            .NoBorder()
            .Padding(0, 0);
    }

    public static Grid Prs(PrItem[] newPrs, int currIdx) =>
        ListGrid(newPrs.Select(p => p.Title), currIdx, ColorPrs);

    public static Grid Reviews(ReviewItem[] reviews, int currIdx) =>
        ListGrid(reviews.Select(r => r.Title), currIdx, ColorReviews);

    public static Grid Issues(IssueItem[] issues, int currIdx) =>
        ListGrid(issues.Select(i => i.Title), currIdx, ColorIssues);

    public static Grid Notifications(Notification[] notifications, int currIdx) =>
        ListGrid(notifications.Select(n => n.Subject.Title), currIdx, ColorNotifications);

    public static Grid ListGrid(IEnumerable<string> titles, int currIdx, string color)
    {
        var grid = new Grid();
        grid.AddColumn();

        var items = titles.ToArray();
        for (int i = 0; i < items.Length; i++)
        {
            if (i == currIdx)
            {
                grid.AddRow(new Markup($"[bold {color}]❯ {items[i]}[/]")).Width(70);
            }
            else
            {
                grid.AddRow(new Markup($"[{ColorMuted}]  {items[i]}[/]")).Width(70);
            }
        }
        return grid;
    }

    public static Grid HelpText()
    {
        var grid3 = new Grid();
        grid3.AddColumn();
        grid3.AddRow(new Text(" "));
        grid3.AddRow(
            new Text(
                new string('─', 60),
                new Style(Color.Grey)
            )
        );
        var help = new Panel(new Markup($"[{ColorDim}]p/r/i/n switch tab   ↑/↓, j/k move   enter select   q quit[/]"))
            .NoBorder()
            .Padding(0, 0);
        grid3.AddRow(help).Width(70);
        grid3.AddRow(new Text(" "));
        return grid3;
    }

    public static Grid TabInfo(char currTab, Grid prs, Grid reviews, Grid issues, Grid notifications)
    {
        switch (currTab)
        {
            case 'p':
                return prs;

            case 'r':
                return reviews;

            case 'i':
                return issues;

            case 'n':
                return notifications;
        }

        return prs;
    }

    public static string Labels(Label[] labels)
    {
        if (labels.Length == 0) return "labels: ";
        // Prepare a formatted label.
        var formattedLabels = new StringBuilder("");
        formattedLabels.Append("labels: ");
        // Append till second last element. 
        for (var i = 0; i < labels.Length - 1; i++)
        {
            formattedLabels.Append($" {labels[i].Name},");
        }
        // append last element without a comma
        formattedLabels.Append($" {labels[labels.Length - 1].Name}");
        return formattedLabels.ToString();
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

    public static string ParseOwnerAndRepoName(string repoUrl)
    {
        var repoParts = repoUrl.Split('/');
        string repo = repoParts[repoParts.Length - 1];
        string owner = repoParts[repoParts.Length - 2];
        return $"{repo}/{owner}";
    }

    public static string ParseRepoName(string repoUrl)
    {
        var repoParts = repoUrl.Split('/');
        string repo = repoParts[repoParts.Length - 1];
        return $"{repo}";
    }

    public static string Assignees(User[] assignees)
    {
        if (assignees.Length == 0) return "";
        var formattedAssignees = new StringBuilder("");
        formattedAssignees.Append("assignees: ");
        for (int i = 0; i < assignees.Length - 1; i++)
        {
            formattedAssignees.Append($"{assignees[i].Login}, ");
        }
        formattedAssignees.Append($"{assignees[assignees.Length - 1].Login}");
        return formattedAssignees.ToString();
    }

    public static string FormatChangePrStatus(bool isDraft)
    {
        if (isDraft) return "Mark as ready for review";
        return "Convert to draft";
    }

    public static string HumanizedReason(string reason)
    {
        switch (reason)
        {
            case "assign":
                return "You were assigned";
            case "author":
                return "You opened this thread";
            case "comment":
                return "New comment on a thread you're watching";
            case "ci_activity":
                return "CI activity on this thread";
            case "invitation":
                return "You accepted an invitation";
            case "manual":
                return "You subscribed to this thread";
            case "mention":
                return "You were mentioned";
            case "review_requested":
                return "Review requested";
            case "security_alert":
                return "Security alert";
            case "state_change":
                return "Thread state changed";
            case "subscribed":
                return "You're watching this repository";
            case "team_mention":
                return "Your team was mentioned";
            default:
                return "Notification";
        }
    }

    public static string CustomSpinner(ref int i)
    {
        var frames = Spinner.Known.Dots.Frames;
        return frames[i++ % frames.Count];
    }

    public static string showClosePrOption(string state)
    {
        if (state == "open")
            return "x close pr";
        return "";
    }
}

