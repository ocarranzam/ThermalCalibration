using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Thermal.Application.Abstractions;
using Thermal.Application.EquipmentTypes;
using Thermal.Infrastructure.Persistence;

namespace Thermal.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Thermal";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Falta la cadena de conexión '{ConnectionStringName}'.");

        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<ThermalDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEquipmentTypeRepository, EquipmentTypeRepository>();
        services.AddScoped<IEquipmentTypeReadStore, EquipmentTypeReadStore>();

        return services;
    }
}
