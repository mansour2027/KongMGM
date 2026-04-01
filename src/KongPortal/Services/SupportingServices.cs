using KongPortal.Data;
using KongPortal.Models.Domain;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace KongPortal.Services;

// ── Audit Log Service ─────────────────────────────────────────────────────────
public class AuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db) => _db = db;

    public async Task Log(string action, string resource, string resourceType,
        string performedBy, string ipAddress, bool success = true, string? reason = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action        = action,
            Resource      = resource,
            ResourceType  = resourceType,
            PerformedBy   = performedBy,
            IpAddress     = ipAddress,
            At            = DateTime.UtcNow,
            Success       = success,
            FailureReason = reason
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetRecent(int count = 50)
        => await _db.AuditLogs
            .OrderByDescending(a => a.At)
            .Take(count)
            .ToListAsync();

    public async Task<List<AuditLog>> Search(
        string? action = null,
        string? resource = null,
        string? performedBy = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        var query = _db.AuditLogs.AsQueryable();
        if (action != null)      query = query.Where(a => a.Action == action);
        if (resource != null)    query = query.Where(a => a.Resource.Contains(resource));
        if (performedBy != null) query = query.Where(a => a.PerformedBy == performedBy);
        if (from != null)        query = query.Where(a => a.At >= from);
        if (to != null)          query = query.Where(a => a.At <= to);
        return await query.OrderByDescending(a => a.At).Take(200).ToListAsync();
    }
}

// ── Notification Service ──────────────────────────────────────────────────────
public class NotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IConfiguration _config;

    public NotificationService(ILogger<NotificationService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task SendNewCredential(ConsumerProfile profile, string authType, object credential)
    {
        if (string.IsNullOrEmpty(profile.ContactEmail))
        {
            _logger.LogWarning("No contact email for {Consumer} — skipping notification", 
                profile.KongConsumerUsername);
            return;
        }

        var subject = $"[Kong Portal] Credential Rotated — {profile.ServiceName}";
        var body    = BuildEmailBody(profile, authType, credential);

        try
        {
            await SendEmail(profile.ContactEmail, subject, body);
            _logger.LogInformation(
                "Notification sent → {Email} for {Consumer}",
                profile.ContactEmail, profile.KongConsumerUsername);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to notify {Email} for {Consumer}",
                profile.ContactEmail, profile.KongConsumerUsername);
        }
    }

    private async Task SendEmail(string to, string subject, string body)
    {
        var smtpHost = _config["Notification:SmtpHost"];
        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogWarning("SMTP not configured — email not sent to {To}", to);
            return;
        }

        var port     = int.Parse(_config["Notification:SmtpPort"] ?? "587");
        var user     = _config["Notification:SmtpUser"];
        var pass     = _config["Notification:SmtpPassword"];
        var from     = _config["Notification:FromEmail"] ?? "kong-portal@internal.com";
        var useSsl   = bool.Parse(_config["Notification:UseSsl"] ?? "true");

        using var client = new SmtpClient(smtpHost, port)
        {
            EnableSsl   = useSsl,
            Credentials = !string.IsNullOrEmpty(user)
                ? new NetworkCredential(user, pass)
                : null
        };

        var message = new MailMessage(from, to, subject, body)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(message);
    }

    private string BuildEmailBody(ConsumerProfile profile, string authType, object credential)
    {
        var credJson = System.Text.Json.JsonSerializer.Serialize(
            credential,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        return $"""
            <html><body style="font-family:sans-serif;max-width:600px;margin:auto">
            <h2 style="color:#1a1f36">🔐 Kong API Credential Rotation</h2>
            <p>Hi <strong>{profile.ContactName}</strong>,</p>
            <p>Your Kong credentials have been rotated for:</p>
            <table style="border-collapse:collapse;width:100%">
              <tr><td style="padding:8px;background:#f8f9fa;font-weight:bold">Service</td>
                  <td style="padding:8px">{profile.ServiceName}</td></tr>
              <tr><td style="padding:8px;background:#f8f9fa;font-weight:bold">Consumer</td>
                  <td style="padding:8px">{profile.KongConsumerUsername}</td></tr>
              <tr><td style="padding:8px;background:#f8f9fa;font-weight:bold">Auth Type</td>
                  <td style="padding:8px">{authType}</td></tr>
              <tr><td style="padding:8px;background:#f8f9fa;font-weight:bold">Team</td>
                  <td style="padding:8px">{profile.TeamName}</td></tr>
            </table>
            <h3>New Credential</h3>
            <pre style="background:#f8f9fa;padding:16px;border-radius:8px">{credJson}</pre>
            <p style="color:#dc3545"><strong>⚠️ Action Required:</strong>
              Update your configuration immediately.
              Your old credentials will be revoked after the grace period.
            </p>
            <p style="color:#6c757d;font-size:0.85rem">
              Sent by Kong DevOps Portal — do not reply to this email.
            </p>
            </body></html>
            """;
    }
}
