namespace Reviews.Application.Reviews.Queries.GetReviewById;

using FluentValidation;
using MongoDB.Bson;

/// <summary>
/// Валідатор запиту отримання відгуку за ID (Критерій 8).
/// </summary>
public class GetReviewByIdQueryValidator : AbstractValidator<GetReviewByIdQuery>
{
    public GetReviewByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Ідентифікатор відгуку є обов'язковим.")
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Ідентифікатор повинен бути валідним 24-значним шістнадцятковим MongoDB ObjectId.");
    }
}
