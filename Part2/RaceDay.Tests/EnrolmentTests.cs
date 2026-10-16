using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;

namespace RaceDay.Tests;

public class EnrolmentTests
{
    [Fact]
    public async Task Participant_CanEnrol_AndTheLinkIsRecorded()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var participant = TestHelper.AddUser(db, "Sipho", "sipho@example.com", RoleNames.Participant);
        var e = TestHelper.AddEvent(db, organiser.UserId);
        var category = TestHelper.AddCategory(db, e.EventId);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, RoleNames.Participant);

        var result = await controller.Enrol(new EnrolDto { CategoryId = category.CategoryId });

        Assert.IsType<CreatedResult>(result);
        var saved = db.Enrolments.Single();
        Assert.Equal(participant.UserId, saved.ParticipantId);   // who
        Assert.Equal(category.CategoryId, saved.CategoryId);     // which category (and so which event)
        Assert.Equal("Confirmed", saved.Status);
    }

    [Fact]
    public async Task Participant_CannotEnrolInTheSameCategoryTwice()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var participant = TestHelper.AddUser(db, "Sipho", "sipho@example.com", RoleNames.Participant);
        var category = TestHelper.AddCategory(db, TestHelper.AddEvent(db, organiser.UserId).EventId);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, RoleNames.Participant);
        await controller.Enrol(new EnrolDto { CategoryId = category.CategoryId });

        var result = await controller.Enrol(new EnrolDto { CategoryId = category.CategoryId });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_InUnknownCategory_ReturnsNotFound()
    {
        var db = TestHelper.CreateDb();
        var participant = TestHelper.AddUser(db, "Sipho", "sipho@example.com", RoleNames.Participant);
        var controller = new EnrolmentsController(db).WithSession(participant.UserId, RoleNames.Participant);

        var result = await controller.Enrol(new EnrolDto { CategoryId = 999 });

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_ReturnsConflict()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var first = TestHelper.AddUser(db, "First", "first@example.com", RoleNames.Participant);
        var second = TestHelper.AddUser(db, "Second", "second@example.com", RoleNames.Participant);
        var category = TestHelper.AddCategory(db, TestHelper.AddEvent(db, organiser.UserId).EventId, max: 1);

        await new EnrolmentsController(db).WithSession(first.UserId, RoleNames.Participant)
            .Enrol(new EnrolDto { CategoryId = category.CategoryId });
        var result = await new EnrolmentsController(db).WithSession(second.UserId, RoleNames.Participant)
            .Enrol(new EnrolDto { CategoryId = category.CategoryId });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Participant_OnlySeesOwnEnrolments()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var mine = TestHelper.AddUser(db, "Sipho", "sipho@example.com", RoleNames.Participant);
        var theirs = TestHelper.AddUser(db, "Naledi", "naledi@example.com", RoleNames.Participant);
        var category = TestHelper.AddCategory(db, TestHelper.AddEvent(db, organiser.UserId).EventId);
        await new EnrolmentsController(db).WithSession(mine.UserId, RoleNames.Participant).Enrol(new EnrolDto { CategoryId = category.CategoryId });
        await new EnrolmentsController(db).WithSession(theirs.UserId, RoleNames.Participant).Enrol(new EnrolDto { CategoryId = category.CategoryId });

        var result = await new EnrolmentsController(db).WithSession(mine.UserId, RoleNames.Participant).GetMine();

        var list = ((IEnumerable<EnrolmentResponseDto>)Assert.IsType<OkObjectResult>(result).Value!).ToList();
        Assert.Single(list);
        Assert.Equal(mine.UserId, list[0].ParticipantId);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolments_ForOwnEvent_ButNotAnotherOrganisers()
    {
        var db = TestHelper.CreateDb();
        var owner = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var other = TestHelper.AddUser(db, "Lerato", "lerato@example.com", RoleNames.Organiser);
        var participant = TestHelper.AddUser(db, "Sipho", "sipho@example.com", RoleNames.Participant);
        var e = TestHelper.AddEvent(db, owner.UserId);
        var category = TestHelper.AddCategory(db, e.EventId);
        await new EnrolmentsController(db).WithSession(participant.UserId, RoleNames.Participant).Enrol(new EnrolDto { CategoryId = category.CategoryId });

        var ownResult = await new EnrolmentsController(db).WithSession(owner.UserId, RoleNames.Organiser).GetForEvent(e.EventId);
        var otherResult = await new EnrolmentsController(db).WithSession(other.UserId, RoleNames.Organiser).GetForEvent(e.EventId);

        var list = (IEnumerable<EnrolmentResponseDto>)Assert.IsType<OkObjectResult>(ownResult).Value!;
        Assert.Single(list);
        Assert.Equal(403, Assert.IsType<ObjectResult>(otherResult).StatusCode);
    }
}
