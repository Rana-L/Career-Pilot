using System.Net;
using System.Text;
using CareerPilot.Api.Controllers;
using CareerPilot.Api.data;
using CareerPilot.Api.dto;
using CareerPilot.Api.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CareerPilot.Api.Tests;

public class JobSearchControllerTests
{
    // A minimal fake handler so tests never make a real network call — the
    // controller gets whatever canned response/status we hand it here.
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }

    private static JobSearchController CreateController(
        HttpStatusCode statusCode,
        string responseBody,
        AppDbContext? context = null,
        int userId = 1)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Adzuna:AppId"] = "test-app-id",
            ["Adzuna:AppKey"] = "test-app-key",
        }).Build();

        var controller = new JobSearchController(factoryMock.Object, config, context ?? TestHelpers.CreateInMemoryContext());
        TestHelpers.SetUser(controller, userId);
        return controller;
    }

    private const string SampleAdzunaResponse = """
        {
          "results": [
            {
              "title": "Backend Engineer",
              "description": "Build APIs with C# and PostgreSQL.",
              "created": "2026-01-15T10:00:00Z",
              "redirect_url": "https://example.com/jobs/123",
              "company": { "display_name": "Acme Corp" },
              "location": { "display_name": "Manchester, Greater Manchester" }
            }
          ]
        }
        """;

    [Fact]
    public async Task Search_WithEmptyTitle_ReturnsBadRequest()
    {
        var controller = CreateController(HttpStatusCode.OK, SampleAdzunaResponse);

        var result = await controller.Search(title: "", location: "manchester");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_WithEmptyLocation_ReturnsBadRequest()
    {
        var controller = CreateController(HttpStatusCode.OK, SampleAdzunaResponse);

        var result = await controller.Search(title: "developer", location: "  ");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_MapsAdzunaResultsCorrectly()
    {
        var controller = CreateController(HttpStatusCode.OK, SampleAdzunaResponse);

        var result = await controller.Search(title: "developer", location: "manchester");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var results = Assert.IsAssignableFrom<List<JobSearchResult>>(ok.Value);
        var job = Assert.Single(results);

        Assert.Equal("Backend Engineer", job.Title);
        Assert.Equal("Acme Corp", job.CompanyName);
        Assert.Equal("Manchester, Greater Manchester", job.Location);
        Assert.Equal("https://example.com/jobs/123", job.Url);
        Assert.Equal(new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), job.Created);
    }

    [Fact]
    public async Task Search_WhenProviderReturnsError_Returns502()
    {
        var controller = CreateController(HttpStatusCode.InternalServerError, "{}");

        var result = await controller.Search(title: "developer", location: "manchester");

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, status.StatusCode);
    }

    [Fact]
    public async Task GetSaved_WhenNoneExists_ReturnsNoContent()
    {
        var controller = CreateController(HttpStatusCode.OK, SampleAdzunaResponse);

        var result = await controller.GetSaved();

        Assert.IsType<NoContentResult>(result.Result);
    }

    [Fact]
    public async Task SaveSearch_ThenGetSaved_ReturnsWhatWasSaved()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var controller = CreateController(HttpStatusCode.OK, SampleAdzunaResponse, context, userId: 1);

        await controller.SaveSearch(new SavedJobSearchRequest
        {
            Title = "Backend Engineer",
            Location = "Manchester",
            RadiusMiles = 25,
        });

        var result = await controller.GetSaved();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var saved = Assert.IsType<SavedJobSearchResponse>(ok.Value);
        Assert.Equal("Backend Engineer", saved.Title);
        Assert.Equal("Manchester", saved.Location);
        Assert.Equal(25, saved.RadiusMiles);
    }

    [Fact]
    public async Task SaveSearch_CalledTwice_UpdatesInPlaceInsteadOfDuplicating()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var controller = CreateController(HttpStatusCode.OK, SampleAdzunaResponse, context, userId: 1);

        await controller.SaveSearch(new SavedJobSearchRequest
        {
            Title = "Backend Engineer",
            Location = "Manchester",
            RadiusMiles = 10,
        });
        await controller.SaveSearch(new SavedJobSearchRequest
        {
            Title = "Frontend Engineer",
            Location = "Leeds",
            RadiusMiles = 50,
        });

        Assert.Equal(1, await context.SavedJobSearches.CountAsync());

        var result = await controller.GetSaved();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var saved = Assert.IsType<SavedJobSearchResponse>(ok.Value);
        Assert.Equal("Frontend Engineer", saved.Title);
        Assert.Equal("Leeds", saved.Location);
        Assert.Equal(50, saved.RadiusMiles);
    }

    [Fact]
    public async Task GetSaved_OnlyReturnsCurrentUsersSearch()
    {
        var context = TestHelpers.CreateInMemoryContext();
        context.SavedJobSearches.Add(new SavedJobSearch
        {
            UserId = 2,
            Title = "Their Search",
            Location = "Leeds",
            RadiusMiles = 10,
        });
        await context.SaveChangesAsync();

        var controller = CreateController(HttpStatusCode.OK, SampleAdzunaResponse, context, userId: 1);

        var result = await controller.GetSaved();

        Assert.IsType<NoContentResult>(result.Result);
    }
}
