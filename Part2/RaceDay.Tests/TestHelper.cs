using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

/// <summary>A simple in-memory session so controllers can be tested without a web server.</summary>
public class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id => "test-session";
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
}

public static class TestHelper
{
    /// <summary>A fresh in-memory database (with the two roles seeded) for every test.</summary>
    public static RaceDayDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new RaceDayDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    /// <summary>Gives the controller a session, optionally already "logged in".</summary>
    public static T WithSession<T>(this T controller, int? userId = null, string? role = null) where T : ControllerBase
    {
        var session = new TestSession();
        if (userId.HasValue && role != null)
        {
            session.SetUser(userId.Value, role);
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = session }
        };
        return controller;
    }

    public static User AddUser(RaceDayDbContext db, string name, string email, string role, string password = "Password123")
    {
        var user = new User
        {
            RoleId = role == RoleNames.Organiser ? 1 : 2,
            FullName = name,
            Email = email,
            PasswordHash = PasswordHasher.Hash(password)
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Event AddEvent(RaceDayDbContext db, int organiserId, string name = "Test Run")
    {
        var e = new Event
        {
            OrganiserId = organiserId,
            EventName = name,
            EventDate = DateTime.Today.AddDays(30),
            Location = "Kagiso",
            Distance = 10,
            EventType = "Run"
        };
        db.Events.Add(e);
        db.SaveChanges();
        return e;
    }

    public static Category AddCategory(RaceDayDbContext db, int eventId, string name = "Senior", int max = 100)
    {
        var c = new Category { EventId = eventId, CategoryName = name, MaxParticipants = max, EntryFee = 50 };
        db.Categories.Add(c);
        db.SaveChanges();
        return c;
    }
}
