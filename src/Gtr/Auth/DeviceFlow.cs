namespace Gtr.Auth;

using System.Net.Http.Json;
using System.Text;
using GitCredentialManager;

using static Gtr.Http.GitHubClient;

public class DeviceFlow
{
    private readonly HttpClient _http;
    public DeviceFlow(HttpClient http) => _http = http;

    // return if operation was successful or not. 
    public async Task<bool> GenerateAccessToken(ICredentialStore store, bool s_keepRunning, CancellationTokenSource cts)
    {
        string newAccessToken;

        string clientId = "Ov23liKhf7NMmZouMr4T";
        string[] scopes = {
            "repo",
            "notifications",
            "user"
        };
        var loginRes = await Login(clientId, scopes);

        if (loginRes != null)
        {
            Console.WriteLine($"Your code is: {loginRes.UserCode} ");
            Console.WriteLine("Please enter this code here: ");
            Console.WriteLine(" https://github.com/login/device ");

            var pollRes = await Poll(clientId, loginRes.DeviceCode, loginRes.ExpiresIn, loginRes.Interval, cts);
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

    public async Task<LoginResponse?> Login(string clientId, params string[] scopes)
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
            HttpResponseMessage res = await _http.PostAsJsonAsync("https://github.com/login/device/code", loginCode, SnakeCaseOptions);
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


    public async Task<PollResponse?> Poll(string clientId, string deviceCode, int expiresIn, int interval, CancellationTokenSource cts)
    {
        // https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps#step-3-app-polls-github-to-check-if-the-user-authorized-the-device
        string deviceAuthReqUrl = "https://github.com/login/oauth/access_token";
        string grantType = "urn:ietf:params:oauth:grant-type:device_code";
        var pollReq = new PollRequest(clientId, deviceCode, grantType);

        var deadline = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

        while (true)
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                Console.WriteLine("Device code expired. Please run the command again.");
                return null;
            }

            try
            {
                HttpResponseMessage res = await _http.PostAsJsonAsync(deviceAuthReqUrl, pollReq, SnakeCaseOptions);
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
    }
}

public record LoginRequest(string ClientId, string Scope);
public record LoginResponse(string DeviceCode, string UserCode, string VerificationUri, int ExpiresIn, int Interval);
public record PollRequest(string ClientId, string DeviceCode, string GrantType);
public record PollResponse(string AccessToken, string TokenType, string Scope, string error, string error_descrption, string error_uri);
