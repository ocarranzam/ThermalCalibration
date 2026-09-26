using Thermal.Domain.Common;

namespace Thermal.Domain.Companies;

/// <summary>
/// Empresa cliente (HU-01), identificada por su RUC, que no cambia. El resto de datos se edita con métodos del
/// negocio. No se borra: se desactiva.
/// </summary>
/// <remarks>
/// El constructor primario valida los datos de creación; sus parámetros tienen los mismos nombres que las propiedades
/// persistidas para que EF Core lo use al materializar.
/// </remarks>
public sealed class Company(
    string taxId,
    string name,
    string? contactName,
    string? phone,
    string? email,
    string? address)
{
    public const int NameMaxLength = 200;
    public const int ContactNameMaxLength = 150;
    public const int PhoneMaxLength = 30;
    public const int EmailMaxLength = 150;
    public const int AddressMaxLength = 300;

    public int Id { get; private set; }

    /// <summary>RUC validado (<see cref="Companies.TaxId"/>); es la identidad de negocio y no se edita.</summary>
    public string TaxId { get; private set; } = Companies.TaxId.Parse(taxId).Value;

    public string Name { get; private set => field = ValidName(value); } = ValidName(name);

    public string? ContactName { get; private set => field = Optional(value, ContactNameMaxLength, nameof(ContactName), "El contacto admite"); }
        = Optional(contactName, ContactNameMaxLength, nameof(ContactName), "El contacto admite");

    public string? Phone { get; private set => field = Optional(value, PhoneMaxLength, nameof(Phone), "El teléfono admite"); }
        = Optional(phone, PhoneMaxLength, nameof(Phone), "El teléfono admite");

    public string? Email { get; private set => field = ValidEmail(value); } = ValidEmail(email);

    public string? Address { get; private set => field = Optional(value, AddressMaxLength, nameof(Address), "La dirección admite"); }
        = Optional(address, AddressMaxLength, nameof(Address), "La dirección admite");

    public bool IsActive { get; private set; } = true;

    /// <summary>Lo asigna la base al insertar.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Lo asigna la infraestructura al guardar una edición.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Versión de concurrencia optimista (<c>ROWVERSION</c>); la API la expone como ETag.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public void Rename(string newName) => Name = newName;

    public void ChangeContact(string? newContactName, string? newPhone, string? newEmail, string? newAddress)
    {
        ContactName = newContactName;
        Phone = newPhone;
        Email = newEmail;
        Address = newAddress;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string ValidName(string value) => value switch
    {
        _ when string.IsNullOrWhiteSpace(value) => throw Invalid(nameof(Name), "La razón social es obligatoria"),
        { Length: > NameMaxLength } => throw Invalid(nameof(Name), $"La razón social admite como máximo {NameMaxLength} caracteres"),
        _ when value != value.Trim() => throw Invalid(nameof(Name), "La razón social no puede empezar ni terminar con espacios"),
        _ => value,
    };

    private static string? ValidEmail(string? value)
    {
        var email = Optional(value, EmailMaxLength, nameof(Email), "El correo admite");
        return email is not null && !IsEmailLike(email)
            ? throw Invalid(nameof(Email), "El correo no tiene un formato válido")
            : email;
    }

    private static bool IsEmailLike(string value)
    {
        var at = value.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < value.Length - 1 && value.IndexOf('@', at + 1) < 0 && !value.Contains(' ', StringComparison.Ordinal);
    }

    private static string? Optional(string? value, int maxLength, string property, string tooLongPrefix) => value switch
    {
        null => null,
        _ when string.IsNullOrWhiteSpace(value) => null,
        { Length: var length } when length > maxLength => throw Invalid(property, $"{tooLongPrefix} como máximo {maxLength} caracteres"),
        _ => value.Trim(),
    };

    private static DomainValidationException Invalid(string property, string message) => new(property, message);
}
