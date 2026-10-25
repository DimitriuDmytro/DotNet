namespace Catalog.Bll.Validators;

using FluentValidation;
using Catalog.Bll.DTOs;

public class CreatePackageDtoValidator : AbstractValidator<CreatePackageDto>
{
    public CreatePackageDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва пакета не може бути порожньою.")
            .MinimumLength(3).WithMessage("Назва пакета повинна містити щонайменше 3 символи.")
            .MaximumLength(150).WithMessage("Назва пакета не може перевищувати 150 символів.");

        RuleFor(x => x.Price)
            .GreaterThan(0m).WithMessage("Вартість пакета повинна бути більшою за нуль.")
            .LessThanOrEqualTo(10_000_000m).WithMessage("Вартість пакета перевищує допустимий ліміт.");

        RuleForEach(x => x.Services).ChildRules(service =>
        {
            service.RuleFor(s => s.ServiceId)
                .GreaterThan(0).WithMessage("Ідентифікатор послуги у пакеті повинен бути додатним.");
            service.RuleFor(s => s.SortOrder)
                .GreaterThan(0).WithMessage("Порядковий номер послуги у пакеті повинен бути більшим за нуль.");
        });

        RuleFor(x => x.Services)
            .Must(services => services.Select(s => s.ServiceId).Distinct().Count() == services.Count)
            .WithMessage("У складі пакета не може бути дублікатів однієї й тієї ж послуги.");
    }
}
