using FluentAssertions;
using KongPortal.Models.Domain;
using Xunit;

namespace KongPortal.Tests;

public class ConsumerProfileTests
{
    [Fact]
    public void IsOverdue_WhenLastRotatedExceedsInterval_ReturnsTrue()
    {
        var profile = new ConsumerProfile
        {
            LastRotatedAt        = DateTime.UtcNow.AddDays(-100),
            RotationIntervalDays = 90
        };
        profile.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenLastRotatedWithinInterval_ReturnsFalse()
    {
        var profile = new ConsumerProfile
        {
            LastRotatedAt        = DateTime.UtcNow.AddDays(-30),
            RotationIntervalDays = 90
        };
        profile.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void NeverRotated_WhenNoLastRotatedAt_ReturnsTrue()
    {
        var profile = new ConsumerProfile { LastRotatedAt = null };
        profile.NeverRotated.Should().BeTrue();
    }

    [Fact]
    public void GetOldIds_ParsesCommaSeparatedIds()
    {
        var record = new RotationRecord
        {
            OldCredentialIds = "id-1,id-2,id-3"
        };
        record.GetOldIds().Should().BeEquivalentTo(new[] { "id-1", "id-2", "id-3" });
    }

    [Fact]
    public void GetOldIds_ReturnsEmpty_WhenNoIds()
    {
        var record = new RotationRecord { OldCredentialIds = "" };
        record.GetOldIds().Should().BeEmpty();
    }
}
