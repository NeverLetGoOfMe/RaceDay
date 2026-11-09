using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>Stands in for Azure Blob Storage so the upload endpoints can be tested without a real account.</summary>
public class FakeBlobService : IBlobStorageService
{
    public int Uploads { get; private set; }

    public Task<string> UploadAsync(Stream content, string contentType, string extension)
    {
        Uploads++;
        return Task.FromResult($"fakeimage{extension}");
    }

    public Task<(Stream Content, string ContentType)?> OpenReadAsync(string blobName) =>
        Task.FromResult<(Stream Content, string ContentType)?>(null);
}

public class Part3FeatureTests
{
    private static IFormFile MakeFile(string contentType = "image/png", int size = 100)
    {
        var bytes = new byte[size];
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "image.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task Organiser_CanUploadBanner_ForOwnEvent()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var e = TestHelper.AddEvent(db, organiser.UserId);
        var blobs = new FakeBlobService();
        var controller = new UploadsController(db, blobs).WithSession(organiser.UserId, RoleNames.Organiser);

        var result = await controller.UploadBanner(e.EventId, MakeFile());

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("fakeimage.png", db.Events.Single().BannerImage);
        Assert.Equal(1, blobs.Uploads);
    }

    [Fact]
    public async Task Organiser_CannotUploadBanner_ForAnotherOrganisersEvent()
    {
        var db = TestHelper.CreateDb();
        var owner = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var other = TestHelper.AddUser(db, "Lerato", "lerato@example.com", RoleNames.Organiser);
        var e = TestHelper.AddEvent(db, owner.UserId);
        var blobs = new FakeBlobService();
        var controller = new UploadsController(db, blobs).WithSession(other.UserId, RoleNames.Organiser);

        var result = await controller.UploadBanner(e.EventId, MakeFile());

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(0, blobs.Uploads);
    }

    [Fact]
    public async Task Upload_WithWrongFileType_ReturnsBadRequest()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var e = TestHelper.AddEvent(db, organiser.UserId);
        var controller = new UploadsController(db, new FakeBlobService()).WithSession(organiser.UserId, RoleNames.Organiser);

