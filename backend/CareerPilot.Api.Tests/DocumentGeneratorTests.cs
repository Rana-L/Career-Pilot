using System.Text;
using CareerPilot.Api.services;
using Xunit;

namespace CareerPilot.Api.Tests;

public class DocumentGeneratorTests
{
    private const string SampleMarkdown = """
        # Jane Smith

        ## Summary
        Backend engineer with cloud experience.

        ## Skills
        - C#
        - PostgreSQL
        """;

    [Fact]
    public void GeneratePdf_ReturnsNonEmptyPdfBytes()
    {
        var generator = new DocumentGenerator();

        var bytes = generator.GeneratePdf(SampleMarkdown);

        Assert.NotEmpty(bytes);
        // PDF files begin with the "%PDF" magic bytes.
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void GenerateDocx_ReturnsNonEmptyDocxBytes()
    {
        var generator = new DocumentGenerator();

        var bytes = generator.GenerateDocx(SampleMarkdown);

        Assert.NotEmpty(bytes);
        // DOCX is a ZIP archive, which begins with "PK".
        Assert.Equal("PK", Encoding.ASCII.GetString(bytes, 0, 2));
    }

    [Fact]
    public void GeneratePdf_HandlesPlainTextWithoutMarkdownStructure()
    {
        var generator = new DocumentGenerator();

        var bytes = generator.GeneratePdf("Just a single line of text.");

        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
