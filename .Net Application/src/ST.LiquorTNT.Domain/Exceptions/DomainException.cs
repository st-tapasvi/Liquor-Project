namespace ST.LiquorTNT.Domain.Exceptions;

/// <summary>A domain invariant was broken.</summary>
public class DomainException : Exception
{
    public DomainException(string errorCode, string message) : base(message) => ErrorCode = errorCode;

    public string ErrorCode { get; }
}
