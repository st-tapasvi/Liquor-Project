namespace ST.LiquorTNT.Business.Common.Exceptions;

/// <summary>409 - a business rule refused the operation.</summary>
public sealed class BusinessException : AppException
{
    public BusinessException(string errorCode, string title, string? detail = null)
        : base(errorCode, 409, title, detail)
    {
    }
}
