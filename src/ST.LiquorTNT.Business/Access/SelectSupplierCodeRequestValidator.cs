using FluentValidation;
using ST.LiquorTNT.Contracts.Access;

namespace ST.LiquorTNT.Business.Access;

public sealed class SelectSupplierCodeRequestValidator : AbstractValidator<SelectSupplierCodeRequest>
{
    public SelectSupplierCodeRequestValidator()
    {
        RuleFor(x => x.SupplierCodeId).GreaterThan(0);
    }
}
