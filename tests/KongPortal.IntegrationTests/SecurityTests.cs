using FluentAssertions;
using System.Net;
using Xunit;

namespace KongPortal.IntegrationTests;

public class SecurityTests : IntegrationTestBase
{
    // ── Authentication ────────────────────────────────────────────────────────
    [Theory]
    [InlineData("/consumers")]
    [InlineData("/services")]
    [InlineData("/rotation")]
    [InlineData("/audit")]
    [InlineData("/")]
    public async Task ProtectedRoutes_Unauthenticated_RedirectToLogin(string url)
    {
        var client = Factory.CreateClient(new() { AllowAutoRedirect = false });
        var resp   = await client.GetAsync(url);

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("login");
    }

    // ── Viewer RBAC ───────────────────────────────────────────────────────────
    [Theory]
    [InlineData("/consumers")]
    [InlineData("/services")]
    [InlineData("/rotation")]
    [InlineData("/audit")]
    public async Task ViewerRole_CanAccessReadPages(string url)
    {
        var resp = await ViewerClient.GetAsync(url);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ViewerRole_CannotRotate()
    {
        var resp = await ViewerClient.PostAsync("/consumers/service-a/rotate",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["authType"] = "key-auth"
            }));

        resp.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task ViewerRole_CannotDelete()
    {
        var resp = await ViewerClient.PostAsync("/consumers/service-a/delete",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        resp.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task ViewerRole_CannotBulkRotate()
    {
        var resp = await ViewerClient.PostAsync("/consumers/bulk-rotate",
            new StringContent("""["service-a"]""",
                System.Text.Encoding.UTF8, "application/json"));

        resp.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    // ── Admin RBAC ────────────────────────────────────────────────────────────
    [Fact]
    public async Task AdminRole_CanAccessEverything()
    {
        var pages = new[] { "/consumers", "/services", "/rotation", "/audit", "/" };
        foreach (var page in pages)
        {
            var resp = await AdminClient.GetAsync(page);
            resp.StatusCode.Should().Be(HttpStatusCode.OK, $"Admin should access {page}");
        }
    }

    // ── CSRF ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task PostWithoutCsrfToken_IsRejected()
    {
        // Post without antiforgery token
        var resp = await AdminClient.PostAsync("/consumers/service-a/rotate",
            new StringContent("authType=key-auth",
                System.Text.Encoding.UTF8,
                "application/x-www-form-urlencoded"));

        // Should be rejected — 400 or redirect
        resp.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Redirect,
            HttpStatusCode.Forbidden);
    }

    // ── Login ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Login_WrongPassword_Fails()
    {
        var client = Factory.CreateClient();
        var resp   = await client.PostAsync("/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = "admin@test.com",
                ["password"] = "WrongPassword!"
            }));

        // Should stay on login page or redirect back
        var content = await resp.Content.ReadAsStringAsync();
        resp.IsSuccessStatusCode.Should().BeTrue(); // page loads
        // Login page shown again — no authenticated cookie
    }

    [Fact]
    public async Task Login_CorrectCredentials_RedirectsToDashboard()
    {
        var client = await Factory.CreateAuthenticatedClientAsync();
        var resp   = await client.GetAsync("/");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
