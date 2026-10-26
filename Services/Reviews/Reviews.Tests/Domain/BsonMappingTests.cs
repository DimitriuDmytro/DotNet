namespace Reviews.Tests.Domain;

using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;

public class BsonMappingTests
{
    public BsonMappingTests()
    {
        MongoMappings.Register();
    }

    [Fact]
    public void Can_Serialize_And_Deserialize_Review()
    {
        var author = new Author(1, "Тестовий Клієнт", "Audi A6");
        var review = new Review(
            serviceId: 5,
            serviceName: "Полірування фар",
            rating: 5,
            title: "Чудова робота",
            text: "Фари як новенькі, світять набагато краще.",
            author: author,
            appointmentId: 10,
            verifiedVisit: true);

        review.AddPhotos(new[] { "https://example.com/before.jpg" }, new[] { "https://example.com/after.jpg" });
        review.AddProsAndCons(new[] { "Швидко", "Якісно" }, new[] { "Немає" });
        review.AddComment(new ReviewComment("Адміністратор", "admin", "Дякуємо за відгук!", DateTime.UtcNow));

        var bson = review.ToBson();
        Assert.NotNull(bson);

        var deserialized = BsonSerializer.Deserialize<Review>(bson);
        Assert.NotNull(deserialized);
        Assert.Equal(review.Id, deserialized.Id);
        Assert.Equal(review.ServiceId, deserialized.ServiceId);
        Assert.Equal(review.ServiceName, deserialized.ServiceName);
        Assert.Equal(review.Rating, deserialized.Rating);
        Assert.Equal(review.Title, deserialized.Title);
        Assert.Equal(review.Text, deserialized.Text);
        Assert.Equal(review.Author.ClientId, deserialized.Author.ClientId);
        Assert.Equal(review.Author.FullName, deserialized.Author.FullName);
        Assert.Single(deserialized.PhotosBefore);
        Assert.Single(deserialized.PhotosAfter);
        Assert.Equal(2, deserialized.Pros.Count);
        Assert.Single(deserialized.Cons);
        Assert.Single(deserialized.Comments);
    }

    [Fact]
    public void Can_Serialize_And_Deserialize_Discussion()
    {
        var discussion = new Discussion(
            targetType: "service",
            targetId: 5,
            targetName: "Полірування фар",
            title: "Питання щодо полірування",
            tags: new[] { "detailing", "polishing" },
            isPinned: true);

        var bson = discussion.ToBson();
        Assert.NotNull(bson);

        var deserialized = BsonSerializer.Deserialize<Discussion>(bson);
        Assert.NotNull(deserialized);
        Assert.Equal(discussion.Id, deserialized.Id);
        Assert.Equal("service", deserialized.TargetType);
        Assert.Equal(5, deserialized.TargetId);
        Assert.True(deserialized.IsPinned);
        Assert.Equal(2, deserialized.Tags.Count);
    }
}
