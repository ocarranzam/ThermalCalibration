using Thermal.Application.Abstractions;
using Thermal.Application.Exceptions;
using Thermal.Domain.Companies;

namespace Thermal.Application.Companies;

public sealed record CompanyDto(
    int Id,
    string TaxId,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string Version)
{
    public static CompanyDto From(Company company) => new(
        company.Id, company.TaxId, company.Name, company.ContactName, company.Phone, company.Email, company.Address,
        company.IsActive, company.CreatedAt, company.UpdatedAt, Convert.ToBase64String(company.RowVersion));
}

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Id de la empresa con ese RUC, o <c>null</c>.</summary>
    Task<int?> FindIdByTaxIdAsync(string taxId, CancellationToken cancellationToken);

    void Add(Company company);

    /// <exception cref="ConcurrencyConflictException">La versión ya no es la esperada.</exception>
    void EnsureVersion(Company company, byte[] expectedRowVersion);
}

public interface ICompanyReadStore
{
    Task<CompanyDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Busca por RUC exacto y/o por parte de la razón social (sin distinguir mayúsculas).</summary>
    Task<IReadOnlyList<CompanyDto>> SearchAsync(string? taxId, string? name, CancellationToken cancellationToken);
}

/// <summary>Registra una empresa cliente activa (HU-01).</summary>
public sealed record CreateCompanyCommand(
    string TaxId, string Name, string? ContactName, string? Phone, string? Email, string? Address) : ICommand<CompanyDto>;

internal sealed class CreateCompanyCommandHandler(ICompanyRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> HandleAsync(CreateCompanyCommand command, CancellationToken cancellationToken)
    {
        var company = new Company(command.TaxId, command.Name, command.ContactName, command.Phone, command.Email, command.Address);

        if (await repository.FindIdByTaxIdAsync(company.TaxId, cancellationToken) is { } existingId)
        {
            throw new ConflictException($"Ya existe una empresa con el RUC {company.TaxId}", existingId: existingId);
        }

        repository.Add(company);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CompanyDto.From(company);
    }
}

/// <summary>Reemplaza los datos editables de una empresa (PUT). El RUC no se edita.</summary>
public sealed record UpdateCompanyCommand(
    int Id,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive,
    byte[]? ExpectedVersion) : ICommand<CompanyDto>;

internal sealed class UpdateCompanyCommandHandler(ICompanyRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> HandleAsync(UpdateCompanyCommand command, CancellationToken cancellationToken)
    {
        var company = await repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"No existe la empresa {command.Id}.");

        if (command.ExpectedVersion is { } expected)
        {
            repository.EnsureVersion(company, expected);
        }

        company.Rename(command.Name);
        company.ChangeContact(command.ContactName, command.Phone, command.Email, command.Address);
        if (command.IsActive)
        {
            company.Activate();
        }
        else
        {
            company.Deactivate();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CompanyDto.From(company);
    }
}

public sealed record GetCompanyByIdQuery(int Id) : IQuery<CompanyDto>;

internal sealed class GetCompanyByIdQueryHandler(ICompanyReadStore readStore) : IQueryHandler<GetCompanyByIdQuery, CompanyDto>
{
    public async Task<CompanyDto> HandleAsync(GetCompanyByIdQuery query, CancellationToken cancellationToken) =>
        await readStore.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException($"No existe la empresa {query.Id}.");
}

/// <summary>Buscador de empresas por RUC y por razón social (HU-01).</summary>
public sealed record SearchCompaniesQuery(string? TaxId, string? Name) : IQuery<IReadOnlyList<CompanyDto>>;

internal sealed class SearchCompaniesQueryHandler(ICompanyReadStore readStore)
    : IQueryHandler<SearchCompaniesQuery, IReadOnlyList<CompanyDto>>
{
    public Task<IReadOnlyList<CompanyDto>> HandleAsync(SearchCompaniesQuery query, CancellationToken cancellationToken) =>
        readStore.SearchAsync(
            string.IsNullOrWhiteSpace(query.TaxId) ? null : query.TaxId.Trim(),
            string.IsNullOrWhiteSpace(query.Name) ? null : query.Name.Trim(),
            cancellationToken);
}
