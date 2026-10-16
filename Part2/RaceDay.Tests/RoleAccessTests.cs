using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Api.Controllers;
using RaceDay.Api.Helpers;

namespace RaceDay.Tests;

public class RoleAccessTests
{
    private static AuthorizationFilterContext MakeContext(string? role, int? userId)
    {
        var session = new TestSession();
        if (userId.HasValue && role != null)
        {
            session.SetUser(userId.Value, role);
        }

        var http = new DefaultHttpContext { Session = session };
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void NotLoggedIn_IsRejectedWith401()
    {
        var filter = new SessionAuthorizeAttribute(RoleNames.Organiser);
        var context = MakeContext(role: null, userId: null);

        filter.OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Fact]
    public void ParticipantOnOrganiserEndpoint_IsRejectedWith403()
    {
        var filter = new SessionAuthorizeAttribute(RoleNames.Organiser);
        var context = MakeContext(RoleNames.Participant, 1);

        filter.OnAuthorization(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public void OrganiserOnParticipantEndpoint_IsRejectedWith403()
    {
        var filter = new SessionAuthorizeAttribute(RoleNames.Participant);
        var context = MakeContext(RoleNames.Organiser, 1);

        filter.OnAuthorization(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Theory]
    [InlineData(RoleNames.Organiser)]
    [InlineData(RoleNames.Participant)]
    public void CorrectRole_IsAllowedThrough(string role)
    {
        var filter = new SessionAuthorizeAttribute(role);
        var context = MakeContext(role, 1);

        filter.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void AnyLoggedInUser_PassesWhenNoRoleIsRequired()
    {
        var filter = new SessionAuthorizeAttribute();
        var context = MakeContext(RoleNames.Participant, 1);

        filter.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    // Checks that the real endpoints are actually decorated with the right role

    [Theory]
    [InlineData(typeof(EventsController), nameof(EventsController.Create), RoleNames.Organiser)]
    [InlineData(typeof(EventsController), nameof(EventsController.Update), RoleNames.Organiser)]
    [InlineData(typeof(EventsController), nameof(EventsController.Delete), RoleNames.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Create), RoleNames.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Update), RoleNames.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Delete), RoleNames.Organiser)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetForEvent), RoleNames.Organiser)]
    [InlineData(typeof(ResultsController), nameof(ResultsController.Capture), RoleNames.Organiser)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol), RoleNames.Participant)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetMine), RoleNames.Participant)]
    [InlineData(typeof(ResultsController), nameof(ResultsController.GetMine), RoleNames.Participant)]
    public void Endpoint_RequiresTheCorrectRole(Type controller, string method, string expectedRole)
    {
        var attribute = controller.GetMethod(method)!.GetCustomAttribute<SessionAuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(expectedRole, attribute!.Role);
    }

    [Fact]
    public void ProfileEndpoints_RequireLogin_ForAnyRole()
    {
        var attribute = typeof(UsersController).GetCustomAttribute<SessionAuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Null(attribute!.Role);
    }
}
