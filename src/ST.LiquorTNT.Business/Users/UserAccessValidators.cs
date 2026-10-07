using FluentValidation;
using ST.LiquorTNT.Contracts.Users;

namespace ST.LiquorTNT.Business.Users;

/// <summary>Shape of one role assignment; whether the role and supplier code may be used is <see cref="UserAccessRules"/>' job.</summary>
public sealed class UserRoleAssignmentValidator : AbstractValidator<UserRoleAssignment>
{
    public UserRoleAssignmentValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0);
        RuleFor(x => x.SupplierCodeId).GreaterThan(0).When(x => x.SupplierCodeId.HasValue);
    }
}

public sealed class UpdateUserRolesRequestValidator : AbstractValidator<UpdateUserRolesRequest>
{
    public UpdateUserRolesRequestValidator()
    {
        // A user without any role could not do anything, and would have no password policy.
        RuleFor(x => x.Roles).NotEmpty().WithMessage("Give the user at least one role.");
        RuleForEach(x => x.Roles).SetValidator(new UserRoleAssignmentValidator());
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
