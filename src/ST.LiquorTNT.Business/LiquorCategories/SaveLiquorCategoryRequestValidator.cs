using FluentValidation;
using ST.LiquorTNT.Contracts.LiquorCategories;

namespace ST.LiquorTNT.Business.LiquorCategories;

public sealed class SaveLiquorCategoryRequestValidator : AbstractValidator<SaveLiquorCategoryRequest>
{
    public SaveLiquorCategoryRequestValidator()
    {
        RuleFor(x => x.CategoryCode)
            .NotEmpty()
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9/_-]+$").WithMessage("Category code may contain letters, digits, '/', '_' and '-' only.");
        RuleFor(x => x.CategoryName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
