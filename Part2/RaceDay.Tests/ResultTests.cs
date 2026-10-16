using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public class ResultTests
{
    private static (Api.Data.RaceDayDbContext db, User organiser, User participant, Enrolment enrolment) Setup()
    {
        var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, "Thabo", "thabo@example.com", RoleNames.Organiser);
        var participant = TestHelper.AddUser(db, "Sipho", "sipho@example.com", RoleNames.Participant);
        var category = TestHelper.AddCategory(db, TestHelper.AddEvent(db, organiser.UserId).EventId);
        var enrolment = new Enrolment { ParticipantId = participant.UserId, CategoryId = category.CategoryId };
        db.Enrolments.Add(enrolment);
        db.SaveChanges();
        return (db, organiser, participant, enrolment);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult()
    {
        var (db, organiser, _, enrolment) = Setup();
        var controller = new ResultsController(db).WithSession(organiser.UserId, RoleNames.Organiser);

        var result = await controller.Capture(new ResultDto { EnrolmentId = enrolment.EnrolmentId, FinishTime = TimeSpan.FromMinutes(52), Position = 3 });

        Assert.IsType<CreatedResult>(result);
        Assert.Equal(3, db.Results.Single().Position);
    }

    [Fact]
    public async Task CapturingASecondResultForTheSameEnrolment_ReturnsConflict()
    {
        var (db, organiser, _, enrolment) = Setup();
        var controller = new ResultsController(db).WithSession(organiser.UserId, RoleNames.Organiser);
        var dto = new ResultDto { EnrolmentId = enrolment.EnrolmentId, FinishTime = TimeSpan.FromMinutes(52), Position = 3 };
        await controller.Capture(dto);

        var result = await controller.Capture(dto);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Results);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResult_ForAnotherOrganisersEvent()
    {
        var (db, _, _, enrolment) = Setup();
        var other = TestHelper.AddUser(db, "Lerato", "lerato@example.com", RoleNames.Organiser);
        var controller = new ResultsController(db).WithSession(other.UserId, RoleNames.Organiser);

        var result = await controller.Capture(new ResultDto { EnrolmentId = enrolment.EnrolmentId, FinishTime = TimeSpan.FromMinutes(52), Position = 1 });

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task Participant_SeesOnlyOwnResults()
    {
        var (db, organiser, participant, enrolment) = Setup();
        await new ResultsController(db).WithSession(organiser.UserId, RoleNames.Organiser)
            .Capture(new ResultDto { EnrolmentId = enrolment.EnrolmentId, FinishTime = TimeSpan.FromMinutes(52), Position = 1 });
        var stranger = TestHelper.AddUser(db, "Naledi", "naledi@example.com", RoleNames.Participant);

        var mine = await new ResultsController(db).WithSession(participant.UserId, RoleNames.Participant).GetMine();
        var theirs = await new ResultsController(db).WithSession(stranger.UserId, RoleNames.Participant).GetMine();

        Assert.Single((IEnumerable<ResultResponseDto>)Assert.IsType<OkObjectResult>(mine).Value!);
        Assert.Empty((IEnumerable<ResultResponseDto>)Assert.IsType<OkObjectResult>(theirs).Value!);
    }
}
