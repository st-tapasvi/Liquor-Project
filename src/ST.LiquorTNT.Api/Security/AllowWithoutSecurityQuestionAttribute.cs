namespace ST.LiquorTNT.Api.Security;

/// <summary>
/// Marks the few endpoints a session may reach before the user has set a security question (first login):
/// the question screen itself, "who am I" and logout. Every other endpoint answers 403 SECURITY_QUESTION_REQUIRED
/// (checked in SessionValidationMiddleware).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowWithoutSecurityQuestionAttribute : Attribute
{
}
