using ExpenseTracker.Api.Hal;

namespace ExpenseTracker.Api.Hal;

/// <summary>IResult extensions for emitting HAL documents with the correct media type.</summary>
public static class HalResults
{
    /// <summary>Returns a <c>200 OK</c> HAL document response with <c>application/hal+json</c>.</summary>
    public static IResult Hal(this IResultExtensions _, HalDocument document) =>
        Results.Text(document.ToJson(), HalDocument.MediaType, System.Text.Encoding.UTF8);

    /// <summary>Returns a HAL document response with a custom status code (e.g. 201).</summary>
    public static IResult Hal(this IResultExtensions _, HalDocument document, int statusCode) =>
        Results.Text(document.ToJson(), HalDocument.MediaType, System.Text.Encoding.UTF8, statusCode);
}