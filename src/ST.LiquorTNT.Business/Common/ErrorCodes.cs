namespace ST.LiquorTNT.Business.Common;

/// <summary>Stable error codes returned in ProblemDetails. The frontend reacts to these, not to messages.</summary>
public static class ErrorCodes
{
    // general
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string NotFound         = "NOT_FOUND";
    public const string Forbidden        = "FORBIDDEN";
    public const string DatabaseError    = "DATABASE_ERROR";
    public const string Unexpected       = "UNEXPECTED_ERROR";

    // refused by the framework before any controller ran (wrong URL, method or body type)
    public const string EndpointNotFound     = "ENDPOINT_NOT_FOUND";
    public const string MethodNotAllowed     = "METHOD_NOT_ALLOWED";
    public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";
    public const string RequestInvalid       = "REQUEST_INVALID";     // unreadable request (too large, broken body)
    public const string RequestRefused       = "REQUEST_REFUSED";     // any other bare 4xx

    // users
    public const string UserNameTaken               = "USERNAME_TAKEN";
    public const string PasswordPolicyNotConfigured = "PASSWORD_POLICY_NOT_CONFIGURED";
    public const string CannotDeactivateSelf        = "CANNOT_DEACTIVATE_SELF";
    public const string CannotChangeOwnRole         = "CANNOT_CHANGE_OWN_ROLE";

    // login / account state
    public const string InvalidCredentials     = "INVALID_CREDENTIALS";
    public const string UserInactive           = "USER_INACTIVE";
    public const string UserBlocked            = "USER_BLOCKED";
    public const string UserLocked             = "USER_LOCKED";
    public const string PasswordChangeRequired = "PASSWORD_CHANGE_REQUIRED";
    public const string PasswordExpired        = "PASSWORD_EXPIRED";

    // sessions / authentication
    public const string Unauthenticated     = "UNAUTHENTICATED";
    public const string SessionLimitReached = "SESSION_LIMIT_REACHED";
    public const string SessionInvalid      = "SESSION_INVALID";     // logged out / revoked / unknown -> login page
    public const string SessionTimedOut     = "SESSION_TIMED_OUT";   // idle longer than SESSION_IDLE_MINUTES -> login page
    public const string SessionExpired      = "SESSION_EXPIRED";     // hard limit SESSION_EXPIRY_MINUTES reached -> password popup, retry
    public const string CsrfRejected        = "CSRF_REJECTED";       // cookie-authenticated write without a matching X-XSRF-TOKEN, or from another site

    // forgot password / security questions
    public const string SecurityQuestionDisabled = "SECURITY_QUESTION_DISABLED";
    public const string SecurityQuestionNotSet   = "SECURITY_QUESTION_NOT_SET";
    public const string SecurityAnswerIncorrect  = "SECURITY_ANSWER_INCORRECT";
    public const string ResetRequestInvalid      = "RESET_REQUEST_INVALID";
    public const string ResetRequestExpired      = "RESET_REQUEST_EXPIRED";
    public const string ResetAttemptsExceeded    = "RESET_ATTEMPTS_EXCEEDED";
}
