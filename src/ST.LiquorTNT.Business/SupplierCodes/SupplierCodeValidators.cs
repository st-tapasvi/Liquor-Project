using FluentValidation;
using ST.LiquorTNT.Contracts.SupplierCodes;

namespace ST.LiquorTNT.Business.SupplierCodes;

public sealed class CreateSupplierCodeRequestValidator : AbstractValidator<CreateSupplierCodeRequest>
{
    public CreateSupplierCodeRequestValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.ExciseId).GreaterThan(0);
        RuleFor(x => x.SupplierCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.LiquorCategoryId).GreaterThan(0);
        RuleFor(x => x.FranchiseName).MaximumLength(200);
    }
}

public sealed class UpdateSupplierCodeRequestValidator : AbstractValidator<UpdateSupplierCodeRequest>
{
    public UpdateSupplierCodeRequestValidator()
    {
        RuleFor(x => x.ExciseId).GreaterThan(0);
        RuleFor(x => x.SupplierCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.LiquorCategoryId).GreaterThan(0);
        RuleFor(x => x.FranchiseName).MaximumLength(200);
    }
}
