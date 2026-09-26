using System.ComponentModel.DataAnnotations;
using Thermal.Application.Companies;

namespace Thermal.Api.Companies;

// Schemas de docs/api/thermal-v1.yaml. Aquí solo se valida la forma; el RUC, las longitudes y los formatos los valida
// el dominio, con los mensajes de HU-01.

/// <summary>Schema <c>CreateCompanyRequest</c>.</summary>
public sealed record CreateCompanyRequest
{
    [Required(ErrorMessage = "El RUC es obligatorio")]
    public string? TaxId { get; init; }

    [Required(ErrorMessage = "La razón social es obligatoria")]
    public string? Name { get; init; }

    public string? ContactName { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }
}

/// <summary>Schema <c>UpdateCompanyRequest</c> (semántica de reemplazo de PUT). El RUC no se edita.</summary>
public sealed record UpdateCompanyRequest
{
    [Required(ErrorMessage = "La razón social es obligatoria")]
    public string? Name { get; init; }

    public string? ContactName { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }

    [Required(ErrorMessage = "Indique si la empresa está activa")]
    public bool? IsActive { get; init; }
}

/// <summary>Schema <c>CompanyResponse</c>.</summary>
public sealed record CompanyResponse(
    int Id,
    string TaxId,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static CompanyResponse From(CompanyDto dto) => new(
        dto.Id, dto.TaxId, dto.Name, dto.ContactName, dto.Phone, dto.Email, dto.Address, dto.IsActive, dto.CreatedAt, dto.UpdatedAt);
}
