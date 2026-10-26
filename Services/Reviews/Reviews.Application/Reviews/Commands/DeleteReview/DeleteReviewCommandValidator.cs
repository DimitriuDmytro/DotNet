namespace Reviews.Application.Reviews.Commands.DeleteReview;

using FluentValidation;
using MongoDB.Bson;

/// <summary>
/// Валідатор команди видалення відгуку (Критерій 8).
/// </summary>
public class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Ідентифікатор відгуку є обов'язковим.")
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Ідентифікатор повинен бути валідним 24-значним шістнадцятковим MongoDB ObjectId.");
    }
}
