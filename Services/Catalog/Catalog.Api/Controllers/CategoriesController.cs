using Catalog.Bll.DTOs;
using Catalog.Bll.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IServiceCategoryService _categoryService;
    private readonly IValidator<CreateCategoryDto> _createValidator;

    public CategoriesController(
        IServiceCategoryService categoryService,
        IValidator<CreateCategoryDto> createValidator)
    {
        _categoryService = categoryService;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Отримати всі активні категорії послуг.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceCategoryDto>>> GetAll(CancellationToken ct)
    {
        var result = await _categoryService.GetAllAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Отримати категорію за ідентифікатором.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ServiceCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceCategoryDto>> GetById([FromRoute] long id, CancellationToken ct)
    {
        var result = await _categoryService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Створити нову категорію.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ServiceCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceCategoryDto>> Create([FromBody] CreateCategoryDto dto, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var created = await _categoryService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Оновити назву та опис категорії.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ServiceCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceCategoryDto>> Update(
        [FromRoute] long id,
        [FromBody] UpdateCategoryDto dto,
        CancellationToken ct)
    {
        var createDto = new CreateCategoryDto { Name = dto.Name, Description = dto.Description };
        var validation = await _createValidator.ValidateAsync(createDto, ct);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var updated = await _categoryService.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }
}
