using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Application.Tenants.DTOs;
using SportFlow.Integration.Tests.Identity;

namespace SportFlow.Integration.Tests.Tenants;

public class TenantsIntegrationTests : IClassFixture<SportFlowWebAppFactory>
{
    private readonly HttpClient _client;

    public TenantsIntegrationTests(SportFlowWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> LoginAsAsync(string email, string password = "Password1!", string slug = "demo")
    {
        var resp = await _client.PostAsJsonAsync("/v1/auth/login",
            new { Identifier = email, Password = password, TenantSlug = slug });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<TokenResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task GetTenantMe_AuthenticatedUser_Returns200()
    {
        var token = await LoginAsAsync("member@demo.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/v1/tenants/me");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TenantMeResponse>();
        body.ShouldNotBeNull();
        body.Slug.ShouldBe("demo");
    }

    [Fact]
    public async Task CreateLocation_AsMember_Returns403()
    {
        var token = await LoginAsAsync("member@demo.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateLocationRequest("Sala A", null, "America/New_York", null);
        var response = await _client.PostAsJsonAsync("/v1/tenants/me/locations", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateListDeactivate_AsOwner_FullFlow()
    {
        var token = await LoginAsAsync("owner@demo.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create
        var createRequest = new CreateLocationRequest("Sala Integración", "123 Test St", "America/Chicago", 30);
        var createResponse = await _client.PostAsJsonAsync("/v1/tenants/me/locations", createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<LocationResponse>();
        created.ShouldNotBeNull();
        created.Name.ShouldBe("Sala Integración");
        created.IsActive.ShouldBeTrue();

        // List — should contain the newly created location
        var listResponse = await _client.GetAsync("/v1/tenants/me/locations");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var locations = await listResponse.Content.ReadFromJsonAsync<List<LocationResponse>>();
        locations.ShouldNotBeNull();
        locations.ShouldContain(l => l.Id == created.Id);

        // Get by ID
        var getResponse = await _client.GetAsync($"/v1/tenants/me/locations/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Deactivate
        var deleteResponse = await _client.DeleteAsync($"/v1/tenants/me/locations/{created.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // After deactivation, location should no longer appear in list (global filter)
        var listAfter = await _client.GetAsync("/v1/tenants/me/locations");
        var locationsAfter = await listAfter.Content.ReadFromJsonAsync<List<LocationResponse>>();
        locationsAfter.ShouldNotBeNull();
        locationsAfter.ShouldNotContain(l => l.Id == created.Id);
    }
}
