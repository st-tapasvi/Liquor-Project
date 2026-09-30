using FluentValidation;
using ST.LiquorTNT.Contracts.Auth;

namespace ST.LiquorTNT.Business.Auth;

public sealed class SetSecurityQuestionRequestValidator : AbstractValidator<SetSecurityQuestionRequest>
{
    public SetSecurityQuestionRequestValidator()
    {
        RuleFor(x => x.QuestionId).GreaterThan(0);
        RuleFor(x => x.Answer).MaximumLength(100)
            .Must(a => SecurityAnswers.Normalize(a ?? string.Empty).Length >= 2)
            .WithMessage("Answer must have at least 2 characters.");
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(128);
    }
}

public sealed class ForgotPasswordStartRequestValidator : AbstractValidator<ForgotPasswordStartRequest>
{
    public ForgotPasswordStartRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(50);
    }
}

public sealed class ForgotPasswordVerifyRequestValidator : AbstractValidator<ForgotPasswordVerifyRequest>
{
    public ForgotPasswordVerifyRequestValidator()
    {
        RuleFor(x => x.RequestToken).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Answer).NotEmpty().MaximumLength(100);
    }
}

public sealed class ForgotPasswordResetRequestValidator : AbstractValidator<ForgotPasswordResetRequest>
{
    public ForgotPasswordResetRequestValidator()
    {
        RuleFor(x => x.RequestToken).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(128);
    }
}
