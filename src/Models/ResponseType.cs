namespace FiveStarSupport.Models;

/// <summary>
/// Response type (bug, feature request, etc.)
/// </summary>
public record ResponseType(
    string Id,
    string Name,
    string Slug,
    string Color,
    string Icon
)
{
    /// <summary>
    /// Creates a ResponseType from JSON data.
    /// </summary>
    public static ResponseType FromJson(JsonElement json) => new(
        Id: json.GetProperty("id").GetString() ?? string.Empty,
        Name: json.GetProperty("name").GetString() ?? string.Empty,
        Slug: json.GetProperty("slug").GetString() ?? string.Empty,
        Color: json.GetProperty("color").GetString() ?? string.Empty,
        Icon: json.GetProperty("icon").GetString() ?? string.Empty
    );
}
