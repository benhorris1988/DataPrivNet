using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataProvisioning.Infrastructure.Data;

namespace DataProvisioning.WebUI.Controllers;

public class LoginController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly bool _testMode;

    public LoginController(ApplicationDbContext context, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _context = context;
        _testMode = configuration.GetValue<bool>("TestMode");
    }

    [HttpGet]
    public async Task<IActionResult> Index(string returnUrl = "/")
    {
        if (!_testMode)
        {
            return RedirectToAction("Index", "Catalog");
        }

        ViewData["ReturnUrl"] = returnUrl;
        
        // For convenience during local dev, list all available users
        var users = await _context.Users.ToListAsync();
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Authenticate(string email, string returnUrl = "/")
    {
        if (!_testMode)
        {
            return RedirectToAction("Index", "Catalog");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found in the local database.";
            return RedirectToAction("Index", new { returnUrl });
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.GivenName, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, 
            principal, 
            new AuthenticationProperties { IsPersistent = true });

        if (Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        else
        {
            return RedirectToAction("Index", "Catalog");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (_testMode)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        return RedirectToAction("Index", "Login");
    }
}
