namespace WMSPro.Domain.Common;

/// <summary>
/// Represents a domain-level exception for unrecoverable business rule violations.
/// Prefer using the Result Pattern over throwing this exception where possible.
/// </summary>
public sealed class DomainException : Exception
{
    public Error Error { get; }

    public DomainException(Error error)
        : base(error.Message)
    {
        Error = error;
    }

    public DomainException(string code, string message)
        : base(message)
    {
        Error = new Error(code, message);
    }
}
