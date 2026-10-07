using FluentValidation;
using ST.LiquorTNT.Contracts.Roles;

namespace ST.LiquorTNT.Business.Roles;

public sealed class SaveRoleRequestValidator : AbstractValidator<SaveRoleRequest>
{
    public SaveRoleRequestValidator()
    {
        RuleFor(x => x.RoleName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
        RuleFor(x => x.PasswordPolicyId).GreaterThan(0);
    }
}

public sealed class UpdateRoleRightsRequestValidator : AbstractValidator<UpdateRoleRightsRequest>
{
    public UpdateRoleRightsRequestValidator()
    {
        RuleFor(x => x.PageActionIds).NotNull();
        RuleForEach(x => x.PageActionIds).GreaterThan(0);
    }
}
