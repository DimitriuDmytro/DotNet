namespace Reviews.Tests.Mapping;

using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Reviews.Application.Reviews.Mapping;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ReviewMappingProfile>();
        }, NullLoggerFactory.Instance);

        configuration.AssertConfigurationIsValid();
        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void MapperConfiguration_IsValid()
    {
        // AssertConfigurationIsValid() викликається в конструкторі
        Assert.NotNull(_mapper);
    }

    [Fact]
    public void Map_Review_To_ReviewDetailDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var author = new Author(1, "Олександр Коваленко", "BMW X5");
        var review = new Review(
            serviceId: 7,
            serviceName: "Кераміка 9H",
            rating: 5,
            title: "Супер блиск",
            text: "Дуже задоволений результатом.",
            author: author,
            appointmentId: 1,
            verifiedVisit: true,
            discussionId: ObjectId.Parse("670900000000000000000101"));

        review.AddPhotos(new[] { "before.jpg" }, new[] { "after.jpg" });
        review.AddProsAndCons(new[] { "Плюс 1" }, new[] { "Мінус 1" });
        review.AddComment(new ReviewComment("Адмін", "admin", "Дякуємо!", DateTime.UtcNow));

        // Act
        var dto = _mapper.Map<ReviewDetailDto>(review);

        // Assert
        Assert.Equal(review.Id.ToString(), dto.Id);
        Assert.Equal("670900000000000000000101", dto.DiscussionId);
        Assert.Equal(7, dto.ServiceId);
        Assert.Equal("Кераміка 9H", dto.ServiceName);
        Assert.Equal(5, dto.Rating);
        Assert.Equal("Супер блиск", dto.Title);
        Assert.Equal("Олександр Коваленко", dto.Author.FullName);
        Assert.Single(dto.PhotosBefore);
        Assert.Single(dto.PhotosAfter);
        Assert.Single(dto.Pros);
        Assert.Single(dto.Cons);
        Assert.Single(dto.Comments);
    }

    [Fact]
    public void Map_Review_To_ReviewListDto_MapsSummaryFieldsCorrectly()
    {
        // Arrange
        var author = new Author(1, "Олександр Коваленко", "BMW X5");
        var review = new Review(
            serviceId: 7,
            serviceName: "Кераміка 9H",
            rating: 5,
            title: "Супер блиск",
            text: "Дуже задоволений результатом.",
            author: author);

        review.AddComment(new ReviewComment("Адмін", "admin", "Дякуємо!", DateTime.UtcNow));

        // Act
        var dto = _mapper.Map<ReviewListDto>(review);

        // Assert
        Assert.Equal(review.Id.ToString(), dto.Id);
        Assert.Equal("Олександр Коваленко", dto.AuthorName);
        Assert.Equal(1, dto.CommentsCount);
        Assert.Equal(1, dto.Version);
    }
}
