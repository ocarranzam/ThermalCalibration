using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Thermal.Infrastructure.Persistence;

[assembly: AssemblyFixture(typeof(Thermal.IntegrationTests.SqlServerFixture))]

namespace Thermal.IntegrationTests;

/// <summary>
/// Un único SQL Server 2022 en Docker para toda la ejecución, creado con <c>docs/db/01-schema.sql</c>.
/// El contenedor se limita a 2 GB (mínimo que exige SQL Server) y se elimina al terminar; si el proceso
/// se interrumpe, el reaper de Testcontainers (Ryuk) lo borra igualmente.
/// </summary>
public sealed partial class SqlServerFixture : IAsyncLifetime
{
    private const string Image = "mcr.microsoft.com/mssql/server:2022-latest";
    private const long MemoryLimitBytes = 2L * 1024 * 1024 * 1024;
    private const string DatabaseName = "ThermalCalibration";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image)
        .WithCreateParameterModifier(parameters =>
        {
            parameters.HostConfig ??= new Docker.DotNet.Models.HostConfig();
            parameters.HostConfig.Memory = MemoryLimitBytes;
        })
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public ThermalDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ThermalDbContext>().UseSqlServer(ConnectionString).Options,
        TimeProvider.System);

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await CreateSchemaAsync();

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = DatabaseName,
        }.ConnectionString;
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    /// <summary>Ejecuta el script por lotes (separados por <c>GO</c>) en una sola conexión, como sqlcmd.</summary>
    private async Task CreateSchemaAsync()
    {
        var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Database", "01-schema.sql"));

        await using var connection = new SqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();

        foreach (var batch in GoSeparator().Split(script).Where(b => !string.IsNullOrWhiteSpace(b)))
        {
            await using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
            await command.ExecuteNonQueryAsync();
        }
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();
}
