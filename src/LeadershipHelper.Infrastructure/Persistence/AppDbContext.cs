using LeadershipHelper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadershipHelper.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<Situation> Situations => Set<Situation>();
    public DbSet<SituationAction> SituationActions => Set<SituationAction>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<ExperienceActionState> ExperienceActionStates => Set<ExperienceActionState>();
    public DbSet<SavedSituation> SavedSituations => Set<SavedSituation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.PhoneNumber).HasMaxLength(32);
            entity.Property(x => x.DisplayName).HasMaxLength(100);
            entity.HasIndex(x => x.Email);
            entity.HasIndex(x => x.PhoneNumber);
        });

        modelBuilder.Entity<OtpChallenge>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Contact).HasMaxLength(320);
            entity.Property(x => x.Channel).HasMaxLength(20);
            entity.Property(x => x.CodeHash).HasMaxLength(128);
            entity.HasIndex(x => new { x.Contact, x.CreatedUtc });
        });

        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.ExpiresUtc });
        });

        modelBuilder.Entity<Situation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(300);
            entity.Property(x => x.ShortDescription).HasMaxLength(300);
            entity.Property(x => x.AuthorName).HasMaxLength(100);
            entity.HasIndex(x => x.Title);
            entity.HasIndex(x => x.AuthorName);
        });

        modelBuilder.Entity<SituationAction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Situation)
                .WithMany(x => x.Actions)
                .HasForeignKey(x => x.SituationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Experience>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Situation)
                .WithMany()
                .HasForeignKey(x => x.SituationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.UserId, x.ExperienceDateUtc });
        });

        modelBuilder.Entity<ExperienceActionState>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Experience)
                .WithMany(x => x.ActionStates)
                .HasForeignKey(x => x.ExperienceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SituationAction)
                .WithMany()
                .HasForeignKey(x => x.SituationActionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.ExperienceId, x.SituationActionId }).IsUnique();
        });

        modelBuilder.Entity<SavedSituation>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.SituationId });
        });
    }
}
