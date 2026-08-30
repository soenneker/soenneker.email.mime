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
    /// Sends a MIME message through the configured SMTP client without applying the public retry wrapper.
    /// When SMTP is disabled, the call logs and returns without connecting.
    /// </summary>
    /// <param name="message">MIME message to send.</param>
    /// <param name="cancellationToken">Token used to cancel the SMTP operation.</param>
    /// <returns>A task that completes after the SMTP send finishes.</returns>
    Task InternalSend(MimeMessage message, CancellationToken cancellationToken = default);
}
