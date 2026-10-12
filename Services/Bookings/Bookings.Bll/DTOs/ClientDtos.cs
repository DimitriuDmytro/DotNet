namespace Bookings.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

public class ClientDto
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class CreateClientDto
{
    [Required(ErrorMessage = "Електронна пошта обов'язкова.")]
    [EmailAddress(ErrorMessage = "Некоректний формат електронної пошти.")]
    [StringLength(255, ErrorMessage = "Email не може перевищувати 255 символів.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Повне ім'я клієнта обов'язкове.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "ПІБ клієнта має містити від 2 до 200 символів.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Номер телефону обов'язковий.")]
    [StringLength(30, MinimumLength = 5, ErrorMessage = "Номер телефону має містити від 5 до 30 символів.")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CreatedBy { get; set; }
}

public class UpdateClientDto
{
    [Required(ErrorMessage = "Електронна пошта обов'язкова.")]
    [EmailAddress(ErrorMessage = "Некоректний формат електронної пошти.")]
    [StringLength(255, ErrorMessage = "Email не може перевищувати 255 символів.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Повне ім'я клієнта обов'язкове.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "ПІБ клієнта має містити від 2 до 200 символів.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Номер телефону обов'язковий.")]
    [StringLength(30, MinimumLength = 5, ErrorMessage = "Номер телефону має містити від 5 до 30 символів.")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100)]
    public string? UpdatedBy { get; set; }
}
