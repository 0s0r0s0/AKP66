using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokerTournamentDirector.Models
{
    public class AppSettings
    {
        [Key]
        public int Id { get; set; }

        // ========== COULEURS ==========
        public string BackgroundColor { get; set; } = "#1a1a2e";
        public string CardColor { get; set; } = "#16213e";
        public string AccentColor { get; set; } = "#00ff88";
        public string WarningColor { get; set; } = "#ffd700";
        public string DangerColor { get; set; } = "#e94560";

        // ========== SONS ==========
        public bool EnableSounds { get; set; } = true;
        public bool SoundOnPauseResume { get; set; } = true;
        public bool SoundOn60Seconds { get; set; } = true;
        public bool SoundOn10Seconds { get; set; } = true;
        public bool SoundOnCountdown { get; set; } = true;
        public bool SoundOnLevelChange { get; set; } = true;
        public bool SoundOnKill { get; set; } = true;
        public bool SoundOnRebuy { get; set; } = true;
        public bool SoundOnUndoKill { get; set; } = true;
        public bool SoundOnWin { get; set; } = true;
        public bool SoundOnBreak{ get; set; } = true;
        public bool SoundOnStart { get; set; } = true;

        // ========== PARAMÈTRES GÉNÉRAUX ==========
        public int DefaultLevelDuration { get; set; } = 20;
        public int DefaultBreakDuration { get; set; } = 15;

        // ========= Configuration SMTP ==========
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool SmtpEnableSsl { get; set; } = true;
        public string SmtpUsername { get; set; } = "ak66poker@gmail.com"; // ton email Gmail
        public string SmtpPassword { get; set; } = "gcik ordc xbqp aode"; // mot de passe d'application
        public string SmtpFromEmail { get; set; } = "ak66poker@gmail.com";
        public string SmtpFromName { get; set; } = "AK-66 POKER CLUB";

        // ========== NOUVEAUX PARAMÈTRES ADMINISTRATIFS ==========

        // Exercice
        public int FiscalYearStartMonth { get; set; } = 9; // Septembre
        public int FiscalYearStartDay { get; set; } = 1;
        public int FiscalYearEndMonth { get; set; } = 6; // Juin
        public int FiscalYearEndDay { get; set; } = 30;
        public int AdministrativeDay { get; set; } = 1; // Lundi (DayOfWeek)

        // Cotisation
        public int AnnualFee { get; set; } = 100; // Montant cotisation annuelle
        public int TrialPeriodWeeks { get; set; } = 4; // Période d'essai en semaines

        // Paiements échelonnés
        public string InstallmentOptions { get; set; } = "2,3,4,6,10"; // Options de mensualités

        // Prorata
        public bool EnableProrata { get; set; } = true;
        public string ProrataMode { get; set; } = "monthly"; // "monthly" ou "percentage"

        // Propriétés calculées pour compatibilité
        [NotMapped]
        public DateTime FiscalYearStart => new DateTime(DateTime.Now.Year, FiscalYearStartMonth, FiscalYearStartDay);

        [NotMapped]
        public DateTime FiscalYearEnd => new DateTime(DateTime.Now.Year + 1, FiscalYearEndMonth, FiscalYearEndDay);

        [NotMapped]
        public DayOfWeek AdministrativeDayOfWeek => (DayOfWeek)AdministrativeDay;
    }
}