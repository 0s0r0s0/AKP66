using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokerTournamentDirector.Models
{
    // ==================== JOUEUR ====================

    // ==================== ENUMS ====================
    public enum PaymentStatus
    {
        Paid,           // Réglée
        InProgress,     // En cours
        Trial,          // Essai
        None            // Aucun paiement
    }

    public enum PlayerStatus
    {
        Active,
        Inactive
    }

    // ==================== JOUEUR ====================
    public class Player
    {
        [Key]
        public int Id { get; set; }

        // Informations personnelles
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Nickname { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        public string? PhotoPath { get; set; }
        public string? Notes { get; set; }

        // Dates
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastTournamentDate { get; set; }
        public DateTime? TrialEnd { get; set; }

        // Statut
        public bool IsActive { get; set; } = true; // Conservé pour compatibilité
        public PlayerStatus Status { get; set; } = PlayerStatus.Active;

        // Cotisation
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.None;

        public decimal TotalDue { get; set; } = 0; // Montant total à payer
        public decimal Paid { get; set; } = 0; // Montant déjà payé

        public int? InstallmentCount { get; set; } // Nombre de mensualités (null si paiement unique)
        public DateTime? NextDueDate { get; set; } // Prochaine échéance

        // Statistiques (conservées pour historique)
        public int TotalTournamentsPlayed { get; set; } = 0;
        public int TotalWins { get; set; } = 0;
        public int TotalITM { get; set; } = 0;
        public decimal TotalWinnings { get; set; } = 0;

        // Relations
        public virtual ICollection<TournamentPlayer> TournamentParticipations { get; set; } = new List<TournamentPlayer>();
        public virtual ICollection<PlayerRebuy> Rebuys { get; set; } = new List<PlayerRebuy>();
        public virtual ICollection<PaymentSchedule> PaymentSchedules { get; set; } = new List<PaymentSchedule>();
        public virtual ICollection<PlayerLog> Logs { get; set; } = new List<PlayerLog>();
    }

    // ==================== ÉCHÉANCIER DE PAIEMENT ====================
    public class PaymentSchedule
    {
        [Key]
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public virtual Player Player { get; set; } = null!;

        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; } = false;
        public DateTime? PaidDate { get; set; }
    }

    // ==================== LOGS DES ACTIONS ====================
    public class PlayerLog
    {
        [Key]
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public virtual Player Player { get; set; } = null!;

        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Action { get; set; } = string.Empty; // "Création", "Paiement", "Modification", etc.
        public string? Details { get; set; }
    }

    // ==================== RECAVE JOUEUR ====================
    public class PlayerRebuy
    {
        [Key]
        public int Id { get; set; }

        public int PlayerId { get; set; }
        [ForeignKey(nameof(PlayerId))]
        public virtual Player? Player { get; set; }

        public int TournamentId { get; set; }
        [ForeignKey(nameof(TournamentId))]
        public virtual Tournament? Tournament { get; set; }

        public DateTime RebuyDate { get; set; } = DateTime.Now;

        public decimal Amount { get; set; }

        public int RebuyNumber { get; set; }
    }

    // ==================== STRUCTURE DE BLINDS ====================
    public class BlindStructure
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public virtual ICollection<BlindLevel> Levels { get; set; } = new List<BlindLevel>();
    }

    // ==================== NIVEAU DE BLINDS ====================
    public class BlindLevel
    {
        [Key]
        public int Id { get; set; }

        public int BlindStructureId { get; set; }

        [ForeignKey(nameof(BlindStructureId))]
        public virtual BlindStructure? BlindStructure { get; set; }

        public int LevelNumber { get; set; }

        public int SmallBlind { get; set; }

        public int BigBlind { get; set; }

        public int Ante { get; set; } = 0;

        public int DurationMinutes { get; set; } = 20;

        public bool IsBreak { get; set; } = false;

        [MaxLength(100)]
        public string? BreakName { get; set; }
    }

    // ==================== TOURNOI ====================
    public class Tournament
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.Now;

        public int? TemplateId { get; set; }
        [ForeignKey(nameof(TemplateId))]
        public virtual TournamentTemplate? Template { get; set; }

        public TournamentType Type { get; set; } = TournamentType.Freezeout;
        public string Currency { get; set; } = "EUR";

        // Config financière
        public decimal BuyIn { get; set; }
        public decimal Rake { get; set; } = 0;
        public RakeType RakeType { get; set; } = RakeType.Percentage;
        public decimal? RebuyAmount { get; set; }
        public decimal? AddOnAmount { get; set; }

        // Limites de recaves
        public bool AllowRebuys { get; set; } = false;
        public int? RebuyLimit { get; set; }
        public RebuyLimitType RebuyLimitType { get; set; } = RebuyLimitType.ByNumber;
        public int? RebuyMaxLevel { get; set; }
        public int? RebuyUntilPlayersLeft { get; set; }
        public int? RebuyStack { get; set; }
        public int MaxRebuysPerPlayer { get; set; } = 3;
        public int RebuyPeriodMonths { get; set; } = 1;

        // Add-ons
        public bool AllowAddOn { get; set; } = false;
        public int? AddOnStack { get; set; }
        public int? AddOnAtLevel { get; set; }

        // Bounty
        public bool AllowBounty { get; set; } = false;
        public decimal? BountyAmount { get; set; }
        public BountyType BountyType { get; set; } = BountyType.Fixed;

        // Payout
        public string? PayoutStructureJson { get; set; }

        // Config technique
        public int StartingStack { get; set; } = 10000;
        public int MaxPlayers { get; set; } = 100;
        public int SeatsPerTable { get; set; } = 9;
        public int LateRegistrationLevels { get; set; } = 4;

        public int BlindStructureId { get; set; }
        [ForeignKey(nameof(BlindStructureId))]
        public virtual BlindStructure? BlindStructure { get; set; }

        // État du tournoi
        public TournamentStatus Status { get; set; } = TournamentStatus.Pending;

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int CurrentLevel { get; set; } = 1;
        public DateTime? CurrentLevelStartTime { get; set; }

        // Stats
        public decimal TotalPrizePool { get; set; } = 0;
        public int TotalRebuys { get; set; } = 0;
        public int TotalAddOns { get; set; } = 0;

        public string? Notes { get; set; }

        // Relations
        public virtual ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
        public virtual ICollection<PokerTable> Tables { get; set; } = new List<PokerTable>();
        public virtual ICollection<PlayerRebuy> Rebuys { get; set; } = new List<PlayerRebuy>();

        public virtual ICollection<ChampionshipMatch> ChampionshipMatches { get; set; } = new List<ChampionshipMatch>();

    }

    // ==================== JOUEUR DANS UN TOURNOI ====================
    public class TournamentPlayer
    {
        [Key]
        public int Id { get; set; }

        public int TournamentId { get; set; }
        [ForeignKey(nameof(TournamentId))]
        public virtual Tournament? Tournament { get; set; }

        public int PlayerId { get; set; }
        [ForeignKey(nameof(PlayerId))]
        public virtual Player? Player { get; set; }

        // Placement - Lien vers la table
        public int? TableId { get; set; }
        [ForeignKey(nameof(TableId))]
        public virtual PokerTable? Table { get; set; }

        public int? SeatNumber { get; set; }
        public bool IsLocked { get; set; } = false;

        // Stack & rebuys
        public int CurrentStack { get; set; }
        public int RebuyCount { get; set; } = 0;
        public bool HasAddOn { get; set; } = false;

        // Résultat
        public bool IsEliminated { get; set; } = false;
        public int? FinishPosition { get; set; }
        public DateTime? EliminationTime { get; set; }
        public int? EliminatedByPlayerId { get; set; }

        public int? EliminatedById { get; set; } 
        public DateTime? EliminatedAt { get; set; }

        public decimal? Winnings { get; set; }

        // Points championnat
        public int ChampionshipPoints { get; set; } = 0;
        public int BountyKills { get; set; } = 0;
    }

    // ==================== TABLE ====================
    public class PokerTable
    {
        [Key]
        public int Id { get; set; }

        public int TournamentId { get; set; }
        [ForeignKey(nameof(TournamentId))]
        public virtual Tournament? Tournament { get; set; }

        public int TableNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public int MaxSeats { get; set; } = 9;

        public virtual ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    }

    // ==================== ENUMS ====================
    public enum TournamentType
    {
        Freezeout,
        RebuyUnlimited,
        RebuyLimited,
        DoubleChance,
        Bounty,
        Progressive
    }

    public enum TournamentStatus
    {
        Pending,
        Registration,
        Running,
        Paused,
        Finished,
        Cancelled
    }
}
