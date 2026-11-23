using System.Collections.Generic;
using Pulumi;
using Keycloak = Pulumi.Keycloak;

return await Deployment.RunAsync(() =>
{
    // 1. Realm: DevRealmDrivers (К11)
    var realm = new Keycloak.Realm("dev-realm", new Keycloak.RealmArgs
    {
        RealmName = "DevRealmDrivers",
        Enabled = true,
        DisplayName = "Dev Realm Drivers Microservices",
        RememberMe = true,
        ResetPasswordAllowed = true
    });

    // 2. Realm Roles (К11): admin, manager, user, guest
    var adminRole = new Keycloak.Role("role-admin", new Keycloak.RoleArgs
    {
        RealmId = realm.Id,
        Name = "admin",
        Description = "Platform administrator with full permissions"
    });

    var managerRole = new Keycloak.Role("role-manager", new Keycloak.RoleArgs
    {
        RealmId = realm.Id,
        Name = "manager",
        Description = "Business manager with order approval and product update permissions"
    });

    var userRole = new Keycloak.Role("role-user", new Keycloak.RoleArgs
    {
        RealmId = realm.Id,
        Name = "user",
        Description = "Regular customer user"
    });

    var guestRole = new Keycloak.Role("role-guest", new Keycloak.RoleArgs
    {
        RealmId = realm.Id,
        Name = "guest",
        Description = "Guest user with public catalog read permissions"
    });

    // 3. Client Scopes & Resource Servers (К11)
    // Orders Scopes: orders:read, orders:create, orders:update, orders:delete, orders:approve
    string[] orderScopes = ["orders:read", "orders:create", "orders:update", "orders:delete", "orders:approve"];
    foreach (var scopeName in orderScopes)
    {
        _ = new Keycloak.OpenId.ClientScope($"scope-{scopeName.Replace(':', '-')}", new Keycloak.OpenId.ClientScopeArgs
        {
            RealmId = realm.Id,
            Name = scopeName,
            Description = $"Scope for {scopeName}"
        });
    }

    // Products Scopes: products:read, products:create, products:update, products:delete, products:publish
    string[] productScopes = ["products:read", "products:create", "products:update", "products:delete", "products:publish"];
    foreach (var scopeName in productScopes)
    {
        _ = new Keycloak.OpenId.ClientScope($"scope-{scopeName.Replace(':', '-')}", new Keycloak.OpenId.ClientScopeArgs
        {
            RealmId = realm.Id,
            Name = scopeName,
            Description = $"Scope for {scopeName}"
        });
    }

    // Payments Scopes: payments:read, payments:process, payments:refund (Table 7: Admin ONLY)
    string[] paymentScopes = ["payments:read", "payments:process", "payments:refund"];
    foreach (var scopeName in paymentScopes)
    {
        _ = new Keycloak.OpenId.ClientScope($"scope-{scopeName.Replace(':', '-')}", new Keycloak.OpenId.ClientScopeArgs
        {
            RealmId = realm.Id,
            Name = scopeName,
            Description = $"Scope for {scopeName} (Admin only)"
        });
    }

    // Resource server clients (orders-api, products-api, payments-api)
    var ordersApiClient = new Keycloak.OpenId.Client("res-orders-api", new Keycloak.OpenId.ClientArgs
    {
        RealmId = realm.Id,
        ClientId = "orders-api",
        Name = "Orders Service API",
        AccessType = "BEARER-ONLY",
        Enabled = true
    });

    var productsApiClient = new Keycloak.OpenId.Client("res-products-api", new Keycloak.OpenId.ClientArgs
    {
        RealmId = realm.Id,
        ClientId = "products-api",
        Name = "Products Service API",
        AccessType = "BEARER-ONLY",
        Enabled = true
    });

    var paymentsApiClient = new Keycloak.OpenId.Client("res-payments-api", new Keycloak.OpenId.ClientArgs
    {
        RealmId = realm.Id,
        ClientId = "payments-api",
        Name = "Payments Service API",
        AccessType = "BEARER-ONLY",
        Enabled = true
    });

    // 4. Clients (К12): postman, swagger, aggregator_service
    var postmanClient = new Keycloak.OpenId.Client("client-postman", new Keycloak.OpenId.ClientArgs
    {
        RealmId = realm.Id,
        ClientId = "postman",
        Name = "Postman Client",
        AccessType = "CONFIDENTIAL",
        ClientSecret = "postman-secret",
        StandardFlowEnabled = true,
        DirectAccessGrantsEnabled = true,
        ValidRedirectUris = { "https://oauth.pstmn.io/v1/callback" },
        Enabled = true
    });

    var swaggerClient = new Keycloak.OpenId.Client("client-swagger", new Keycloak.OpenId.ClientArgs
    {
        RealmId = realm.Id,
        ClientId = "swagger",
        Name = "Swagger UI PKCE Client",
        AccessType = "PUBLIC",
        StandardFlowEnabled = true,
        DirectAccessGrantsEnabled = true,
        PkceCodeChallengeMethod = "S256",
        ValidRedirectUris =
        {
            "http://localhost:5000/swagger/oauth2-redirect.html",
            "http://localhost:5001/swagger/oauth2-redirect.html"
        },
        WebOrigins = { "*" },
        Enabled = true
    });

    var aggregatorClient = new Keycloak.OpenId.Client("client-aggregator", new Keycloak.OpenId.ClientArgs
    {
        RealmId = realm.Id,
        ClientId = "aggregator_service",
        Name = "Aggregator M2M Client",
        AccessType = "CONFIDENTIAL",
        ClientSecret = "aggregator-secret",
        ServiceAccountsEnabled = true,
        StandardFlowEnabled = false,
        DirectAccessGrantsEnabled = false,
        Enabled = true
    });

    // Protocol mapper for authorization.permissions claim (К12)
    _ = new Keycloak.OpenId.UserClientRoleProtocolMapper("mapper-permissions", new Keycloak.OpenId.UserClientRoleProtocolMapperArgs
    {
        RealmId = realm.Id,
        ClientId = postmanClient.Id,
        Name = "authorization-permissions-mapper",
        ClaimName = "authorization.permissions",
        AddToIdToken = true,
        AddToAccessToken = true,
        AddToUserinfo = true,
        Multivalued = true
    });

    // 5. Test Users (К12): alice (admin), john.manager (manager), bob (user), guest (guest)
    var userAlice = new Keycloak.User("user-alice", new Keycloak.UserArgs
    {
        RealmId = realm.Id,
        Username = "alice",
        Email = "alice@dev.local",
        FirstName = "Alice",
        LastName = "Admin",
        Enabled = true,
        InitialPassword = new Keycloak.Inputs.UserInitialPasswordArgs
        {
            Value = "Alice123!Secure",
            Temporary = false
        }
    });

    var userJohn = new Keycloak.User("user-john-manager", new Keycloak.UserArgs
    {
        RealmId = realm.Id,
        Username = "john.manager",
        Email = "john.manager@dev.local",
        FirstName = "John",
        LastName = "Manager",
        Enabled = true,
        InitialPassword = new Keycloak.Inputs.UserInitialPasswordArgs
        {
            Value = "Manager123!Secure",
            Temporary = false
        }
    });

    var userBob = new Keycloak.User("user-bob", new Keycloak.UserArgs
    {
        RealmId = realm.Id,
        Username = "bob",
        Email = "bob@dev.local",
        FirstName = "Bob",
        LastName = "User",
        Enabled = true,
        InitialPassword = new Keycloak.Inputs.UserInitialPasswordArgs
        {
            Value = "Bob123!Secure",
            Temporary = false
        }
    });

    var userGuest = new Keycloak.User("user-guest", new Keycloak.UserArgs
    {
        RealmId = realm.Id,
        Username = "guest",
        Email = "guest@dev.local",
        FirstName = "Guest",
        LastName = "Visitor",
        Enabled = true,
        InitialPassword = new Keycloak.Inputs.UserInitialPasswordArgs
        {
            Value = "Guest123!Secure",
            Temporary = false
        }
    });

    // User Role Mappings
    _ = new Keycloak.UserRoles("mapping-alice-admin", new Keycloak.UserRolesArgs
    {
        RealmId = realm.Id,
        UserId = userAlice.Id,
        RoleIds = { adminRole.Id }
    });

    _ = new Keycloak.UserRoles("mapping-john-manager", new Keycloak.UserRolesArgs
    {
        RealmId = realm.Id,
        UserId = userJohn.Id,
        RoleIds = { managerRole.Id }
    });

    _ = new Keycloak.UserRoles("mapping-bob-user", new Keycloak.UserRolesArgs
    {
        RealmId = realm.Id,
        UserId = userBob.Id,
        RoleIds = { userRole.Id }
    });

    _ = new Keycloak.UserRoles("mapping-guest", new Keycloak.UserRolesArgs
    {
        RealmId = realm.Id,
        UserId = userGuest.Id,
        RoleIds = { guestRole.Id }
    });

    // Outputs (К12)
    return new Dictionary<string, object?>
    {
        ["RealmUrl"] = Output.Format($"http://localhost:8080/realms/{realm.RealmName}"),
        ["AdminConsoleUrl"] = Output.Format($"http://localhost:8080/admin/{realm.RealmName}/console"),
        ["ClientIds"] = new Dictionary<string, Output<string>>
        {
            ["Postman"] = postmanClient.ClientId,
            ["Swagger"] = swaggerClient.ClientId,
            ["Aggregator"] = aggregatorClient.ClientId,
            ["OrdersApi"] = ordersApiClient.ClientId,
            ["ProductsApi"] = productsApiClient.ClientId,
            ["PaymentsApi"] = paymentsApiClient.ClientId
        },
        ["ClientSecrets"] = new Dictionary<string, object?>
        {
            ["Postman"] = postmanClient.ClientSecret,
            ["Aggregator"] = aggregatorClient.ClientSecret
        }
    };
});
