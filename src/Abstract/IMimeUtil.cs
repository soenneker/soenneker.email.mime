using MimeKit;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Email.Mime.Abstract;

/// <summary>
/// A utility for sending <see cref="MimeMessage"/> objects using SMTP with retry logic and optional logging.
/// </summary>
public interface IMimeUtil
{
    /// <summary>
    /// Sends a <see cref="MimeMessage"/> using configured SMTP credentials.
    /// Automatically retries on failure using a backoff policy.
    /// </summary>
    /// <param name="message">The MIME email message to send.</param>
    /// <param name="cancellationToken">Optional token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous send operation.</returns>
    ValueTask Send(MimeMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a <see cref="MimeMessage"/> into a string representation for logging or inspection.
    /// </summary>
    /// <param name="message">The message to convert.</param>
    /// <param name="cancellationToken">Optional token to cancel the operation.</param>
    /// <returns>A string version of the MIME message.</returns>
    ValueTask<string> ConvertMimeMessageToString(MimeMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// For Testing
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task InternalSend(MimeMessage message, CancellationToken cancellationToken = default);
}