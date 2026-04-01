using KongPortal.Data;
using KongPortal.Models.Domain;
using Microsoft.EntityFrameworkCore;

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
        // TODO: plug in your SMTP / Teams / Slack
        // Credential is delivered here — never stored in DB, never shown in UI

        _logger.LogInformation(
            "Credential notification → {Email} for {Consumer} [{AuthType}]",
            profile.ContactEmail, profile.KongConsumerUsername, authType);

        // Example email body (implement with SmtpClient or SendGrid)
        var body = BuildEmailBody(profile, authType, credential);

        // Uncomment and configure when ready:
        // await SendEmail(profile.ContactEmail, "Kong API Credential Rotation", body);
        // await SendSlack(profile.ContactSlack, body);

        await Task.CompletedTask;
    }

    private string BuildEmailBody(ConsumerProfile profile, string authType, object credential)
    {
        return $"""
            Hi {profile.ContactName},

            Your Kong API credentials for {profile.ServiceName} have been rotated.

            Auth Type: {authType}
            Credential: {System.Text.Json.JsonSerializer.Serialize(credential)}

            Please update your configuration immediately.
            Your old credentials will be revoked after the grace period.

            Kong DevOps Portal
            """;
    }
}
