using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokerTournamentDirector.Models
{
    // ==================== MODÈLE DE TOURNOI (Template) ====================
    public class TournamentTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Type & Finance
        public TournamentTemplateType Type { get; set; } = TournamentTemplateType.Cash;
        public string Currency { get; set; } = "EUR"; // EUR, USD, GBP, POINTS

        public decimal BuyIn { get; set; }
        public decimal Rake { get; set; } = 0;
        public RakeType RakeType { get; set; } = RakeType.Percentage;

        // Structure & Technique
        public int BlindStructureId { get; set; }
        [ForeignKey(nameof(BlindStructureId))]
        public virtual BlindStructure? BlindStructure { get; set; }

        public int StartingStack { get; set; } = 10000;
        public int MaxPlayers { get; set; } = 100;
        public int SeatsPerTable { get; set; } = 9;
        public int LateRegLevels { get; set; } = 4;

        // Recaves
        public bool AllowRebuys { get; set; } = false;
        public decimal? RebuyAmount { get; set; }
        public int? RebuyLimit { get; set; } // null = illimité
        public RebuyLimitType RebuyLimitType { get; set; } = RebuyLimitType.ByNumber;
        public int? RebuyMaxLevel { get; set; } // Jusqu'au niveau X
        public int? RebuyUntilPlayersLeft { get; set; } // Jusqu'à X joueurs restants
        public int? RebuyStack { get; set; } // null = même que starting stack
        public int MaxRebuysPerPlayer { get; set; } = 3;
        public int RebuyPeriodMonths { get; set; } = 1;

        // Add-ons
        public bool AllowAddOn { get; set; } = false;
        public decimal? AddOnAmount { get; set; }
        public int? AddOnStack { get; set; }
        public int? AddOnAtLevel { get; set; } // À quel niveau proposer l'add-on

        // Bounty
        public bool AllowBounty { get; set; } = false;
        public decimal? BountyAmount { get; set; }
        public BountyType BountyType { get; set; } = BountyType.Fixed;

        // Payout
        public string? PayoutStructureJson { get; set; } // Structure de payout par défaut en JSON

        // Dates
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastModified { get; set; }

        // Relations
        public virtual ICollection<Tournament> TournamentsCreated { get; set; } = new List<Tournament>();
    }

    // ==================== ENUMS POUR TEMPLATES ====================
    public enum TournamentTemplateType
    {
        Cash,       // Avec argent réel
        Freeroll,   // Gratuit, pas d'argent
        Points      // Uniquement des points (championnat)
    }

    public enum RakeType
    {
        Percentage, // % du buy-in
        Fixed       // Montant fixe
    }

    /// <summary>
    /// Type de limitation pour les rebuys
    /// </summary>
    public enum RebuyLimitType
    {
        ByNumber,              // X recaves maximum par joueur
        ByLevel,               // Jusqu'au niveau X
        ByPeriod,              // X rebuys par période de temps (mois)
        UntilXPlayersLeft,     // Jusqu'à ce qu'il reste X joueurs
        Combined,              // Plusieurs conditions combinées
        Unlimited              // Pas de limite
    }

    public enum BountyType
    {
        Fixed,       // Montant fixe par élimination
        Progressive  // Augmente à chaque élimination
    }

    // ==================== STRUCTURE DE PAYOUT ====================
    public class PayoutStructure
    {
        public string Type { get; set; } = "percentage"; // "percentage" ou "fixed"
        public List<PayoutPosition> Payouts { get; set; } = new();
        public int MinPlayers { get; set; } = 10; // Payout activé si >= X joueurs
        public int? BubblePosition { get; set; } // Position bubble (optionnel)
    }

    public class PayoutPosition
    {
        public int Position { get; set; }
        public decimal? Percentage { get; set; } // Si type = percentage
        public decimal? Amount { get; set; }     // Si type = fixed
    }
}