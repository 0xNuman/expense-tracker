using System.Net;
using System.Net.Http.Json;

namespace ExpenseTracker.IntegrationTests;

public class ApiTests : IClassFixture<ExpenseTrackerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiTests(ExpenseTrackerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealthReady_ReturnsOk_WhenDatabaseIsAvailable()
    {
        var response = await _client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRoot_ReturnsHalDocument()
    {
        var response = await _client.GetAsync("/api");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/hal+json");
    }

    private class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
    }
}
