using FluentValidation;
using ST.LiquorTNT.Contracts.Users;

namespace ST.LiquorTNT.Business.Users;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.EmployeeCode).MaximumLength(50);
    }
}
