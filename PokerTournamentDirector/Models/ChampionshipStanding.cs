using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokerTournamentDirector.Models
{
    /// <summary>
    /// Classement d'un joueur dans un championnat (cache calculé)
    /// </summary>
    public class ChampionshipStanding
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChampionshipId { get; set; }

        [Required]
        public int PlayerId { get; set; }

        // === STATISTIQUES GLOBALES ===
        public int TotalPoints { get; set; } = 0;
        public int CurrentPosition { get; set; } = 0;
        public int? PreviousPosition { get; set; }

        public int MatchesPlayed { get; set; } = 0;
        public int Victories { get; set; } = 0;
        public int Top3Finishes { get; set; } = 0;

        public decimal AveragePosition { get; set; } = 0;
        public int? BestPosition { get; set; }
        public int? WorstPosition { get; set; }

        public int TotalBounties { get; set; } = 0;
        public decimal TotalWinnings { get; set; } = 0;

        // === POINTS PAR PÉRIODE (JSON) ===
        /// <summary>
        /// Points mensuels : {"2026-01": 150, "2026-02": 200, ...}
        /// </summary>
        public string? MonthlyPoints { get; set; }

        /// <summary>
        /// Points trimestriels : {"2026-Q1": 450, "2026-Q2": 380, ...}
        /// </summary>
        public string? QuarterlyPoints { get; set; }

        // === RECAVES ===
        public int RebuysUsed { get; set; } = 0;

        // === STATISTIQUES AVANCÉES (calculées) ===
        public double PositionStdDev { get; set; } = 0; // Écart-type = régularité
        public decimal ROI { get; set; } = 0; // Return on Investment %

        // === TEMPS DE JEU ===
        public int TotalMinutesPlayed { get; set; } = 0;
        public int AverageMinutesPerMatch { get; set; } = 0;

        // === ÉLIMINATIONS ===
        public string? EliminatedMostByPlayerId { get; set; } // JSON: {"PlayerId": count}
        public string? EliminatedMostPlayerId { get; set; } // JSON: {"PlayerId": count}

        // === STATUT ===
        public bool IsActive { get; set; } = true;
        public bool IsQualified { get; set; } = false;

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey(nameof(ChampionshipId))]
        public virtual Championship Championship { get; set; } = null!;

        [ForeignKey(nameof(PlayerId))]
        public virtual Player Player { get; set; } = null!;
    }
}
