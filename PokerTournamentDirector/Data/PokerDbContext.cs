using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Models;
using System;
using System.IO;

namespace PokerTournamentDirector.Data
{
    public class PokerDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<TournamentPlayer> TournamentPlayers { get; set; }
        public DbSet<PokerTable> PokerTables { get; set; }
        public DbSet<BlindStructure> BlindStructures { get; set; }
        public DbSet<BlindLevel> BlindLevels { get; set; }
        public DbSet<PlayerRebuy> PlayerRebuys { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }
        public DbSet<TournamentTemplate> TournamentTemplates { get; set; }

        // Alias pour compatibilité avec le code existant
        public DbSet<PokerTable> TournamentTables => PokerTables;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Chemin de la base de données locale
                string dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PokerTournamentDirector",
                    "poker.db"
                );

                // Créer le dossier si nécessaire
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des relations
            modelBuilder.Entity<BlindLevel>()
                .HasOne(bl => bl.BlindStructure)
                .WithMany(bs => bs.Levels)
                .HasForeignKey(bl => bl.BlindStructureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TournamentPlayer>()
                .HasOne(tp => tp.Tournament)
                .WithMany(t => t.Players)
                .HasForeignKey(tp => tp.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TournamentPlayer>()
                .HasOne(tp => tp.Player)
                .WithMany(p => p.TournamentParticipations)
                .HasForeignKey(tp => tp.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relation TournamentPlayer -> PokerTable
            modelBuilder.Entity<TournamentPlayer>()
                .HasOne(tp => tp.Table)
                .WithMany(t => t.Players)
                .HasForeignKey(tp => tp.TableId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PokerTable>()
                .HasOne(tt => tt.Tournament)
                .WithMany(t => t.Tables)
                .HasForeignKey(tt => tt.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerRebuy>()
                .HasOne(pr => pr.Player)
                .WithMany(p => p.Rebuys)
                .HasForeignKey(pr => pr.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerRebuy>()
                .HasOne(pr => pr.Tournament)
                .WithMany(t => t.Rebuys)
                .HasForeignKey(pr => pr.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index pour optimiser les recherches
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.Name);

            modelBuilder.Entity<Tournament>()
                .HasIndex(t => t.Date);

            modelBuilder.Entity<BlindLevel>()
                .HasIndex(bl => new { bl.BlindStructureId, bl.LevelNumber });

            // Seed data : structures de blinds par défaut
            SeedDefaultBlindStructures(modelBuilder);
        }

        private void SeedDefaultBlindStructures(ModelBuilder modelBuilder)
        {
            // Structure Standard (2h environ)
            var standardStructure = new BlindStructure
            {
                Id = 1,
                Name = "Standard (2h)",
                Description = "Structure classique pour home games"
            };

            modelBuilder.Entity<BlindStructure>().HasData(standardStructure);

            var standardLevels = new[]
            {
                new BlindLevel { Id = 1, BlindStructureId = 1, LevelNumber = 1, SmallBlind = 25, BigBlind = 50, Ante = 0, DurationMinutes = 20 },
                new BlindLevel { Id = 2, BlindStructureId = 1, LevelNumber = 2, SmallBlind = 50, BigBlind = 100, Ante = 0, DurationMinutes = 20 },
                new BlindLevel { Id = 3, BlindStructureId = 1, LevelNumber = 3, SmallBlind = 75, BigBlind = 150, Ante = 0, DurationMinutes = 20 },
                new BlindLevel { Id = 4, BlindStructureId = 1, LevelNumber = 4, SmallBlind = 100, BigBlind = 200, Ante = 25, DurationMinutes = 15, IsBreak = true, BreakName = "Pause 15 min" },
                new BlindLevel { Id = 5, BlindStructureId = 1, LevelNumber = 5, SmallBlind = 150, BigBlind = 300, Ante = 25, DurationMinutes = 20 },
                new BlindLevel { Id = 6, BlindStructureId = 1, LevelNumber = 6, SmallBlind = 200, BigBlind = 400, Ante = 50, DurationMinutes = 20 },
                new BlindLevel { Id = 7, BlindStructureId = 1, LevelNumber = 7, SmallBlind = 300, BigBlind = 600, Ante = 75, DurationMinutes = 20 },
                new BlindLevel { Id = 8, BlindStructureId = 1, LevelNumber = 8, SmallBlind = 400, BigBlind = 800, Ante = 100, DurationMinutes = 20 },
                new BlindLevel { Id = 9, BlindStructureId = 1, LevelNumber = 9, SmallBlind = 500, BigBlind = 1000, Ante = 100, DurationMinutes = 20 },
                new BlindLevel { Id = 10, BlindStructureId = 1, LevelNumber = 10, SmallBlind = 600, BigBlind = 1200, Ante = 200, DurationMinutes = 20 },
            };

            modelBuilder.Entity<BlindLevel>().HasData(standardLevels);

            // Structure Turbo (1h30)
            var turboStructure = new BlindStructure
            {
                Id = 2,
                Name = "Turbo (1h30)",
                Description = "Structure rapide, niveaux de 12 minutes"
            };

            modelBuilder.Entity<BlindStructure>().HasData(turboStructure);

            var turboLevels = new[]
            {
                new BlindLevel { Id = 11, BlindStructureId = 2, LevelNumber = 1, SmallBlind = 25, BigBlind = 50, Ante = 0, DurationMinutes = 12 },
                new BlindLevel { Id = 12, BlindStructureId = 2, LevelNumber = 2, SmallBlind = 50, BigBlind = 100, Ante = 0, DurationMinutes = 12 },
                new BlindLevel { Id = 13, BlindStructureId = 2, LevelNumber = 3, SmallBlind = 100, BigBlind = 200, Ante = 25, DurationMinutes = 12 },
                new BlindLevel { Id = 14, BlindStructureId = 2, LevelNumber = 4, SmallBlind = 150, BigBlind = 300, Ante = 25, DurationMinutes = 10, IsBreak = true, BreakName = "Pause 10 min" },
                new BlindLevel { Id = 15, BlindStructureId = 2, LevelNumber = 5, SmallBlind = 200, BigBlind = 400, Ante = 50, DurationMinutes = 12 },
                new BlindLevel { Id = 16, BlindStructureId = 2, LevelNumber = 6, SmallBlind = 300, BigBlind = 600, Ante = 75, DurationMinutes = 12 },
                new BlindLevel { Id = 17, BlindStructureId = 2, LevelNumber = 7, SmallBlind = 500, BigBlind = 1000, Ante = 100, DurationMinutes = 12 },
                new BlindLevel { Id = 18, BlindStructureId = 2, LevelNumber = 8, SmallBlind = 800, BigBlind = 1600, Ante = 200, DurationMinutes = 12 },
            };

            modelBuilder.Entity<BlindLevel>().HasData(turboLevels);
        }

        public void EnsureDatabaseCreated()
        {
            Database.EnsureCreated();
        }
    }
}