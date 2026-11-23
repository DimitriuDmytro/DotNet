namespace Platform.EventBus.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Єдині стандартизовані налаштування System.Text.Json для publisher і consumer (Критерій 5).
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
