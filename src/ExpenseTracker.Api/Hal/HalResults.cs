using ExpenseTracker.Api.Hal;

namespace ExpenseTracker.Api.Hal;

/// <summary>IResult extensions for emitting HAL documents with the correct media type.</summary>
public static class HalResults
{
    /// <summary>Returns a <c>200 OK</c> HAL document response with <c>application/hal+json</c>.</summary>
    public static IResult Hal(this IResultExtensions _, HalDocument document) =>
        Results.Text(document.ToJson(), HalDocument.MediaType, System.Text.Encoding.UTF8);
}