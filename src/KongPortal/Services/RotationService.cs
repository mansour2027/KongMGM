using KongPortal.Data;
using KongPortal.Models.Domain;
using KongPortal.Models.Kong;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace KongPortal.Services;

// ── Consumer Profile Repository ───────────────────────────────────────────────
public class ConsumerProfileRepository
{
    private readonly AppDbContext _db;

    public ConsumerProfileRepository(AppDbContext db) => _db = db;

    public async Task<List<ConsumerProfile>> GetAll()
        => await _db.ConsumerProfiles.ToListAsync();

    public async Task<ConsumerProfile?> Get(string username)
        => await _db.ConsumerProfiles
            .FirstOrDefaultAsync(p => p.KongConsumerUsername == username);

    public async Task Upsert(string username, ConsumerProfile profile)
    {
        var existing = await Get(username);
        if (existing == null)
        {
            profile.KongConsumerUsername = username;
            profile.CreatedAt = DateTime.UtcNow;
            _db.ConsumerProfiles.Add(profile);
        }
        else
        {
            existing.ServiceName           = profile.ServiceName;
            existing.TeamName              = profile.TeamName;
            existing.Environment           = profile.Environment;
            existing.AuthType              = profile.AuthType;
            existing.Notes                 = profile.Notes;
            existing.ContactName           = profile.ContactName;
            existing.ContactEmail          = profile.ContactEmail;
            existing.ContactPhone          = profile.ContactPhone;
            existing.ContactSlack          = profile.ContactSlack;
            existing.RotationIntervalDays  = profile.RotationIntervalDays;
            existing.UpdatedAt             = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<List<RotationRecord>> GetRotationHistory(string username)
        => await _db.RotationRecords
            .Where(r => r.KongConsumerUsername == username)
            .OrderByDescending(r => r.RotatedAt)
            .ToListAsync();

    public async Task<List<RotationRecord>> GetPendingRotations()
        => await _db.RotationRecords
            .Where(r => !r.Confirmed && !r.OldCredentialsDeleted)
            .OrderByDescending(r => r.RotatedAt)
            .ToListAsync();

    public async Task<List<RotationRecord>> GetConfirmedPendingDeletion()
        => await _db.RotationRecords
            .Where(r => r.Confirmed && !r.OldCredentialsDeleted)
            .ToListAsync();
}

// ── Rotation Service ──────────────────────────────────────────────────────────
public class RotationService
{
    private readonly KongAdminClient _kong;
    private readonly AppDbContext _db;
    private readonly NotificationService _notify;
    private readonly ILogger<RotationService> _logger;

    public RotationService(
        KongAdminClient kong,
        AppDbContext db,
        NotificationService notify,
        ILogger<RotationService> logger)
    {
        _kong   = kong;
        _db     = db;
        _notify = notify;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, object? NewCredential)>
        RotateConsumer(string username, string authType, string performedBy)
    {
        try
        {
            // 1. Get all current credential IDs
            var oldIds = await GetAllCredentialIds(username, authType);

            // 2. Add new credential
            var (newCred, displayData) = await AddNewCredential(username, authType);

            // 3. Get profile for notification
            var profile = await _db.ConsumerProfiles
                .FirstOrDefaultAsync(p => p.KongConsumerUsername == username);

            // 4. Save rotation record
            var record = new RotationRecord
            {
                KongConsumerUsername = username,
                AuthType             = authType,
                ServiceName          = profile?.ServiceName ?? "",
                ContactEmail         = profile?.ContactEmail ?? "",
                OldCredentialIds     = string.Join(",", oldIds),
                RotatedBy            = performedBy,
                RotatedAt            = DateTime.UtcNow,
                Confirmed            = false
            };
            _db.RotationRecords.Add(record);

            // 5. Update profile
            if (profile != null)
            {
                profile.LastRotatedAt  = DateTime.UtcNow;
                profile.LastRotatedBy  = performedBy;
            }

            await _db.SaveChangesAsync();

            // 6. Notify contact (fire and forget — don't block rotation)
            if (profile?.ContactEmail != null)
                _ = _notify.SendNewCredential(profile, authType, displayData);

            return (true, "Rotation successful. New credential sent to contact.", displayData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rotation failed for {Username}", username);
            return (false, ex.Message, null);
        }
    }

    public async Task<RotationBulkResult> RotateMany(List<string> usernames, string performedBy)
    {
        var result = new RotationBulkResult();

        foreach (var username in usernames)
        {
            var profile  = await _db.ConsumerProfiles
                .FirstOrDefaultAsync(p => p.KongConsumerUsername == username);
            var authType = profile?.AuthType ?? "key-auth";

            var (success, message, _) = await RotateConsumer(username, authType, performedBy);

            if (success) result.Succeeded.Add(username);
            else         result.Failed[username] = message;
        }

        return result;
    }

    public async Task<(int Deleted, int Skipped)> DeleteOldCredentials(
        List<int> recordIds, string performedBy)
    {
        int deleted = 0, skipped = 0;

        var records = await _db.RotationRecords
            .Where(r => recordIds.Contains(r.Id) && r.Confirmed && !r.OldCredentialsDeleted)
            .ToListAsync();

        foreach (var record in records)
        {
            try
            {
                foreach (var oldId in record.GetOldIds())
                    await DeleteCredential(record.KongConsumerUsername, record.AuthType, oldId);

                record.OldCredentialsDeleted = true;
                record.DeletedAt  = DateTime.UtcNow;
                record.DeletedBy  = performedBy;
                deleted++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete failed for {Username}", record.KongConsumerUsername);
                skipped++;
            }
        }

        await _db.SaveChangesAsync();
        return (deleted, skipped);
    }

    public async Task ConfirmRotation(int recordId)
    {
        var record = await _db.RotationRecords.FindAsync(recordId);
        if (record == null) return;
        record.Confirmed    = true;
        record.ConfirmedAt  = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ── Private helpers ───────────────────────────────────────────────────────
    private async Task<List<string>> GetAllCredentialIds(string consumer, string authType)
    {
        return authType switch
        {
            "key-auth"   => (await _kong.GetKeyAuths(consumer)).Select(k => k.Id).ToList(),
            "jwt"        => (await _kong.GetJwts(consumer)).Select(k => k.Id).ToList(),
            "basic-auth" => (await _kong.GetBasicAuths(consumer)).Select(k => k.Id).ToList(),
            "hmac-auth"  => (await _kong.GetHmacAuths(consumer)).Select(k => k.Id).ToList(),
            "oauth2"     => (await _kong.GetOAuth2s(consumer)).Select(k => k.Id).ToList(),
            _            => new List<string>()
        };
    }

    private async Task<(object Credential, object DisplayData)>
        AddNewCredential(string consumer, string authType)
    {
        switch (authType)
        {
            case "key-auth":
                var kc = await _kong.AddKeyAuth(consumer);
                return (kc, new { type = "key-auth", key = kc.Key });

            case "jwt":
                var jc = await _kong.AddJwt(consumer);
                return (jc, new { type = "jwt", key = jc.Key, secret = jc.Secret, algorithm = jc.Algorithm });

            case "basic-auth":
                var existing = await _kong.GetBasicAuths(consumer);
                var username = existing.FirstOrDefault()?.Username ?? consumer;
                var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
                var bc       = await _kong.AddBasicAuth(consumer, username, password);
                return (bc, new { type = "basic-auth", username, password });

            case "hmac-auth":
                var he = await _kong.GetHmacAuths(consumer);
                var hu = he.FirstOrDefault()?.Username ?? consumer;
                var hc = await _kong.AddHmacAuth(consumer, hu);
                return (hc, new { type = "hmac-auth", username = hu, secret = hc.Secret });

            case "oauth2":
                var oe = await _kong.GetOAuth2s(consumer);
                var on = oe.FirstOrDefault()?.Name ?? $"{consumer}-app";
                var or = oe.FirstOrDefault()?.RedirectUris ?? new List<string> { "https://example.com/callback" };
                var oc = await _kong.AddOAuth2(consumer, on, or);
                return (oc, new { type = "oauth2", clientId = oc.ClientId, clientSecret = oc.ClientSecret });

            default:
                throw new NotSupportedException($"Auth type {authType} not supported");
        }
    }

    private async Task DeleteCredential(string consumer, string authType, string credId)
    {
        switch (authType)
        {
            case "key-auth":   await _kong.DeleteKeyAuth(consumer, credId);   break;
            case "jwt":        await _kong.DeleteJwt(consumer, credId);        break;
            case "basic-auth": await _kong.DeleteBasicAuth(consumer, credId); break;
            case "hmac-auth":  await _kong.DeleteHmacAuth(consumer, credId);  break;
            case "oauth2":     await _kong.DeleteOAuth2(consumer, credId);    break;
        }
    }
}

public class RotationBulkResult
{
    public List<string> Succeeded { get; set; } = new();
    public Dictionary<string, string> Failed { get; set; } = new();
}
