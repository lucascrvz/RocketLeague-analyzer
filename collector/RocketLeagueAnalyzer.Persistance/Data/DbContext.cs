using Microsoft.EntityFrameworkCore;
using RocketLeagueAnalyzer.Domain.Entities;

namespace RocketLeagueAnalyzer.Persistance.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Match>(e =>
        {
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<MatchEvent>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Payload).HasColumnType("jsonb");

            e.HasOne(x => x.Match).WithMany().HasForeignKey(x => x.MatchId);
        });
    }
}
