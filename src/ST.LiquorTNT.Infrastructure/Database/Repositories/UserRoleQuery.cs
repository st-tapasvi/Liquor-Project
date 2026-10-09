namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

/// <summary>
/// The one definition of "the roles a user holds": given directly (USER_ROLES) plus every role of the user's active
/// role groups (USER_ROLE_GROUPS → ROLE_GROUP → ROLE_GROUP_ROLES). Rights, the supplier code picker, the password policy
/// and the admin-user check all use it, so a role reached through a group counts exactly like a direct one.
/// </summary>
internal static class UserRoleQuery
{
    /// <summary>Role ids the user holds, still a database query (use with <c>Contains</c>).</summary>
    public static IQueryable<int> RoleIds(AppDbContext db, int userId) =>
        db.USER_ROLES.Where(r => r.UserId == userId).Select(r => r.RoleId)
            .Union(
                from ug in db.USER_ROLE_GROUPS
                join g in db.ROLE_GROUP on ug.RoleGroupId equals g.Id
                join gr in db.ROLE_GROUP_ROLES on g.Id equals gr.RoleGroupId
                where ug.UserId == userId && g.IsActive
                select gr.RoleId);
}
