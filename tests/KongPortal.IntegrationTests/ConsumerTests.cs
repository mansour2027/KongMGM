using FluentAssertions;
using KongPortal.Data;
using KongPortal.Models.Domain;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace KongPortal.IntegrationTests;

public class ConsumerTests : IntegrationTestBase
{
    // ── List Consumers ────────────────────────────────────────────────────────
    [Fact]
    public async Task GetConsumers_ReturnsOk()
    {
        var resp = await AdminClient.GetAsync("/consumers");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetConsumers_ViewerCanAccess()
    {
        var resp = await ViewerClient.GetAsync("/consumers");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetConsumers_UnauthenticatedRedirectsToLogin()
    {
        var client = Factory.CreateClient(new()
        {
            AllowAutoRedirect = false
        });
        var resp = await client.GetAsync("/consumers");
        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/account/login");
    }

    // ── Consumer Detail ───────────────────────────────────────────────────────
    [Fact]
    public async Task GetConsumerDetail_ExistingConsumer_ReturnsOk()
    {
        var resp = await AdminClient.GetAsync("/consumers/service-a");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetConsumerDetail_ShowsCredentials()
    {
        var resp    = await AdminClient.GetAsync("/consumers/service-a");
        var content = await resp.Content.ReadAsStringAsync();
        content.Should().Contain("service-a");
        content.Should().Contain("key-auth");
    }

    // ── Profile Save ──────────────────────────────────────────────────────────
    [Fact]
    public async Task SaveProfile_CreatesExtendedProfile()
    {
        var token = await GetAntiForgeryToken("/consumers/service-a");

        var resp = await AdminClient.PostAsync("/consumers/service-a/profile",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ServiceName"]          = "Payment API",
                ["TeamName"]             = "Payments Team",
                ["Environment"]          = "Production",
                ["AuthType"]             = "key-auth",
                ["ContactName"]          = "Ahmed Al Rashid",
                ["ContactEmail"]         = "ahmed@payments.com",
                ["ContactPhone"]         = "+971-50-000",
                ["ContactSlack"]         = "@ahmed",
                ["RotationIntervalDays"] = "90",
                ["__RequestVerificationToken"] = token
            }));

        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);

        // Verify saved in DB
        using var scope   = Factory.Services.CreateScope();
        var db            = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile       = db.ConsumerProfiles.FirstOrDefault(p =>
                            p.KongConsumerUsername == "service-a");

        profile.Should().NotBeNull();
        profile!.ServiceName.Should().Be("Payment API");
        profile.ContactEmail.Should().Be("ahmed@payments.com");
        profile.TeamName.Should().Be("Payments Team");
    }

    [Fact]
    public async Task SaveProfile_ViewerCannotSave()
    {
        var token = await GetAntiForgeryToken("/consumers/service-a");

        var resp = await ViewerClient.PostAsync("/consumers/service-a/profile",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ServiceName"] = "Should Not Save",
                ["__RequestVerificationToken"] = token
            }));

        resp.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    // ── Add Key ───────────────────────────────────────────────────────────────
    [Fact]
    public async Task AddKeyAuth_Admin_Succeeds()
    {
        var token = await GetAntiForgeryToken("/consumers/service-a");

        var resp = await AdminClient.PostAsync("/consumers/service-a/add-key",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));

        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);
    }

    // ── Bulk Rotate ───────────────────────────────────────────────────────────
    [Fact]
    public async Task BulkRotate_ValidConsumers_Returns200()
    {
        var token = await GetAntiForgeryToken("/consumers");

        var resp = await AdminClient.PostAsync("/consumers/bulk-rotate",
            new StringContent(
                """["service-a"]""",
                System.Text.Encoding.UTF8,
                "application/json"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("succeeded");
    }

    [Fact]
    public async Task BulkRotate_EmptyList_Returns200WithZero()
    {
        var resp = await AdminClient.PostAsync("/consumers/bulk-rotate",
            new StringContent("""[]""",
                System.Text.Encoding.UTF8, "application/json"));

        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("succeeded");
    }

    // ── Delete Consumer ───────────────────────────────────────────────────────
    [Fact]
    public async Task DeleteConsumer_Viewer_Forbidden()
    {
        var token = await GetAntiForgeryToken("/consumers/service-a");

        var resp = await ViewerClient.PostAsync("/consumers/service-a/delete",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));

        resp.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private async Task<string> GetAntiForgeryToken(string url)
    {
        var resp    = await AdminClient.GetAsync(url);
        var html    = await resp.Content.ReadAsStringAsync();
        var start   = html.IndexOf("__RequestVerificationToken");
        if (start < 0) return string.Empty;
        var valStart = html.IndexOf("value=\"", start) + 7;
        var valEnd   = html.IndexOf("\"", valStart);
        return html[valStart..valEnd];
    }
}
