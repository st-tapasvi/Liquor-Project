using System.Reflection;

namespace ST.LiquorTNT.Api.Logging;

public static class MethodLoggingRegistration
{
    /// <summary>
    /// Puts every Business use-case service behind <see cref="MethodLoggingProxy{TService}"/>: an <c>I…Service</c>
    /// interface whose implementation class is also in <paramref name="businessAssembly"/>. Interfaces that
    /// Business declares but Infrastructure implements (e.g. IAccessTokenService, whose output is a raw JWT)
    /// are never wrapped, whatever the registration order. The registration is replaced in place, lifetime kept.
    /// </summary>
    public static IServiceCollection AddMethodLogging(this IServiceCollection services, Assembly businessAssembly)
    {
        for (var i = 0; i < services.Count; i++)
        {
            var descriptor = services[i];
            var implementationType = descriptor.ImplementationType;

            if (!descriptor.ServiceType.IsInterface
                || descriptor.ServiceType.Assembly != businessAssembly
                || !descriptor.ServiceType.Name.EndsWith("Service", StringComparison.Ordinal)
                || implementationType?.Assembly != businessAssembly)
            {
                continue;
            }

            var create = typeof(MethodLoggingProxy<>).MakeGenericType(descriptor.ServiceType).GetMethod("Create")!;

            services[i] = new ServiceDescriptor(descriptor.ServiceType, provider =>
            {
                var inner = ActivatorUtilities.CreateInstance(provider, implementationType);
                return create.Invoke(null, new[] { inner })!;
            }, descriptor.Lifetime);
        }

        return services;
    }
}
