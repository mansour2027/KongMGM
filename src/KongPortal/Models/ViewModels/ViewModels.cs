using KongPortal.Models.Domain;
using KongPortal.Models.Kong;

namespace KongPortal.Models.ViewModels;

public class ConsumerViewModel
{
    public KongConsumer Kong { get; set; } = new();
    public ConsumerProfile? Profile { get; set; }
    public List<KeyAuthCredential> KeyAuths { get; set; } = new();
    public List<JwtCredential> JwtCredentials { get; set; } = new();
    public List<BasicAuthCredential> BasicAuths { get; set; } = new();
    public List<HmacCredential> HmacAuths { get; set; } = new();
    public List<OAuth2Credential> OAuth2s { get; set; } = new();
    public List<AclGroup> AclGroups { get; set; } = new();
    public List<KongPlugin> Plugins { get; set; } = new();
    public List<RotationRecord> RotationHistory { get; set; } = new();

    public bool IsOverdue => Profile?.IsOverdue ?? false;
    public bool NeverRotated => Profile?.NeverRotated ?? true;
    public int TotalCredentials =>
        KeyAuths.Count + JwtCredentials.Count +
        BasicAuths.Count + HmacAuths.Count + OAuth2s.Count;
}

public class ConsumerListViewModel
{
    public List<ConsumerViewModel> Consumers { get; set; } = new();
    public string? FilterTeam { get; set; }
    public string? FilterAuthType { get; set; }
    public string? FilterEnvironment { get; set; }
    public bool OverdueOnly { get; set; }
    public int TotalCount => Consumers.Count;
    public int OverdueCount => Consumers.Count(c => c.IsOverdue);
    public int NeverRotatedCount => Consumers.Count(c => c.NeverRotated);
}

public class ServiceViewModel
{
    public KongService Service { get; set; } = new();
    public List<KongRoute> Routes { get; set; } = new();
    public List<KongPlugin> Plugins { get; set; } = new();
    public List<ConsumerViewModel> Consumers { get; set; } = new();
}

public class RotationStatusViewModel
{
    public List<RotationRecord> Records { get; set; } = new();
    public DateTime Date { get; set; }
    public int Total => Records.Count;
    public int Confirmed => Records.Count(r => r.Confirmed);
    public int Pending => Records.Count(r => !r.Confirmed);
    public int Deleted => Records.Count(r => r.OldCredentialsDeleted);
}

public class DashboardViewModel
{
    public int TotalConsumers { get; set; }
    public int TotalServices { get; set; }
    public int TotalRoutes { get; set; }
    public int TotalPlugins { get; set; }
    public int OverdueRotations { get; set; }
    public int NeverRotated { get; set; }
    public int PendingDeletions { get; set; }
    public List<AuditLog> RecentActivity { get; set; } = new();
}
