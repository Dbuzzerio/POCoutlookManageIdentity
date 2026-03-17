using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace POCoutlookManageIdentity.OutlookManageTrigger.Services
{
    public interface IOutlookManageTriggerService
    {
        /// <summary>
        /// Refreshes the Graph access token using the configured refresh token and credentials.
        /// Reads credentials from IConfiguration (GraphClientId, GraphClientSecret, GraphRefreshToken, AadTenantId).
        /// </summary>
              Task<Result<string>> RefreshAccessTokenAsync(CancellationToken cancellationToken = default);

          /// <summary>
          /// Retrieves unread messages from the user's Inbox (up to a page of results).
          /// Returns the raw JSON response as string.
          /// </summary>
          Task<Result<string>> GetUnreadMessagesAsync(CancellationToken cancellationToken = default);

          /// <summary>
          /// Retrieves the raw EML content for a message as a byte array.
          /// </summary>
          Task<Result<byte[]>> GetMessageEmlAsync(string messageId, CancellationToken cancellationToken = default);

          /// <summary>
          /// Retrieves attachments metadata and content list for a message as raw JSON string.
          /// </summary>
          Task<Result<string>> GetAttachmentsAsync(string messageId, CancellationToken cancellationToken = default);

          /// <summary>
          /// Marks the specified message as read.
          /// </summary>
          Task<Result> MarkMessageAsReadAsync(string messageId, CancellationToken cancellationToken = default);
    }
}


