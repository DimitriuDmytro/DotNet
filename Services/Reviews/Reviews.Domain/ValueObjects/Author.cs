namespace Reviews.Domain.ValueObjects;

using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

/// <summary>
/// Незмінний Value Object автора відгуку (Критерій 3).
/// Зберігається як вкладений піддокумент author у MongoDB.
/// </summary>
public sealed class Author : ValueObject
{
    public long ClientId { get; }
    public string FullName { get; }
    public string? CarInfo { get; }

    public Author(long clientId, string fullName, string? carInfo = null)
    {
        if (clientId <= 0)
        {
            throw new DomainException("Ідентифікатор клієнта (ClientId) повинен бути додатним числом.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Ім'я автора (FullName) є обов'язковим.");
        }

        var trimmedName = fullName.Trim();
        if (trimmedName.Length > 100)
        {
            throw new DomainException("Ім'я автора не може перевищувати 100 символів.");
        }

        ClientId = clientId;
        FullName = trimmedName;
        CarInfo = string.IsNullOrWhiteSpace(carInfo) ? null : carInfo.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ClientId;
        yield return FullName;
        yield return CarInfo;
    }
}
