using LinkMeet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace LinkMeet.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User
        modelBuilder.Entity<User>(e =>
        {
            e.ToCollection("Users");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
        });

        // Meeting
        modelBuilder.Entity<Meeting>(e =>
        {
            e.ToCollection("Meetings");
            e.HasKey(m => m.Id);
            e.HasIndex(m => m.MeetingCode).IsUnique();
        });

        // Participant
        modelBuilder.Entity<Participant>(e =>
        {
            e.ToCollection("Participants");
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.MeetingId, p.UserId }).IsUnique();
        });

        // ChatMessage
        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.ToCollection("ChatMessages");
            e.HasKey(c => c.Id);
        });
    }
}
