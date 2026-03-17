using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PLN.Azure.Function.Common.Model.AzureFunctionBase;
using POCoutlookManageIdentity.OutlookManageTrigger.Services;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace POCoutlookManageIdentity.OutlookManageTrigger
{
    public class OutlookManageTrigger : TimerTriggerAzureFunctionBase
    {
        private readonly ILogger<OutlookManageTrigger> _logger;
        private readonly IOutlookManageTriggerService _outlookmanagetriggerService;

        public OutlookManageTrigger(
            IConfiguration configuration,
            ILogger<OutlookManageTrigger> logger,
            IOutlookManageTriggerService outlookmanagetriggerService)
            : base(configuration, logger)
        {
            _logger = logger;
            _outlookmanagetriggerService = outlookmanagetriggerService;
        }

        [Function(nameof(OutlookManageTrigger))]
        public async Task Run([TimerTrigger("%OutlookManageTriggerCronExpression%", RunOnStartup = false)] TimerInfo timerInfo)
        {
            await RunAsync(timerInfo);
        }

        protected override async Task ExecuteAsync(TimerInfo timerInfo, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("OutlookManageTrigger started at {Now}", DateTimeOffset.UtcNow);

            try
            {
                var messagesResult = await _outlookmanagetriggerService.GetUnreadMessagesAsync(cancellationToken).ConfigureAwait(false);
                if (messagesResult.IsFailed)
                {
                    _logger.LogError("Failed to retrieve unread messages: {Errors}", string.Join("; ", messagesResult.Errors.Select(e => e.Message)));
                    return;
                }

                var json = messagesResult.Value;
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("value", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        if (!item.TryGetProperty("id", out var idEl)) continue;
                        var messageId = idEl.GetString();
                        if (string.IsNullOrWhiteSpace(messageId)) continue;

                        var markResult = await _outlookmanagetriggerService.MarkMessageAsReadAsync(messageId, cancellationToken).ConfigureAwait(false);
                        if (markResult.IsFailed)
                        {
                            _logger.LogWarning("Failed to mark message {MessageId} as read: {Errors}", messageId, string.Join("; ", markResult.Errors.Select(e => e.Message)));
                        }
                        else
                        {
                            _logger.LogInformation("Marked message {MessageId} as read", messageId);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No unread messages found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing OutlookManageTrigger");
            }
            finally
            {
                _logger.LogInformation("OutlookManageTrigger finished at {Now}", DateTimeOffset.UtcNow);
            }
        }
    }
}
