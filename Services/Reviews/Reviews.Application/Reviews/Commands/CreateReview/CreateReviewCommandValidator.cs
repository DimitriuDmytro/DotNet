namespace Reviews.Application.Reviews.Commands.CreateReview;

using FluentValidation;

/// <summary>
/// Валідатор команди створення відгуку з асинхронним правилом MustAsync (Критерій 8).
/// </summary>
public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    private readonly IReviewRepository _reviewRepository;

    public CreateReviewCommandValidator(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;

        RuleFor(x => x.ServiceId)
            .GreaterThan(0)
            .WithMessage("Ідентифікатор послуги повинен бути більшим за 0.");

        RuleFor(x => x.ServiceName)
            .NotEmpty()
            .WithMessage("Назва послуги є обов'язковою.")
            .MaximumLength(150);

        RuleFor(x => x.AuthorClientId)
            .GreaterThan(0)
            .WithMessage("Ідентифікатор клієнта повинен бути більшим за 0.");

        RuleFor(x => x.AuthorFullName)
            .NotEmpty()
            .WithMessage("ПІБ автора є обов'язковим.")
            .MaximumLength(100);

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Оцінка повинна бути від 1 до 5.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Заголовок відгуку є обов'язковим.")
            .Length(3, 200)
            .WithMessage("Заголовок відгуку повинен містити від 3 до 200 символів.");

        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("Текст відгуку є обов'язковим.")
            .Length(5, 2000)
            .WithMessage("Текст відгуку повинен містити від 5 до 2000 символів.");

        RuleFor(x => x)
            .MustAsync(async (cmd, cancellationToken) =>
            {
                var exists = await _reviewRepository.ExistsAsync(cmd.ServiceId, cmd.AuthorClientId, cancellationToken);
                return !exists;
            })
            .WithMessage("Клієнт вже залишив відгук для цієї послуги. Повторні відгуки заборонені.")
            .WithName("AuthorClientId");
    }
}