        var result = await controller.UploadBanner(e.EventId, MakeFile("application/pdf"));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(db.Events.Single().BannerImage);
    }

    [Fact]
    public async Task AnyUser_CanUploadProfilePicture()
    {
        var db = TestHelper.CreateDb();
        var participant = TestHelper.AddUser(db, "Sipho", "sipho@example.com", RoleNames.Participant);
        var controller = new UploadsController(db, new FakeBlobService()).WithSession(participant.UserId, RoleNames.Participant);

        var result = await controller.UploadProfilePicture(MakeFile("image/jpeg"));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("fakeimage.jpg", db.Users.Single().ProfilePicture);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyOwnEvents_WithEnrolmentCounts()
    {
        var db = TestHelper.CreateDb();
        var mine = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var other = TestHelper.AddUser(db, "Lerato", "lerato@example.com", RoleNames.Organiser);
        var participant = TestHelper.AddUser(db, "Sipho", "sipho@example.com", RoleNames.Participant);
        var myEvent = TestHelper.AddEvent(db, mine.UserId, "My Event");
        TestHelper.AddEvent(db, other.UserId, "Not Mine");
        var category = TestHelper.AddCategory(db, myEvent.EventId);
        db.Enrolments.Add(new Enrolment { ParticipantId = participant.UserId, CategoryId = category.CategoryId });
        db.SaveChanges();
        var controller = new EventsController(db).WithSession(mine.UserId, RoleNames.Organiser);

        var result = await controller.GetMine();

        var list = ((IEnumerable<OrganiserEventSummaryDto>)Assert.IsType<OkObjectResult>(result).Value!).ToList();
        Assert.Single(list);
        Assert.Equal("My Event", list[0].EventName);
        Assert.Equal(1, list[0].EnrolmentCount);
    }

    [Fact]
    public async Task GetAll_CanFilterByEventType()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        TestHelper.AddEvent(db, organiser.UserId, "Run A");
        db.Events.Add(new Event { OrganiserId = organiser.UserId, EventName = "Cycle B", EventDate = DateTime.Today.AddDays(5), Location = "Pretoria", Distance = 50, EventType = "Cycle" });
        db.SaveChanges();
        var controller = new EventsController(db).WithSession();

        var result = await controller.GetAll(type: "Cycle");

        var list = ((IEnumerable<EventResponseDto>)Assert.IsType<OkObjectResult>(result).Value!).ToList();
        Assert.Single(list);
        Assert.Equal("Cycle B", list[0].EventName);
    }

    [Fact]
    public async Task Organiser_CanChangeEnrolmentStatus_OnOwnEvent_ButNotOnAnotherOrganisersEvent()
    {
        var db = TestHelper.CreateDb();
        var owner = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var other = TestHelper.AddUser(db, "Lerato", "lerato@example.com", RoleNames.Organiser);
        var participant = TestHelper.AddUser(db, "Sipho", "sipho@example.com", RoleNames.Participant);
        var category = TestHelper.AddCategory(db, TestHelper.AddEvent(db, owner.UserId).EventId);
        var enrolment = new Enrolment { ParticipantId = participant.UserId, CategoryId = category.CategoryId };
        db.Enrolments.Add(enrolment);
        db.SaveChanges();

        var forbidden = await new EnrolmentsController(db).WithSession(other.UserId, RoleNames.Organiser)
            .UpdateStatus(enrolment.EnrolmentId, new StatusDto { Status = "Cancelled" });
        var ok = await new EnrolmentsController(db).WithSession(owner.UserId, RoleNames.Organiser)
            .UpdateStatus(enrolment.EnrolmentId, new StatusDto { Status = "Pending" });

        Assert.Equal(403, Assert.IsType<ObjectResult>(forbidden).StatusCode);
        Assert.IsType<OkObjectResult>(ok);
        Assert.Equal("Pending", db.Enrolments.Single().Status);
    }

    [Fact]
    public async Task CategoryResults_IncludeTotalFinishers()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var first = TestHelper.AddUser(db, "First", "first@example.com", RoleNames.Participant);
        var second = TestHelper.AddUser(db, "Second", "second@example.com", RoleNames.Participant);
        var category = TestHelper.AddCategory(db, TestHelper.AddEvent(db, organiser.UserId).EventId);
        var e1 = new Enrolment { ParticipantId = first.UserId, CategoryId = category.CategoryId };
        var e2 = new Enrolment { ParticipantId = second.UserId, CategoryId = category.CategoryId };
        db.Enrolments.AddRange(e1, e2);
        db.SaveChanges();
        var results = new ResultsController(db).WithSession(organiser.UserId, RoleNames.Organiser);
        await results.Capture(new ResultDto { EnrolmentId = e1.EnrolmentId, FinishTime = TimeSpan.FromMinutes(40), Position = 1 });
        await results.Capture(new ResultDto { EnrolmentId = e2.EnrolmentId, FinishTime = TimeSpan.FromMinutes(45), Position = 2 });

        var mine = await new ResultsController(db).WithSession(second.UserId, RoleNames.Participant).GetMine();

        var list = ((IEnumerable<ResultResponseDto>)Assert.IsType<OkObjectResult>(mine).Value!).ToList();
        Assert.Single(list);
        Assert.Equal(2, list[0].Position);
        Assert.Equal(2, list[0].TotalFinishers);
    }

    [Theory]
    [InlineData(typeof(UploadsController), nameof(UploadsController.UploadBanner), RoleNames.Organiser)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.UpdateStatus), RoleNames.Organiser)]
    [InlineData(typeof(EventsController), nameof(EventsController.GetMine), RoleNames.Organiser)]
    public void NewEndpoints_RequireTheOrganiserRole(Type controller, string method, string expectedRole)
    {
        var attribute = controller.GetMethod(method)!.GetCustomAttribute<SessionAuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(expectedRole, attribute!.Role);
    }
}
