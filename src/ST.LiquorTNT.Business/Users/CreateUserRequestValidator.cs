using FluentValidation;
using ST.LiquorTNT.Contracts.Users;

namespace ST.LiquorTNT.Business.Users;

/// <summary>Shape checks only. Password strength is the password policy's job, not this validator's.</summary>
public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9._@-]+$").WithMessage("User name may contain letters, digits, '.', '_', '@' and '-' only.");

        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Roles).NotNull()
            .Must((request, roles) => roles.Count + (request.RoleGroupIds?.Count ?? 0) > 0)
            .WithMessage("Give the user at least one role or one role group.");
        RuleForEach(x => x.Roles).SetValidator(new UserRoleAssignmentValidator());
        RuleFor(x => x.RoleGroupIds).NotNull();
        RuleForEach(x => x.RoleGroupIds).GreaterThan(0);
        RuleFor(x => x.CompanyId).GreaterThan(0).When(x => x.CompanyId.HasValue);
        RuleFor(x => x.FullName).MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.EmployeeCode).MaximumLength(50);
    }
}
