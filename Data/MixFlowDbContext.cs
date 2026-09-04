namespace MixFlowWebApp.Data;
using Microsoft.EntityFrameworkCore;
using MixFlowWebApp.Models;

public class MixFlowDbContext : DbContext
{
    public MixFlowDbContext(DbContextOptions<MixFlowDbContext> options) : base(options) { }

    // DbSets
    public DbSet<Organizer> Organizers { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<SessionPlayer> SessionPlayers { get; set; }
    public DbSet<QueueEntry> QueueEntries { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<MatchPlayer> MatchPlayers { get; set; }
    public DbSet<PlayerMatchHistory> PlayerMatchHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Organizer Relationships
        modelBuilder.Entity<Organizer>()
            .HasMany(o => o.Sessions)
            .WithOne(s => s.Organizer)
            .HasForeignKey(s => s.OrganizerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Organizer>()
            .HasMany<Player>()
            .WithOne(p => p.Organizer)
            .HasForeignKey(p => p.OrganizerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Session Relationships
        modelBuilder.Entity<Session>()
            .HasMany(s => s.SessionPlayers)
            .WithOne(sp => sp.Session)
            .HasForeignKey(sp => sp.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Session>()
            .HasMany(s => s.Matches)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Player Relationships
        modelBuilder.Entity<Player>()
            .HasMany(p => p.SessionPlayers)
            .WithOne(sp => sp.Player)
            .HasForeignKey(sp => sp.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Player>()
            .HasMany(p => p.QueueEntries)
            .WithOne(q => q.Player)
            .HasForeignKey(q => q.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Player>()
            .HasMany(p => p.MatchHistory)
            .WithOne(h => h.Player)
            .HasForeignKey(h => h.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Match Relationships
        modelBuilder.Entity<Match>()
            .HasMany(m => m.MatchPlayers)
            .WithOne(mp => mp.Match)
            .HasForeignKey(mp => mp.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // MatchPlayer Relationships
        modelBuilder.Entity<MatchPlayer>()
            .HasOne(mp => mp.Player)
            .WithMany()
            .HasForeignKey(mp => mp.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for better performance
        modelBuilder.Entity<SessionPlayer>()
            .HasIndex(sp => new { sp.SessionId, sp.PlayerId })
            .IsUnique();

        modelBuilder.Entity<Player>()
            .HasIndex(p => p.OrganizerId);

        modelBuilder.Entity<QueueEntry>()
            .HasIndex(q => new { q.SessionId, q.PlayerId })
            .IsUnique();

        // Primary Key Configurations for non-standard naming
        modelBuilder.Entity<SessionPlayer>()
            .HasKey(sp => sp.SessionPlayerId);

        modelBuilder.Entity<Match>()
            .HasKey(m => m.MatchId);

        modelBuilder.Entity<MatchPlayer>()
            .HasKey(mp => mp.MatchPlayerId);

        modelBuilder.Entity<QueueEntry>()
            .HasKey(q => q.QueueId);

        modelBuilder.Entity<PlayerMatchHistory>()
            .HasKey(h => h.HistoryId);

        modelBuilder.Entity<PlayerMatchHistory>()
            .HasIndex(h => new { h.PlayerId, h.PlayedAt });
    }
}