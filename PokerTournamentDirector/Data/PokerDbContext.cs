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
        public DbSet<Championship> Championships { get; set; }
        public DbSet<ChampionshipMatch> ChampionshipMatches { get; set; }
        public DbSet<ChampionshipStanding> ChampionshipStandings { get; set; }
        public DbSet<ChampionshipLog> ChampionshipLogs { get; set; }
        public DbSet<TournamentLog> TournamentLogs { get; set; }
        public DbSet<PaymentSchedule> PaymentSchedules { get; set; }
        public DbSet<PlayerLog> PlayerLogs { get; set; }


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

        }

        public void EnsureDatabaseCreated()
        {
            Database.EnsureCreated();
        }
    }
}