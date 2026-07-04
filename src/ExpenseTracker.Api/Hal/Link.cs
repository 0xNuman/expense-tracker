namespace ExpenseTracker.Api.Hal;

/// <summary>
/// Represents a hyperlink relation in a HAL document (RFC 5988 / draft-kelly-json-hal).
/// </summary>
public sealed record Link
{
    public required string Href { get; init; }

    public string? Method { get; init; }

    public string? Title { get; init; }

    public string? Type { get; init; }

    public string? Name { get; init; }

    public bool? Templated { get; init; }

    public static Link Get(string href, string? title = null) => new()
    {
        Href = href,
        Method = "GET",
        Title = title
    };

    public static Link Post(string href, string? title = null) => new()
    {
        Href = href,
        Method = "POST",
        Title = title
    };
}