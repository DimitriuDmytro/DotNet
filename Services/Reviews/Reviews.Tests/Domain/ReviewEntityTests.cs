namespace Reviews.Tests.Domain;

using MongoDB.Bson;

public class ReviewEntityTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesReviewSuccessfully()
    {
        // Arrange
        var author = new Author(1, "Олександр Коваленко", "BMW X5");

        // Act
        var review = new Review(
            serviceId: 7,
            serviceName: "Керамічне покриття 9H",
            rating: 5,
            title: "Чудовий результат!",
            text: "Дуже задоволений захисним покриттям кузова.",
            author: author);

        // Assert
        Assert.NotEqual(ObjectId.Empty, review.Id);
        Assert.Equal(7, review.ServiceId);
        Assert.Equal("Керамічне покриття 9H", review.ServiceName);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Чудовий результат!", review.Title);
        Assert.Equal("Дуже задоволений захисним покриттям кузова.", review.Text);
        Assert.Equal(1, review.Version);
        Assert.Null(review.UpdatedAt);
        Assert.Empty(review.Comments);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void Constructor_WithInvalidRating_ThrowsDomainExceptionWithoutDb(int invalidRating)
    {
        // Arrange
        var author = new Author(1, "Тестовий Клієнт", null);

        // Act & Assert (Критерій 2)
        var exception = Assert.Throws<DomainException>(() => new Review(
            serviceId: 1,
            serviceName: "Мийка",
            rating: invalidRating,
            title: "Нормально",
            text: "Звичайна мийка авто.",
            author: author));

        Assert.Contains("від 1 до 5", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")] // < 3 chars
    public void Constructor_WithInvalidTitle_ThrowsDomainException(string invalidTitle)
    {
        // Arrange
        var author = new Author(1, "Тестовий Клієнт", null);

        // Act & Assert
        Assert.Throws<DomainException>(() => new Review(
            serviceId: 1,
            serviceName: "Мийка",
            rating: 5,
            title: invalidTitle,
            text: "Валідний довгий текст відгуку.",
            author: author));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abcd")] // < 5 chars
    public void Constructor_WithInvalidText_ThrowsDomainException(string invalidText)
    {
        // Arrange
        var author = new Author(1, "Тестовий Клієнт", null);

        // Act & Assert
        Assert.Throws<DomainException>(() => new Review(
            serviceId: 1,
            serviceName: "Мийка",
            rating: 5,
            title: "Валідний заголовок",
            text: invalidText,
            author: author));
    }

    [Fact]
    public void Constructor_WithNullAuthor_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new Review(
            serviceId: 1,
            serviceName: "Мийка",
            rating: 5,
            title: "Заголовок",
            text: "Текст відгуку",
            author: null!));
    }

    [Fact]
    public void AddComment_UpToThreeComments_SucceedsAndUpdatesTouch()
    {
        // Arrange
        var review = new Review(
            serviceId: 1,
            serviceName: "Мийка",
            rating: 5,
            title: "Гарна мийка",
            text: "Все зроблено чисто та вчасно.",
            author: new Author(1, "Іван", null));

        // Act
        review.AddComment(new ReviewComment("Адмін", "admin", "Дякуємо за відгук!", DateTime.UtcNow));
        review.AddComment(new ReviewComment("Майстер", "master", "Було приємно працювати!", DateTime.UtcNow));
        review.AddComment(new ReviewComment("Клієнт", "client", "Ще заїду!", DateTime.UtcNow));

        // Assert
        Assert.Equal(3, review.Comments.Count);
        Assert.NotNull(review.UpdatedAt);
    }

    [Fact]
    public void AddComment_ExceedingThreeComments_ThrowsDomainException()
    {
        // Arrange
        var review = new Review(
            serviceId: 1,
            serviceName: "Мийка",
            rating: 5,
            title: "Гарна мийка",
            text: "Все зроблено чисто та вчасно.",
            author: new Author(1, "Іван", null));

        review.AddComment(new ReviewComment("Адмін", "admin", "Відповідь 1", DateTime.UtcNow));
        review.AddComment(new ReviewComment("Майстер", "master", "Відповідь 2", DateTime.UtcNow));
        review.AddComment(new ReviewComment("Клієнт", "client", "Відповідь 3", DateTime.UtcNow));

        // Act & Assert (Критерій 2)
        var ex = Assert.Throws<DomainException>(() =>
            review.AddComment(new ReviewComment("Адмін", "admin", "Відповідь 4", DateTime.UtcNow)));

        Assert.Contains("щонайбільше 3", ex.Message);
    }

    [Fact]
    public void BumpVersion_IncrementsVersionAndTouchesUpdatedAt()
    {
        // Arrange
        var review = new Review(
            serviceId: 1,
            serviceName: "Мийка",
            rating: 5,
            title: "Гарна мийка",
            text: "Все зроблено чисто та вчасно.",
            author: new Author(1, "Іван", null));

        var initialVersion = review.Version;
        Assert.Equal(1, initialVersion);

        // Act
        review.BumpVersion();

        // Assert
        Assert.Equal(2, review.Version);
        Assert.NotNull(review.UpdatedAt);
    }
}
