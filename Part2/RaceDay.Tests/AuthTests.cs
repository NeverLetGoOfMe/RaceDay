using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;

namespace RaceDay.Tests;

public class AuthTests
{
    private static RegisterDto ValidRegister(string email = "sipho@example.com", string role = "Participant") => new()
    {
        FullName = "Sipho Dlamini",
        Email = email,
        Password = "Password123",
        ContactNumber = "0731234567",
        Role = role
    };

    [Fact]
    public async Task Register_WithValidDetails_ReturnsCreated_AndHashesPassword()
    {
        var db = TestHelper.CreateDb();
        var controller = new AuthController(db).WithSession();

        var result = await controller.Register(ValidRegister());

        Assert.IsType<CreatedResult>(result);
        var saved = db.Users.Single();
        Assert.NotEqual("Password123", saved.PasswordHash);   // never stored in plain text
        Assert.True(PasswordHasher.Verify("Password123", saved.PasswordHash));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var db = TestHelper.CreateDb();
        var controller = new AuthController(db).WithSession();
        await controller.Register(ValidRegister());

        var result = await controller.Register(ValidRegister());

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task Register_WithUnknownRole_ReturnsBadRequest()
    {
        var db = TestHelper.CreateDb();
        var controller = new AuthController(db).WithSession();

        var result = await controller.Register(ValidRegister(role: "Admin"));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Users);
    }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Login_WithCorrectCredentials_StoresUserIdAndRoleInSession(string role)
    {
        var db = TestHelper.CreateDb();
        var user = TestHelper.AddUser(db, "Test User", "test@example.com", role);
        var controller = new AuthController(db).WithSession();

        var result = await controller.Login(new LoginDto { Email = "test@example.com", Password = "Password123" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(user.UserId, controller.HttpContext.Session.GetUserId());
        Assert.Equal(role, controller.HttpContext.Session.GetRole());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized_AndNoSession()
    {
        var db = TestHelper.CreateDb();
        TestHelper.AddUser(db, "Test User", "test@example.com", "Participant");
        var controller = new AuthController(db).WithSession();

        var result = await controller.Login(new LoginDto { Email = "test@example.com", Password = "WrongPassword" });

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(controller.HttpContext.Session.GetUserId());
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var db = TestHelper.CreateDb();
        var controller = new AuthController(db).WithSession();

        var result = await controller.Login(new LoginDto { Email = "nobody@example.com", Password = "Password123" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Logout_ClearsTheSession()
    {
        var db = TestHelper.CreateDb();
        var controller = new AuthController(db).WithSession(userId: 1, role: "Participant");

        var result = controller.Logout();

        Assert.IsType<OkObjectResult>(result);
        Assert.Null(controller.HttpContext.Session.GetUserId());
    }
}

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ProducesDifferentHashesForTheSamePassword()
    {
        Assert.NotEqual(PasswordHasher.Hash("Password123"), PasswordHasher.Hash("Password123"));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPasswordOrBadHash()
    {
        var hash = PasswordHasher.Hash("Password123");

        Assert.True(PasswordHasher.Verify("Password123", hash));
        Assert.False(PasswordHasher.Verify("password123", hash));
        Assert.False(PasswordHasher.Verify("Password123", "not-a-real-hash"));
    }
}
