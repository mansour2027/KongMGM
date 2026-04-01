using FluentAssertions;
using KongPortal.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace KongPortal.IntegrationTests;

public class KongAdminClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly KongAdminClient _client;

    public KongAdminClientTests()
    {
        _server = WireMockServer.Start();
        var http = new HttpClient { BaseAddress = new Uri(_server.Url!) };
        _client  = new KongAdminClient(http, NullLogger<KongAdminClient>.Instance);
    }

    // ── GetConsumers ──────────────────────────────────────────────────────────
    [Fact]
    public async Task GetConsumers_ReturnsAllConsumers()
    {
        _server.Given(Request.Create().WithPath("/consumers").UsingGet())
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody("""
                   {
                     "data": [
                       {"id":"1","username":"svc-a","tags":[],"created_at":1700000000},
                       {"id":"2","username":"svc-b","tags":[],"created_at":1700000001}
                     ],
                     "total": 2
                   }
                   """));

        var result = await _client.GetConsumers();

        result.Should().HaveCount(2);
        result[0].Username.Should().Be("svc-a");
        result[1].Username.Should().Be("svc-b");
    }

    [Fact]
    public async Task GetConsumers_EmptyKong_ReturnsEmptyList()
    {
        _server.Given(Request.Create().WithPath("/consumers").UsingGet())
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody("""{ "data": [], "total": 0 }"""));

        var result = await _client.GetConsumers();
        result.Should().BeEmpty();
    }

    // ── Key Auth ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetKeyAuths_ReturnsCredentials()
    {
        _server.Given(Request.Create().WithPath("/consumers/svc-a/key-auth").UsingGet())
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody("""
                   {
                     "data": [
                       {"id":"key-1","key":"abc123","consumer":{"id":"1"},"created_at":1700000000,"tags":[]},
                       {"id":"key-2","key":"def456","consumer":{"id":"1"},"created_at":1700000001,"tags":[]}
                     ],
                     "total": 2
                   }
                   """));

        var result = await _client.GetKeyAuths("svc-a");
        result.Should().HaveCount(2);
        result[0].Id.Should().Be("key-1");
        result[1].Id.Should().Be("key-2");
    }

    [Fact]
    public async Task AddKeyAuth_ReturnsNewCredential()
    {
        _server.Given(Request.Create().WithPath("/consumers/svc-a/key-auth").UsingPost())
               .RespondWith(Response.Create()
                   .WithStatusCode(201)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody("""
                   {"id":"key-new","key":"newkey789","consumer":{"id":"1"},"created_at":1700002000,"tags":[]}
                   """));

        var result = await _client.AddKeyAuth("svc-a");
        result.Id.Should().Be("key-new");
        result.Key.Should().Be("newkey789");
    }

    [Fact]
    public async Task DeleteKeyAuth_Returns204_Succeeds()
    {
        _server.Given(Request.Create()
                   .WithPath("/consumers/svc-a/key-auth/key-1").UsingDelete())
               .RespondWith(Response.Create().WithStatusCode(204));

        var act = async () => await _client.DeleteKeyAuth("svc-a", "key-1");
        await act.Should().NotThrowAsync();
    }

    // ── JWT ───────────────────────────────────────────────────────────────────
    [Fact]
    public async Task AddJwt_ReturnsCredentialWithKeyAndSecret()
    {
        _server.Given(Request.Create().WithPath("/consumers/svc-a/jwt").UsingPost())
               .RespondWith(Response.Create()
                   .WithStatusCode(201)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody("""
                   {
                     "id":"jwt-1","key":"iss-key","secret":"jwt-secret",
                     "algorithm":"HS256","consumer":{"id":"1"},"created_at":1700000000
                   }
                   """));

        var result = await _client.AddJwt("svc-a");
        result.Id.Should().Be("jwt-1");
        result.Algorithm.Should().Be("HS256");
        result.Secret.Should().Be("jwt-secret");
    }

    // ── Services ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetServices_ReturnsServices()
    {
        _server.Given(Request.Create().WithPath("/services").UsingGet())
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody("""
                   {
                     "data": [
                       {"id":"svc-1","name":"property-api","host":"property-svc",
                        "port":80,"protocol":"http","enabled":true,"tags":[],
                        "connect_timeout":60000,"read_timeout":60000,"write_timeout":60000}
                     ],
                     "total": 1
                   }
                   """));

        var result = await _client.GetServices();
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("property-api");
        result[0].Enabled.Should().BeTrue();
    }

    // ── Plugins ───────────────────────────────────────────────────────────────
    [Fact]
    public async Task TogglePlugin_SendsPatchRequest()
    {
        _server.Given(Request.Create().WithPath("/plugins/plug-1").UsingPatch())
               .RespondWith(Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody("""
                   {"id":"plug-1","name":"rate-limiting","enabled":false,"config":{},"tags":[]}
                   """));

        var act = async () => await _client.TogglePlugin("plug-1", false);
        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _server.Dispose();
}
