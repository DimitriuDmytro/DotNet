namespace Catalog.Bll.DTOs;

using System.ComponentModel.DataAnnotations;

public record ServiceImageDto
{
    public long Id { get; init; }
    public long ServiceId { get; init; }
    public string Url { get; init; } = string.Empty;
    public bool IsMain { get; init; }
}

public record AddServiceImageDto
{
    [Required(ErrorMessage = "URL-адреса зображення обов'язкова.")]
    [Url(ErrorMessage = "Некоректний формат URL-адреси.")]
    [StringLength(500, ErrorMessage = "URL не може перевищувати 500 символів.")]
    public string Url { get; init; } = string.Empty;

    public bool IsMain { get; init; } = false;
}
