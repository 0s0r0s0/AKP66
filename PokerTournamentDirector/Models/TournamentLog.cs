
using System;
using System.ComponentModel.DataAnnotations;

namespace PokerTournamentDirector.Models
{
    /// <summary>
    /// Log des actions effectuées pendant un tournoi
    /// </summary>
    public class TournamentLog
    {
        [Key]
        public int Id { get; set; }

        public int TournamentId { get; set; }
        public virtual Tournament Tournament { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; }

        public int Level { get; set; }

        public int PlayersRemaining { get; set; }

        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;
    }
}
