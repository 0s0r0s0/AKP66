using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokerTournamentDirector.Models
{
    public enum ChampionshipPeriodType
    {
        Annual,        // Annuel
        Quarterly,     // Trimestriel
        Monthly,       // Mensuel
        Custom         // Personnalisé
    }

    public enum ChampionshipPointsMode
    {
        Linear,        // Linéaire : 1er = N, décroissant
        FixedByPosition, // Points fixes par position
        ProportionalPrizePool // Proportionnel au prize pool (ICM)
    }

    public enum ChampionshipTiebreaker
    {
        NumberOfWins,           // Nombre de victoires
        BestIndividualResult,   // Meilleur résultat individuel
        HeadToHead,             // Confrontation directe
        SumOfPositions,         // Somme des positions
        MoreMatchesPlayed       // Plus de manches jouées
    }

    public enum ChampionshipCountingMode
    {
        AllMatches,             // Tous les tournois comptent
        BestXOfSeason,          // Meilleurs X résultats sur la saison
        BestXPerPeriod          // Meilleurs X résultats par période
    }

    public enum ChampionshipRebuyMode
    {
        NoRebuy,                // Aucune recave
        Unlimited,              // Illimitées
        LimitedPerMatch,        // Limitées par manche
        LimitedPerMonth,        // Limitées par mois
        LimitedPerQuarter,      // Limitées par trimestre
        LimitedPerSeason        // Limitées par saison
    }

    public enum ChampionshipStatus
    {
        Upcoming,               // À venir
        Active,                 // En cours
        Completed,              // Terminé
        Archived                // Archivé
    }

    public class Championship
    {
        [Key]
        public int Id { get; set; }

        // === INFORMATIONS GÉNÉRALES ===
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Season { get; set; } = string.Empty; // Ex: "2026-2027"

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? LogoPath { get; set; }

        [MaxLength(7)]
        public string? ThemeColor { get; set; } // Ex: "#00ff88"

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public ChampionshipStatus Status { get; set; } = ChampionshipStatus.Upcoming;

        // === PÉRIODE ET CYCLES ===
        public ChampionshipPeriodType PeriodType { get; set; } = ChampionshipPeriodType.Annual;

        public bool EnableMonthlyStandings { get; set; } = false;
        public bool EnableQuarterlyStandings { get; set; } = false;
        public bool GenerateProvisionalAfterEachMatch { get; set; } = true;

        // === SYSTÈME DE POINTS ===
        public ChampionshipPointsMode PointsMode { get; set; } = ChampionshipPointsMode.Linear;

        // Pour mode Linéaire
        public int LinearFirstPlacePoints { get; set; } = 100;

        // Pour mode Points fixes (JSON)
        public string? FixedPointsTable { get; set; } // JSON: {"1":100,"2":80,"3":60,...}

        // Pour mode Proportionnel
        public int ProportionalTotalPoints { get; set; } = 1000;

        // Points de participation
        public bool EnableParticipationPoints { get; set; } = false;
        public int ParticipationPoints { get; set; } = 10;

        // Départage égalité
        public ChampionshipTiebreaker Tiebreaker1 { get; set; } = ChampionshipTiebreaker.NumberOfWins;
        public ChampionshipTiebreaker? Tiebreaker2 { get; set; }
        public ChampionshipTiebreaker? Tiebreaker3 { get; set; }

        // === COMPTAGE SÉLECTIF ===
        public ChampionshipCountingMode CountingMode { get; set; } = ChampionshipCountingMode.AllMatches;

        public int? BestXOfSeason { get; set; } // Ex: 10 meilleurs résultats
        public int? BestXPerMonth { get; set; }
        public int? BestXPerQuarter { get; set; }
        public int? ExcludeWorstX { get; set; } // Ex: retirer 2 pires

        // === BONUS ET PRIMES ===
        public bool CountBounties { get; set; } = false;
        public int PointsPerBounty { get; set; } = 5;

        public int VictoryBonus { get; set; } = 0;
        public int Top3Bonus { get; set; } = 0;
        public int FirstEliminatedConsolation { get; set; } = 0;

        public bool EnableSeasonPrizes { get; set; } = false;
        public decimal SeasonPrizePool { get; set; } = 0;
        public string? PrizeDistribution { get; set; } // JSON: [50,30,20] en %

        // === PONDÉRATION ===
        public decimal DefaultMatchCoefficient { get; set; } = 1.0m;
        public decimal FinalMatchCoefficient { get; set; } = 2.0m;
        public decimal MainEventCoefficient { get; set; } = 1.5m;

        // === RECAVES ===
        public ChampionshipRebuyMode RebuyMode { get; set; } = ChampionshipRebuyMode.NoRebuy;
        public int? RebuyLimit { get; set; }
        public int RebuyPointsPenalty { get; set; } = 0; // Pénalité en points
        public decimal RebuyPointsMultiplier { get; set; } = 1.0m; // Ex: 0.5 = points réduits de moitié

        // === QUALIFICATION ===
        public bool IsOpenChampionship { get; set; } = true;
        public int? QualificationTopX { get; set; }
        public int? QualificationMinPoints { get; set; }
        public int? QualificationMinMatches { get; set; }

        public bool AllowLateRegistration { get; set; } = true;
        public int? LateRegistrationUntilMatch { get; set; }
        public bool AllowRetroactivePoints { get; set; } = false;

        // === TIMESTAMPS ===
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // === RELATIONS ===
        public virtual ICollection<ChampionshipMatch> Matches { get; set; } = new List<ChampionshipMatch>();
        public virtual ICollection<ChampionshipStanding> Standings { get; set; } = new List<ChampionshipStanding>();
        public virtual ICollection<ChampionshipLog> Logs { get; set; } = new List<ChampionshipLog>();
    }
}
