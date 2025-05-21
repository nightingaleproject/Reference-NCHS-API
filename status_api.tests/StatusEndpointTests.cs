namespace status_api.tests;

public class StatusEndpointTests :
    IClassFixture<StatusApiTestsWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly StatusApiTestsWebApplicationFactory<Program>
        _factory;

    public StatusEndpointTests(
        StatusApiTestsWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Get_Status_ReturnsOkResult()
    {
        var response = await _client.GetAsync("/api/v1/status");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_StatusUIIndex_ReturnsOkResult()
    {
        var response = await _client.GetAsync("/StatusUI/index.html");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers?.ContentType?.MediaType);
    }
}