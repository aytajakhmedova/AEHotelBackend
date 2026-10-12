using System.Text.Json;

namespace AEHotel.Service.Helpers;

internal static class JsonLists
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static List<string> ReadStrings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(json, Options) ?? [];
    }

    public static string WriteStrings(IEnumerable<string>? values)
    {
        return JsonSerializer.Serialize(values ?? [], Options);
    }

    public static List<T> Read<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<T>>(json, Options) ?? [];
    }

    public static string Write<T>(IEnumerable<T>? values)
    {
        return JsonSerializer.Serialize(values ?? [], Options);
    }
}
