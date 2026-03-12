using LinkMeet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
        });

        // Meeting
        modelBuilder.Entity<Meeting>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => m.MeetingCode).IsUnique();
            e.Property(m => m.Title).HasMaxLength(200).IsRequired();
            e.Property(m => m.MeetingCode).HasMaxLength(20).IsRequired();
            e.HasOne(m => m.Host)
             .WithMany(u => u.HostedMeetings)
             .HasForeignKey(m => m.HostId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Participant
        modelBuilder.Entity<Participant>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.MeetingId, p.UserId }).IsUnique();
            e.HasOne(p => p.Meeting)
             .WithMany(m => m.Participants)
             .HasForeignKey(p => p.MeetingId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.User)
             .WithMany(u => u.Participations)
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatMessage
        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.Meeting)
             .WithMany(m => m.ChatMessages)
             .HasForeignKey(c => c.MeetingId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Sender)
             .WithMany(u => u.Messages)
             .HasForeignKey(c => c.SenderId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
