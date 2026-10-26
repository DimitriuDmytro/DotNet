namespace Reviews.Application.Common.Interfaces;

using MediatR;

/// <summary>
/// Маркерний інтерфейс для команд CQRS без результату (Критерій 5).
/// </summary>
public interface ICommand : IRequest
{
}

/// <summary>
/// Маркерний інтерфейс для команд CQRS з поверненням результату (Критерій 5).
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Маркерний інтерфейс для запитів CQRS (Критерій 6).
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
