using Microsoft.Extensions.Logging;
using OrderHub.Application.DTOs;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

public class SalaAuthService : IAuthService
{
    private const string REDIRECT_URI = "http://127.0.0.1:9000/callback/";
    private const string AUTH_URL = "https://accounts.salla.sa/oauth2/auth";
    private const string TOKEN_URL = "https://accounts.salla.sa/oauth2/token";
    private const string SCOPE = "offline_access";
    private const int AUTHORIZATION_TIMEOUT_SECONDS = 120;

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger; // Optional: add your logging interface
    private string _pendingState;

    public SalaAuthService(HttpClient httpClient, ILogger logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
    }

    public async Task<Result<Token>> AuthorizeAsync(ClientCredentials clientCredentials)
    {
        ValidateClientCredentials(clientCredentials);

        HttpListener listener = null;
        try
        {
            listener = CreateListener(REDIRECT_URI);
            _pendingState = GenerateSecureState();

            string authUrl = BuildAuthUrl(clientCredentials, _pendingState);
            OpenBrowser(authUrl);

            HttpListenerContext context = await WaitForCallbackAsync(listener);
            if (context == null)
            {
                return Result<Token>.Failure($"Authorization timeout after {AUTHORIZATION_TIMEOUT_SECONDS} seconds");
            }

            var queryParams = context.Request.QueryString;
            ValidateCallback(queryParams);

            string code = queryParams["code"];
            Result<Token> token = await GetAccessTokenAsync(clientCredentials, code);

            await WriteResponseHtmlAsync(context);

            return token;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Authorization failed: {ex.Message}");
            return Result<Token>.Failure($"Authorization failed: {ex.Message}");
        }
        finally
        {
            listener?.Stop();
            listener?.Close();
            _pendingState = null;
        }
    }

    public async Task<Result<Token>> GetAccessTokenAsync(ClientCredentials clientCredentials, string code)
    {
        ValidateClientCredentials(clientCredentials);

        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<Token>.Failure("Authorization code cannot be null or empty");
        }

        var data = new Dictionary<string, string>
        {
            { "client_id", clientCredentials.ClientId },
            { "client_secret", clientCredentials.ClientSecret },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "scope", SCOPE },
            { "redirect_uri", REDIRECT_URI }
        };

        return await RequestTokenAsync(data);
    }

    public async Task<Result<Token>> RefreshTokenAsync(ClientCredentials clientCredentials, Token token)
    {
        ValidateClientCredentials(clientCredentials);

        if (token == null)
        {
            return Result<Token>.Failure("Token is nulll");
        }

        if (string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            return Result<Token>.Failure("Refresh token cannot be null or empty");
        }

        var data = new Dictionary<string, string>
        {
            { "client_id", clientCredentials.ClientId },
            { "client_secret", clientCredentials.ClientSecret },
            { "grant_type", "refresh_token" },
            { "refresh_token", token.RefreshToken }
        };

        return await RequestTokenAsync(data);
    }

    private async Task<Result<Token>> RequestTokenAsync(Dictionary<string, string> data)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(
                TOKEN_URL,
                new FormUrlEncodedContent(data)
            );

            string result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return Result<Token>.Failure(
                    $"Token request failed with status {response.StatusCode}: {result}"
                );
            }

            TokenResponse tokenResponse = JsonSerializer.Deserialize<TokenResponse>(result);

            if (tokenResponse == null)
            {
                return Result<Token>.Failure("Failed to deserialize token response");
            }

            return MapToToken(tokenResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError($"HTTP request failed: {ex.Message}");
            return Result<Token>.Failure($"HTTP request failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger?.LogError($"Failed to parse token response: {ex.Message}");
            return Result<Token>.Failure("Invalid token response format");
        }
    }

    private async Task<HttpListenerContext> WaitForCallbackAsync(HttpListener listener)
    {
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(AUTHORIZATION_TIMEOUT_SECONDS)))
        {
            try
            {
                var contextTask = listener.GetContextAsync();
                var tcs = new TaskCompletionSource<bool>();

                using (cts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    var completedTask = await Task.WhenAny(contextTask, tcs.Task);

                    if (completedTask == tcs.Task)
                    {
                        return null;
                    }

                    return await contextTask;
                }
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }
    }

    private Result ValidateCallback(System.Collections.Specialized.NameValueCollection queryParams)
    {
        // Check for OAuth errors
        string error = queryParams["error"];
        if (!string.IsNullOrEmpty(error))
        {
            string errorDescription = queryParams["error_description"] ?? "No description provided";
            return Result.Failure(errorDescription);
        }

        // Validate state parameter
        string returnedState = queryParams["state"];
        if (string.IsNullOrEmpty(returnedState) || returnedState != _pendingState)
        {
            return Result.Failure("Invalid state parameter - potential CSRF attack detected");
        }

        // Validate authorization code
        string code = queryParams["code"];
        if (string.IsNullOrEmpty(code))
        {
            return Result.Failure("Authorization code is missing in the callback");
        }

        return Result.Success();
    }

    private Result ValidateClientCredentials(ClientCredentials clientCredentials)
    {
        if (clientCredentials == null)
        {
            return Result.Failure("Client credentials cannot be null");
        }

        if (string.IsNullOrWhiteSpace(clientCredentials.ClientId))
        {
            return Result.Failure("Client ID cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(clientCredentials.ClientSecret))
        {
            return Result.Failure("Client Secret cannot be null or empty");
        }

        return Result.Success();
    }

    private static string BuildAuthUrl(ClientCredentials credentials, string state)
    {
        return $"{AUTH_URL}?client_id={Uri.EscapeDataString(credentials.ClientId)}" +
               $"&response_type=code" +
               $"&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&scope={Uri.EscapeDataString(SCOPE)}";
    }

    private static string GenerateSecureState()
    {
        byte[] randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }

    private static Token MapToToken(TokenResponse tokenResponse)
    {
        DateTime expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        return new Token(
            tokenResponse.TokenType,
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            expiresAt,
            tokenResponse.Scope
        );
    }

    private static HttpListener CreateListener(string url)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        return listener;
    }

    private static void OpenBrowser(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private async Task WriteResponseHtmlAsync(HttpListenerContext context)
    {
        try
        {
            string html = await GetSuccessHtmlAsync();
            byte[] buffer = Encoding.UTF8.GetBytes(html);

            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = 200;

            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to write response: {ex.Message}");
        }
    }

    private async Task<string> GetSuccessHtmlAsync()
    {
        string filePath = "./response.txt";

        if (File.Exists(filePath))
        {
            return await File.ReadAllTextAsync(filePath);
        }

        // Fallback HTML if file doesn't exist
        return @"
<!DOCTYPE html>
<html>
<head>
    <title>Authorization Success</title>
    <style>
        body { font-family: Arial, sans-serif; text-align: center; padding: 50px; }
        .success { color: green; font-size: 24px; }
    </style>
</head>
<body>
    <div class='success'>✓ Authorization Successful</div>
    <p>You can close this window and return to the application.</p>
</body>
</html>";
    }
}

// Security exception for CSRF validation
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
}