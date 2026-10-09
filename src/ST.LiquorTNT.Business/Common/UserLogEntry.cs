namespace ST.LiquorTNT.Business.Common;

/// <summary>
/// What a business operation wants recorded in USER_LOG. IP, user agent, correlation id and time
/// are added by the writer; the caller only says what happened.
/// OldValue / NewValue are serialised to JSON — pass plain response objects, never entities or secrets.
/// </summary>
public sealed record UserLogEntry(
    string ActionType,
    string ModuleName,
    string ActionStatus,
    string? EntityName = null,
    string? EntityId = null,
    string? Description = null,
    object? OldValue = null,
    object? NewValue = null,
    int? ActorUserId = null)      // null => the authenticated caller; set explicitly for login events
{
    public const string StatusSuccess = "SUCCESS";
    public const string StatusFailed = "FAILED";

    public static UserLogEntry Success(
        string actionType,
        string moduleName,
        string? entityName = null,
        string? entityId = null,
        string? description = null,
        object? oldValue = null,
        object? newValue = null,
        int? actorUserId = null)
        => new(actionType, moduleName, StatusSuccess, entityName, entityId, description, oldValue, newValue, actorUserId);

    public static UserLogEntry Failed(
        string actionType,
        string moduleName,
        string? description = null,
        string? entityName = null,
        string? entityId = null,
        int? actorUserId = null)
        => new(actionType, moduleName, StatusFailed, entityName, entityId, description, null, null, actorUserId);
}

/// <summary>The fixed set of ACTION_TYPE values. One place, so the audit screen can filter on them.</summary>
public static class UserLogActions
{
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string Logout = "LOGOUT";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string AccountUnlocked = "ACCOUNT_UNLOCKED";
    public const string SessionCreated = "SESSION_CREATED";
    public const string SessionExpired = "SESSION_EXPIRED";
    public const string SessionRevoked = "SESSION_REVOKED";

    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string PasswordResetRequested = "PASSWORD_RESET_REQUESTED";
    public const string PasswordResetSuccess = "PASSWORD_RESET_SUCCESS";
    public const string PasswordResetFailed = "PASSWORD_RESET_FAILED";
    public const string SecurityQuestionChanged = "SECURITY_QUESTION_CHANGED";

    public const string UserCreated = "USER_CREATED";
    public const string UserUpdated = "USER_UPDATED";
    public const string UserActivated = "USER_ACTIVATED";
    public const string UserDeactivated = "USER_DEACTIVATED";

    public const string SecurityConfigChanged = "SECURITY_CONFIG_CHANGED";
    public const string PasswordPolicyChanged = "PASSWORD_POLICY_CHANGED";

    public const string SupplierCodeSelected = "SUPPLIER_CODE_SELECTED";
    public const string RoleCreated = "ROLE_CREATED";
    public const string RoleUpdated = "ROLE_UPDATED";
    public const string RoleDeleted = "ROLE_DELETED";
    public const string RoleRightsChanged = "ROLE_RIGHTS_CHANGED";
    public const string RoleTemplatesCopied = "ROLE_TEMPLATES_COPIED";
    public const string UserRolesChanged = "USER_ROLES_CHANGED";
    public const string UserRoleGroupsChanged = "USER_ROLE_GROUPS_CHANGED";
    public const string RoleGroupCreated = "ROLE_GROUP_CREATED";
    public const string RoleGroupUpdated = "ROLE_GROUP_UPDATED";
    public const string RoleGroupDeleted = "ROLE_GROUP_DELETED";
    public const string RoleGroupRolesChanged = "ROLE_GROUP_ROLES_CHANGED";
    public const string UserRightsChanged = "USER_RIGHTS_CHANGED";

    public const string MasterCreated = "MASTER_CREATED";
    public const string MasterUpdated = "MASTER_UPDATED";
}

public static class UserLogModules
{
    public const string Users = "Users";
    public const string Auth = "Auth";
    public const string Security = "Security";
    public const string Roles = "Roles";
    public const string Masters = "Masters";
}
