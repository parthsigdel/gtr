namespace Gtr.Api;

using System.Net.Http.Json;

using static Gtr.Http.GitHubClient;
using Gtr.Models;

public static class NotificationApi
{
    private const string NotificationsUrl = "https://api.github.com/notifications";
    private const string MarkAsReadUrl = "https://api.github.com/notifications/threads";

    public static async Task<Notification[]?> GetUnreadNotifications()
    {
        var res = await GhClient.GetAsync(NotificationsUrl);
        res.EnsureSuccessStatusCode();
        var unreadNotifications = await res.Content.ReadFromJsonAsync<Notification[]>(SnakeCaseOptions);
        return unreadNotifications;
    }

    public static async Task<Details?> GetNotificationDetails(string subjectUrl)
    {
        var res = await GhClient.GetAsync(subjectUrl);
        res.EnsureSuccessStatusCode();
        var details = await res.Content.ReadFromJsonAsync<Details>(SnakeCaseOptions);
        return details;
    }

    public static async Task MarkAsRead(string notificationId)
    {
        var res = await GhClient.PatchAsync($"{MarkAsReadUrl}/{notificationId}", null);
        res.EnsureSuccessStatusCode();
    }
}
