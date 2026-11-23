using System.ComponentModel.DataAnnotations;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Platform.IdentityServer.Models;

namespace Platform.IdentityServer.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IIdentityServerInteractionService interaction,
        IEventService events,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _interaction = interaction;
        _events = events;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl)
    {
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        var vm = new LoginViewModel
        {
            ReturnUrl = returnUrl,
            Username = context?.LoginHint ?? string.Empty
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByNameAsync(model.Username)
                       ?? await _userManager.FindByEmailAsync(model.Username);

            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberLogin, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId));
                    _logger.LogInformation("User {UserName} logged in successfully", user.UserName);

                    if (context != null)
                    {
                        return Redirect(model.ReturnUrl ?? "~/");
                    }

                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    if (string.IsNullOrEmpty(model.ReturnUrl))
                    {
                        return Redirect("~/");
                    }

                    throw new ArgumentException("invalid return URL");
                }
            }

            await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials", clientId: context?.Client.ClientId));
            ModelState.AddModelError(string.Empty, "Invalid username or password");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Logout(string? logoutId)
    {
        var context = await _interaction.GetLogoutContextAsync(logoutId);

        if (User.Identity?.IsAuthenticated == true)
        {
            await _signInManager.SignOutAsync();
            await _events.RaiseAsync(new UserLogoutSuccessEvent(User.Identity.Name, User.Identity.Name));
            _logger.LogInformation("User {UserName} logged out", User.Identity.Name);
        }

        var vm = new LoggedOutViewModel
        {
            PostLogoutRedirectUri = context?.PostLogoutRedirectUri,
            ClientName = string.IsNullOrEmpty(context?.ClientName) ? context?.ClientId : context.ClientName,
            SignOutIFrameUrl = context?.SignOutIFrameUrl
        };

        if (context?.ShowSignoutPrompt == false || string.IsNullOrEmpty(vm.PostLogoutRedirectUri))
        {
            return View("LoggedOut", vm);
        }

        return Redirect(vm.PostLogoutRedirectUri);
    }
}

public class LoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberLogin { get; set; } = true;
    public string? ReturnUrl { get; set; }
}

public class LoggedOutViewModel
{
    public string? PostLogoutRedirectUri { get; set; }
    public string? ClientName { get; set; }
    public string? SignOutIFrameUrl { get; set; }
}
