namespace Thermal.Domain.Common;

/// <summary>
/// Violación de una invariante del dominio. <see cref="Property"/> es el nombre de la propiedad
/// en el contrato de la API (camelCase), para que la API la informe en <c>errors</c> (RFC 7807).
/// </summary>
public sealed class DomainValidationException(string property, string message) : Exception(message)
{
    public string Property { get; } = property;
}
