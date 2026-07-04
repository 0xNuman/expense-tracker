using System.Text.Json;

namespace ExpenseTracker.Api.Hal;

/// <summary>Shared HAL JSON serialization configuration.</summary>
public static class HalJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}