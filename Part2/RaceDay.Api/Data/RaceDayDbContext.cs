using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Lookup data - the two roles from the ERD
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Organiser" },
            new Role { RoleId = 2, RoleName = "Participant" });

        modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        // Roles 1 - many Users
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role).WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Users (organiser) 1 - many Events
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser).WithMany()
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Events 1 - many Categories (deleting an event removes its categories)
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Event).WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Categories 1 - many Enrolments
        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Category).WithMany(c => c.Enrolments)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Users (participant) 1 - many Enrolments
        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Participant).WithMany()
            .HasForeignKey(e => e.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        // A participant can only enter a category once
        modelBuilder.Entity<Enrolment>()
            .HasIndex(e => new { e.ParticipantId, e.CategoryId }).IsUnique();

        // Enrolments 1 - 1 Results
        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrolment).WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Users (organiser) 1 - many Results captured
        modelBuilder.Entity<Result>()
            .HasOne(r => r.CapturedBy).WithMany()
            .HasForeignKey(r => r.CapturedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
