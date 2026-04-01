namespace KongPortal.Models.Kong;

public class KongList<T>
{
    public List<T> Data { get; set; } = new();
    public int Total { get; set; }
    public string? Next { get; set; }
}

public class KongConsumer
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? CustomId { get; set; }
    public List<string> Tags { get; set; } = new();
    public long CreatedAt { get; set; }
}

public class KongService
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string? Path { get; set; }
    public int Port { get; set; } = 80;
    public string Protocol { get; set; } = "http";
    public bool Enabled { get; set; } = true;
    public int ConnectTimeout { get; set; } = 60000;
    public int ReadTimeout { get; set; } = 60000;
    public int WriteTimeout { get; set; } = 60000;
    public List<string> Tags { get; set; } = new();
}

public class KongRoute
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public List<string> Paths { get; set; } = new();
    public List<string> Methods { get; set; } = new();
    public List<string> Hosts { get; set; } = new();
    public string? ServiceId { get; set; }
    public bool StripPath { get; set; } = true;
    public bool PreserveHost { get; set; } = false;
    public List<string> Tags { get; set; } = new();
}

public class KongPlugin
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, object> Config { get; set; } = new();
    public string? ServiceId { get; set; }
    public string? ConsumerId { get; set; }
    public string? RouteId { get; set; }
    public List<string> Tags { get; set; } = new();
}

// Credentials
public class KeyAuthCredential
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string ConsumerId { get; set; } = string.Empty;
    public long CreatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class JwtCredential
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "HS256";
    public string ConsumerId { get; set; } = string.Empty;
    public long CreatedAt { get; set; }
}

public class BasicAuthCredential
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string ConsumerId { get; set; } = string.Empty;
    public long CreatedAt { get; set; }
}

public class HmacCredential
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string ConsumerId { get; set; } = string.Empty;
    public long CreatedAt { get; set; }
}

public class OAuth2Credential
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public List<string> RedirectUris { get; set; } = new();
    public string ConsumerId { get; set; } = string.Empty;
    public long CreatedAt { get; set; }
}

public class AclGroup
{
    public string Id { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string ConsumerId { get; set; } = string.Empty;
}

// Requests
public class CreateServiceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string? Path { get; set; }
    public int Port { get; set; } = 80;
    public string Protocol { get; set; } = "http";
    public List<string> Tags { get; set; } = new();
}

public class CreateRouteRequest
{
    public string? Name { get; set; }
    public List<string> Paths { get; set; } = new();
    public List<string> Methods { get; set; } = new();
    public List<string> Hosts { get; set; } = new();
    public bool StripPath { get; set; } = true;
}

public class CreatePluginRequest
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Config { get; set; } = new();
    public string? ServiceId { get; set; }
    public string? ConsumerId { get; set; }
    public string? RouteId { get; set; }
}
