namespace Catalog.Bll.Validators;

using FluentValidation;
using Catalog.Bll.DTOs;

public class UpdatePackageDtoValidator : AbstractValidator<UpdatePackageDto>
{
    public UpdatePackageDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва пакета не може бути порожньою.")
            .MinimumLength(3).WithMessage("Назва пакета повинна містити щонайменше 3 символи.")
            .MaximumLength(150).WithMessage("Назва пакета не може перевищувати 150 символів.");

        RuleFor(x => x.Price)
            .GreaterThan(0m).WithMessage("Вартість пакета повинна бути більшою за нуль.")
            .LessThanOrEqualTo(10_000_000m).WithMessage("Вартість пакета перевищує допустимий ліміт.");
    }
}
