using CareerPilot.Api.Controllers;
using CareerPilot.Api.data;
using CareerPilot.Api.dto;
using CareerPilot.Api.models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CareerPilot.Api.Tests;

public class ApplicationsControllerTests
{
    private static ApplicationsController CreateController(AppDbContext context, int userId)
    {
        var controller = new ApplicationsController(context);
        TestHelpers.SetUser(controller, userId);
        return controller;
    }

    [Fact]
    public async Task GetAll_OnlyReturnsCurrentUsersApplications()
    {
        var context = TestHelpers.CreateInMemoryContext();
        context.JobApplications.Add(new JobApplication { UserId = 1, CompanyName = "MineCo", JobTitle = "Dev" });
        context.JobApplications.Add(new JobApplication { UserId = 2, CompanyName = "TheirCo", JobTitle = "Dev" });
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId: 1);
        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var applications = Assert.IsAssignableFrom<List<ApplicationResponse>>(ok.Value);
        Assert.Single(applications);
        Assert.Equal("MineCo", applications[0].CompanyName);
    }

    [Fact]
    public async Task GetById_ForAnotherUsersApplication_ReturnsNotFound()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var theirApp = new JobApplication { UserId = 2, CompanyName = "TheirCo", JobTitle = "Dev" };
        context.JobApplications.Add(theirApp);
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId: 1);
        var result = await controller.GetById(theirApp.Id);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_AssociatesApplicationWithCurrentUser()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var controller = CreateController(context, userId: 7);

        await controller.Create(new CreateApplicationRequest { CompanyName = "NewCo", JobTitle = "Engineer" });

        var saved = Assert.Single(context.JobApplications);
        Assert.Equal(7, saved.UserId);
    }

    [Fact]
    public async Task Update_ForAnotherUsersApplication_ReturnsNotFoundAndDoesNotModify()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var theirApp = new JobApplication { UserId = 2, CompanyName = "TheirCo", JobTitle = "Original" };
        context.JobApplications.Add(theirApp);
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId: 1);
        var result = await controller.Update(theirApp.Id, new UpdateApplicationRequest
        {
            CompanyName = "Hacked",
            JobTitle = "Hacked",
            Status = ApplicationStatus.Offer
        });

        Assert.IsType<NotFoundResult>(result);
        var unchanged = await context.JobApplications.FindAsync(theirApp.Id);
        Assert.Equal("Original", unchanged!.JobTitle);
    }

    [Fact]
    public async Task Delete_ForAnotherUsersApplication_ReturnsNotFoundAndDoesNotDelete()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var theirApp = new JobApplication { UserId = 2, CompanyName = "TheirCo", JobTitle = "Dev" };
        context.JobApplications.Add(theirApp);
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId: 1);
        var result = await controller.Delete(theirApp.Id);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(1, context.JobApplications.Count());
    }

    [Fact]
    public async Task Delete_ForOwnApplication_RemovesIt()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var myApp = new JobApplication { UserId = 1, CompanyName = "MineCo", JobTitle = "Dev" };
        context.JobApplications.Add(myApp);
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId: 1);
        var result = await controller.Delete(myApp.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.JobApplications);
    }
}
