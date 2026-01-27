using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokerTournamentDirector.Models
{
    /// <summary>
    /// Lien entre un championnat et un tournoi
    /// </summary>
    public class ChampionshipMatch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChampionshipId { get; set; }

        [Required]
        public int TournamentId { get; set; }

        /// <summary>
        /// Numéro de la manche dans le championnat (1, 2, 3...)
        /// </summary>
        public int MatchNumber { get; set; }

        /// <summary>
        /// Date du tournoi
        /// </summary>
        public DateTime MatchDate { get; set; }

        /// <summary>
        /// Coefficient de pondération pour cette manche (1.0 = normal, 2.0 = finale...)
        /// </summary>
        public decimal Coefficient { get; set; } = 1.0m;

        /// <summary>
        /// Est-ce une finale ?
        /// </summary>
        public bool IsFinal { get; set; } = false;

        /// <summary>
        /// Est-ce un Main Event ?
        /// </summary>
        public bool IsMainEvent { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey(nameof(ChampionshipId))]
        public virtual Championship Championship { get; set; } = null!;

        [ForeignKey(nameof(TournamentId))]
        public virtual Tournament Tournament { get; set; } = null!;
    }
}
