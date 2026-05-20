using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Valuator.Services;

namespace Valuator.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(IUserService userService, ILogger<RegisterModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        string login = Request.Form["login"];
        string password = Request.Form["password"];

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("", "Логин и пароль обязательны");
            return Page();
        }

        if (password.Length < 4)
        {
            ModelState.AddModelError("", "Пароль должен быть не менее 4 символов");
            return Page();
        }

        var success = await _userService.RegisterAsync(login, password);

        if (!success)
        {
            ModelState.AddModelError("", "Пользователь с таким логином уже существует");
            return Page();
        }

        var user = await _userService.AuthenticateAsync(login, password);

        if (user != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Login)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            _logger.LogInformation("Пользователь {Login} зарегистрировался", user.Login);
            return RedirectToPage("/Index");
        }

        return RedirectToPage("/Account/Login");
    }
}