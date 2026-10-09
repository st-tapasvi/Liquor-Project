using FluentValidation;
using ST.LiquorTNT.Contracts.RoleGroups;

namespace ST.LiquorTNT.Business.RoleGroups;

public sealed class SaveRoleGroupRequestValidator : AbstractValidator<SaveRoleGroupRequest>
{
    public SaveRoleGroupRequestValidator()
    {
        RuleFor(x => x.GroupName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public sealed class UpdateRoleGroupRolesRequestValidator : AbstractValidator<UpdateRoleGroupRolesRequest>
{
    public UpdateRoleGroupRolesRequestValidator()
    {
        RuleFor(x => x.RoleIds).NotNull();
        RuleForEach(x => x.RoleIds).GreaterThan(0);
    }
}
