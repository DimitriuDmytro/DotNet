namespace Reviews.Application.Reviews.Queries.GetReviewsList;

using FluentValidation;

/// <summary>
/// Валідатор запиту пагінованого списку відгуків (Критерій 8).
/// </summary>
public class GetReviewsListQueryValidator : AbstractValidator<GetReviewsListQuery>
{
    public GetReviewsListQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Номер сторінки повинен бути більшим або рівним 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Розмір сторінки повинен бути в межах від 1 до 100.");

        When(x => x.ServiceId.HasValue, () =>
        {
            RuleFor(x => x.ServiceId!.Value)
                .GreaterThan(0)
                .WithMessage("Ідентифікатор послуги повинен бути більшим за 0.");
        });
    }
}
