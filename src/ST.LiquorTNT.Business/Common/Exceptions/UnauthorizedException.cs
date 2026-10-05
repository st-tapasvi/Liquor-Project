namespace ST.LiquorTNT.Business.Common.Exceptions;

/// <summary>401 - the caller could not be authenticated.</summary>
public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string errorCode, string title, string? detail = null)
        : base(errorCode, 401, title, detail)
    {
    }
}
