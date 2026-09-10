using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CareerPilot.Api.services;

public class DocumentGenerator
{
    public byte[] GeneratePdf(string markdown)
    {
        var parsed = Markdown.Parse(markdown);
        
        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    foreach (var block in parsed)
                    {
                        RenderPdfBlock(col, block);
                    }
                });
            });
        }).GeneratePdf();
    }

    private void RenderPdfBlock(ColumnDescriptor col, Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                var size = heading.Level switch { 1 => 20f, 2 => 14f, _ => 12f };
                col.Item().PaddingTop(heading.Level == 1 ? 0 : 8)
                   .Text(GetText(heading.Inline)).FontSize(size).Bold();
                break;

            case ParagraphBlock para:
                col.Item().Text(GetText(para.Inline));
                break;

            case ListBlock list:
                foreach (var item in list)
                {
                    if (item is not ListItemBlock listItem) continue;
                    foreach (var sub in listItem)
                    {
                        if (sub is not ParagraphBlock p) continue;
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(12).Text("•");
                            row.RelativeItem().Text(GetText(p.Inline));
                        });
                    }
                }
                break;
        }
    }

    public byte[] GenerateDocx(string markdown)
    {
        var parsed = Markdown.Parse(markdown);
        using var stream = new MemoryStream();

        using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            
            var body = mainPart.Document.AppendChild(new Body());

            foreach (var block in parsed)
            {
                RenderDocxBlock(body, block);
            }
        }

        return stream.ToArray();
    }

    private void RenderDocxBlock(Body body, Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                var sizeVal = heading.Level switch { 1 => "40", 2 => "28", _ => "24" };
                var run = new Run(new Text(GetText(heading.Inline)));
                run.RunProperties = new RunProperties(new Bold(), new FontSize { Val = sizeVal });
                body.Append(new Paragraph(run));
                break;

            case ParagraphBlock para:
                body.Append(new Paragraph(new Run(
                    new Text(GetText(para.Inline)) { Space = SpaceProcessingModeValues.Preserve })));
                break;

            case ListBlock list:
                foreach (var item in list)
                {
                    if (item is not ListItemBlock listItem) continue;
                    foreach (var sub in listItem)
                    {
                        if (sub is not ParagraphBlock p) continue;
                        body.Append(new Paragraph(new Run(
                            new Text("• " + GetText(p.Inline)) { Space = SpaceProcessingModeValues.Preserve })));
                    }
                }
                break;
        }
    }

    private string GetText(ContainerInline? inline)
    {
        if (inline == null) return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var node in inline)
        {
            switch (node)
            {
                case LiteralInline literal:
                    sb.Append(literal.Content.ToString());
                    break;
                case EmphasisInline emphasis:
                    sb.Append(GetText(emphasis));
                    break;
                case LineBreakInline:
                    sb.Append(' ');
                    break;
                default:
                    sb.Append(node.ToString());
                    break;
            }
        }
        return sb.ToString();
    }
}
