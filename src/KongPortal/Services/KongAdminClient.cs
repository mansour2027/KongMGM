using KongPortal.Models.Kong;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace KongPortal.Services;

public class KongAdminClient
{
    private readonly HttpClient _http;
    private readonly ILogger<KongAdminClient> _logger;

    public KongAdminClient(HttpClient http, ILogger<KongAdminClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    // ── Consumers ─────────────────────────────────────────────────────────────
    public async Task<List<KongConsumer>> GetConsumers()
    {
        var result = await _http.GetFromJsonAsync<KongList<KongConsumer>>("/consumers?size=1000");
        return result?.Data ?? new();
    }

    public async Task<KongConsumer?> GetConsumer(string username)
        => await _http.GetFromJsonAsync<KongConsumer>($"/consumers/{username}");

    public async Task<KongConsumer> CreateConsumer(string username, string? customId = null, List<string>? tags = null)
    {
        var resp = await _http.PostAsJsonAsync("/consumers", new
        {
            username,
            custom_id = customId,
            tags = tags ?? new List<string>()
        });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<KongConsumer>())!;
    }

    public async Task DeleteConsumer(string username)
    {
        var resp = await _http.DeleteAsync($"/consumers/{username}");
        if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.NotFound)
            resp.EnsureSuccessStatusCode();
    }

    // ── Key Auth ──────────────────────────────────────────────────────────────
    public async Task<List<KeyAuthCredential>> GetKeyAuths(string consumer)
    {
        var result = await _http.GetFromJsonAsync<KongList<KeyAuthCredential>>($"/consumers/{consumer}/key-auth");
        return result?.Data ?? new();
    }

    public async Task<KeyAuthCredential> AddKeyAuth(string consumer, string? key = null)
    {
        key ??= Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var resp = await _http.PostAsJsonAsync($"/consumers/{consumer}/key-auth", new { key });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<KeyAuthCredential>())!;
    }

    public async Task DeleteKeyAuth(string consumer, string keyId)
        => await _http.DeleteAsync($"/consumers/{consumer}/key-auth/{keyId}");

    // ── JWT ───────────────────────────────────────────────────────────────────
    public async Task<List<JwtCredential>> GetJwts(string consumer)
    {
        var result = await _http.GetFromJsonAsync<KongList<JwtCredential>>($"/consumers/{consumer}/jwt");
        return result?.Data ?? new();
    }

    public async Task<JwtCredential> AddJwt(string consumer, string algorithm = "HS256")
    {
        var key    = Guid.NewGuid().ToString();
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var resp   = await _http.PostAsJsonAsync($"/consumers/{consumer}/jwt",
            new { key, algorithm, secret });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<JwtCredential>())!;
    }

    public async Task DeleteJwt(string consumer, string jwtId)
        => await _http.DeleteAsync($"/consumers/{consumer}/jwt/{jwtId}");

    // ── Basic Auth ────────────────────────────────────────────────────────────
    public async Task<List<BasicAuthCredential>> GetBasicAuths(string consumer)
    {
        var result = await _http.GetFromJsonAsync<KongList<BasicAuthCredential>>($"/consumers/{consumer}/basic-auth");
        return result?.Data ?? new();
    }

    public async Task<BasicAuthCredential> AddBasicAuth(string consumer, string username, string password)
    {
        var resp = await _http.PostAsJsonAsync($"/consumers/{consumer}/basic-auth",
            new { username, password });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<BasicAuthCredential>())!;
    }

    public async Task DeleteBasicAuth(string consumer, string id)
        => await _http.DeleteAsync($"/consumers/{consumer}/basic-auth/{id}");

    // ── HMAC Auth ─────────────────────────────────────────────────────────────
    public async Task<List<HmacCredential>> GetHmacAuths(string consumer)
    {
        var result = await _http.GetFromJsonAsync<KongList<HmacCredential>>($"/consumers/{consumer}/hmac-auth");
        return result?.Data ?? new();
    }

    public async Task<HmacCredential> AddHmacAuth(string consumer, string username)
    {
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var resp   = await _http.PostAsJsonAsync($"/consumers/{consumer}/hmac-auth",
            new { username, secret });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<HmacCredential>())!;
    }

    public async Task DeleteHmacAuth(string consumer, string id)
        => await _http.DeleteAsync($"/consumers/{consumer}/hmac-auth/{id}");

    // ── OAuth2 ────────────────────────────────────────────────────────────────
    public async Task<List<OAuth2Credential>> GetOAuth2s(string consumer)
    {
        var result = await _http.GetFromJsonAsync<KongList<OAuth2Credential>>($"/consumers/{consumer}/oauth2");
        return result?.Data ?? new();
    }

    public async Task<OAuth2Credential> AddOAuth2(string consumer, string name, List<string> redirectUris)
    {
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var resp   = await _http.PostAsJsonAsync($"/consumers/{consumer}/oauth2",
            new { name, client_secret = secret, redirect_uris = redirectUris });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OAuth2Credential>())!;
    }

    public async Task DeleteOAuth2(string consumer, string id)
        => await _http.DeleteAsync($"/consumers/{consumer}/oauth2/{id}");

    // ── ACL Groups ────────────────────────────────────────────────────────────
    public async Task<List<AclGroup>> GetAclGroups(string consumer)
    {
        var result = await _http.GetFromJsonAsync<KongList<AclGroup>>($"/consumers/{consumer}/acls");
        return result?.Data ?? new();
    }

    public async Task AddToGroup(string consumer, string group)
        => await _http.PostAsJsonAsync($"/consumers/{consumer}/acls", new { group });

    public async Task RemoveFromGroup(string consumer, string aclId)
        => await _http.DeleteAsync($"/consumers/{consumer}/acls/{aclId}");

    // ── Services ──────────────────────────────────────────────────────────────
    public async Task<List<KongService>> GetServices()
    {
        var result = await _http.GetFromJsonAsync<KongList<KongService>>("/services?size=1000");
        return result?.Data ?? new();
    }

    public async Task<KongService?> GetService(string id)
        => await _http.GetFromJsonAsync<KongService>($"/services/{id}");

    public async Task<KongService> CreateService(CreateServiceRequest req)
    {
        var resp = await _http.PostAsJsonAsync("/services", req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<KongService>())!;
    }

    public async Task<KongService> UpdateService(string id, CreateServiceRequest req)
    {
        var resp = await _http.PatchAsJsonAsync($"/services/{id}", req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<KongService>())!;
    }

    public async Task DeleteService(string id)
        => await _http.DeleteAsync($"/services/{id}");

    // ── Routes ────────────────────────────────────────────────────────────────
    public async Task<List<KongRoute>> GetRoutes(string? serviceId = null)
    {
        var url    = serviceId != null ? $"/services/{serviceId}/routes?size=1000" : "/routes?size=1000";
        var result = await _http.GetFromJsonAsync<KongList<KongRoute>>(url);
        return result?.Data ?? new();
    }

    public async Task<KongRoute> CreateRoute(string serviceId, CreateRouteRequest req)
    {
        var resp = await _http.PostAsJsonAsync($"/services/{serviceId}/routes", req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<KongRoute>())!;
    }

    public async Task<KongRoute> UpdateRoute(string routeId, CreateRouteRequest req)
    {
        var resp = await _http.PatchAsJsonAsync($"/routes/{routeId}", req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<KongRoute>())!;
    }

    public async Task DeleteRoute(string routeId)
        => await _http.DeleteAsync($"/routes/{routeId}");

    // ── Plugins ───────────────────────────────────────────────────────────────
    public async Task<List<KongPlugin>> GetPlugins(string? serviceId = null, string? consumerId = null)
    {
        var url = serviceId != null ? $"/services/{serviceId}/plugins" :
                  consumerId != null ? $"/consumers/{consumerId}/plugins" :
                  "/plugins?size=1000";
        var result = await _http.GetFromJsonAsync<KongList<KongPlugin>>(url);
        return result?.Data ?? new();
    }

    public async Task<KongPlugin> CreatePlugin(CreatePluginRequest req)
    {
        var resp = await _http.PostAsJsonAsync("/plugins", req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<KongPlugin>())!;
    }

    public async Task<KongPlugin> UpdatePlugin(string pluginId, CreatePluginRequest req)
    {
        var resp = await _http.PatchAsJsonAsync($"/plugins/{pluginId}", req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<KongPlugin>())!;
    }

    public async Task TogglePlugin(string pluginId, bool enabled)
        => await _http.PatchAsJsonAsync($"/plugins/{pluginId}", new { enabled });

    public async Task DeletePlugin(string pluginId)
        => await _http.DeleteAsync($"/plugins/{pluginId}");
}
