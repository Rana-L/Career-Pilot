using CareerPilot.Api.dto;
using CareerPilot.Api.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerPilot.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly DocumentGenerator _generator;

    public DocumentsController(DocumentGenerator generator)
    {
        _generator = generator;
    }

    [HttpPost("pdf")]
    public IActionResult GeneratePdf(DocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Markdown))
            return BadRequest("No content provided.");

        var bytes = _generator.GeneratePdf(request.Markdown);
        var fileName = string.IsNullOrWhiteSpace(request.FileName) ? "document" : request.FileName;
        return File(bytes, "application/pdf", $"{fileName}.pdf");
    }

    [HttpPost("docx")]
    public IActionResult GenerateDocx(DocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Markdown))
            return BadRequest("No content provided.");

        var bytes = _generator.GenerateDocx(request.Markdown);
        var fileName = string.IsNullOrWhiteSpace(request.FileName) ? "document" : request.FileName;
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            $"{fileName}.docx");
    }
}
