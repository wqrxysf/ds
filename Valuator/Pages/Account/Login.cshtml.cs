using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using Valuator.Services;

namespace Valuator.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(IUserService userService, ILogger<LoginModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string returnUrl = "/")
    {
        string login = Request.Form["login"];
        string password = Request.Form["password"];

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("", "Логин и пароль обязательны");
            return Page();
        }

        var user = await _userService.AuthenticateAsync(login, password);

        if (user == null)
        {
            ModelState.AddModelError("", "Неверный логин или пароль");
            return Page();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Login)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties();

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("Пользователь {Login} вошел в систему", user.Login);
        return LocalRedirect(returnUrl);
    }
}