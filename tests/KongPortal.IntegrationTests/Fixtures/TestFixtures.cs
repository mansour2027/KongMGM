using KongPortal.Data;
using KongPortal.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace KongPortal.IntegrationTests;

// ── WireMock Kong Server ──────────────────────────────────────────────────────
public class MockKongServer : IDisposable
{
    public WireMockServer Server { get; }
    public string Url => Server.Url!;

    public MockKongServer()
    {
        Server = WireMockServer.Start();
        SetupDefaults();
    }

    private void SetupDefaults()
    {
        // GET /consumers
        Server.Given(Request.Create().WithPath("/consumers").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""
                  {
                    "data": [
                      { "id": "aaa-111", "username": "service-a", "tags": [], "created_at": 1700000000 },
                      { "id": "bbb-222", "username": "service-b", "tags": [], "created_at": 1700000001 }
                    ],
                    "total": 2
                  }
                  """));

        // GET /consumers/service-a
        Server.Given(Request.Create().WithPath("/consumers/service-a").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""
                  { "id": "aaa-111", "username": "service-a", "tags": [], "created_at": 1700000000 }
                  """));

        // GET /consumers/service-a/key-auth
        Server.Given(Request.Create().WithPath("/consumers/service-a/key-auth").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""
                  {
                    "data": [
                      { "id": "key-old-1", "key": "oldkey123abc", "consumer": {"id": "aaa-111"}, "created_at": 1700000000, "tags": [] }
                    ],
                    "total": 1
                  }
                  """));

        // POST /consumers/service-a/key-auth
        Server.Given(Request.Create().WithPath("/consumers/service-a/key-auth").UsingPost())
              .RespondWith(Response.Create()
                  .WithStatusCode(201)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""
                  { "id": "key-new-1", "key": "newkey456def", "consumer": {"id": "aaa-111"}, "created_at": 1700001000, "tags": [] }
                  """));

        // DELETE /consumers/service-a/key-auth/key-old-1
        Server.Given(Request.Create().WithPath("/consumers/service-a/key-auth/key-old-1").UsingDelete())
              .RespondWith(Response.Create().WithStatusCode(204));

        // GET /consumers/service-a/jwt
        Server.Given(Request.Create().WithPath("/consumers/service-a/jwt").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""{ "data": [], "total": 0 }"""));

        // GET /consumers/service-a/basic-auth
        Server.Given(Request.Create().WithPath("/consumers/service-a/basic-auth").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""{ "data": [], "total": 0 }"""));

        // GET /consumers/service-a/hmac-auth
        Server.Given(Request.Create().WithPath("/consumers/service-a/hmac-auth").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""{ "data": [], "total": 0 }"""));

        // GET /consumers/service-a/oauth2
        Server.Given(Request.Create().WithPath("/consumers/service-a/oauth2").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""{ "data": [], "total": 0 }"""));

        // GET /consumers/service-a/acls
        Server.Given(Request.Create().WithPath("/consumers/service-a/acls").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""{ "data": [], "total": 0 }"""));

        // GET /consumers/service-a/plugins
        Server.Given(Request.Create().WithPath("/consumers/service-a/plugins").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""{ "data": [], "total": 0 }"""));

        // GET /services
        Server.Given(Request.Create().WithPath("/services").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""
                  {
                    "data": [
                      { "id": "svc-1", "name": "property-api", "host": "property-service",
                        "port": 80, "protocol": "http", "enabled": true, "tags": [],
                        "connect_timeout": 60000, "read_timeout": 60000, "write_timeout": 60000 }
                    ],
                    "total": 1
                  }
                  """));

        // GET /routes
        Server.Given(Request.Create().WithPath("/routes").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""{ "data": [], "total": 0 }"""));

        // GET /plugins
        Server.Given(Request.Create().WithPath("/plugins").UsingGet())
              .RespondWith(Response.Create()
                  .WithStatusCode(200)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""{ "data": [], "total": 0 }"""));

        // POST /consumers
        Server.Given(Request.Create().WithPath("/consumers").UsingPost())
              .RespondWith(Response.Create()
                  .WithStatusCode(201)
                  .WithHeader("Content-Type", "application/json")
                  .WithBody("""
                  { "id": "ccc-333", "username": "new-consumer", "tags": [], "created_at": 1700002000 }
                  """));
    }

    public void Dispose() => Server.Dispose();
}

// ── WebApplicationFactory ─────────────────────────────────────────────────────
public class KongPortalFactory : WebApplicationFactory<Program>
{
    private readonly MockKongServer _mockKong;

    public KongPortalFactory(MockKongServer mockKong)
    {
        _mockKong = mockKong;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace DB with InMemory
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

            // Replace Kong HTTP client with mock
            services.AddHttpClient<KongPortal.Services.KongAdminClient>(client =>
            {
                client.BaseAddress = new Uri(_mockKong.Url);
                client.Timeout = TimeSpan.FromSeconds(10);
            });
        });

        builder.UseEnvironment("Testing");
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string role = Roles.Admin)
    {
        var client = CreateClient();

        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedRolesAsync(scope.ServiceProvider);

        var username = $"testuser-{role.ToLower()}@test.com";
        var user     = await userManager.FindByNameAsync(username);
        if (user == null)
        {
            user = new AppUser { UserName = username, Email = username, FullName = "Test User" };
            await userManager.CreateAsync(user, "Test@Password123!");
            await userManager.AddToRoleAsync(user, role);
        }

        // Login
        var loginResp = await client.PostAsync("/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = "Test@Password123!"
            }));

        return client;
    }
}

// ── Base test class ───────────────────────────────────────────────────────────
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected MockKongServer MockKong { get; private set; } = null!;
    protected KongPortalFactory Factory { get; private set; } = null!;
    protected HttpClient AdminClient { get; private set; } = null!;
    protected HttpClient ViewerClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        MockKong     = new MockKongServer();
        Factory      = new KongPortalFactory(MockKong);
        AdminClient  = await Factory.CreateAuthenticatedClientAsync(Roles.Admin);
        ViewerClient = await Factory.CreateAuthenticatedClientAsync(Roles.Viewer);
    }

    public Task DisposeAsync()
    {
        AdminClient.Dispose();
        ViewerClient.Dispose();
        Factory.Dispose();
        MockKong.Dispose();
        return Task.CompletedTask;
    }
}
