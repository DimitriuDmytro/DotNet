namespace Reviews.Tests.Application;

using FluentValidation.TestHelper;
using NSubstitute;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Application.Reviews.Commands.UpdateReview;
using Reviews.Application.Reviews.Queries.GetReviewsList;

public class ValidatorTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>();

    [Fact]
    public async Task CreateReviewCommandValidator_WhenReviewAlreadyExists_FailsMustAsyncRule()
    {
        // Arrange (Критерій 8 - MustAsync з перевіркою існування)
        _reviewRepository.ExistsAsync(7, 10, Arg.Any<CancellationToken>())
            .Returns(true);

        var validator = new CreateReviewCommandValidator(_reviewRepository);

        var command = new CreateReviewCommand(
            ServiceId: 7,
            ServiceName: "Кераміка",
            AppointmentId: 1,
            DiscussionId: null,
            Rating: 5,
            Title: "Чудовий результат",
            Text: "Дуже сподобалося обслуговування.",
            VerifiedVisit: true,
            AuthorClientId: 10,
            AuthorFullName: "Іван Петренко",
            AuthorCarInfo: "Audi A6");

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AuthorClientId)
            .WithErrorMessage("Клієнт вже залишив відгук для цієї послуги. Повторні відгуки заборонені.");
    }

    [Fact]
    public async Task CreateReviewCommandValidator_WhenRatingIsInvalid_FailsValidation()
    {
        // Arrange
        _reviewRepository.ExistsAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var validator = new CreateReviewCommandValidator(_reviewRepository);

        var command = new CreateReviewCommand(
            ServiceId: 1,
            ServiceName: "Мийка",
            AppointmentId: null,
            DiscussionId: null,
            Rating: 6, // Invalid
            Title: "Заголовок",
            Text: "Текст відгуку",
            VerifiedVisit: false,
            AuthorClientId: 5,
            AuthorFullName: "Клієнт",
            AuthorCarInfo: null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void UpdateReviewCommandValidator_WhenInvalidObjectId_FailsValidation()
    {
        // Arrange
        var validator = new UpdateReviewCommandValidator();
        var command = new UpdateReviewCommand("invalid-id", 1, 5, "Заголовок", "Текст відгуку");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void UpdateReviewCommandValidator_WhenExpectedVersionIsZero_FailsValidation()
    {
        // Arrange
        var validator = new UpdateReviewCommandValidator();
        var command = new UpdateReviewCommand("670900000000000000000301", 0, 5, "Заголовок", "Текст відгуку");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExpectedVersion);
    }

    [Fact]
    public void GetReviewsListQueryValidator_WhenPageIsZero_FailsValidation()
    {
        // Arrange
        var validator = new GetReviewsListQueryValidator();
        var query = new GetReviewsListQuery(ServiceId: null, Page: 0, PageSize: 20);

        // Act
        var result = validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }
}
