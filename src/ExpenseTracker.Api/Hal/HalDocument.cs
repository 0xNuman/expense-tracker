using System.Text.Json;
using System.Text.Json.Nodes;

namespace ExpenseTracker.Api.Hal;

/// <summary>
/// A minimal HAL document model supporting <c>_links</c> and <c>_embedded</c>.
/// Renders as JSON per draft-kelly-json-hal with the <c>application/hal+json</c> media type.
/// </summary>
public class HalDocument
{
    private readonly Dictionary<string, object> _links = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object> _embedded = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _state = new(StringComparer.Ordinal);

    public const string MediaType = "application/hal+json";

    /// <summary>Adds or replaces a single link under the given rel.</summary>
    public HalDocument WithLink(string rel, Link link)
    {
        _links[rel] = link;
        return this;
    }

    /// <summary>Adds or replaces an array of links under the given rel.</summary>
    public HalDocument WithLinks(string rel, IReadOnlyCollection<Link> links)
    {
        _links[rel] = links;
        return this;
    }

    /// <summary>Adds a Curies definition allowing custom rel prefixes.</summary>
    public HalDocument WithCuries(IEnumerable<Link> curies)
    {
        _links["curies"] = curies.ToList();
        return this;
    }

    /// <summary>Adds an embedded resource under the given rel.</summary>
    public HalDocument WithEmbedded(string rel, HalDocument document)
    {
        _embedded[rel] = document;
        return this;
    }

    /// <summary>Adds an embedded collection under the given rel.</summary>
    public HalDocument WithEmbedded(string rel, IReadOnlyCollection<HalDocument> documents)
    {
        _embedded[rel] = documents;
        return this;
    }

    /// <summary>Adds a state property. Keys beginning with <c>_</c> are reserved.</summary>
    public HalDocument WithState(string key, object? value)
    {
        if (key.StartsWith('_'))
        {
            throw new ArgumentException("State keys beginning with '_' are reserved for HAL.", nameof(key));
        }
        _state[key] = value;
        return this;
    }

    public JsonNode ToJsonNode(JsonSerializerOptions? options = null)
    {
        options ??= HalJson.SerializerOptions;
        var node = new JsonObject();

        foreach (var (key, value) in _state)
        {
            node[key] = JsonSerializer.SerializeToNode(value, options);
        }

        if (_links.Count > 0)
        {
            node["_links"] = JsonSerializer.SerializeToNode(_links, options);
        }

        if (_embedded.Count > 0)
        {
            var embeddedNode = new JsonObject();
            foreach (var (key, value) in _embedded)
            {
                if (value is HalDocument singleDoc)
                {
                    embeddedNode[key] = singleDoc.ToJsonNode(options);
                }
                else if (value is IEnumerable<HalDocument> docCollection)
                {
                    var array = new JsonArray();
                    foreach (var doc in docCollection)
                    {
                        array.Add(doc.ToJsonNode(options));
                    }
                    embeddedNode[key] = array;
                }
                else
                {
                    embeddedNode[key] = JsonSerializer.SerializeToNode(value, options);
                }
            }
            node["_embedded"] = embeddedNode;
        }

        return node;
    }

    public string ToJson(JsonSerializerOptions? options = null) => ToJsonNode(options).ToJsonString(options);
}