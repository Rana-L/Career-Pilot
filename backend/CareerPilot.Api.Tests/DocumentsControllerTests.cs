using CareerPilot.Api.Controllers;
using CareerPilot.Api.dto;
using CareerPilot.Api.services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CareerPilot.Api.Tests;

public class DocumentsControllerTests
{
    private static DocumentsController CreateController()
    {
        var controller = new DocumentsController(new DocumentGenerator());
        TestHelpers.SetUser(controller, userId: 1);
        return controller;
    }

    [Fact]
    public void GeneratePdf_WithEmptyMarkdown_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = controller.GeneratePdf(new DocumentRequest { Markdown = "" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GenerateDocx_WithEmptyMarkdown_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = controller.GenerateDocx(new DocumentRequest { Markdown = "   " });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GeneratePdf_WithContent_ReturnsPdfFile()
    {
        var controller = CreateController();

        var result = controller.GeneratePdf(new DocumentRequest
        {
            Markdown = "# Title\n\nSome body text.",
            FileName = "my-doc",
        });

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("my-doc.pdf", file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }

    [Fact]
    public void GenerateDocx_WithContent_ReturnsDocxFile()
    {
        var controller = CreateController();

        var result = controller.GenerateDocx(new DocumentRequest
        {
            Markdown = "# Title\n\nSome body text.",
            FileName = "my-doc",
        });

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            file.ContentType);
        Assert.Equal("my-doc.docx", file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }

    [Fact]
    public void GeneratePdf_WithoutFileName_FallsBackToDefaultName()
    {
        var controller = CreateController();

        var result = controller.GeneratePdf(new DocumentRequest { Markdown = "# Title" });

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("document.pdf", file.FileDownloadName);
    }
}
