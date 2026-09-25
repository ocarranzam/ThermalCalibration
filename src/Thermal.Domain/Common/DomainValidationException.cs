namespace Thermal.Domain.Common;

/// <summary>
/// Violación de una invariante del dominio. <see cref="Property"/> es el nombre de la propiedad
/// del dominio que la provoca (p. ej. <c>MaxTemperatureC</c>); la API lo adapta a su formato.
/// </summary>
public sealed class DomainValidationException(string property, string message) : Exception(message)
{
    public string Property { get; } = property;
}
