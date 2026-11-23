using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth.Api.Data;
using Services.Auth.Api.DTOs;
using Services.Auth.Api.Models;

namespace Services.Auth.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext db,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto(
                Id: user.Id,
                UserName: user.UserName,
                Email: user.Email,
                EmailConfirmed: user.EmailConfirmed,
                FirstName: user.FirstName,
                LastName: user.LastName,
                CreatedAt: user.CreatedAt,
                Roles: roles.ToList()
            ));
        }

        return Ok(userDtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        if (!IsOwnerOrAdmin(id))
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserDto(
            Id: user.Id,
            UserName: user.UserName,
            Email: user.Email,
            EmailConfirmed: user.EmailConfirmed,
            FirstName: user.FirstName,
            LastName: user.LastName,
            CreatedAt: user.CreatedAt,
            Roles: roles.ToList()
        ));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        if (!IsOwnerOrAdmin(id))
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("User {UserId} updated profile", user.Id);
        return Ok(new { message = "User profile updated successfully." });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("Admin deleted user {UserId}", id);
        return Ok(new { message = "User deleted successfully." });
    }

    [HttpGet("{id}/refresh-tokens")]
    public async Task<IActionResult> GetUserRefreshTokens(string id)
    {
        if (!IsOwnerOrAdmin(id))
        {
            return Forbid();
        }

        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new RefreshTokenDto(
                t.Id,
                t.TokenHash,
                t.CreatedAt,
                t.ExpiresAt,
                t.IsActive,
                t.IsRevoked,
                t.CreatedByIp
            ))
            .ToListAsync();

        return Ok(tokens);
    }

    [HttpGet("{id}/roles")]
    public async Task<IActionResult> GetUserRoles(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(roles);
    }

    [HttpPost("{id}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddUserRole(string id, [FromBody] AddRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            return BadRequest(new { message = $"Role '{request.Role}' does not exist." });
        }

        var result = await _userManager.AddToRoleAsync(user, request.Role);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("Admin assigned role {Role} to user {UserId}", request.Role, id);
        return Ok(new { message = $"Role '{request.Role}' added successfully." });
    }

    [HttpDelete("{id}/roles/{role}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveUserRole(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("Admin removed role {Role} from user {UserId}", role, id);
        return Ok(new { message = $"Role '{role}' removed successfully." });
    }

    private bool IsOwnerOrAdmin(string targetUserId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return currentUserId == targetUserId || User.IsInRole("Admin");
    }
}
