using KongPortal.Data;
using KongPortal.Models.ViewModels;
using KongPortal.Security;
using KongPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KongPortal.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly KongAdminClient _kong;
    private readonly AppDbContext _db;
    private readonly AuditLogService _audit;

    public HomeController(KongAdminClient kong, AppDbContext db, AuditLogService audit)
    {
        _kong  = kong;
        _db    = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var consumers = await _kong.GetConsumers();
        var services  = await _kong.GetServices();
        var routes    = await _kong.GetRoutes();
        var plugins   = await _kong.GetPlugins();
        var profiles  = await _db.ConsumerProfiles.ToListAsync();

        var vm = new DashboardViewModel
        {
            TotalConsumers   = consumers.Count,
            TotalServices    = services.Count,
            TotalRoutes      = routes.Count,
            TotalPlugins     = plugins.Count,
            OverdueRotations = profiles.Count(p => p.IsOverdue),
            NeverRotated     = consumers.Count - profiles.Count(p => p.LastRotatedAt.HasValue),
            PendingDeletions = await _db.RotationRecords
                .CountAsync(r => r.Confirmed && !r.OldCredentialsDeleted),
            RecentActivity   = await _audit.GetRecent(10)
        };

        return View(vm);
    }
}

public class AccountController : Controller
{
    private readonly Microsoft.AspNetCore.Identity.SignInManager<AppUser> _signIn;

    public AccountController(Microsoft.AspNetCore.Identity.SignInManager<AppUser> signIn)
        => _signIn = signIn;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl)
    {
        var result = await _signIn.PasswordSignInAsync(username, password,
            isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/");

        ModelState.AddModelError("", "Invalid credentials.");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
