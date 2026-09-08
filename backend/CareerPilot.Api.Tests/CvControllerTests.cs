using Amazon.S3;
using Amazon.S3.Model;
using CareerPilot.Api.Controllers;
using CareerPilot.Api.data;
using CareerPilot.Api.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using OpenAI.Chat;
using Xunit;

namespace CareerPilot.Api.Tests;

public class CvControllerTests
{
    private static IConfiguration CreateConfig()
    {
        var values = new Dictionary<string, string?>
        {
            ["Aws:BucketName"] = "test-bucket"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static CvController CreateController(AppDbContext context, int userId, Mock<IAmazonS3> s3Mock)
    {
        var chatClient = new ChatClient("gpt-4o-mini", "test-api-key");
        var controller = new CvController(context, s3Mock.Object, CreateConfig(), chatClient);
        TestHelpers.SetUser(controller, userId);
        return controller;
    }

    private static Mock<IFormFile> CreateFakeFile(string fileName, long sizeBytes)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.Length).Returns(sizeBytes);
        file.Setup(f => f.ContentType).Returns("text/plain");
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[sizeBytes]));
        return file;
    }

    [Fact]
    public async Task Upload_RejectsFileOverSizeLimit()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var s3Mock = new Mock<IAmazonS3>();
        var controller = CreateController(context, userId: 1, s3Mock);
        var oversizedFile = CreateFakeFile("cv.txt", 6 * 1024 * 1024);

        var result = await controller.Upload(oversizedFile.Object);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        s3Mock.Verify(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task Upload_RejectsUnsupportedExtension()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var s3Mock = new Mock<IAmazonS3>();
        var controller = CreateController(context, userId: 1, s3Mock);
        var badFile = CreateFakeFile("virus.exe", 1024);

        var result = await controller.Upload(badFile.Object);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        s3Mock.Verify(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task Upload_WithValidFile_SavesRecordAndUploadsToS3()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var s3Mock = new Mock<IAmazonS3>();
        s3Mock.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
              .ReturnsAsync(new PutObjectResponse());
        var controller = CreateController(context, userId: 1, s3Mock);
        var goodFile = CreateFakeFile("cv.txt", 1024);

        var result = await controller.Upload(goodFile.Object);

        Assert.IsType<OkObjectResult>(result.Result);
        var saved = Assert.Single(context.Cvs);
        Assert.Equal(1, saved.UserId);
        s3Mock.Verify(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Once);
    }

    [Fact]
    public async Task Delete_ForAnotherUsersCv_ReturnsNotFoundAndDoesNotCallS3()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var theirCv = new Cv { UserId = 2, FileName = "cv.txt", S3Url = "cvs/2/cv.txt" };
        context.Cvs.Add(theirCv);
        await context.SaveChangesAsync();

        var s3Mock = new Mock<IAmazonS3>();
        var controller = CreateController(context, userId: 1, s3Mock);

        var result = await controller.Delete(theirCv.Id);

        Assert.IsType<NotFoundResult>(result);
        s3Mock.Verify(s => s.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task GetDownloadUrl_ForAnotherUsersCv_ReturnsNotFound()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var theirCv = new Cv { UserId = 2, FileName = "cv.txt", S3Url = "cvs/2/cv.txt" };
        context.Cvs.Add(theirCv);
        await context.SaveChangesAsync();

        var s3Mock = new Mock<IAmazonS3>();
        var controller = CreateController(context, userId: 1, s3Mock);

        var result = await controller.GetDownloadUrl(theirCv.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Analyze_WhenAnalyzedRecently_ReturnsTooManyRequestsWithoutCallingS3()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var cv = new Cv { UserId = 1, FileName = "cv.txt", S3Url = "cvs/1/cv.txt" };
        var jobApp = new JobApplication { UserId = 1, CompanyName = "Co", JobTitle = "Dev", JobDescription = "Needs C#" };
        context.Cvs.Add(cv);
        context.JobApplications.Add(jobApp);
        await context.SaveChangesAsync();

        context.CvAnalyses.Add(new CvAnalysis
        {
            CvId = cv.Id,
            JobApplicationId = jobApp.Id,
            MatchScore = 80,
            MissingSkills = "",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var s3Mock = new Mock<IAmazonS3>();
        var controller = CreateController(context, userId: 1, s3Mock);

        var result = await controller.Analyze(cv.Id, jobApp.Id);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(429, statusResult.StatusCode);
        s3Mock.Verify(s => s.GetObjectAsync(It.IsAny<GetObjectRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task GetAnalyses_OnlyReturnsAnalysesForOwnedCv()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var myCv = new Cv { UserId = 1, FileName = "cv.txt", S3Url = "cvs/1/cv.txt" };
        var theirCv = new Cv { UserId = 2, FileName = "cv.txt", S3Url = "cvs/2/cv.txt" };
        context.Cvs.AddRange(myCv, theirCv);
        await context.SaveChangesAsync();

        var s3Mock = new Mock<IAmazonS3>();
        var controller = CreateController(context, userId: 1, s3Mock);

        var result = await controller.GetAnalyses(theirCv.Id);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
