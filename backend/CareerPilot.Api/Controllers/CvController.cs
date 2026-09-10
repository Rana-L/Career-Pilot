using CareerPilot.Api.data;
using CareerPilot.Api.dto;
using CareerPilot.Api.models;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;
using System.Text.Json;


namespace CareerPilot.Api.Controllers;

[ApiController]
[Route("api/cv")]
[Authorize]
public class CvController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _configuration;

    private readonly ChatClient _chatClient;

    public CvController(AppDbContext context, IAmazonS3 s3Client, IConfiguration configuration, ChatClient chatClient)
    {
        _context = context;
        _s3Client = s3Client;
        _configuration = configuration;
        _chatClient = chatClient;
    }

    private int GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return int.Parse(sub!);
    }

    private async Task<string> ExtractCvTextAsync(Cv cv)
    {
        var bucketName = _configuration["Aws:BucketName"];
        var getRequest = new GetObjectRequest { BucketName = bucketName, Key = cv.S3Url };
        using var response = await _s3Client.GetObjectAsync(getRequest);
        using var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        if (cv.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            using var pdf = PdfDocument.Open(fileBytes);
            var textBuilder = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }
            return textBuilder.ToString();
        }

        if (cv.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        {
            using var docStream = new MemoryStream(fileBytes);
            using var wordDoc = WordprocessingDocument.Open(docStream, false);
            var body = wordDoc.MainDocumentPart?.Document?.Body;
            var textBuilder = new StringBuilder();
            if (body != null)
            {
                foreach (var paragraph in body.Descendants<Paragraph>())
                {
                    textBuilder.AppendLine(paragraph.InnerText);
                }
            }
            return textBuilder.ToString();
        }

        return Encoding.UTF8.GetString(fileBytes);
    }

    private static readonly string[] AllowedCvExtensions = [".pdf", ".docx", ".txt"];
    private const long MaxCvFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly TimeSpan AnalyzeCooldown = TimeSpan.FromSeconds(60);

    [HttpPost("upload")]
    public async Task<ActionResult<CvResponse>> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (file.Length > MaxCvFileSizeBytes)
            return BadRequest("File is too large. Maximum size is 5 MB.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedCvExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Unsupported file type. Please upload a PDF, DOCX, or TXT file.");

        var userId = GetUserId();
        var bucketName = _configuration["Aws:BucketName"];
        var objectKey = $"cvs/{userId}/{Guid.NewGuid()}-{file.FileName}";

        using (var stream = file.OpenReadStream())
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                InputStream = stream,
                ContentType = file.ContentType
            };
            await _s3Client.PutObjectAsync(putRequest);
        }

        var cv = new Cv
        {
            UserId = userId,
            FileName = file.FileName,
            S3Url = objectKey,
            UploadedAt = DateTime.UtcNow
        };

        _context.Cvs.Add(cv);
        await _context.SaveChangesAsync();

        return Ok(new CvResponse
        {
            Id = cv.Id,
            FileName = cv.FileName,
            UploadedAt = cv.UploadedAt
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<CvResponse>>> GetAll()
    {
        var userId = GetUserId();
        var cvs = await _context.Cvs
            .Where(c => c.UserId == userId)
            .Select(c => new CvResponse
            {
                Id = c.Id,
                FileName = c.FileName,
                UploadedAt = c.UploadedAt
            })
            .ToListAsync();

        return Ok(cvs);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var cv = await _context.Cvs.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (cv == null) return NotFound();

        var bucketName = _configuration["Aws:BucketName"];
        await _s3Client.DeleteObjectAsync(bucketName, cv.S3Url);

        _context.Cvs.Remove(cv);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> GetDownloadUrl(int id)
    {
        var userId = GetUserId();
        var cv = await _context.Cvs.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (cv == null) return NotFound();
        
        var bucketName = _configuration["Aws:BucketName"];
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = cv.S3Url,
            Expires = DateTime.UtcNow.AddMinutes(5)
        };
        
        var url = _s3Client.GetPreSignedURL(request);
        return Ok(new { url });
    }

    [HttpPost("{cvId}/analyze/{jobApplicationId}")]
public async Task<ActionResult<CvAnalysisResponse>> Analyze(int cvId, int jobApplicationId)
{
    var userId = GetUserId();

    var cv = await _context.Cvs.FirstOrDefaultAsync(c => c.Id == cvId && c.UserId == userId);
    if (cv == null) return NotFound("CV not found.");

    var jobApplication = await _context.JobApplications
        .FirstOrDefaultAsync(j => j.Id == jobApplicationId && j.UserId == userId);
    if (jobApplication == null) return NotFound("Job application not found.");

    var recentAnalysis = await _context.CvAnalyses
        .Where(a => a.CvId == cvId && a.JobApplicationId == jobApplicationId)
        .OrderByDescending(a => a.CreatedAt)
        .FirstOrDefaultAsync();

    if (recentAnalysis != null && DateTime.UtcNow - recentAnalysis.CreatedAt < AnalyzeCooldown)
    {
        var secondsLeft = (int)(AnalyzeCooldown - (DateTime.UtcNow - recentAnalysis.CreatedAt)).TotalSeconds;
        return StatusCode(429, $"Please wait {secondsLeft} more second(s) before re-analyzing this CV against this job.");
    }

    var cvText = await ExtractCvTextAsync(cv);

   var prompt = $@"Compare this CV against the job description below. Respond ONLY with a JSON object
   in this exact shape: {{""matchScore"": <integer 0-100>, ""missingSkills"": ""<comma-separated list of missing skills>""}}
   
   CV:
   {cvText}
   
   Job Description:
   {jobApplication.JobDescription}";


    var chatResponse = await _chatClient.CompleteChatAsync(
        [new UserChatMessage(prompt)],
        new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() });

    var resultJson = chatResponse.Value.Content[0].Text;
    var parsed = JsonSerializer.Deserialize<AnalysisResult>(resultJson,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    var analysis = new CvAnalysis
    {
        CvId = cv.Id,
        JobApplicationId = jobApplication.Id,
        MatchScore = parsed.MatchScore,
        MissingSkills = parsed.MissingSkills,
        CreatedAt = DateTime.UtcNow
    };

    _context.CvAnalyses.Add(analysis);
    await _context.SaveChangesAsync();

    return Ok(new CvAnalysisResponse
    {
        Id = analysis.Id,
        CvId = analysis.CvId,
        JobApplicationId = analysis.JobApplicationId,
        MatchScore = analysis.MatchScore,
        MissingSkills = analysis.MissingSkills,
        CreatedAt = analysis.CreatedAt
    });
}

[HttpGet("{cvId}/analyses")]
public async Task<ActionResult<List<CvAnalysisResponse>>> GetAnalyses(int cvId)
{
    var userId = GetUserId();
    var cv = await _context.Cvs.FirstOrDefaultAsync(c => c.Id == cvId && c.UserId == userId);
    if (cv == null) return NotFound();

    var analyses = await _context.CvAnalyses
        .Where(a => a.CvId == cvId)
        .OrderByDescending(a => a.CreatedAt)
        .Select(a => new CvAnalysisResponse
        {
            Id = a.Id,
            CvId = a.CvId,
            JobApplicationId = a.JobApplicationId,
            MatchScore = a.MatchScore,
            MissingSkills = a.MissingSkills,
            CreatedAt = a.CreatedAt
        })
        .ToListAsync();

    return Ok(analyses);
}

[HttpPost("{cvId}/rewrite/{jobApplicationId}")]
public async Task<ActionResult<CvRewriteResponse>> Rewrite(int cvId, int jobApplicationId)
{
    var userId = GetUserId();

    var cv = await _context.Cvs.FirstOrDefaultAsync(c => c.Id == cvId && c.UserId == userId);
    if (cv == null) return NotFound("CV not found.");

    var jobApplication = await _context.JobApplications
        .FirstOrDefaultAsync(j => j.Id == jobApplicationId && j.UserId == userId);
    if (jobApplication == null) return NotFound("Job application not found.");

    var cvText = await ExtractCvTextAsync(cv);

        var prompt = $@"Rewrite this CV to better match the job description below, so it reads well to both a
    human recruiter and an ATS (applicant tracking system) keyword scan. Keep it truthful — reorder, rephrase,
    and emphasise relevant existing experience and skills, but do not invent experience, skills, or qualifications
    that aren't already present in the original CV.
    
    Format the result as clean Markdown: use a level-1 heading (#) for the candidate's name if present, level-2
    headings (##) for sections like Summary, Experience, Skills, Education, and bullet points (-) for lists of
    responsibilities or skills. Do not wrap the output in a code block. Return ONLY the Markdown CV, no commentary.

Original CV:
{cvText}

Job Description:
{jobApplication.JobDescription}";


    var chatResponse = await _chatClient.CompleteChatAsync([new UserChatMessage(prompt)]);
    var rewrittenCv = chatResponse.Value.Content[0].Text;

    return Ok(new CvRewriteResponse { RewrittenCv = rewrittenCv });
}

private class AnalysisResult
{
    public int MatchScore { get; set; }
    public string MissingSkills { get; set; } = string.Empty;
}

}
