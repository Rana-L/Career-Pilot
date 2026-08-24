using CareerPilot.Api.data;
using CareerPilot.Api.dto;
using CareerPilot.Api.models;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerPilot.Api.Controllers;

[ApiController]
[Route("api/cv")]
[Authorize]
public class CvController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _configuration;

    public CvController(AppDbContext context, IAmazonS3 s3Client, IConfiguration configuration)
    {
        _context = context;
        _s3Client = s3Client;
        _configuration = configuration;
    }

    private int GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return int.Parse(sub!);
    }

    [HttpPost("upload")]
    public async Task<ActionResult<CvResponse>> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

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
}
