namespace PixPro.Services.Projects.Application.Common.Exceptions;

/// <summary>
/// Thrown by the persistence layer when an optimistic concurrency conflict is
/// detected (a concurrent request modified or inserted the same row first).
/// The application layer should catch this and retry the operation with fresh data.
/// This abstracts the underlying provider-specific exceptions (e.g. EF Core's
/// <c>DbUpdateConcurrencyException</c> or a unique-constraint violation) so the
/// application layer stays decoupled from the persistence technology.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
