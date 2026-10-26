namespace Reviews.Tests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void Author_WithSameValues_AreEqual()
    {
        // Arrange
        var author1 = new Author(10, "Олександр Коваленко", "BMW X5");
        var author2 = new Author(10, "Олександр Коваленко", "BMW X5");

        // Act & Assert (Критерій 3)
        Assert.Equal(author1, author2);
        Assert.True(author1 == author2);
        Assert.False(author1 != author2);
        Assert.Equal(author1.GetHashCode(), author2.GetHashCode());
    }

    [Fact]
    public void Author_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var author1 = new Author(10, "Олександр Коваленко", "BMW X5");
        var author2 = new Author(20, "Олександр Коваленко", "BMW X5");
        var author3 = new Author(10, "Марина Шевченко", "BMW X5");
        var author4 = new Author(10, "Олександр Коваленко", "Audi Q7");

        // Act & Assert (Критерій 3)
        Assert.NotEqual(author1, author2);
        Assert.True(author1 != author2);
        Assert.False(author1 == author2);

        Assert.NotEqual(author1, author3);
        Assert.NotEqual(author1, author4);
    }

    [Fact]
    public void ReviewComment_WithSameValues_AreEqual()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var comment1 = new ReviewComment("Андрій", "master", "Дякуємо!", now);
        var comment2 = new ReviewComment("Андрій", "master", "Дякуємо!", now);

        // Act & Assert (Критерій 3)
        Assert.Equal(comment1, comment2);
        Assert.True(comment1 == comment2);
        Assert.False(comment1 != comment2);
        Assert.Equal(comment1.GetHashCode(), comment2.GetHashCode());
    }

    [Fact]
    public void ReviewComment_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var comment1 = new ReviewComment("Андрій", "master", "Дякуємо!", now);
        var comment2 = new ReviewComment("Ірина", "admin", "Дякуємо!", now);

        // Act & Assert (Критерій 3)
        Assert.NotEqual(comment1, comment2);
        Assert.True(comment1 != comment2);
    }
}
