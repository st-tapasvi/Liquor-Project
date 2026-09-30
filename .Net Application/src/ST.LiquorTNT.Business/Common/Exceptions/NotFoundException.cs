namespace ST.LiquorTNT.Business.Common.Exceptions;

/// <summary>404 - the requested object does not exist in this tenant.</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string what)
        : base(ErrorCodes.NotFound, 404, $"{what} was not found.")
    {
    }
}
