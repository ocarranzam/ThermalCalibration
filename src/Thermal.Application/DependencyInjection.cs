using Microsoft.Extensions.DependencyInjection;
using Thermal.Application.Abstractions;

namespace Thermal.Application;

public static class DependencyInjection
{
    /// <summary>Registra todos los handlers de comandos y consultas de este ensamblado.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Type[] handlerInterfaces = [typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

        var registrations =
            from type in typeof(DependencyInjection).Assembly.GetTypes()
            where type is { IsClass: true, IsAbstract: false }
            from contract in type.GetInterfaces()
            where contract.IsGenericType && handlerInterfaces.Contains(contract.GetGenericTypeDefinition())
            select (contract, type);

        foreach (var (contract, implementation) in registrations)
        {
            services.AddScoped(contract, implementation);
        }

        return services;
    }
}
