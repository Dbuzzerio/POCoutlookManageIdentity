using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PLN.SourceGenerators;
using POCoutlookManageIdentity.OutlookManageTrigger.Options;
using PLN.Azure.Function.Common.Attributes;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace POCoutlookManageIdentity.OutlookManageTrigger.Services
{
    /// <summary>
    /// Service implementation for the <see cref="OutlookManageTrigger"/> function business logic.
    /// </summary>
    [FunctionService]
    public class OutlookManageTriggerService : IOutlookManageTriggerService
    {
        private readonly IConfiguration _configuration;
        private readonly IOptions<OutlookManageTriggerOptions> _options;
        private readonly ILogger<OutlookManageTriggerService> _logger;
        private readonly HttpClient _httpClient;

        private readonly string _tenant;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _refreshToken;

        public OutlookManageTriggerService(
            IConfiguration configuration,
            IOptions<OutlookManageTriggerOptions> options,
            ILogger<OutlookManageTriggerService> logger,
            HttpClient httpClient)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            _tenant = _configuration["AadTenantId"] ?? "common";
            _clientId = _configuration["GraphClientId"] ?? string.Empty;
            _clientSecret = _configuration["GraphClientSecret"] ?? string.Empty;
            _refreshToken = _configuration["GraphRefreshToken"] ?? string.Empty;
        }

        public async Task<Result<string>> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret) || string.IsNullOrWhiteSpace(_refreshToken))
            {
                return Result.Fail<string>("Graph credentials are not configured in IConfiguration.");
            }

            var pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("scope", "Mail.Read Mail.ReadWrite offline_access User.Read"),
                new KeyValuePair<string, string>("refresh_token", _refreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{_tenant}/oauth2/v2.0/token")
            {
                Content = new FormUrlEncodedContent(pairs)
            };

            var resp = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return Result.Fail<string>($"Token endpoint returned {(int)resp.StatusCode}: {err}");
            }

            var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("access_token", out var tokenEl))
            {
                var token = tokenEl.GetString();
                if (string.IsNullOrWhiteSpace(token))
                {
                    return Result.Fail<string>("access_token is empty in token response");
                }

                return Result.Ok(token);
            }

            return Result.Fail<string>("access_token not found in token response");
        }

        public async Task<Result<string>> GetUnreadMessagesAsync(CancellationToken cancellationToken = default)
        {
            var tokenResult = await RefreshAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (tokenResult.IsFailed)
            {
                return Result.Fail<string>(tokenResult.Errors);
            }

            var token = tokenResult.Value;
            _logger.LogInformation("Token richiamato: {Json}", token);

            using var req = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/mailFolders/Inbox/messages?$filter=isRead%20eq%20false&$select=id,subject,from,toRecipients,receivedDateTime,body,hasAttachments&$top=50");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return Result.Fail<string>($"Graph messages endpoint returned {(int)resp.StatusCode}: {err}");
            }

            var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Messaggi recuperati: {Json}", json);

            return Result.Ok(json);
        }

        public async Task<Result<byte[]>> GetMessageEmlAsync(string messageId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageId)) return Result.Fail<byte[]>("messageId is required");
            var tokenResult = await RefreshAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (tokenResult.IsFailed) return Result.Fail<byte[]>(tokenResult.Errors);
            var token = tokenResult.Value;

            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/me/messages/{messageId}/$value");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return Result.Fail<byte[]>($"Graph EML endpoint returned {(int)resp.StatusCode}: {err}");
            }

            var bytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            return Result.Ok(bytes);
        }

        public async Task<Result<string>> GetAttachmentsAsync(string messageId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageId)) return Result.Fail<string>("messageId is required");
            var tokenResult = await RefreshAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (tokenResult.IsFailed) return Result.Fail<string>(tokenResult.Errors);
            var token = tokenResult.Value;

            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/me/messages/{messageId}/attachments");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return Result.Fail<string>($"Graph attachments endpoint returned {(int)resp.StatusCode}: {err}");
            }

            var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return Result.Ok(json);
        }

        public async Task<Result> MarkMessageAsReadAsync(string messageId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageId)) return Result.Fail("messageId is required");
            var tokenResult = await RefreshAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (tokenResult.IsFailed) return Result.Fail(tokenResult.Errors);
            var token = tokenResult.Value;

            using var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"https://graph.microsoft.com/v1.0/me/messages/{messageId}")
            {
                Content = new StringContent("{\"isRead\":true}", System.Text.Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return Result.Fail($"Graph mark-as-read returned {(int)resp.StatusCode}: {err}");
            }

            return Result.Ok();
        }
    }
}
