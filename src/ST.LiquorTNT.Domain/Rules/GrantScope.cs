namespace ST.LiquorTNT.Domain.Rules;

/// <summary>
/// Who may hand a right (a page action) to a role or a user. Stored as text in PAGE_ACTIONS.GRANT_SCOPE.
/// </summary>
public enum GrantScope
{
    /// <summary>Anyone who manages roles or user access.</summary>
    ANY,

    /// <summary>Only a holder of <c>user.manageadmin</c> (Plant Admin by default), e.g. security settings.</summary>
    ADMIN,

    /// <summary>Nobody: only Super Admin has it (masters that come from the CRM, such as supplier codes).</summary>
    SYSTEM,
}
