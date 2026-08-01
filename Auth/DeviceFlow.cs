namespace Gtr.Auth;

using System.Net.Http.Json;
using System.Text;
using GitCredentialManager;

using static Gtr.Http.GitHubClient;

public static class DeviceFlow
{
    // return if operation was successful or not. 
    public static async Task<bool> GenerateAccessToken(ICredentialStore store, bool s_keepRunning, CancellationTokenSource cts)
    {
        string newAccessToken;

        string clientId = "Ov23liKhf7NMmZouMr4T";
        string[] scopes = {
            "repo",
            // "repo:status",
            // "repo_deployment",
            // "public_repo",
            // "repo:invite",
            // "security_events",
            // "admin:repo_hook",
            // "admin:org",
            // "admin:public_key",
            // "admin:org_hook",
            // "gist",
            "notifications",
            // "user",
            // "project", 
            // "delete_repo", // don't need this
            // "write:packages",
            // "read:packages", 
            // "delete:packages", // don't need this
            // "admin:gpg_key",
            // "codespace",
            // "workflow",
            // "read:audit_log",
            "user"
        };
        var loginRes = await Login(clientId, scopes);

        if (loginRes != null)
        {
            Console.WriteLine($"Your code is: {loginRes.UserCode} ");
            Console.WriteLine("Please enter this code here: ");
            Console.WriteLine(" https://github.com/login/device ");

            int interval = loginRes.Interval;
            var pollRes = await Poll(clientId, loginRes.DeviceCode, interval, s_keepRunning, cts);
            if (pollRes != null && pollRes.error == null)
            {
                newAccessToken = pollRes.AccessToken;
                store.AddOrUpdate("gt", "accessToken", newAccessToken);
                Console.WriteLine($"New access token generated and saved: {newAccessToken}");
                return true;
            }
        }
        return false;
    }

    public static async Task<LoginResponse?> Login(string clientId, params string[] scopes)
    {
        // Create a space delimited list of scopes. 
        var sb = new StringBuilder("");
        foreach (var scope in scopes)
        {
            sb.Append(scope);
            sb.Append(" ");
        }
        string spaceDelimitedScopes = sb.ToString();
        var loginCode = new LoginRequest(clientId, spaceDelimitedScopes);

        try
        {
            HttpResponseMessage res = await GhClient.PostAsJsonAsync("https://github.com/login/device/code", loginCode, SnakeCaseOptions);
            res.EnsureSuccessStatusCode();
            var loginResponse = await res.Content.ReadFromJsonAsync<LoginResponse>(SnakeCaseOptions);
            return loginResponse;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Login Request error: {e.Message}");
            return null;
        }
    }


    public static async Task<PollResponse?> Poll(string clientId, string deviceCode, int interval, bool s_keepRunning, CancellationTokenSource cts)
    {
        // https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps#step-3-app-polls-github-to-check-if-the-user-authorized-the-device
        string deviceAuthReqUrl = "https://github.com/login/oauth/access_token";
        string grantType = "urn:ietf:params:oauth:grant-type:device_code";

        var pollReq = new PollRequest(clientId, deviceCode, grantType);

        while (true && s_keepRunning)
        {
            try
            {
                HttpResponseMessage res = await GhClient.PostAsJsonAsync(deviceAuthReqUrl, pollReq, SnakeCaseOptions);
                res.EnsureSuccessStatusCode();

                var pollResponse = await res.Content.ReadFromJsonAsync<PollResponse>(SnakeCaseOptions);
                if (pollResponse?.error == "authorization_pending")
                {
                    try
                    {
                        if (cts != null)
                            await Task.Delay(TimeSpan.FromSeconds(interval), cts.Token);
                        continue;
                    }
                    catch (OperationCanceledException)
                    {
                        Console.Write("\r\x1b[K");
                        Console.WriteLine("Program Terminated");
                    }
                }
                return pollResponse;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error Polling: {e.Message}");
                return null;
            }
        }

        return null;
    }
}

public record LoginRequest(string ClientId, string Scope);
public record LoginResponse(string DeviceCode, string UserCode, string VerificationUri, int ExpiresIn, int Interval);
public record PollRequest(string ClientId, string DeviceCode, string GrantType);
public record PollResponse(string AccessToken, string TokenType, string Scope, string error, string error_descrption, string error_uri);
