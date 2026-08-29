[![](https://img.shields.io/nuget/v/soenneker.email.mime.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.email.mime/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.email.mime/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.email.mime/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.email.mime.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.email.mime/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.email.mime/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.email.mime/actions/workflows/codeql.yml)

# Soenneker.Email.Mime

A utility for sending `MimeMessage` objects using SMTP with retry logic and optional logging.

## Install

```bash
dotnet add package Soenneker.Email.Mime
```

## Quick start

```csharp
using Soenneker.Email.Mime.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddMimeUtilAsSingleton();
```

Adds `IMimeUtil` as a singleton service.

## What you get

- `IMimeUtil` — A utility for sending `MimeMessage` objects using SMTP with retry logic and optional logging.
- `MimeUtilRegistrar` — A resilient, configurable SMTP email sender.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IMimeUtil.Send(message, cancellationToken)` | Sends a `MimeMessage` using configured SMTP credentials. Automatically retries on failure using a backoff policy. | A `ValueTask` representing the asynchronous send operation. |
| `IMimeUtil.ConvertMimeMessageToString(message, cancellationToken)` | Converts a `MimeMessage` into a string representation for logging or inspection. | A string version of the MIME message. |
| `IMimeUtil.InternalSend(message, cancellationToken)` | Sends a MIME message through the configured SMTP client without applying the public retry wrapper. | A task that completes after the SMTP send finishes. |
| `MimeUtilRegistrar.AddMimeUtilAsSingleton(services)` | Adds `IMimeUtil` as a singleton service. | The same service collection, so additional registrations can be chained. |
| `MimeUtilRegistrar.AddMimeUtilAsScoped(services)` | Adds `IMimeUtil` as a scoped service. | The same service collection, so additional registrations can be chained. |

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
