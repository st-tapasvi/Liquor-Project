namespace ST.LiquorTNT.Business.Common.Exceptions;

/// <summary>400 - the request was rejected. Errors are keyed by field so the form can show them.</summary>
public sealed class ValidationException : AppException
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base(ErrorCodes.ValidationFailed, 400, "One or more fields are invalid.")
        => Errors = errors;

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
