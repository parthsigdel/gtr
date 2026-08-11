using Spectre.Console;
using System.Diagnostics;
using GitCredentialManager;

using Gtr.Auth;
using Gtr.UI;
using Gtr.Api;
using Gtr.Models;
using Gtr.Utils;
using static Gtr.Http.GitHubClient;

namespace Gtr;

public static class Program
{
    public static bool s_keepRunning = true;
    private static CancellationTokenSource? cts;

    public static async Task Main(string[] args)
    {

        ICredentialStore store = CredentialManager.Create("gtApp");
        if (args.Length > 0)
        {
            if (args[0] == "-rm")
            {
                bool deleted = store.Remove("gt", "accessToken");
                if (deleted) Console.WriteLine("Access token removed");
            }
            return;
        }

        cts = new CancellationTokenSource();
        Console.CancelKeyPress += Console_CancelKeyPress;

        ICredential accessToken = store.Get("gt", "accessToken");

        // If token does not exist, start the process to get token.  
        try
        {
            while (s_keepRunning)
            {
                if (accessToken == null)
                {
                    bool success = await DeviceFlow.GenerateAccessToken(store, s_keepRunning, cts);
                    if (success)
                    {
                        Console.WriteLine("Trying to get the access token from store now");
                        accessToken = store.Get("gt", "accessToken"); // returns an object, so .Password is done to retrieve the actual data
                        if (accessToken != null)
                        {
                            Console.WriteLine($"Token found: {accessToken.Password}");
                        }
                    }
                }
                break;
            }
        }
        catch (OperationCanceledException)
        {
            Console.Write("\r\x1b[K");
            Console.WriteLine("Program Terminated");
        }
        finally
        {
            cts?.Dispose();
        }

        if (accessToken != null)
        {
            string token = accessToken.Password;

            GhClient.DefaultRequestHeaders.Add("User-Agent", "gt");
            GhClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var openPrs = new OpenPrs(0, Array.Empty<PrItem>());
            var openReviews = new OpenReviews(0, Array.Empty<ReviewItem>());
            var openIssues = new OpenIssues(0, Array.Empty<IssueItem>());
            var unreadNotifications = Array.Empty<Notification>();

            int currIdx = 0;
            char currTab = 'p';
            bool finalPageMode = false;
            string urlToOpen = ""; // this gets assigned a new value when the user reaches the final page. 
            string notificationId = ""; // a record of which notification the user is looking at - use this id to mark as read. 

            var prs = Display.Prs(openPrs.Items, currIdx);
            var prDescriptions = Display.PrDescriptions(openPrs.Items);

            var reviews = Display.Reviews(openReviews.Items, currIdx);
            var reviewDescriptions = Display.ReviewDescription(openReviews.Items);

            var issues = Display.Issues(openIssues.Items, currIdx);
            var issueDescriptions = Display.IssueDescription(openIssues.Items);

            var notifications = Display.Notifications(unreadNotifications, currIdx);
            var notificationDescriptions = Display.NotificationDescription(unreadNotifications);

            var tabs = Display.Tabs(currTab, openPrs.TotalCount, openReviews.TotalCount,
                        openIssues.TotalCount, unreadNotifications.Length);
            var tabInfo = Display.TabInfo(currTab, prs, reviews, issues, notifications);
            var helpText = Display.HelpText();

            /* Render */
            var rows = new Rows(tabs, tabInfo, helpText);

            AnsiConsole.Clear();
            await AnsiConsole.Live(rows).StartAsync(async ctx =>
                {
                    ctx.Refresh();

                    // Guarantees that two thread never interleave mid-draw.
                    var renderLock = new Object();
                    void RefreshUi()
                    {
                        lock (renderLock)
                        {
                            tabs = Display.Tabs(currTab, openPrs.TotalCount, openReviews.TotalCount,
                                                                        openIssues.TotalCount, unreadNotifications.Length);
                            tabInfo = Display.TabInfo(currTab, prs, reviews, issues, notifications);
                            if (!finalPageMode) // if the user is already in the final page mode, don't refresh/re-render.
                                ctx.UpdateTarget(new Rows(tabs, tabInfo, helpText));
                        }
                    }

                    try
                    {
                        var prTask = Repo.GetOpenPrs();
                        var reviewsTask = Repo.GetOpenReviews();
                        var issuesTask = Repo.GetOpenIssues();
                        var notificationsTask = NotificationApi.GetUnreadNotifications();

                        _ = prTask.ContinueWith(t =>
                        {
                            openPrs = t.Result ?? openPrs;
                            prs = Display.Prs(openPrs.Items, currIdx);
                            prDescriptions = Display.PrDescriptions(openPrs.Items);
                            RefreshUi();
                        });

                        _ = reviewsTask.ContinueWith(async t =>
                        {
                            openReviews = t.Result ?? openReviews;

                            // Get file changed info for reviews. 
                            var changeInfoTasks = openReviews.Items.Select(r => Repo.GetFilesChangedInfo(r.PullRequest.Url)).ToArray();
                            await Task.WhenAll(changeInfoTasks);

                            for (int i = 0; i < openReviews.Items.Length; i++)
                            {
                                openReviews.Items[i] = openReviews.Items[i] with { ChangeInfo = await changeInfoTasks[i] ?? new ChangeInfo(0, 0, 0) };
                            }

                            reviews = Display.Reviews(openReviews.Items, currIdx);
                            reviewDescriptions = Display.ReviewDescription(openReviews.Items);
                            RefreshUi();
                        });

                        _ = issuesTask.ContinueWith(t =>
                        {
                            openIssues = t.Result ?? openIssues;
                            issues = Display.Issues(openIssues.Items, currIdx);
                            issueDescriptions = Display.IssueDescription(openIssues.Items);
                            RefreshUi();
                        });


                        _ = notificationsTask.ContinueWith(async t =>
                        {
                            unreadNotifications = t.Result ?? unreadNotifications;
                            // Call subject url for more information about the notification.
                            var detailsTask = unreadNotifications.Select(u => NotificationApi.GetNotificationDetails(u.Subject.Url)).ToArray();

                            for (int i = 0; i < unreadNotifications.Length; i++)
                            {
                                unreadNotifications[i] = unreadNotifications[i] with { Details = await detailsTask[i] ?? new Details("") };

                                // Generate a proper notification url. 
                                var number = unreadNotifications[i].Subject.Url.Split('/').Last();
                                var path = unreadNotifications[i].Subject.Type == "PullRequest" ? "pull" : "issues";
                                var notificationUrl = $"{unreadNotifications[i].Repository.HtmlUrl}/{path}/{number}";
                                unreadNotifications[i] = unreadNotifications[i] with { NotificationUrl = notificationUrl };
                            }

                            notifications = Display.Notifications(unreadNotifications, currIdx);
                            notificationDescriptions = Display.NotificationDescription(unreadNotifications);
                            RefreshUi();
                        });
                    }
                    catch (Exception e)
                    {
                        Console.Write("\r\x1b[K");
                        Console.WriteLine(e.Message);
                        cts?.Dispose();
                        return;
                    }

                    var keyInput = Task.Run(async () =>
                    {
                        while (s_keepRunning)
                        {
                            var keyInfo = Console.ReadKey(intercept: true);

                            if (keyInfo is ConsoleKeyInfo info)
                            {
                                switch (info.Key)
                                {
                                    case ConsoleKey.Enter:
                                        if (finalPageMode) break;
                                        Grid? descriptionPage = null;
                                        switch (currTab)
                                        {
                                            case 'p':
                                                if (currIdx >= prDescriptions.Count) break;
                                                descriptionPage = prDescriptions[currIdx];
                                                urlToOpen = openPrs.Items[currIdx].HtmlUrl;
                                                break;

                                            case 'r':
                                                if (currIdx >= reviewDescriptions.Count) break;
                                                descriptionPage = reviewDescriptions[currIdx];
                                                urlToOpen = openReviews.Items[currIdx].HtmlUrl;
                                                break;

                                            case 'i':
                                                if (currIdx >= issueDescriptions.Count) break;
                                                descriptionPage = issueDescriptions[currIdx];
                                                urlToOpen = openIssues.Items[currIdx].HtmlUrl;
                                                break;

                                            case 'n':
                                                if (currIdx >= notificationDescriptions.Count) break;
                                                descriptionPage = notificationDescriptions[currIdx];
                                                urlToOpen = unreadNotifications[currIdx].NotificationUrl;
                                                notificationId = unreadNotifications[currIdx].Id;
                                                break;
                                        }
                                        if (descriptionPage is null) break;
                                        ctx.UpdateTarget(new Rows(descriptionPage));
                                        finalPageMode = true;
                                        break;

                                    case ConsoleKey.Q:
                                        s_keepRunning = false;
                                        cts?.Dispose();
                                        break;

                                    case ConsoleKey.UpArrow or ConsoleKey.K:
                                        if (finalPageMode)
                                            break;
                                        currIdx--;
                                        switch (currTab)
                                        {
                                            case 'p':
                                                if (currIdx < 0) currIdx = openPrs.Items.Length - 1;
                                                prs = Display.Prs(openPrs.Items, currIdx);
                                                break;

                                            case 'r':
                                                if (currIdx < 0) currIdx = openReviews.Items.Length - 1;
                                                reviews = Display.Reviews(openReviews.Items, currIdx);
                                                break;

                                            case 'i':
                                                if (currIdx < 0) currIdx = openIssues.Items.Length - 1;
                                                issues = Display.Issues(openIssues.Items, currIdx);
                                                break;

                                            case 'n':
                                                if (currIdx < 0) currIdx = unreadNotifications.Length - 1;
                                                notifications = Display.Notifications(unreadNotifications, currIdx);
                                                break;
                                        }
                                        tabInfo = Display.TabInfo(currTab, prs, reviews, issues, notifications);
                                        ctx.UpdateTarget(new Rows(tabs, tabInfo, helpText));
                                        break;

                                    case ConsoleKey.DownArrow or ConsoleKey.J:
                                        if (finalPageMode) break;
                                        currIdx++;
                                        switch (currTab)
                                        {
                                            case 'p':
                                                if (currIdx >= openPrs.Items.Length) currIdx = 0;
                                                prs = Display.Prs(openPrs.Items, currIdx);
                                                break;

                                            case 'r':
                                                if (currIdx >= openReviews.Items.Length) currIdx = 0;
                                                reviews = Display.Reviews(openReviews.Items, currIdx);
                                                break;

                                            case 'i':
                                                if (currIdx >= openIssues.Items.Length) currIdx = 0;
                                                issues = Display.Issues(openIssues.Items, currIdx);
                                                break;

                                            case 'n':
                                                if (currIdx >= unreadNotifications.Length) currIdx = 0;
                                                notifications = Display.Notifications(unreadNotifications, currIdx);
                                                break;
                                        }
                                        tabInfo = Display.TabInfo(currTab, prs, reviews, issues, notifications);
                                        ctx.UpdateTarget(new Rows(tabs, tabInfo, helpText));
                                        break;

                                    case ConsoleKey.B:
                                        if (!finalPageMode) break;
                                        finalPageMode = false;
                                        urlToOpen = "";
                                        ctx.UpdateTarget(new Rows(tabs, tabInfo, helpText));
                                        break;

                                    case ConsoleKey.P:
                                        if (finalPageMode) break;
                                        currIdx = 0;
                                        currTab = 'p';
                                        RefreshUi();
                                        break;

                                    case ConsoleKey.R:
                                        if (finalPageMode) break;
                                        currIdx = 0;
                                        currTab = 'r';
                                        RefreshUi();
                                        break;

                                    case ConsoleKey.I:
                                        if (finalPageMode) break;
                                        currIdx = 0;
                                        currTab = 'i';
                                        RefreshUi();
                                        break;

                                    case ConsoleKey.N:
                                        if (finalPageMode) break;
                                        currIdx = 0;
                                        currTab = 'n';
                                        RefreshUi();
                                        break;

                                    case ConsoleKey.M:
                                        if (!finalPageMode || notificationId.Length == 0) break;
                                        try
                                        {
                                            await NotificationApi.MarkAsRead(notificationId);
                                        }
                                        catch (Exception)
                                        {
                                            ctx.UpdateTarget(new Markup($"{notificationId} Something went wrong. Please try again."));
                                            await Task.Delay(TimeSpan.FromSeconds(1));
                                        }
                                        var notificationsTask = NotificationApi.GetUnreadNotifications();
                                        _ = notificationsTask.ContinueWith(async t =>
                                        {
                                            unreadNotifications = t.Result ?? unreadNotifications;
                                            // Call subject url for more information about the notification.
                                            var detailsTask = unreadNotifications.Select(u => NotificationApi.GetNotificationDetails(u.Subject.Url)).ToArray();

                                            for (int i = 0; i < unreadNotifications.Length; i++)
                                            {
                                                unreadNotifications[i] = unreadNotifications[i] with { Details = await detailsTask[i] ?? new Details("") };

                                                // Generate a proper notification url. 
                                                var number = unreadNotifications[i].Subject.Url.Split('/').Last();
                                                var path = unreadNotifications[i].Subject.Type == "PullRequest" ? "pull" : "issues";
                                                var notificationUrl = $"{unreadNotifications[i].Repository.HtmlUrl}/{path}/{number}";
                                                unreadNotifications[i] = unreadNotifications[i] with { NotificationUrl = notificationUrl };
                                            }

                                            notifications = Display.Notifications(unreadNotifications, currIdx);
                                            notificationDescriptions = Display.NotificationDescription(unreadNotifications);
                                            RefreshUi();
                                        });
                                        finalPageMode = false;
                                        urlToOpen = "";
                                        notificationId = "";
                                        ctx.UpdateTarget(new Rows(tabs, tabInfo, helpText));
                                        break;


                                    case ConsoleKey.O:
                                        if (!finalPageMode) break;
                                        Process.Start(new ProcessStartInfo
                                        {
                                            FileName = urlToOpen,
                                            UseShellExecute = true
                                        });
                                        break;

                                    case ConsoleKey.C:
                                        if (!finalPageMode && currTab != 'p') break;

                                        var prId = openPrs.Items[currIdx].NodeId;
                                        bool isDraft = openPrs.Items[currIdx].Draft;
                                        var changePrStatusTask = Task.CompletedTask;
                                        string message;
                                        if (isDraft)
                                        {
                                            changePrStatusTask = Repo.ChangePrStatus(prId, Repo.PrStatus.Ready);
                                            message = "Marking as ready for review";
                                        }
                                        else
                                        {
                                            changePrStatusTask = Repo.ChangePrStatus(prId, Repo.PrStatus.Draft);
                                            message = "Converting to draft";
                                        }

                                        try
                                        {
                                            int i = 0;
                                            while (!changePrStatusTask.IsCompleted)
                                            {
                                                ctx.UpdateTarget(new Markup($"{Display.CustomSpinner(ref i)} {message}"));
                                                await Task.Delay(80);// let the fetch actually progress and avoid pegging the CPU
                                            }
                                            await changePrStatusTask;
                                        }
                                        catch (Exception e)
                                        {
                                            ctx.UpdateTarget(new Markup($"Error: {e.Message}"));
                                            await Task.Delay(TimeSpan.FromSeconds(3));
                                        }

                                        openPrs = await Repo.GetOpenPrs() ?? openPrs;
                                        prs = Display.Prs(openPrs.Items, currIdx);
                                        prDescriptions = Display.PrDescriptions(openPrs.Items);
                                        descriptionPage = prDescriptions[currIdx];
                                        ctx.UpdateTarget(new Rows(descriptionPage));
                                        break;

                                    case ConsoleKey.X:
                                        {
                                            if (!finalPageMode && currTab != 'p') break;
                                            string repoUrl = openPrs.Items[currIdx].RepositoryUrl;
                                            var (repo, owner) = StringUtils.ParseRepoAndOwnerName(repoUrl);
                                            int pullNum = openPrs.Items[currIdx].Number;
                                            var closePrTask = Repo.ClosePr(owner, repo, pullNum);

                                            try
                                            {
                                                int i = 0;
                                                while (!closePrTask.IsCompleted)
                                                {
                                                    ctx.UpdateTarget(new Markup($"{Display.CustomSpinner(ref i)} Closing PR"));
                                                    await Task.Delay(80);// let the fetch actually progress and avoid pegging the CPU
                                                }
                                                await closePrTask;
                                            }
                                            catch (Exception e)
                                            {
                                                ctx.UpdateTarget(new Markup($"Failed to close PR: {Markup.Escape(e.Message)}"));
                                                await Task.Delay(TimeSpan.FromSeconds(3));
                                            }

                                            finalPageMode = false;
                                            urlToOpen = "";

                                            var prTask = Repo.GetOpenPrs();
                                            _ = prTask.ContinueWith(t =>
                                            {
                                                openPrs = t.Result ?? openPrs;
                                                prs = Display.Prs(openPrs.Items, currIdx);
                                                prDescriptions = Display.PrDescriptions(openPrs.Items);
                                                RefreshUi();
                                            });
                                            break;
                                        }

                                    case ConsoleKey.V:
                                        {
                                            if (!finalPageMode) break;
                                            switch (currTab)
                                            {
                                                case 'p':  // view pr comments. 
                                                    string repoUrl = openPrs.Items[currIdx].RepositoryUrl;
                                                    var (repo, owner) = StringUtils.ParseRepoAndOwnerName(repoUrl);
                                                    int pullNum = openPrs.Items[currIdx].Number;
                                                    var viewCommentsTask = Repo.ViewPrComments(owner, repo, pullNum);
                                                    var comments = Array.Empty<Comment>();

                                                    try
                                                    {
                                                        int i = 0;
                                                        while (!viewCommentsTask.IsCompleted)
                                                        {
                                                            ctx.UpdateTarget(new Markup($"{Display.CustomSpinner(ref i)} Fetching Comments"));
                                                            await Task.Delay(80);// let the fetch actually progress and avoid pegging the CPU
                                                        }
                                                        comments = await viewCommentsTask ?? comments;
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        ctx.UpdateTarget(new Markup($"Failed to fetch comments: {Markup.Escape(e.Message)}"));
                                                        await Task.Delay(TimeSpan.FromSeconds(3));
                                                    }

                                                    string prTitle = openPrs.Items[currIdx].Title;
                                                    var commentView = Comments.View(comments,
                                                            $"[[p]] PRs > #{pullNum} > Comments",
                                                            prTitle);
                                                    if (comments.Length > 0)
                                                        // Temporary solution; Todo: navigate comments and assign urlToOpen accordingly. 
                                                        urlToOpen = comments[0].HtmlUrl;
                                                    ctx.UpdateTarget(new Rows(commentView));
                                                    break;
                                            }
                                            break;
                                        }
                                }
                            }
                        }
                    });
                    await keyInput;
                });
        }
    }

    private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        s_keepRunning = false;
        cts?.Cancel();
    }

}
