namespace Bookings.Domain.Exceptions;

/// <summary>
/// Доменний виняток, що сигналізує про порушення бізнес-інваріанта або конфлікт стану (HTTP 409).
/// Виникає при: неприпустимому переході статусів, спробі переплати понад TotalAmount,
/// спробі видати неоплачене авто, конфлікті унікальності (Email тощо).
/// </summary>
public class BusinessConflictException : Exception
{
    public BusinessConflictException(string message) : base(message)
    {
    }

    public BusinessConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
