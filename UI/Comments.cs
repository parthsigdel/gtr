namespace Gtr.UI;

using Spectre.Console;
using Spectre.Console.Rendering;

using Gtr.Models;
using Gtr.Utils;

public static class Comments
{
    // Design tokens     
    public const string ColorComments = "cadetblue";  // accent — border, author name
    public const string ColorCommentsDim = "grey62";    // breadcrumb, rules, footer
    public const string ColorCommentsMuted = "grey74";  // timestamps, secondary meta
    public const string ColorCommentsText = "grey93";   // comment body

    public static Panel CommentsPanel(string breadcrumb, string title, Comment[] comments)
    {
        var lines = new List<IRenderable>
    {
        new Markup($"[{ColorCommentsDim}]{breadcrumb}[/]"),
        new Markup($"[bold {ColorCommentsText}]{Markup.Escape(title)}[/]"),
        new Rule().RuleStyle(ColorCommentsDim),
    };

        for (int i = 0; i < comments.Length; i++)
        {
            var c = comments[i];
            lines.Add(new Markup($"[bold {ColorComments}]{Markup.Escape(c.User.Login)}[/] [{ColorCommentsMuted}]· {StringUtils.GetAgoTime(c.UpdatedAt)}[/]"));
            lines.Add(new Markup($"[{ColorCommentsText}]{Markup.Escape(c.Body)}[/]"));

            if (i < comments.Length - 1)
                lines.Add(new Rule().RuleStyle(ColorCommentsDim));
        }

        lines.Add(new Rule().RuleStyle(ColorCommentsDim));
        lines.Add(new Markup($"[{ColorCommentsDim}] o open in browser  b back  q quit[/]"));

        return new Panel(new Rows(lines))
            .Border(BoxBorder.Rounded)
            .BorderColor(Spectre.Console.Color.FromName(ColorComments) ?? Spectre.Console.Color.Grey)
            .Padding(1, 0);
    }

    public static Grid View(Comment[] comments, string breadcrumb, string title)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddRow(CommentsPanel(breadcrumb, title, comments));
        return grid;
    }
}


