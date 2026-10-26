namespace Reviews.Tests.Application;

using MongoDB.Bson;
using NSubstitute;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Application.Reviews.Commands.DeleteReview;
using Reviews.Application.Reviews.Commands.UpdateReview;

public class HandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>();

    [Fact]
    public async Task CreateReviewCommandHandler_ValidCommand_AddsReviewAndReturnsStringId()
    {
        // Arrange
        var handler = new CreateReviewCommandHandler(_reviewRepository);
        var command = new CreateReviewCommand(
            ServiceId: 7,
            ServiceName: "Кераміка 9H",
            AppointmentId: 1,
            DiscussionId: null,
            Rating: 5,
            Title: "Чудовий блиск",
            Text: "Дійсно якісна послуга.",
            VerifiedVisit: true,
            AuthorClientId: 1,
            AuthorFullName: "Олександр Коваленко",
            AuthorCarInfo: "BMW X5");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(ObjectId.TryParse(result, out _));
        await _reviewRepository.Received(1).AddAsync(Arg.Any<Review>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateReviewCommandHandler_WhenFound_UpdatesDetailsAndSaves()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();
        var existingReview = new Review(
            serviceId: 1,
            serviceName: "Мийка",
            rating: 4,
            title: "Старий заголовок",
            text: "Старий довгий текст відгуку",
            author: new Author(1, "Автор", null));

        _reviewRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(existingReview);

        var handler = new UpdateReviewCommandHandler(_reviewRepository);
        var command = new UpdateReviewCommand(
            Id: id.ToString(),
            ExpectedVersion: 1,
            Rating: 5,
            Title: "Новий класний заголовок",
            Text: "Оновлений якісний текст відгуку");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(5, existingReview.Rating);
        Assert.Equal("Новий класний заголовок", existingReview.Title);
        await _reviewRepository.Received(1).UpdateAsync(existingReview, 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateReviewCommandHandler_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();
        _reviewRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((Review?)null);

        var handler = new UpdateReviewCommandHandler(_reviewRepository);
        var command = new UpdateReviewCommand(id.ToString(), 1, 5, "Заголовок", "Текст відгуку");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteReviewCommandHandler_WhenFound_DeletesReview()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();
        var existingReview = new Review(
            serviceId: 1,
            serviceName: "Мийка",
            rating: 5,
            title: "Заголовок",
            text: "Текст відгуку",
            author: new Author(1, "Автор", null));

        _reviewRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(existingReview);

        var handler = new DeleteReviewCommandHandler(_reviewRepository);
        var command = new DeleteReviewCommand(id.ToString());

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await _reviewRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteReviewCommandHandler_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();
        _reviewRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((Review?)null);

        var handler = new DeleteReviewCommandHandler(_reviewRepository);
        var command = new DeleteReviewCommand(id.ToString());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }
}
