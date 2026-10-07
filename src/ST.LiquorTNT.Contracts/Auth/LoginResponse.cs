using ST.LiquorTNT.Contracts.SupplierCodes;

namespace ST.LiquorTNT.Contracts.Auth;

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Hard limit of this session (IST), even while the user is working. After it every call answers
    /// 401 SESSION_EXPIRED: show the password popup, log in again and retry the call.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Minutes without any API call after which the session ends (401 SESSION_TIMED_OUT → login page).</summary>
    public int IdleTimeoutMinutes { get; set; }

    public CurrentUserResponse User { get; set; } = new();

    /// <summary>
    /// The supplier codes this user may work in. With more than one, show a picker and call
    /// <c>POST /api/auth/selectsuppliercode</c>; business APIs answer 409 SUPPLIER_CODE_NOT_SELECTED until then.
    /// </summary>
    public IReadOnlyCollection<SupplierCodeResponse> SupplierCodes { get; set; } = Array.Empty<SupplierCodeResponse>();

    /// <summary>Already selected when the user has exactly one supplier code; otherwise null.</summary>
    public SupplierCodeResponse? ActiveSupplierCode { get; set; }
}
