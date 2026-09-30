using FluentValidation;
using ST.LiquorTNT.Contracts.PasswordPolicies;

namespace ST.LiquorTNT.Business.PasswordPolicies;

public sealed class UpdatePasswordPolicyRequestValidator : AbstractValidator<UpdatePasswordPolicyRequest>
{
    public UpdatePasswordPolicyRequestValidator()
    {
        RuleFor(x => x.MinLength).InclusiveBetween(1, 128);
        RuleFor(x => x.MaxLength).InclusiveBetween(1, 128)
            .GreaterThanOrEqualTo(x => x.MinLength).WithMessage("Maximum length must not be below the minimum length.");
        RuleFor(x => x.PasswordHistoryCount).InclusiveBetween(0, 50);
        RuleFor(x => x.PasswordExpiryDays)
            .NotNull().GreaterThanOrEqualTo(1).When(x => x.PasswordExpiryEnabled)
            .WithMessage("Expiry days must be at least 1 when expiry is enabled.");
    }
}
