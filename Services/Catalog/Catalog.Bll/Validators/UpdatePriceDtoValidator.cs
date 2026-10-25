namespace Catalog.Bll.Validators;

using FluentValidation;
using Catalog.Bll.DTOs;

public class UpdatePriceDtoValidator : AbstractValidator<UpdatePriceDto>
{
    public UpdatePriceDtoValidator()
    {
        RuleFor(x => x.NewPrice)
            .GreaterThan(0m).WithMessage("Нова вартість послуги повинна бути більшою за нуль.")
            .LessThanOrEqualTo(10_000_000m).WithMessage("Вартість послуги перевищує максимально допустиму.");
    }
}
