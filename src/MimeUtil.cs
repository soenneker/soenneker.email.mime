using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Polly;
using Polly.Retry;
using Soenneker.Email.Mime.Abstract;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.Stream;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.MemoryStream.Abstract;
using Soenneker.Utils.Random;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Email.Mime;

/// <inheritdoc cref="IMimeUtil"/>
public sealed class MimeUtil : IMimeUtil
{
    private readonly ILogger<MimeUtil> _logger;
    private readonly IMemoryStreamUtil _memoryStreamUtil;

    private readonly string? _username;
    private readonly string? _password;
    private readonly string? _host;
    private readonly int? _port;
    private readonly bool _logContent;
    private readonly bool _enabled;
    private readonly bool _useSsl;
    private readonly bool _acceptAnyCert;

    private readonly AsyncRetryPolicy _retryPolicy;

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
        }

        _retryPolicy = Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
                             .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(RandomUtil.Next(100, 750)),
                                 (exception, timeSpan, attempt, _) =>
                                 {
                                     _logger.LogWarning(exception, "[MimeUtil] Retry {attempt} after {timeSpan} for email send failure.", attempt, timeSpan);
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
            await _retryPolicy.ExecuteAsync(ct => InternalSend(message, ct), cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[MimeUtil] Failed to send email after retries.");
            throw;
        }
    }

    public async Task InternalSend(MimeMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[MimeUtil] Connecting to SMTP client...");

        using var client = new SmtpClient();

        if (_acceptAnyCert)
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        }

        await client.ConnectAsync(_host, _port!.Value, _useSsl, cancellationToken).NoSync();
        await client.AuthenticateAsync(_username, _password, cancellationToken).NoSync();
        await client.SendAsync(message, cancellationToken).NoSync();
        await client.DisconnectAsync(true, cancellationToken).NoSync();

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