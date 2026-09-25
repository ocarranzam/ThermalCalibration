using Thermal.Domain.Common;

namespace Thermal.Domain.EquipmentTypes;

/// <summary>
/// Agregado del catálogo de tipos de equipo (HU-02). Guarda el límite máximo, que puede quedar
/// pendiente, y la duración mínima de sesión que exige el tipo (RN-04, RN-06).
/// </summary>
/// <remarks>
/// El constructor primario valida los datos de creación. Sus parámetros tienen los mismos nombres
/// que las propiedades persistidas, de modo que EF Core también lo usa al materializar.
/// El estado solo cambia mediante métodos con nombre del negocio (ADR-001 §2.3), y cada propiedad
/// validada pasa por su setter (palabra clave <c>field</c> de C# 14), tanto al crear como al editar.
/// </remarks>
public sealed class EquipmentType(
    string name,
    decimal? maxTemperatureC,
    int minSessionDurationMinutes,
    string? description)
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int BaseSessionDurationMinutes = 60;
    public const int MaxSessionDurationMinutes = 43200;

    public int Id { get; private set; }

    public string Name { get; private set => field = ValidName(value); } = ValidName(name);

    /// <summary>Valor persistido del límite; <c>null</c> = pendiente.</summary>
    public decimal? MaxTemperatureC { get; private set; } = TemperatureLimit.Create(maxTemperatureC).MaxC;

    public int MinSessionDurationMinutes { get; private set => field = ValidMinSessionDuration(value); }
        = ValidMinSessionDuration(minSessionDurationMinutes);

    public string? Description { get; private set => field = ValidDescription(value); }
        = ValidDescription(description);

    public bool IsActive { get; private set; } = true;

    /// <summary>Lo asigna la base al insertar (<c>DF_EquipmentType_CreatedAt</c>).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Lo asigna la infraestructura al guardar una edición.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Versión de concurrencia optimista (<c>ROWVERSION</c>); la API la expone como ETag.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public TemperatureLimit MaxTemperature => TemperatureLimit.Create(MaxTemperatureC);

    public void Rename(string newName) => Name = newName;

    /// <summary>Solo afecta a las sesiones que se inicien después: las demás conservan su copia (RN-08).</summary>
    public void ChangeLimit(TemperatureLimit limit) => MaxTemperatureC = limit.MaxC;

    public void ChangeMinSessionDuration(int minutes) => MinSessionDurationMinutes = minutes;

    public void Describe(string? newDescription) => Description = newDescription;

    public void Activate() => IsActive = true;

    /// <summary>Un tipo con equipos no se borra: se desactiva y deja de ofrecerse para equipos nuevos.</summary>
    public void Deactivate() => IsActive = false;

    private static string ValidName(string value) => value switch
    {
        _ when string.IsNullOrWhiteSpace(value) =>
            throw Invalid(nameof(Name), "El nombre del tipo de equipo es obligatorio"),
        { Length: > NameMaxLength } =>
            throw Invalid(nameof(Name), $"El nombre admite como máximo {NameMaxLength} caracteres"),
        _ when value != value.Trim() =>
            throw Invalid(nameof(Name), "El nombre no puede empezar ni terminar con espacios"),
        _ => value,
    };

    private static int ValidMinSessionDuration(int minutes) => minutes switch
    {
        < BaseSessionDurationMinutes => throw Invalid(
            nameof(MinSessionDurationMinutes),
            $"La duración mínima no puede ser menor que {BaseSessionDurationMinutes} minutos"),
        > MaxSessionDurationMinutes => throw Invalid(
            nameof(MinSessionDurationMinutes),
            $"La duración mínima no puede ser mayor que {MaxSessionDurationMinutes} minutos"),
        _ => minutes,
    };

    private static string? ValidDescription(string? value) => value switch
    {
        { Length: > DescriptionMaxLength } => throw Invalid(
            nameof(Description), $"La descripción admite como máximo {DescriptionMaxLength} caracteres"),
        _ => value,
    };

    private static DomainValidationException Invalid(string property, string message) => new(property, message);
}
