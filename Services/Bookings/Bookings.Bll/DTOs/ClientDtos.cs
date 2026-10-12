namespace Bookings.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

public record ClientDto
{
    public long Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

public record CreateClientDto
{
    [Required(ErrorMessage = "Електронна пошта обов'язкова.")]
    [EmailAddress(ErrorMessage = "Некоректний формат електронної пошти.")]
    [StringLength(255, ErrorMessage = "Email не може перевищувати 255 символів.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Повне ім'я клієнта обов'язкове.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "ПІБ клієнта має містити від 2 до 200 символів.")]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Номер телефону обов'язковий.")]
    [StringLength(30, MinimumLength = 5, ErrorMessage = "Номер телефону має містити від 5 до 30 символів.")]
    public string Phone { get; init; } = string.Empty;

    [StringLength(100)]
    public string? CreatedBy { get; init; }
}

public record UpdateClientDto
{
    [Required(ErrorMessage = "Електронна пошта обов'язкова.")]
    [EmailAddress(ErrorMessage = "Некоректний формат електронної пошти.")]
    [StringLength(255, ErrorMessage = "Email не може перевищувати 255 символів.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Повне ім'я клієнта обов'язкове.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "ПІБ клієнта має містити від 2 до 200 символів.")]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Номер телефону обов'язковий.")]
    [StringLength(30, MinimumLength = 5, ErrorMessage = "Номер телефону має містити від 5 до 30 символів.")]
    public string Phone { get; init; } = string.Empty;

    [StringLength(100)]
    public string? UpdatedBy { get; init; }
}
