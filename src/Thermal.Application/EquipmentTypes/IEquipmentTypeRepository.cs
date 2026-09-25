using Thermal.Domain.EquipmentTypes;

namespace Thermal.Application.EquipmentTypes;

/// <summary>Repositorio de escritura del agregado <see cref="EquipmentType"/>.</summary>
public interface IEquipmentTypeRepository
{
    Task<EquipmentType?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Indica si otro tipo usa el nombre (la base compara sin distinguir mayúsculas).</summary>
    Task<bool> NameExistsAsync(string name, int? excludingId, CancellationToken cancellationToken);

    void Add(EquipmentType equipmentType);

    /// <summary>
    /// Comprueba que la versión guardada sea <paramref name="expectedRowVersion"/> y la exige también al
    /// guardar, para detectar una edición concurrente entre la lectura y el guardado.
    /// </summary>
    /// <exception cref="Exceptions.ConcurrencyConflictException">La versión ya no es la esperada.</exception>
    void EnsureVersion(EquipmentType equipmentType, byte[] expectedRowVersion);
}

/// <summary>Lecturas del catálogo, sin pasar por el agregado (CQRS lógico, ADR-001 §2.2).</summary>
public interface IEquipmentTypeReadStore
{
    Task<EquipmentTypeDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
}
