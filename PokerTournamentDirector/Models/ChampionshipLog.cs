using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokerTournamentDirector.Models
{
    public enum ChampionshipLogAction
    {
        // Création/Modification
        ChampionshipCreated,
        ChampionshipModified,
        ChampionshipArchived,
        ChampionshipDeleted,

        // Manches
        MatchAdded,
        MatchRemoved,
        MatchCoefficientModified,

        // Joueurs
        PlayerAdded,
        PlayerRemoved,
        PlayerStatusChanged,

        // Points
        PointsAdjustedManually,
        PenaltyApplied,
        BonusGranted,
        StandingsRecalculated,

        // Classements
        IntermediateStandingGenerated,
        StandingExported,
        FinalStandingGenerated,

        // Recaves
        RebuyUsed,
        RebuyLimitChanged
    }

    public class ChampionshipLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChampionshipId { get; set; }

        [Required]
        public ChampionshipLogAction Action { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? Username { get; set; } // Pour multi-utilisateurs futur

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Détails avant modification (JSON)
        /// </summary>
        public string? BeforeData { get; set; }

        /// <summary>
        /// Détails après modification (JSON)
        /// </summary>
        public string? AfterData { get; set; }

        /// <summary>
        /// Joueur concerné (si applicable)
        /// </summary>
        public int? PlayerId { get; set; }

        /// <summary>
        /// Manche concernée (si applicable)
        /// </summary>
        public int? MatchId { get; set; }

        // Navigation
        [ForeignKey(nameof(ChampionshipId))]
        public virtual Championship Championship { get; set; } = null!;
    }
}
