using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using VideoRentingSystem.Api;

namespace VideoRentingSystem.Tests.Api;

[TestClass]
public sealed class MarketplaceApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [TestMethod]
    public async Task Auth_RegisterLoginLogout_ShouldIssueAndRevokeToken()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        // force https base address so middleware does not redirect test requests

        string username = $"cust_{Guid.NewGuid():N}";
        HttpResponseMessage registerResponse = await client.PostAsync(
            "/api/auth/register",
            ToJson(new
            {
                Username = username,
                Password = "password123",
                Role = "Customer"
            }));
        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        AuthPayload auth = await ReadJson<AuthPayload>(registerResponse);
        Assert.IsFalse(string.IsNullOrWhiteSpace(auth.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        HttpResponseMessage logout = await client.PostAsync("/api/auth/logout", content: null);
        Assert.AreEqual(HttpStatusCode.NoContent, logout.StatusCode);

        HttpResponseMessage logoutAgain = await client.PostAsync("/api/auth/logout", content: null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, logoutAgain.StatusCode);
        // second logout should fail because the first call revoked the token
    }

    [TestMethod]
    public async Task PublisherEndpoints_ShouldForbidCustomerRole()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        AuthPayload customer = await RegisterUser(client, "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);

        int videoId = 800000 + Random.Shared.Next(100000);
        HttpResponseMessage createResponse = await client.PostAsync(
            "/api/publisher/videos",
            ToJson(new
            {
                VideoId = videoId,
                Title = "Customer Should Not Publish",
                Genre = "Drama",
                ReleaseYear = 2025,
                Type = "Movie",
                RentalPrice = 2.50m,
                RentalHours = 48,
                IsPublished = true
            }));
        Assert.AreEqual(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }

    [TestMethod]
    public async Task PublisherEndpoints_PublisherCanCreateAndDeleteOwnTitle()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        AuthPayload publisher = await RegisterUser(client, "Publisher");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", publisher.Token);

        int videoId = 900000 + Random.Shared.Next(100000);
        HttpResponseMessage createResponse = await client.PostAsync(
            "/api/publisher/videos",
            ToJson(new
            {
                VideoId = videoId,
                Title = "Publisher Original",
                Genre = "Sci-Fi",
                ReleaseYear = 2026,
                Type = "Series",
                RentalPrice = 3.10m,
                RentalHours = 72,
                IsPublished = true
            }));
        Assert.AreEqual(HttpStatusCode.OK, createResponse.StatusCode);

        HttpResponseMessage deleteResponse = await client.DeleteAsync($"/api/publisher/videos/{videoId}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [TestMethod]
    public async Task Videos_Filter_ShouldReturnOnlyMatchingGenreAndPrice()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync("/api/videos?keyword=Interstellar&genre=Sci-Fi&maxPrice=5");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        VideoPayload[] videos = await ReadJson<VideoPayload[]>(response);
        for (int i = 0; i < videos.Length; i++)
        {
            Assert.IsTrue(videos[i].Genre.Equals("Sci-Fi", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(videos[i].RentalPrice <= 5m);
        }
    }

    [TestMethod]
    public async Task Rentals_RentAndReturn_ShouldTrackMyRentals()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        AuthPayload customer = await RegisterUser(client, "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);

        HttpResponseMessage browse = await client.GetAsync("/api/videos");
        Assert.AreEqual(HttpStatusCode.OK, browse.StatusCode);
        VideoPayload[] videos = await ReadJson<VideoPayload[]>(browse);
        VideoPayload? available = videos.FirstOrDefault(v => !v.IsRented);
        Assert.IsNotNull(available, "Expected at least one available seed title.");

        HttpResponseMessage rent = await client.PostAsync($"/api/rentals/{available.VideoId}/rent", content: null);
        Assert.AreEqual(HttpStatusCode.NoContent, rent.StatusCode);

        HttpResponseMessage mine = await client.GetAsync("/api/rentals/me");
        Assert.AreEqual(HttpStatusCode.OK, mine.StatusCode);
        RentalPayload[] rentals = await ReadJson<RentalPayload[]>(mine);
        Assert.IsTrue(rentals.Any(r => r.VideoId == available.VideoId));

        HttpResponseMessage ret = await client.PostAsync($"/api/rentals/{available.VideoId}/return", content: null);
        Assert.AreEqual(HttpStatusCode.NoContent, ret.StatusCode);
    }

    private static StringContent ToJson(object payload)
    {
        return new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    }

    private static async Task<T> ReadJson<T>(HttpResponseMessage response)
    {
        await using Stream stream = await response.Content.ReadAsStreamAsync();
        T? value = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
        Assert.IsNotNull(value);
        return value;
    }

    private static async Task<AuthPayload> RegisterUser(HttpClient client, string role)
    {
        string username = $"{role.ToLowerInvariant()}_{Guid.NewGuid():N}";
        HttpResponseMessage registerResponse = await client.PostAsync(
            "/api/auth/register",
            ToJson(new
            {
                Username = username,
                Password = "password123",
                Role = role,
                StudioName = role == "Publisher" ? "Test Studio" : null
            }));
        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);
        return await ReadJson<AuthPayload>(registerResponse);
    }

    private sealed class AuthPayload
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    private sealed class VideoPayload
    {
        public int VideoId { get; set; }
        public string Genre { get; set; } = string.Empty;
        public decimal RentalPrice { get; set; }
        public bool IsRented { get; set; }
    }

    private sealed class RentalPayload
    {
        public int VideoId { get; set; }
    }
}
