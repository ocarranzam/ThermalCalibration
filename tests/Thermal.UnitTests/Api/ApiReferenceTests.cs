using Thermal.Api.Settings;

namespace Thermal.UnitTests.Api;

/// <summary>La referencia de Scalar sirve exactamente el contrato del repositorio (no una copia desactualizada).</summary>
[Trait("Story", "HU-02")]
public sealed class ApiReferenceTests
{
    [Fact] // HU-02 · Regla: el contrato docs/api/thermal-v1.yaml es la fuente de verdad que muestra Scalar
    public void EmbeddedContract_IsIdenticalToTheRepositoryFile()
    {
        using var stream = typeof(SettingsController).Assembly.GetManifestResourceStream("thermal-v1.yaml");
        stream.Should().NotBeNull("el .csproj de la API incrusta el contrato");
        using var reader = new StreamReader(stream!);
        var embedded = reader.ReadToEnd();

        var repositoryFile = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "api", "thermal-v1.yaml"));

        embedded.Should().Be(repositoryFile);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Thermal.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio (Thermal.slnx).");
    }
}
