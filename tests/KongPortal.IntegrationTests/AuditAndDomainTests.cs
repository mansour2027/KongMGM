using FluentAssertions;
using KongPortal.Data;
using KongPortal.Models.Domain;
using KongPortal.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Xunit;

namespace KongPortal.IntegrationTests;

// ── Audit Log Tests ───────────────────────────────────────────────────────────
public class AuditLogTests : IntegrationTestBase
{
    [Fact]
    public async Task AuditLog_PageLoads()
    {
        var resp = await AdminClient.GetAsync("/audit");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuditLog_RecordsRotationActions()
    {
        // Perform a rotation
        await AdminClient.PostAsync("/consumers/service-a/rotate",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["authType"] = "key-auth"
            }));

        // Check audit log page shows it
        var resp    = await AdminClient.GetAsync("/audit?action=Rotate");
        var content = await resp.Content.ReadAsStringAsync();
        content.Should().Contain("Rotate");
    }

    [Fact]
    public async Task AuditLog_FilterByResource_Works()
    {
        var resp    = await AdminClient.GetAsync("/audit?resource=service-a");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

// ── Domain Model Unit Tests ───────────────────────────────────────────────────
public class DomainModelTests
{
    [Fact]
    public void ConsumerProfile_IsOverdue_WhenPastInterval()
    {
        var profile = new ConsumerProfile
        {
            LastRotatedAt        = DateTime.UtcNow.AddDays(-100),
            RotationIntervalDays = 90
        };
        profile.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void ConsumerProfile_NotOverdue_WhenWithinInterval()
    {
        var profile = new ConsumerProfile
        {
            LastRotatedAt        = DateTime.UtcNow.AddDays(-30),
            RotationIntervalDays = 90
        };
        profile.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void ConsumerProfile_NeverRotated_WhenNoDate()
    {
        var profile = new ConsumerProfile { LastRotatedAt = null };
        profile.NeverRotated.Should().BeTrue();
        profile.IsOverdue.Should().BeFalse(); // no date = not "overdue", just never
    }

    [Fact]
    public void RotationRecord_GetOldIds_ParsesCorrectly()
    {
        var record = new RotationRecord
        {
            OldCredentialIds = "id-1,id-2,id-3"
        };
        record.GetOldIds().Should().BeEquivalentTo(new[] { "id-1", "id-2", "id-3" });
    }

    [Fact]
    public void RotationRecord_GetOldIds_HandlesEmpty()
    {
        var record = new RotationRecord { OldCredentialIds = "" };
        record.GetOldIds().Should().BeEmpty();
    }

    [Fact]
    public void RotationRecord_GetOldIds_HandlesSingleId()
    {
        var record = new RotationRecord { OldCredentialIds = "only-one" };
        record.GetOldIds().Should().ContainSingle().Which.Should().Be("only-one");
    }
}

// ── AuditLogService Unit Tests ────────────────────────────────────────────────
public class AuditLogServiceTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AuditTest_" + Guid.NewGuid())
            .Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task Log_SavesRecord()
    {
        using var db      = CreateDb();
        var service       = new AuditLogService(db);

        await service.Log("Rotate", "service-a", "Consumer",
            "admin", "10.0.0.1");

        db.AuditLogs.Should().HaveCount(1);
        var log = db.AuditLogs.First();
        log.Action.Should().Be("Rotate");
        log.Resource.Should().Be("service-a");
        log.PerformedBy.Should().Be("admin");
        log.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Log_FailedAction_SavesWithReason()
    {
        using var db = CreateDb();
        var service  = new AuditLogService(db);

        await service.Log("Rotate", "service-a", "Consumer",
            "admin", "10.0.0.1", success: false, reason: "Kong unreachable");

        var log = db.AuditLogs.First();
        log.Success.Should().BeFalse();
        log.FailureReason.Should().Be("Kong unreachable");
    }

    [Fact]
    public async Task GetRecent_ReturnsLatestFirst()
    {
        using var db = CreateDb();
        var service  = new AuditLogService(db);

        await service.Log("Action1", "res1", "Consumer", "user1", "ip");
        await Task.Delay(10);
        await service.Log("Action2", "res2", "Consumer", "user2", "ip");

        var recent = await service.GetRecent(10);
        recent.First().Action.Should().Be("Action2");
    }

    [Fact]
    public async Task Search_FiltersByAction()
    {
        using var db = CreateDb();
        var service  = new AuditLogService(db);

        await service.Log("Rotate", "service-a", "Consumer", "admin", "ip");
        await service.Log("Delete", "service-b", "Consumer", "admin", "ip");

        var results = await service.Search(action: "Rotate");
        results.Should().HaveCount(1);
        results.First().Action.Should().Be("Rotate");
    }

    [Fact]
    public async Task Search_FiltersByResource()
    {
        using var db = CreateDb();
        var service  = new AuditLogService(db);

        await service.Log("Rotate", "service-a", "Consumer", "admin", "ip");
        await service.Log("Rotate", "service-b", "Consumer", "admin", "ip");

        var results = await service.Search(resource: "service-a");
        results.Should().HaveCount(1);
    }
}
