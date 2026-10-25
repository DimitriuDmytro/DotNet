namespace Catalog.Bll.Validators;

using FluentValidation;
using Catalog.Bll.DTOs;

public class CreateServiceDtoValidator : AbstractValidator<CreateServiceDto>
{
    public CreateServiceDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва послуги не може бути порожньою.")
            .MinimumLength(3).WithMessage("Назва послуги повинна містити щонайменше 3 символи.")
            .MaximumLength(200).WithMessage("Назва послуги не може бути довшою за 200 символів.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Необхідно обрати валідну категорію послуг (CategoryId > 0).");

        RuleFor(x => x.Price)
            .GreaterThan(0m).WithMessage("Вартість послуги повинна бути більшою за нуль.")
            .LessThanOrEqualTo(10_000_000m).WithMessage("Вартість послуги перевищує максимально допустимий ліміт.");

        RuleFor(x => x.DurationMin)
            .InclusiveBetween(15, 2880).WithMessage("Тривалість послуги повинна складати від 15 хвилин до 48 годин (2880 хв).");

        When(x => x.Spec != null, () =>
        {
            RuleFor(x => x.Spec!.Materials)
                .NotEmpty().WithMessage("Матеріали специфікації обов'язкові.")
                .MaximumLength(500).WithMessage("Перелік матеріалів не може перевищувати 500 символів.");

            RuleFor(x => x.Spec!.WarrantyMonths)
                .GreaterThanOrEqualTo(0).WithMessage("Гарантійний термін не може бути від'ємним.");

            RuleFor(x => x.Spec!.Description)
                .NotEmpty().WithMessage("Опис специфікації обов'язковий.")
                .MinimumLength(10).WithMessage("Опис специфікації має містити щонайменше 10 символів.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.MainImageUrl), () =>
        {
            RuleFor(x => x.MainImageUrl!)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .WithMessage("Некоректний формат URL головного зображення.");
        });
    }
}
