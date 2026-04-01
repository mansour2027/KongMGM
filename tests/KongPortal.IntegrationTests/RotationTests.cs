using FluentAssertions;
using KongPortal.Data;
using KongPortal.Models.Domain;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace KongPortal.IntegrationTests;

public class RotationTests : IntegrationTestBase
{
    // ── Rotation Center ───────────────────────────────────────────────────────
    [Fact]
    public async Task RotationCenter_ReturnsOk()
    {
        var resp = await AdminClient.GetAsync("/rotation");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Single Rotate ─────────────────────────────────────────────────────────
    [Fact]
    public async Task RotateConsumer_KeyAuth_CreatesRotationRecord()
    {
        // Seed a profile first
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ConsumerProfiles.Add(new ConsumerProfile
            {
                KongConsumerUsername = "service-a",
                ServiceName          = "Payment API",
                AuthType             = "key-auth",
                ContactEmail         = "ahmed@payments.com",
                ContactName          = "Ahmed"
            });
            await db.SaveChangesAsync();
        }

        // Rotate
        var resp = await AdminClient.PostAsync("/consumers/service-a/rotate",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["authType"] = "key-auth"
            }));

        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);

        // Verify rotation record created
        using (var scope = Factory.Services.CreateScope())
        {
            var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var record = db.RotationRecords
                .FirstOrDefault(r => r.KongConsumerUsername == "service-a");

            record.Should().NotBeNull();
            record!.AuthType.Should().Be("key-auth");
            record.Confirmed.Should().BeFalse();           // not confirmed yet
            record.OldCredentialsDeleted.Should().BeFalse(); // not deleted yet
            record.OldCredentialIds.Should().Contain("key-old-1");
        }
    }

    [Fact]
    public async Task RotateConsumer_UpdatesLastRotatedAt()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ConsumerProfiles.Add(new ConsumerProfile
            {
                KongConsumerUsername = "service-a",
                AuthType             = "key-auth",
                LastRotatedAt        = DateTime.UtcNow.AddDays(-100)
            });
            await db.SaveChangesAsync();
        }

        await AdminClient.PostAsync("/consumers/service-a/rotate",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["authType"] = "key-auth"
            }));

        using (var scope = Factory.Services.CreateScope())
        {
            var db      = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var profile = db.ConsumerProfiles
                .First(p => p.KongConsumerUsername == "service-a");

            profile.LastRotatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }
    }

    // ── Confirm Rotation ──────────────────────────────────────────────────────
    [Fact]
    public async Task ConfirmRotation_SetsConfirmedTrue()
    {
        int recordId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var r  = new RotationRecord
            {
                KongConsumerUsername = "service-a",
                AuthType             = "key-auth",
                OldCredentialIds     = "key-old-1",
                RotatedBy            = "testuser",
                Confirmed            = false
            };
            db.RotationRecords.Add(r);
            await db.SaveChangesAsync();
            recordId = r.Id;
        }

        var resp = await AdminClient.PostAsync($"/rotation/confirm/{recordId}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);

        using (var scope = Factory.Services.CreateScope())
        {
            var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var record = await db.RotationRecords.FindAsync(recordId);

            record!.Confirmed.Should().BeTrue();
            record.ConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }
    }

    // ── Delete Old Credentials ────────────────────────────────────────────────
    [Fact]
    public async Task DeleteOldCredentials_ConfirmedRecord_DeletesAndMarks()
    {
        int recordId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var r  = new RotationRecord
            {
                KongConsumerUsername = "service-a",
                AuthType             = "key-auth",
                OldCredentialIds     = "key-old-1",
                RotatedBy            = "testuser",
                Confirmed            = true,
                ConfirmedAt          = DateTime.UtcNow,
                OldCredentialsDeleted = false
            };
            db.RotationRecords.Add(r);
            await db.SaveChangesAsync();
            recordId = r.Id;
        }

        var resp = await AdminClient.PostAsync("/rotation/delete-old",
            new StringContent(
                $"[{recordId}]",
                System.Text.Encoding.UTF8,
                "application/json"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("deleted");

        using (var scope = Factory.Services.CreateScope())
        {
            var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var record = await db.RotationRecords.FindAsync(recordId);
            record!.OldCredentialsDeleted.Should().BeTrue();
            record.DeletedAt.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task DeleteOldCredentials_UnconfirmedRecord_Skips()
    {
        int recordId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var r  = new RotationRecord
            {
                KongConsumerUsername  = "service-a",
                AuthType              = "key-auth",
                OldCredentialIds      = "key-old-1",
                RotatedBy             = "testuser",
                Confirmed             = false,   // ← not confirmed
                OldCredentialsDeleted = false
            };
            db.RotationRecords.Add(r);
            await db.SaveChangesAsync();
            recordId = r.Id;
        }

        var resp = await AdminClient.PostAsync("/rotation/delete-old",
            new StringContent($"[{recordId}]",
                System.Text.Encoding.UTF8, "application/json"));

        var body = await resp.Content.ReadAsStringAsync();

        // Skipped — not deleted
        using (var scope = Factory.Services.CreateScope())
        {
            var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var record = await db.RotationRecords.FindAsync(recordId);
            record!.OldCredentialsDeleted.Should().BeFalse();
        }
    }

    // ── Rotation History ──────────────────────────────────────────────────────
    [Fact]
    public async Task RotationHistory_ReturnsOk()
    {
        var resp = await AdminClient.GetAsync("/rotation/history");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RotationHistory_FilterByConsumer_ReturnsFiltered()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.RotationRecords.AddRange(
                new RotationRecord
                {
                    KongConsumerUsername = "service-a",
                    AuthType = "key-auth", RotatedBy = "user1"
                },
                new RotationRecord
                {
                    KongConsumerUsername = "service-b",
                    AuthType = "jwt", RotatedBy = "user1"
                }
            );
            await db.SaveChangesAsync();
        }

        var resp    = await AdminClient.GetAsync("/rotation/history?consumer=service-a");
        var content = await resp.Content.ReadAsStringAsync();
        content.Should().Contain("service-a");
    }
}
