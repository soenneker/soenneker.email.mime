using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Email.Mime.Abstract;
using Soenneker.Utils.MemoryStream.Registrars;

namespace Soenneker.Email.Mime.Registrars;

/// <summary>
/// A resilient, configurable SMTP email sender
/// </summary>
public static class MimeUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IMimeUtil"/> as a singleton service. <para/>
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddMimeUtilAsSingleton(this IServiceCollection services)
    {
        services.AddMemoryStreamUtilAsSingleton();
        services.TryAddSingleton<IMimeUtil, MimeUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IMimeUtil"/> as a scoped service. <para/>
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddMimeUtilAsScoped(this IServiceCollection services)
    {
        services.AddMemoryStreamUtilAsSingleton();
        services.TryAddScoped<IMimeUtil, MimeUtil>();

        return services;
    }
}
