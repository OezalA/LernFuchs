using LernFuchs.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LernFuchs.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<VocabularyWord> VocabularyWords => Set<VocabularyWord>();
    public DbSet<VocabularyProgress> VocabularyProgress => Set<VocabularyProgress>();
    public DbSet<ReadingPassage> ReadingPassages => Set<ReadingPassage>();
    public DbSet<ComprehensionQuestion> ComprehensionQuestions => Set<ComprehensionQuestion>();
    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<DailyActivity> DailyActivities => Set<DailyActivity>();
    public DbSet<SystemState> SystemStates => Set<SystemState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enums als lesbare Strings speichern (SQLite-freundlich).
        modelBuilder.Entity<VocabularyWord>(e =>
        {
            e.Property(w => w.Article).HasConversion<string>();
            e.Property(w => w.WordType).HasConversion<string>();
            e.Property(w => w.Difficulty).HasConversion<string>();
            e.HasIndex(w => w.Word);
        });

        modelBuilder.Entity<ReadingPassage>(e =>
        {
            e.Property(p => p.Difficulty).HasConversion<string>();
        });

        modelBuilder.Entity<ComprehensionQuestion>(e =>
        {
            e.Property(q => q.QuestionType).HasConversion<string>();
            e.HasOne(q => q.ReadingPassage)
             .WithMany(p => p.Questions)
             .HasForeignKey(q => q.ReadingPassageId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // 1:1 Vokabel <-> Fortschritt
        modelBuilder.Entity<VocabularyProgress>(e =>
        {
            e.Ignore(p => p.Mastered); // berechnete Eigenschaft, nicht persistieren
            e.HasOne(p => p.VocabularyWord)
             .WithOne(w => w.Progress)
             .HasForeignKey<VocabularyProgress>(p => p.VocabularyWordId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Jeder Abzeichen-Code kommt höchstens einmal vor.
        modelBuilder.Entity<Achievement>(e => e.HasIndex(a => a.Code).IsUnique());

        // Pro Tag ein Aktivitäts-Eintrag.
        modelBuilder.Entity<DailyActivity>(e => e.HasIndex(a => a.Date).IsUnique());

        base.OnModelCreating(modelBuilder);
    }
}
