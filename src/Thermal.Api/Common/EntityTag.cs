using Thermal.Application.Exceptions;

namespace Thermal.Api.Common;

/// <summary>Conversión entre la <c>RowVersion</c> (Base64) y las cabeceras <c>ETag</c> / <c>If-Match</c>.</summary>
internal static class EntityTag
{
    public static string From(string version) => $"\"{version}\"";

    /// <summary>
    /// Devuelve la versión esperada, o <c>null</c> si no hay <c>If-Match</c> o es <c>*</c>.
    /// Un valor que no corresponde a ninguna versión se trata como versión desactualizada (412).
    /// </summary>
    public static byte[]? ParseIfMatch(string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch) || ifMatch.Trim() == "*")
        {
            return null;
        }

        var tag = ifMatch.Trim();
        if (tag.StartsWith("W/", StringComparison.Ordinal))
        {
            tag = tag[2..];
        }

        tag = tag.Trim('"');
        var buffer = new byte[tag.Length];
        return Convert.TryFromBase64String(tag, buffer, out var written) && written > 0
            ? buffer[..written]
            : throw new ConcurrencyConflictException(
                "El If-Match no corresponde a la versión actual del recurso. Vuelva a leerlo antes de guardar.");
    }
}
