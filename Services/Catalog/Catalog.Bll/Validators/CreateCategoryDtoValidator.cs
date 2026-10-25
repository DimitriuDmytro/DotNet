namespace Catalog.Bll.Validators;

using FluentValidation;
using Catalog.Bll.DTOs;

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва категорії не може бути порожньою.")
            .MinimumLength(2).WithMessage("Назва категорії повинна містити щонайменше 2 символи.")
            .MaximumLength(100).WithMessage("Назва категорії не може перевищувати 100 символів.");
    }
}
