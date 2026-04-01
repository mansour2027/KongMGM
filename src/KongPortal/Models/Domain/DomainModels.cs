using System.ComponentModel.DataAnnotations;

namespace KongPortal.Models.Domain;

public class ConsumerProfile
{
    public int Id { get; set; }

    [Required]
    public string KongConsumerUsername { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Environment { get; set; } = "Production";
    public string AuthType { get; set; } = "key-auth";
    public string? Notes { get; set; }

    // Contact
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactSlack { get; set; } = string.Empty;

    // Rotation
    public DateTime? LastRotatedAt { get; set; }
    public string? LastRotatedBy { get; set; }
    public int RotationIntervalDays { get; set; } = 90;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsOverdue => LastRotatedAt.HasValue &&
        (DateTime.UtcNow - LastRotatedAt.Value).TotalDays > RotationIntervalDays;

    public bool NeverRotated => !LastRotatedAt.HasValue;
}

public class RotationRecord
{
    public int Id { get; set; }
    public string KongConsumerUsername { get; set; } = string.Empty;
    public string AuthType { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;

    // Old credential IDs to delete after grace period (comma separated)
    public string OldCredentialIds { get; set; } = string.Empty;

    public string RotatedBy { get; set; } = string.Empty;
    public DateTime RotatedAt { get; set; } = DateTime.UtcNow;
    public bool Confirmed { get; set; } = false;
    public DateTime? ConfirmedAt { get; set; }

    // Deletion tracking
    public bool OldCredentialsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public List<string> GetOldIds() =>
        OldCredentialIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
}

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime At { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; } = true;
    public string? FailureReason { get; set; }
    public string? Notes { get; set; }
}
