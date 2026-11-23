using Duende.IdentityServer.Models;

namespace Platform.IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile()
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new("orders.read", "Read Orders"),
            new("orders.write", "Write Orders"),
            new("products.read", "Read Products/Catalog"),
            new("products.write", "Write Products/Catalog"),
            new("payments.read", "Read Payments"),
            new("payments.process", "Process Payments"),
            new("payments.refund", "Refund Payments")
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new ApiResource[]
        {
            new("orders_api", "Orders Service API")
            {
                Scopes = { "orders.read", "orders.write" }
            },
            new("products_api", "Products Service API")
            {
                Scopes = { "products.read", "products.write" }
            },
            new("payments_api", "Payments Service API")
            {
                Scopes = { "payments.read", "payments.process", "payments.refund" }
            }
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // 1. Postman: Authorization Code + Client Secret (К8)
            new()
            {
                ClientId = "postman",
                ClientName = "Postman OAuth 2.0 Client",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = false,
                RequireClientSecret = true,
                ClientSecrets = { new Secret("postman-secret".Sha256()) },
                RedirectUris =
                {
                    "https://oauth.pstmn.io/v1/callback",
                    "http://localhost:5000/oauth/callback"
                },
                AllowedScopes =
                {
                    "openid",
                    "profile",
                    "orders.read",
                    "orders.write",
                    "products.read",
                    "products.write",
                    "payments.read",
                    "payments.process",
                    "payments.refund"
                },
                AllowOfflineAccess = true,
                AccessTokenLifetime = 3600
            },

            // 2. Aggregator Service: Client Credentials M2M (К8)
            new()
            {
                ClientId = "aggregator_service",
                ClientName = "Aggregator M2M Client",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                RequireClientSecret = true,
                ClientSecrets = { new Secret("aggregator-secret".Sha256()) },
                AllowedScopes =
                {
                    "orders.read",
                    "products.read"
                },
                AccessTokenLifetime = 3600
            },

            // 3. Swagger: Authorization Code + PKCE, Public client (RequireClientSecret = false, RequirePkce = true) (К8)
            new()
            {
                ClientId = "swagger",
                ClientName = "Swagger UI PKCE Client",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                RedirectUris =
                {
                    "http://localhost:5000/swagger/oauth2-redirect.html",
                    "http://localhost:5001/swagger/oauth2-redirect.html",
                    "http://localhost:5002/swagger/oauth2-redirect.html",
                    "http://localhost:5003/swagger/oauth2-redirect.html",
                    "http://localhost:5005/swagger/oauth2-redirect.html"
                },
                AllowedCorsOrigins =
                {
                    "http://localhost:5000",
                    "http://localhost:5001",
                    "http://localhost:5002",
                    "http://localhost:5003",
                    "http://localhost:5005"
                },
                AllowedScopes =
                {
                    "openid",
                    "profile",
                    "orders.read",
                    "orders.write",
                    "products.read",
                    "products.write",
                    "payments.read",
                    "payments.process",
                    "payments.refund"
                },
                AllowAccessTokensViaBrowser = true,
                AccessTokenLifetime = 3600
            }
        };
}
