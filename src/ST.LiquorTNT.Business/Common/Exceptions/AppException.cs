namespace ST.LiquorTNT.Business.Common.Exceptions;

/// <summary>Base class for every exception the API turns into a ProblemDetails response.</summary>
public abstract class AppException : Exception
{
    protected AppException(string errorCode, int statusCode, string title, string? detail = null)
        : base(detail ?? title)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
    }

    public string ErrorCode { get; }
    public int StatusCode { get; }
    public string Title { get; }
    public string? Detail { get; }
}
