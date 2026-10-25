namespace Catalog.Bll.Validators;

using FluentValidation;
using Catalog.Bll.DTOs;

public class UpdateServiceDtoValidator : AbstractValidator<UpdateServiceDto>
{
    public UpdateServiceDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва послуги не може бути порожньою.")
            .MinimumLength(3).WithMessage("Назва послуги повинна містити щонайменше 3 символи.")
            .MaximumLength(200).WithMessage("Назва послуги не може бути довшою за 200 символів.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Необхідно обрати валідну категорію послуг (CategoryId > 0).");

        RuleFor(x => x.DurationMin)
            .InclusiveBetween(15, 2880).WithMessage("Тривалість послуги повинна складати від 15 хвилин до 48 годин (2880 хв).");
    }
}
