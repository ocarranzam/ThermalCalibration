using System.ComponentModel.DataAnnotations;
using Thermal.Application.Equipments;

namespace Thermal.Api.Equipments;

// Schemas de docs/api/thermal-v1.yaml. Aquí solo se valida la forma; las longitudes y las reglas las validan el
// dominio y el caso de uso, con los mensajes de HU-01.

/// <summary>Schema <c>CreateEquipmentRequest</c>. La empresa va en la ruta.</summary>
public sealed record CreateEquipmentRequest
{
    [Required(ErrorMessage = "El tipo de equipo es obligatorio")]
    public int? EquipmentTypeId { get; init; }

    [Required(ErrorMessage = "La marca es obligatoria")]
    public string? Brand { get; init; }

    [Required(ErrorMessage = "El modelo es obligatorio")]
    public string? Model { get; init; }

    /// <summary>Falso si el modelo es inferido y falta confirmarlo en la placa. Sin valor: confirmado.</summary>
    public bool? IsModelConfirmed { get; init; }

    [Required(ErrorMessage = "El número de serie es obligatorio")]
    public string? SerialNumber { get; init; }

    public string? InternalCode { get; init; }

    public string? Notes { get; init; }
}

/// <summary>Schema <c>UpdateEquipmentRequest</c> (semántica de reemplazo de PUT). La empresa no cambia.</summary>
public sealed record UpdateEquipmentRequest
{
    [Required(ErrorMessage = "El tipo de equipo es obligatorio")]
    public int? EquipmentTypeId { get; init; }

    [Required(ErrorMessage = "La marca es obligatoria")]
    public string? Brand { get; init; }

    [Required(ErrorMessage = "El modelo es obligatorio")]
    public string? Model { get; init; }

    [Required(ErrorMessage = "Indique si el modelo está confirmado en la placa")]
    public bool? IsModelConfirmed { get; init; }

    [Required(ErrorMessage = "El número de serie es obligatorio")]
    public string? SerialNumber { get; init; }

    public string? InternalCode { get; init; }

    public string? Notes { get; init; }

    [Required(ErrorMessage = "Indique si el equipo está activo")]
    public bool? IsActive { get; init; }
}

/// <summary>Schema <c>EquipmentResponse</c>.</summary>
public sealed record EquipmentResponse(
    int Id,
    int CompanyId,
    int EquipmentTypeId,
    string EquipmentTypeName,
    string Brand,
    string Model,
    bool IsModelConfirmed,
    string SerialNumber,
    string? InternalCode,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static EquipmentResponse From(EquipmentDto dto) => new(
        dto.Id, dto.CompanyId, dto.EquipmentTypeId, dto.EquipmentTypeName, dto.Brand, dto.Model, dto.IsModelConfirmed,
        dto.SerialNumber, dto.InternalCode, dto.Notes, dto.IsActive, dto.CreatedAt, dto.UpdatedAt);
}
