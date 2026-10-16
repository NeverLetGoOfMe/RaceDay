using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;

namespace RaceDay.Tests;

public class EventTests
{
    private static EventDto ValidEvent(string name = "Kagiso Fun Run") => new()
    {
        EventName = name,
        EventDate = DateTime.Today.AddDays(20),
        Location = "Kagiso, Gauteng",
        Description = "Community run",
        Distance = 10,
        EventType = "Run"
    };

    [Fact]
    public async Task Organiser_CanCreateEvent_AndIsRecordedAsOwner()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var controller = new EventsController(db).WithSession(organiser.UserId, RoleNames.Organiser);

        var result = await controller.Create(ValidEvent());

        Assert.IsType<CreatedResult>(result);
        var saved = db.Events.Single();
        Assert.Equal(organiser.UserId, saved.OrganiserId);
        Assert.Equal("Run", saved.EventType);
    }

    [Fact]
    public async Task GetAll_ReturnsEvents_WithoutNeedingToLogIn()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        TestHelper.AddEvent(db, organiser.UserId, "Event A");
        TestHelper.AddEvent(db, organiser.UserId, "Event B");
        var controller = new EventsController(db).WithSession();

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, ((IEnumerable<EventResponseDto>)ok.Value!).Count());
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        var db = TestHelper.CreateDb();
        var controller = new EventsController(db).WithSession();

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var e = TestHelper.AddEvent(db, organiser.UserId);
        var controller = new EventsController(db).WithSession(organiser.UserId, RoleNames.Organiser);

        var result = await controller.Update(e.EventId, ValidEvent("Renamed Run"));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Renamed Run", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent()
    {
        var db = TestHelper.CreateDb();
        var owner = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var other = TestHelper.AddUser(db, "Lerato", "lerato@example.com", RoleNames.Organiser);
        var e = TestHelper.AddEvent(db, owner.UserId, "Original");
        var controller = new EventsController(db).WithSession(other.UserId, RoleNames.Organiser);

        var result = await controller.Update(e.EventId, ValidEvent("Hijacked"));

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Equal("Original", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var e = TestHelper.AddEvent(db, organiser.UserId);
        var controller = new EventsController(db).WithSession(organiser.UserId, RoleNames.Organiser);

        var result = await controller.Delete(e.EventId);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent()
    {
        var db = TestHelper.CreateDb();
        var owner = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var other = TestHelper.AddUser(db, "Lerato", "lerato@example.com", RoleNames.Organiser);
        var e = TestHelper.AddEvent(db, owner.UserId);
        var controller = new EventsController(db).WithSession(other.UserId, RoleNames.Organiser);

        var result = await controller.Delete(e.EventId);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Delete_WithUnknownId_ReturnsNotFound()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var controller = new EventsController(db).WithSession(organiser.UserId, RoleNames.Organiser);

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Organiser_CanAddCategory_ToOwnEvent()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var e = TestHelper.AddEvent(db, organiser.UserId);
        var controller = new CategoriesController(db).WithSession(organiser.UserId, RoleNames.Organiser);

        var result = await controller.Create(e.EventId, new CategoryDto { CategoryName = "Under 20", MaxParticipants = 50, EntryFee = 40 });

        Assert.IsType<CreatedResult>(result);
        Assert.Equal("Under 20", db.Categories.Single().CategoryName);
    }

    [Fact]
    public async Task Organiser_CannotAddCategory_ToAnotherOrganisersEvent()
    {
        var db = TestHelper.CreateDb();
        var owner = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var other = TestHelper.AddUser(db, "Lerato", "lerato@example.com", RoleNames.Organiser);
        var e = TestHelper.AddEvent(db, owner.UserId);
        var controller = new CategoriesController(db).WithSession(other.UserId, RoleNames.Organiser);

        var result = await controller.Create(e.EventId, new CategoryDto { CategoryName = "Senior", MaxParticipants = 50, EntryFee = 40 });

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task GetCategories_ForUnknownEvent_ReturnsNotFound()
    {
        var db = TestHelper.CreateDb();
        var controller = new CategoriesController(db).WithSession();

        var result = await controller.GetForEvent(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
