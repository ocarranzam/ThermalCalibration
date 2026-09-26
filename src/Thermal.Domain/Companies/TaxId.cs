using Thermal.Domain.Common;

namespace Thermal.Domain.Companies;

/// <summary>
/// RUC peruano (HU-01): 11 dígitos, prefijo 10, 15, 17 o 20 y dígito verificador módulo 11 con los pesos
/// 5, 4, 3, 2, 7, 6, 5, 4, 3, 2. La base aplica la misma regla en <c>CK_Company_TaxId</c>.
/// </summary>
public readonly record struct TaxId
{
    public const int Length = 11;
    private static readonly int[] Weights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
    private static readonly string[] ValidPrefixes = ["10", "15", "17", "20"];

    private TaxId(string value) => Value = value;

    public string Value { get; }

    /// <exception cref="DomainValidationException">Con el mensaje de HU-01 que corresponda.</exception>
    public static TaxId Parse(string? value)
    {
        var ruc = value ?? string.Empty;
        if (!ruc.All(char.IsAsciiDigit))
        {
            throw Invalid("El RUC solo admite dígitos");
        }

        if (ruc.Length != Length)
        {
            throw Invalid($"El RUC debe tener {Length} dígitos");
        }

        if (!ValidPrefixes.Contains(ruc[..2]))
        {
            throw Invalid("El RUC debe empezar con 10, 15, 17 o 20");
        }

        var sum = Weights.Select((weight, i) => (ruc[i] - '0') * weight).Sum();
        var checkDigit = (11 - (sum % 11)) % 10;
        return ruc[^1] - '0' == checkDigit ? new TaxId(ruc) : throw Invalid("El dígito verificador del RUC no es válido");
    }

    public override string ToString() => Value;

    private static DomainValidationException Invalid(string message) => new(nameof(Company.TaxId), message);
}
