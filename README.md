[![](https://img.shields.io/nuget/v/soenneker.email.mime.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.email.mime/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.email.mime/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.email.mime/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.email.mime.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.email.mime/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.email.mime/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.email.mime/actions/workflows/codeql.yml)

# Soenneker.Email.Mime

A configurable MailKit SMTP sender for `MimeMessage` objects, with retry handling and optional MIME-content logging.

## Install

```bash
dotnet add package Soenneker.Email.Mime
```

## Configuration

```json
{
  "Smtp": {
    "Enable": true,
    "Host": "smtp.example.com",
    "Port": 587,
    "Username": "mailer@example.com",
    "Password": "use-a-secret-provider",
    "UseSsl": false,
    "UseStartTls": true,
    "AcceptAnyCert": false,
    "LogContent": false
  }
}
```

`Enable`, `Host`, `Port`, `Username`, `Password`, and `UseSsl` are required when SMTP is enabled. `UseStartTls`, `AcceptAnyCert`, and `LogContent` default to `false`. If both TLS flags are true, `UseSsl` takes precedence and MailKit uses TLS immediately on connection.

Keep credentials in a secret provider rather than source-controlled configuration. Leave `AcceptAnyCert` false outside isolated development: enabling it disables server-certificate validation and permits interception. If both TLS flags are false, the connection is unencrypted.

## Registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Email.Mime.Abstract;
using Soenneker.Email.Mime.Registrars;

services.AddMimeUtilAsSingleton();
```

`AddMimeUtilAsScoped()` is also available. In that registration, the utility is scoped while its reusable memory-stream dependency remains singleton; disposing a scope does not tear down that shared dependency.

## Send a message

```csharp
using MimeKit;

var message = new MimeMessage();
message.From.Add(MailboxAddress.Parse("mailer@example.com"));
message.To.Add(MailboxAddress.Parse("recipient@example.net"));
message.Subject = "Deployment complete";
message.Body = new TextPart("plain") { Text = "Version 42 is live." };

IMimeUtil mime = serviceProvider.GetRequiredService<IMimeUtil>();
await mime.Send(message, cancellationToken);
```

When SMTP is disabled, `Send` and `InternalSend` log and return without sending. `Send` retries I/O, socket, timeout, SMTP protocol, and temporary 4xx SMTP command failures five times with exponential backoff and jitter, then rethrows the final failure. Authentication, certificate, invalid-message, and permanent 5xx command failures are not retried. `InternalSend` performs one attempt and is mainly useful when the caller owns retry behavior.

SMTP delivery is not idempotent: a connection can fail after a server accepts a message but before the client receives confirmation, so any retry strategy can produce duplicates. Use a stable message identifier and downstream deduplication when duplicates are unacceptable. A disconnect failure after an acknowledged send is logged and does not trigger another send.

`ConvertMimeMessageToString` returns the complete MIME representation, including headers, body, and encoded attachments. `LogContent: true` writes that data at debug level; leave it disabled when messages may contain personal data, credentials, or confidential attachments.

Cancellation stops pending client work but cannot recall a message already accepted by the SMTP server.
