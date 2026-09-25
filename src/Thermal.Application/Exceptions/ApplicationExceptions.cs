namespace Thermal.Application.Exceptions;

/// <summary>El recurso solicitado no existe (HTTP 404).</summary>
public sealed class NotFoundException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>La operación choca con el estado actual, p. ej. un nombre duplicado (HTTP 409).</summary>
public sealed class ConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>El recurso cambió desde que el cliente lo leyó (HTTP 412, <c>If-Match</c>).</summary>
public sealed class ConcurrencyConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
