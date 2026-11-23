using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public class KeycloakResource : ContainerResource, IResourceWithEndpoints
{
    public KeycloakResource(string name) : base(name)
    {
    }
}

public static class KeycloakHostingExtensions
{
    /// <summary>
    /// Trajectory B: Aspire container integration for Keycloak (Критерій 10).
    /// </summary>
    public static IResourceBuilder<KeycloakResource> AddKeycloak(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = 8080)
    {
        var resource = new KeycloakResource(name);
        return builder.AddResource(resource)
            .WithImage("quay.io/keycloak/keycloak")
            .WithImageTag("26.0.0")
            .WithHttpEndpoint(port: port, targetPort: 8080, name: "http")
            .WithEnvironment("KEYCLOAK_ADMIN", "admin")
            .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
            .WithArgs("start-dev");
    }

    /// <summary>
    /// Configures persistent data volume for Keycloak container.
    /// </summary>
    public static IResourceBuilder<KeycloakResource> WithDataVolume(
        this IResourceBuilder<KeycloakResource> builder,
        string? name = null)
    {
        return builder.WithVolume(name ?? $"{builder.Resource.Name}-data", "/opt/keycloak/data");
    }
}
