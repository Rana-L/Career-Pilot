using System.Net;
using System.Text;
using CareerPilot.Api.Controllers;
using CareerPilot.Api.dto;
using Microsoft.AspNetCore.Mvc;
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

    private static JobSearchController CreateController(HttpStatusCode statusCode, string responseBody)
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

        return new JobSearchController(factoryMock.Object, config);
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
}
