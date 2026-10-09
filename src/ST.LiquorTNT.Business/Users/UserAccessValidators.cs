using FluentValidation;
using ST.LiquorTNT.Contracts.Users;

namespace ST.LiquorTNT.Business.Users;

/// <summary>Shape of one role assignment; whether the role may be used is <see cref="UserAccessRules"/>' job.</summary>
public sealed class UserRoleAssignmentValidator : AbstractValidator<UserRoleAssignment>
{
    public UserRoleAssignmentValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}

public sealed class UpdateUserRolesRequestValidator : AbstractValidator<UpdateUserRolesRequest>
{
    public UpdateUserRolesRequestValidator()
    {
        // May be empty while the user holds a role group; "at least one role or group" is checked by the service.
        RuleFor(x => x.Roles).NotNull();
        RuleForEach(x => x.Roles).SetValidator(new UserRoleAssignmentValidator());
    }
}

public sealed class UpdateUserRoleGroupsRequestValidator : AbstractValidator<UpdateUserRoleGroupsRequest>
{
    public UpdateUserRoleGroupsRequestValidator()
    {
        RuleFor(x => x.RoleGroupIds).NotNull();
        RuleForEach(x => x.RoleGroupIds).GreaterThan(0);
    }
}

public sealed class UpdateUserRightsRequestValidator : AbstractValidator<UpdateUserRightsRequest>
{
    public UpdateUserRightsRequestValidator()
    {
        RuleFor(x => x.Rights).NotNull();
        RuleForEach(x => x.Rights).ChildRules(right =>
        {
            right.RuleFor(x => x.PageActionId).GreaterThan(0);
            right.RuleFor(x => x.SupplierCodeId).GreaterThan(0).When(x => x.SupplierCodeId.HasValue);
        });
    }
}
