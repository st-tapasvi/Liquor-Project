using ST.LiquorTNT.Domain.Rules;

namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>PAGE_ACTIONS</c>: one thing a user can do on a page (view, add, edit ... and any page-specific step such as approve, added later per page).
/// <see cref="PermissionKey"/> ("user.add") is what <c>[HasPermission]</c> checks and what the frontend
/// uses to show or hide a button. Seeded by SQL; read-only here.
/// </summary>
public class PAGE_ACTIONS
{
    private PAGE_ACTIONS()
    {
        ActionKey = string.Empty;
        PermissionKey = string.Empty;
        ActionName = string.Empty;
    }

    public int Id { get; private set; }
    public int PageId { get; private set; }
    public string ActionKey { get; private set; }         // add
    public string PermissionKey { get; private set; }     // user.add
    public string ActionName { get; private set; }        // label for the rights grid: Create
    public GrantScope GrantScope { get; private set; }    // who may hand this right out
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
}
