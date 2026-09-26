using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thermal.Api.Common;
using Thermal.Api.Equipments;
using Thermal.Application.Abstractions;
using Thermal.Application.Companies;
using Thermal.Application.Equipments;

namespace Thermal.Api.Companies;

/// <summary>Empresas cliente y sus equipos (HU-01). Contrato: <c>docs/api/thermal-v1.yaml</c>, tag <c>Companies</c>.</summary>
[ApiController]
[Route("api/v1/companies")]
[Authorize]
public sealed class CompaniesController(
    ICommandHandler<CreateCompanyCommand, CompanyDto> createHandler,
    IQueryHandler<GetCompanyByIdQuery, CompanyDto> getByIdHandler,
    IQueryHandler<SearchCompaniesQuery, IReadOnlyList<CompanyDto>> searchHandler,
    ICommandHandler<UpdateCompanyCommand, CompanyDto> updateHandler,
    ICommandHandler<CreateEquipmentCommand, EquipmentDto> createEquipmentHandler,
    IQueryHandler<ListCompanyEquipmentQuery, IReadOnlyList<EquipmentDto>> listEquipmentHandler) : ControllerBase
{
    /// <summary>operationId <c>createCompany</c>.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Editors)]
    [Consumes("application/json")]
    public async Task<ActionResult<CompanyResponse>> Create(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var company = await createHandler.HandleAsync(
            new CreateCompanyCommand(
                request.TaxId!, request.Name!, request.ContactName, request.Phone, request.Email, request.Address),
            cancellationToken);

        Response.Headers.ETag = EntityTag.From(company.Version);
        return CreatedAtAction(nameof(GetById), new { id = company.Id }, CompanyResponse.From(company));
    }

    /// <summary>operationId <c>searchCompanies</c>: por RUC exacto y/o parte de la razón social.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<CompanyResponse>> Search(
        [FromQuery] string? taxId, [FromQuery] string? name, CancellationToken cancellationToken) =>
        [.. (await searchHandler.HandleAsync(new SearchCompaniesQuery(taxId, name), cancellationToken)).Select(CompanyResponse.From)];

    /// <summary>operationId <c>getCompany</c>.</summary>
    [HttpGet("{id:int:min(1)}")]
    public async Task<ActionResult<CompanyResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var company = await getByIdHandler.HandleAsync(new GetCompanyByIdQuery(id), cancellationToken);

        Response.Headers.ETag = EntityTag.From(company.Version);
        return CompanyResponse.From(company);
    }

    /// <summary>operationId <c>updateCompany</c>.</summary>
    [HttpPut("{id:int:min(1)}")]
    [Authorize(Roles = Roles.Editors)]
    [Consumes("application/json")]
    public async Task<ActionResult<CompanyResponse>> Update(
        int id,
        UpdateCompanyRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var company = await updateHandler.HandleAsync(
            new UpdateCompanyCommand(
                id,
                request.Name!,
                request.ContactName,
                request.Phone,
                request.Email,
                request.Address,
                request.IsActive!.Value,
                EntityTag.ParseIfMatch(ifMatch)),
            cancellationToken);

        Response.Headers.ETag = EntityTag.From(company.Version);
        return CompanyResponse.From(company);
    }

    /// <summary>operationId <c>createCompanyEquipment</c>.</summary>
    [HttpPost("{id:int:min(1)}/equipment")]
    [Authorize(Roles = Roles.Editors)]
    [Consumes("application/json")]
    public async Task<ActionResult<EquipmentResponse>> CreateEquipment(
        int id, CreateEquipmentRequest request, CancellationToken cancellationToken)
    {
        var equipment = await createEquipmentHandler.HandleAsync(
            new CreateEquipmentCommand(
                id,
                request.EquipmentTypeId!.Value,
                request.Brand!,
                request.Model!,
                request.IsModelConfirmed,
                request.SerialNumber!,
                request.InternalCode,
                request.Notes),
            cancellationToken);

        Response.Headers.ETag = EntityTag.From(equipment.Version);
        return CreatedAtAction(
            nameof(EquipmentController.GetById), "Equipment", new { id = equipment.Id }, EquipmentResponse.From(equipment));
    }

    /// <summary>operationId <c>listCompanyEquipment</c>.</summary>
    [HttpGet("{id:int:min(1)}/equipment")]
    public async Task<IReadOnlyList<EquipmentResponse>> ListEquipment(int id, CancellationToken cancellationToken) =>
        [.. (await listEquipmentHandler.HandleAsync(new ListCompanyEquipmentQuery(id), cancellationToken)).Select(EquipmentResponse.From)];
}
