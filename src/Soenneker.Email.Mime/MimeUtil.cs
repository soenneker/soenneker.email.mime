using MailKit.Net.Smtp;
using MailKit.Security;
using Kevlar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Soenneker.Email.Mime.Abstract;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.Stream;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.MemoryStream.Abstract;
using Soenneker.Utils.Random;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Email.Mime;

/// <inheritdoc cref="IMimeUtil" />
public sealed class MimeUtil : IMimeUtil
{
    private readonly ILogger<MimeUtil> _logger;
    private readonly IMemoryStreamUtil _memoryStreamUtil;

    private readonly string _username = null!;
    private readonly string _password = null!;
    private readonly string _host = null!;
    private readonly int _port;
    private readonly bool _logContent;
    private readonly bool _enabled;
    private readonly bool _useSsl;
    private readonly bool _useStartTls;
    private readonly bool _acceptAnyCert;

    private readonly Shield _retryShield;

    public MimeUtil(IConfiguration config, ILogger<MimeUtil> logger, IMemoryStreamUtil memoryStreamUtil)
    {
        _logger = logger;
        _memoryStreamUtil = memoryStreamUtil;

        _enabled = config.GetValueStrict<bool>("Smtp:Enable");
        _logContent = config.GetValue("Smtp:LogContent", false);
        _acceptAnyCert = config.GetValue("Smtp:AcceptAnyCert", false);

        if (_enabled)
        {
            _username = config.GetValueStrict<string>("Smtp:Username");
            _password = config.GetValueStrict<string>("Smtp:Password");
            _host = config.GetValueStrict<string>("Smtp:Host");
            _port = config.GetValueStrict<int>("Smtp:Port");
            _useSsl = config.GetValueStrict<bool>("Smtp:UseSsl");
            _useStartTls = config.GetValue("Smtp:UseStartTls", false);
        }

        _retryShield = Shield.When<IOException>()
                             .Or<SocketException>()
                             .Or<TimeoutException>()
                             .Or<SmtpProtocolException>()
                             .Or<SmtpCommandException>(static ex => (int) ex.StatusCode is >= 400 and < 500)
                             .Retry(options =>
                             {
                                 options.MaxRetries = 5;
                                 options.Backoff = Backoff.Custom(static attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))
                                     + TimeSpan.FromMilliseconds(RandomUtil.Next(100, 750)));
                                 options.OnRetry = retry =>
                                 {
                                     _logger.LogWarning(retry.Exception, "[MimeUtil] Retry {attempt} after {timeSpan} for email send failure.",
                                         retry.AttemptNumber + 1, retry.Delay);
                                     return default;
                                 };
                             });
    }

    public async ValueTask Send(MimeMessage message, CancellationToken cancellationToken = default)
    {
        if (!_enabled)
        {
            _logger.LogInformation("[MimeUtil] SMTP sending disabled by config.");
            return;
        }

        try
        {
            await _retryShield.ExecuteAsync(ct => InternalSend(message, ct), cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[MimeUtil] Failed to send email after retries.");
            throw;
        }
    }

    public async Task InternalSend(MimeMessage message, CancellationToken cancellationToken = default)
    {
        if (!_enabled)
        {
            _logger.LogInformation("[MimeUtil] SMTP sending disabled by config.");
            return;
        }

        _logger.LogDebug("[MimeUtil] Connecting to SMTP client...");

        using var client = new SmtpClient();

        if (_acceptAnyCert)
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        }

        SecureSocketOptions options = _useSsl ? SecureSocketOptions.SslOnConnect :  _useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

        await client.ConnectAsync(_host, _port, options, cancellationToken).NoSync();
        await client.AuthenticateAsync(_username, _password, cancellationToken).NoSync();
        await client.SendAsync(message, cancellationToken).NoSync();

        try
        {
            await client.DisconnectAsync(true, CancellationToken.None).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[MimeUtil] Email was accepted, but the SMTP client did not disconnect cleanly.");
        }

        _logger.LogDebug("[MimeUtil] Email sent successfully via SMTP.");

        if (_logContent && _logger.IsEnabled(LogLevel.Debug))
        {
            string serialized = await ConvertMimeMessageToString(message, cancellationToken).NoSync();
            _logger.LogDebug("[MimeUtil] Email content:\n{content}", serialized);
        }
    }

    public async ValueTask<string> ConvertMimeMessageToString(MimeMessage message, CancellationToken cancellationToken = default)
    {
        await using MemoryStream stream = await _memoryStreamUtil.Get(cancellationToken).NoSync();

        await message.WriteToAsync(stream, cancellationToken).NoSync();

        stream.ToStart();

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken).NoSync();
    }
}
