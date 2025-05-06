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
        var response = await _client.GetAsync("/status");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}