namespace Reviews.Tests.Application;

using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using Reviews.Application.Common.Behaviors;

public class ValidationBehaviorTests
{
    public record TestRequest(string Name) : IRequest<string>;

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsValidationExceptionAndDoesNotCallNext()
    {
        // Arrange (Критерій 8)
        var validator = Substitute.For<IValidator<TestRequest>>();
        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("Name", "Name is required")
        };
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validator });

        var nextCalled = false;
        RequestHandlerDelegate<string> next = (cancellationToken) =>
        {
            nextCalled = true;
            return Task.FromResult("OK");
        };

        var request = new TestRequest(string.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(request, next, CancellationToken.None));
        Assert.False(nextCalled, "Метод next() не повинен викликатися, якщо валідація зазнала невдачі!");
    }

    [Fact]
    public async Task Handle_WhenValidationSucceeds_CallsNextAndReturnsResult()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validator });

        var nextCalled = false;
        RequestHandlerDelegate<string> next = (cancellationToken) =>
        {
            nextCalled = true;
            return Task.FromResult("OK");
        };

        var request = new TestRequest("Valid Name");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("OK", result);
    }
}
