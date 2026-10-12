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

    /// <summary>
    /// Extra fields written into the ProblemDetails body next to the standard ones, for errors where the client needs
    /// data to act on (e.g. "sessions" on 409 SESSION_LIMIT_REACHED). Keys are camelCase; never put secrets here.
    /// </summary>
    public IDictionary<string, object?> Extensions { get; } = new Dictionary<string, object?>();
}
