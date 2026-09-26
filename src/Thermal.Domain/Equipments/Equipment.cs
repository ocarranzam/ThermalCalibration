using Thermal.Domain.Common;

namespace Thermal.Domain.Equipments;

/// <summary>
/// Equipo bajo prueba de una empresa cliente (HU-01). La marca y el modelo son obligatorios porque son la clave de
/// los perfiles de eficacia por modelo; un modelo deducido sin ver la placa se registra como no confirmado.
/// </summary>
/// <remarks>
/// La unicidad de la serie dentro de la empresa y que el tipo de equipo esté activo son reglas entre agregados:
/// las comprueba el caso de uso (y la base con <c>UQ_Equipment_Company_Serial</c>).
/// </remarks>
public sealed class Equipment(
    int companyId,
    int equipmentTypeId,
    string brand,
    string model,
    bool isModelConfirmed,
    string serialNumber,
    string? internalCode,
    string? notes)
{
    public const int BrandMaxLength = 100;
    public const int ModelMaxLength = 100;
    public const int SerialNumberMaxLength = 100;
    public const int InternalCodeMaxLength = 50;
    public const int NotesMaxLength = 500;

    public int Id { get; private set; }

    public int CompanyId { get; private set; } = companyId;

    public int EquipmentTypeId { get; private set; } = equipmentTypeId;

    public string Brand { get; private set => field = Required(value, BrandMaxLength, nameof(Brand), "La marca es obligatoria", "La marca admite"); }
        = Required(brand, BrandMaxLength, nameof(Brand), "La marca es obligatoria", "La marca admite");

    public string Model { get; private set => field = Required(value, ModelMaxLength, nameof(Model), "El modelo es obligatorio", "El modelo admite"); }
        = Required(model, ModelMaxLength, nameof(Model), "El modelo es obligatorio", "El modelo admite");

    /// <summary>Falso si el modelo se infirió (p. ej. con la ficha del fabricante) y falta confirmarlo en la placa.</summary>
    public bool IsModelConfirmed { get; private set; } = isModelConfirmed;

    public string SerialNumber { get; private set => field = Required(value, SerialNumberMaxLength, nameof(SerialNumber), "El número de serie es obligatorio", "El número de serie admite"); }
        = Required(serialNumber, SerialNumberMaxLength, nameof(SerialNumber), "El número de serie es obligatorio", "El número de serie admite");

    public string? InternalCode { get; private set => field = Optional(value, InternalCodeMaxLength, nameof(InternalCode), "El código interno admite"); }
        = Optional(internalCode, InternalCodeMaxLength, nameof(InternalCode), "El código interno admite");

    public string? Notes { get; private set => field = Optional(value, NotesMaxLength, nameof(Notes), "Las notas admiten"); }
        = Optional(notes, NotesMaxLength, nameof(Notes), "Las notas admiten");

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    /// <summary>Cambia el tipo de equipo; el caso de uso comprueba que el tipo exista y esté activo.</summary>
    public void Reclassify(int newEquipmentTypeId) => EquipmentTypeId = newEquipmentTypeId;

    /// <summary>Corrige la marca y el modelo, indicando si el modelo está confirmado en la placa.</summary>
    public void Identify(string newBrand, string newModel, bool modelConfirmed)
    {
        Brand = newBrand;
        Model = newModel;
        IsModelConfirmed = modelConfirmed;
    }

    /// <summary>Confirma el modelo leído en la placa del equipo.</summary>
    public void ConfirmModel(string modelOnNameplate) => Identify(Brand, modelOnNameplate, modelConfirmed: true);

    public void ChangeSerialNumber(string newSerialNumber) => SerialNumber = newSerialNumber;

    public void Describe(string? newInternalCode, string? newNotes)
    {
        InternalCode = newInternalCode;
        Notes = newNotes;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string Required(string value, int maxLength, string property, string missingMessage, string tooLongPrefix) => value switch
    {
        _ when string.IsNullOrWhiteSpace(value) => throw Invalid(property, missingMessage),
        { Length: var length } when length > maxLength => throw Invalid(property, $"{tooLongPrefix} como máximo {maxLength} caracteres"),
        _ when value != value.Trim() => throw Invalid(property, "El valor no puede empezar ni terminar con espacios"),
        _ => value,
    };

    private static string? Optional(string? value, int maxLength, string property, string tooLongPrefix) => value switch
    {
        null => null,
        _ when string.IsNullOrWhiteSpace(value) => null,
        { Length: var length } when length > maxLength => throw Invalid(property, $"{tooLongPrefix} como máximo {maxLength} caracteres"),
        _ => value.Trim(),
    };

    private static DomainValidationException Invalid(string property, string message) => new(property, message);
}
