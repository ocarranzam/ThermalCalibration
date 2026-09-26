using Thermal.Domain.Common;
using Thermal.Domain.Companies;

namespace Thermal.UnitTests.Domain;

/// <summary>Empresa cliente y validación del RUC (HU-01).</summary>
[Trait("Story", "HU-01")]
public sealed class CompanyTests
{
    private const string ValidTaxId = "20100070970";

    private static Company NewCompany(
        string taxId = ValidTaxId, string name = "Laboratorios Andinos S.A.C.", string? email = null) =>
        new(taxId, name, contactName: null, phone: null, email, address: null);

    [Fact] // HU-01 · Scenario: Registrar una empresa nueva
    public void Create_ValidCompany_IsActive()
    {
        var company = NewCompany();

        company.TaxId.Should().Be(ValidTaxId);
        company.Name.Should().Be("Laboratorios Andinos S.A.C.");
        company.IsActive.Should().BeTrue();
    }

    [Theory] // HU-01 · Scenario Outline: Rechazar un RUC con formato inválido
    [InlineData("2010007097", "El RUC debe tener 11 dígitos")]
    [InlineData("2010007097A", "El RUC solo admite dígitos")]
    [InlineData("30100070970", "El RUC debe empezar con 10, 15, 17 o 20")]
    [InlineData("20100070971", "El dígito verificador del RUC no es válido")]
    public void Create_InvalidTaxId_IsRejected(string taxId, string message)
    {
        var create = () => NewCompany(taxId);

        create.Should().Throw<DomainValidationException>().WithMessage(message)
            .Which.Property.Should().Be(nameof(Company.TaxId));
    }

    [Theory] // HU-01 · Regla: RUC peruano válido (módulo 11), también el del ejemplo de otra empresa
    [InlineData("20100070970")]
    [InlineData("20601234565")]
    public void TaxIdParse_ValidRuc_IsAccepted(string ruc) => TaxId.Parse(ruc).Value.Should().Be(ruc);

    [Theory] // HU-01 · Regla: RUC vacío o ausente
    [InlineData(null)]
    [InlineData("")]
    public void TaxIdParse_Missing_RequiresElevenDigits(string? ruc)
    {
        var parse = () => TaxId.Parse(ruc);

        parse.Should().Throw<DomainValidationException>().WithMessage("El RUC debe tener 11 dígitos");
    }

    [Theory] // HU-01 · Regla: razón social obligatoria, sin espacios en los extremos
    [InlineData("", "La razón social es obligatoria")]
    [InlineData("  ", "La razón social es obligatoria")]
    [InlineData(" Andinos", "La razón social no puede empezar ni terminar con espacios")]
    public void Create_InvalidName_IsRejected(string name, string message)
    {
        var create = () => NewCompany(name: name);

        create.Should().Throw<DomainValidationException>().WithMessage(message)
            .Which.Property.Should().Be(nameof(Company.Name));
    }

    [Theory] // HU-01 · Regla: correo de contacto con formato básico
    [InlineData("calidad")]
    [InlineData("@andinos.pe")]
    [InlineData("calidad@")]
    [InlineData("a@b@c.pe")]
    [InlineData("cali dad@andinos.pe")]
    public void Create_InvalidEmail_IsRejected(string email)
    {
        var create = () => NewCompany(email: email);

        create.Should().Throw<DomainValidationException>().WithMessage("El correo no tiene un formato válido");
    }

    [Fact] // HU-01 · Regla: datos de contacto opcionales; vacío equivale a sin dato y se recortan espacios
    public void ChangeContact_BlankValuesBecomeNullAndValuesAreTrimmed()
    {
        var company = NewCompany();

        company.ChangeContact("  Ana Torres ", " ", " calidad@andinos.pe ", null);

        company.ContactName.Should().Be("Ana Torres");
        company.Phone.Should().BeNull();
        company.Email.Should().Be("calidad@andinos.pe");
        company.Address.Should().BeNull();
    }

    [Fact] // HU-01 · Regla: longitudes máximas (NVARCHAR del esquema)
    public void ChangeContact_TooLongPhone_IsRejected()
    {
        var company = NewCompany();

        var change = () => company.ChangeContact(null, new string('9', Company.PhoneMaxLength + 1), null, null);

        change.Should().Throw<DomainValidationException>().WithMessage("El teléfono admite como máximo 30 caracteres");
    }

    [Fact] // HU-01 · Regla: una empresa no se borra, se desactiva
    public void Deactivate_ThenActivate_TogglesIsActive()
    {
        var company = NewCompany();

        company.Deactivate();
        company.IsActive.Should().BeFalse();
        company.Activate();
        company.IsActive.Should().BeTrue();
    }
}
