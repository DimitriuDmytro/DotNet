namespace Reviews.Application.Reviews.Commands.UpdateReview;

using FluentValidation;
using MongoDB.Bson;

/// <summary>
/// Валідатор команди оновлення відгуку (Критерій 8).
/// </summary>
public class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Ідентифікатор відгуку є обов'язковим.")
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Ідентифікатор повинен бути валідним 24-значним шістнадцятковим MongoDB ObjectId.");

        RuleFor(x => x.ExpectedVersion)
            .GreaterThan(0)
            .WithMessage("Очікувана версія (ExpectedVersion) повинна бути більшою за 0.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Оцінка повинна бути від 1 до 5.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Заголовок відгуку є обов'язковим.")
            .Length(3, 200)
            .WithMessage("Заголовок повинен містити від 3 до 200 символів.");

        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("Текст відгуку є обов'язковим.")
            .Length(5, 2000)
            .WithMessage("Текст повинен містити від 5 до 2000 символів.");
    }
}
