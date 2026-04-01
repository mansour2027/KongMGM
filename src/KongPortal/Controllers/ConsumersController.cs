using KongPortal.Models.Domain;
using KongPortal.Models.ViewModels;
using KongPortal.Security;
using KongPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KongPortal.Controllers;

[Authorize(Policy = Policies.CanView)]
public class ConsumersController : Controller
{
    private readonly KongAdminClient _kong;
    private readonly ConsumerProfileRepository _profiles;
    private readonly RotationService _rotation;
    private readonly AuditLogService _audit;

    public ConsumersController(
        KongAdminClient kong,
        ConsumerProfileRepository profiles,
        RotationService rotation,
        AuditLogService audit)
    {
        _kong     = kong;
        _profiles = profiles;
        _rotation = rotation;
        _audit    = audit;
    }

    // GET /consumers
    public async Task<IActionResult> Index(
        string? team, string? authType, string? environment, bool overdueOnly = false)
    {
        var kongConsumers = await _kong.GetConsumers();
        var allProfiles   = await _profiles.GetAll();

        var merged = kongConsumers.Select(k => new ConsumerViewModel
        {
            Kong    = k,
            Profile = allProfiles.FirstOrDefault(p => p.KongConsumerUsername == k.Username)
        }).ToList();

        if (!string.IsNullOrEmpty(team))
            merged = merged.Where(m => m.Profile?.TeamName == team).ToList();
        if (!string.IsNullOrEmpty(authType))
            merged = merged.Where(m => m.Profile?.AuthType == authType).ToList();
        if (!string.IsNullOrEmpty(environment))
            merged = merged.Where(m => m.Profile?.Environment == environment).ToList();
        if (overdueOnly)
            merged = merged.Where(m => m.IsOverdue || m.NeverRotated).ToList();

        var vm = new ConsumerListViewModel
        {
            Consumers   = merged,
            FilterTeam  = team,
            FilterAuthType = authType,
            FilterEnvironment = environment,
            OverdueOnly = overdueOnly
        };

        return View(vm);
    }

    // GET /consumers/{username}
    public async Task<IActionResult> Detail(string username)
    {
        var consumer = await _kong.GetConsumer(username);
        if (consumer == null) return NotFound();

        var vm = new ConsumerViewModel
        {
            Kong             = consumer,
            Profile          = await _profiles.Get(username),
            KeyAuths         = await _kong.GetKeyAuths(username),
            JwtCredentials   = await _kong.GetJwts(username),
            BasicAuths       = await _kong.GetBasicAuths(username),
            HmacAuths        = await _kong.GetHmacAuths(username),
            OAuth2s          = await _kong.GetOAuth2s(username),
            AclGroups        = await _kong.GetAclGroups(username),
            Plugins          = await _kong.GetPlugins(consumerId: consumer.Id),
            RotationHistory  = await _profiles.GetRotationHistory(username)
        };

        return View(vm);
    }

    // GET /consumers/create
    [Authorize(Policy = Policies.CanOperate)]
    public IActionResult Create() => View();

    // POST /consumers/create
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanOperate)]
    public async Task<IActionResult> Create(string username, ConsumerProfile profile)
    {
        var consumer = await _kong.CreateConsumer(username);
        await _profiles.Upsert(username, profile);
        await _audit.Log("CreateConsumer", username, "Consumer",
            User.Identity!.Name!, GetIp());
        return RedirectToAction(nameof(Detail), new { username });
    }

    // POST /consumers/{username}/profile
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanOperate)]
    public async Task<IActionResult> SaveProfile(string username, ConsumerProfile profile)
    {
        await _profiles.Upsert(username, profile);
        await _audit.Log("UpdateProfile", username, "Consumer",
            User.Identity!.Name!, GetIp());
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Detail), new { username });
    }

    // POST /consumers/{username}/rotate
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanOperate)]
    public async Task<IActionResult> Rotate(string username, string authType)
    {
        var (success, message, _) = await _rotation.RotateConsumer(
            username, authType, User.Identity!.Name!);

        await _audit.Log("Rotate", username, "Consumer",
            User.Identity!.Name!, GetIp(), success, success ? null : message);

        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Detail), new { username });
    }

    // POST /consumers/bulk-rotate
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanOperate)]
    public async Task<IActionResult> BulkRotate([FromBody] List<string> usernames)
    {
        var result = await _rotation.RotateMany(usernames, User.Identity!.Name!);
        await _audit.Log("BulkRotate", string.Join(",", usernames), "Consumer",
            User.Identity!.Name!, GetIp());
        return Ok(new { succeeded = result.Succeeded.Count, failed = result.Failed.Count });
    }

    // POST /consumers/{username}/delete
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanAdmin)]
    public async Task<IActionResult> Delete(string username)
    {
        await _kong.DeleteConsumer(username);
        await _audit.Log("DeleteConsumer", username, "Consumer",
            User.Identity!.Name!, GetIp());
        return RedirectToAction(nameof(Index));
    }

    // POST /consumers/{username}/add-key
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanOperate)]
    public async Task<IActionResult> AddKeyAuth(string username)
    {
        await _kong.AddKeyAuth(username);
        await _audit.Log("AddKeyAuth", username, "Credential",
            User.Identity!.Name!, GetIp());
        return RedirectToAction(nameof(Detail), new { username });
    }

    // POST /consumers/{username}/delete-credential
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanOperate)]
    public async Task<IActionResult> DeleteCredential(
        string username, string authType, string credId)
    {
        switch (authType)
        {
            case "key-auth":   await _kong.DeleteKeyAuth(username, credId);   break;
            case "jwt":        await _kong.DeleteJwt(username, credId);        break;
            case "basic-auth": await _kong.DeleteBasicAuth(username, credId); break;
            case "hmac-auth":  await _kong.DeleteHmacAuth(username, credId);  break;
            case "oauth2":     await _kong.DeleteOAuth2(username, credId);    break;
        }
        await _audit.Log("DeleteCredential", $"{username}/{authType}/{credId}",
            "Credential", User.Identity!.Name!, GetIp());
        return RedirectToAction(nameof(Detail), new { username });
    }

    // POST /consumers/{username}/acl
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.CanOperate)]
    public async Task<IActionResult> AddToGroup(string username, string group)
    {
        await _kong.AddToGroup(username, group);
        await _audit.Log("AddToGroup", $"{username}/{group}", "ACL",
            User.Identity!.Name!, GetIp());
        return RedirectToAction(nameof(Detail), new { username });
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
