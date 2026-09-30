namespace ST.LiquorTNT.Business.Common.Exceptions;

/// <summary>403 - authenticated, but not allowed.</summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string errorCode, string title, string? detail = null)
        : base(errorCode, 403, title, detail)
    {
    }
}
