using KongPortal.Models.Kong;
using KongPortal.Models.ViewModels;
using KongPortal.Security;
using KongPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KongPortal.Controllers;

// ── Services Controller ───────────────────────────────────────────────────────
[Authorize(Policy = Policies.CanView)]
public class ServicesController : Controller
{
    private readonly KongAdminClient _kong;
    private readonly AuditLogService _audit;

    public ServicesController(KongAdminClient kong, AuditLogService audit)
    {
        _kong  = kong;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var services = await _kong.GetServices();
        return View(services);
    }

    public async Task<IActionResult> Detail(string id)
    {
        var service = await _kong.GetService(id);
        if (service == null) return NotFound();

        var vm = new ServiceViewModel
        {
            Service = service,
            Routes  = await _kong.GetRoutes(id),
            Plugins = await _kong.GetPlugins(serviceId: id)
        };
        return View(vm);
    }

    [Authorize(Policy = Policies.CanAdmin)]
    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanAdmin)]
    public async Task<IActionResult> Create(CreateServiceRequest req)
    {
        var service = await _kong.CreateService(req);
        await _audit.Log("CreateService", service.Name, "Service",
            User.Identity!.Name!, GetIp());
        return RedirectToAction(nameof(Detail), new { id = service.Id });
    }

    [Authorize(Policy = Policies.CanAdmin)]
    public async Task<IActionResult> Edit(string id)
    {
        var service = await _kong.GetService(id);
        if (service == null) return NotFound();
        return View(service);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanAdmin)]
    public async Task<IActionResult> Edit(string id, CreateServiceRequest req)
    {
        await _kong.UpdateService(id, req);
        await _audit.Log("UpdateService", id, "Service",
            User.Identity!.Name!, GetIp());
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanAdmin)]
    public async Task<IActionResult> Delete(string id)
    {
        await _kong.DeleteService(id);
        await _audit.Log("DeleteService", id, "Service",
            User.Identity!.Name!, GetIp());
        return RedirectToAction(nameof(Index));
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

// ── Routes Controller ─────────────────────────────────────────────────────────
[Authorize(Policy = Policies.CanView)]
public class RoutesController : Controller
{
    private readonly KongAdminClient _kong;
    private readonly AuditLogService _audit;

    public RoutesController(KongAdminClient kong, AuditLogService audit)
    {
        _kong  = kong;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? serviceId)
    {
        var routes = await _kong.GetRoutes(serviceId);
        ViewBag.ServiceId = serviceId;
        return View(routes);
    }

    [Authorize(Policy = Policies.CanAdmin)]
    public IActionResult Create(string serviceId)
    {
        ViewBag.ServiceId = serviceId;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanAdmin)]
    public async Task<IActionResult> Create(string serviceId, CreateRouteRequest req)
    {
        var route = await _kong.CreateRoute(serviceId, req);
        await _audit.Log("CreateRoute", route.Id, "Route",
            User.Identity!.Name!, GetIp());
        return RedirectToAction("Detail", "Services", new { id = serviceId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanAdmin)]
    public async Task<IActionResult> Delete(string routeId, string? serviceId)
    {
        await _kong.DeleteRoute(routeId);
        await _audit.Log("DeleteRoute", routeId, "Route",
            User.Identity!.Name!, GetIp());
        return serviceId != null
            ? RedirectToAction("Detail", "Services", new { id = serviceId })
            : RedirectToAction(nameof(Index));
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

// ── Plugins Controller ────────────────────────────────────────────────────────
[Authorize(Policy = Policies.CanView)]
public class PluginsController : Controller
{
    private readonly KongAdminClient _kong;
    private readonly AuditLogService _audit;

    public PluginsController(KongAdminClient kong, AuditLogService audit)
    {
        _kong  = kong;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? serviceId, string? consumerId)
    {
        var plugins = await _kong.GetPlugins(serviceId, consumerId);
        ViewBag.ServiceId  = serviceId;
        ViewBag.ConsumerId = consumerId;
        return View(plugins);
    }

    [Authorize(Policy = Policies.CanAdmin)]
    public IActionResult Create(string? serviceId, string? consumerId)
    {
        ViewBag.ServiceId  = serviceId;
        ViewBag.ConsumerId = consumerId;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanAdmin)]
    public async Task<IActionResult> Create(CreatePluginRequest req)
    {
        var plugin = await _kong.CreatePlugin(req);
        await _audit.Log("CreatePlugin", plugin.Name, "Plugin",
            User.Identity!.Name!, GetIp());
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanOperate)]
    public async Task<IActionResult> Toggle(string pluginId, bool enabled)
    {
        await _kong.TogglePlugin(pluginId, enabled);
        await _audit.Log(enabled ? "EnablePlugin" : "DisablePlugin", pluginId, "Plugin",
            User.Identity!.Name!, GetIp());
        return Ok();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanAdmin)]
    public async Task<IActionResult> Delete(string pluginId)
    {
        await _kong.DeletePlugin(pluginId);
        await _audit.Log("DeletePlugin", pluginId, "Plugin",
            User.Identity!.Name!, GetIp());
        return RedirectToAction(nameof(Index));
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
