using KongPortal.Data;
using KongPortal.Models.ViewModels;
using KongPortal.Security;
using KongPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KongPortal.Controllers;

// ── Rotation Controller ───────────────────────────────────────────────────────
[Authorize(Policy = Policies.CanOperate)]
public class RotationController : Controller
{
    private readonly RotationService _rotation;
    private readonly ConsumerProfileRepository _profiles;
    private readonly AuditLogService _audit;
    private readonly AppDbContext _db;

    public RotationController(
        RotationService rotation,
        ConsumerProfileRepository profiles,
        AuditLogService audit,
        AppDbContext db)
    {
        _rotation = rotation;
        _profiles = profiles;
        _audit    = audit;
        _db       = db;
    }

    // GET /rotation — pending + overdue board
    public async Task<IActionResult> Index()
    {
        var pending  = await _profiles.GetPendingRotations();
        var toDelete = await _profiles.GetConfirmedPendingDeletion();

        var vm = new RotationStatusViewModel
        {
            Records = pending.Concat(toDelete).DistinctBy(r => r.Id).ToList(),
            Date    = DateTime.UtcNow
        };
        return View(vm);
    }

    // POST /rotation/confirm/{id} — mark new key confirmed by consumer
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        await _rotation.ConfirmRotation(id);
        await _audit.Log("ConfirmRotation", id.ToString(), "Rotation",
            User.Identity!.Name!, GetIp());
        TempData["Success"] = "Rotation confirmed. Old credentials ready for deletion.";
        return RedirectToAction(nameof(Index));
    }

    // POST /rotation/delete-old — delete old credentials for confirmed records
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOld([FromBody] List<int> recordIds)
    {
        var (deleted, skipped) = await _rotation.DeleteOldCredentials(
            recordIds, User.Identity!.Name!);

        await _audit.Log("DeleteOldCredentials",
            string.Join(",", recordIds), "Rotation",
            User.Identity!.Name!, GetIp());

        return Ok(new { deleted, skipped });
    }

    // GET /rotation/history — full rotation history
    public async Task<IActionResult> History(string? consumer, DateTime? from, DateTime? to)
    {
        var query = _db.RotationRecords.AsQueryable();

        if (!string.IsNullOrEmpty(consumer))
            query = query.Where(r => r.KongConsumerUsername.Contains(consumer));
        if (from.HasValue)
            query = query.Where(r => r.RotatedAt >= from);
        if (to.HasValue)
            query = query.Where(r => r.RotatedAt <= to);

        var records = await query
            .OrderByDescending(r => r.RotatedAt)
            .Take(200)
            .ToListAsync();

        return View(records);
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

// ── Audit Controller ──────────────────────────────────────────────────────────
[Authorize(Policy = Policies.CanView)]
public class AuditController : Controller
{
    private readonly AuditLogService _audit;

    public AuditController(AuditLogService audit) => _audit = audit;

    public async Task<IActionResult> Index(
        string? action, string? resource, string? performedBy,
        DateTime? from, DateTime? to)
    {
        var logs = await _audit.Search(action, resource, performedBy, from, to);
        return View(logs);
    }
}
