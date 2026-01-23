using System;
using System.ComponentModel.DataAnnotations;

namespace PokerTournamentDirector.Models
{
    public class AppSettings
    {
        [Key]
        public int Id { get; set; }

        // Thème couleurs
        public string BackgroundColor { get; set; } = "#1a1a2e";
        public string CardColor { get; set; } = "#16213e";
        public string AccentColor { get; set; } = "#00ff88";
        public string WarningColor { get; set; } = "#ffd700";
        public string DangerColor { get; set; } = "#e94560";

        // Paramètres sons
        public bool EnableSounds { get; set; } = true;
        public bool SoundOnPauseResume { get; set; } = true;
        public bool SoundOn60Seconds { get; set; } = true;
        public bool SoundOn10Seconds { get; set; } = true;
        public bool SoundOnCountdown { get; set; } = true;
        public bool SoundOnLevelChange { get; set; } = true;

        // Paramètres généraux
        public int DefaultLevelDuration { get; set; } = 20;
        public int DefaultBreakDuration { get; set; } = 15;

        // Date de dernière modification
        public DateTime LastModified { get; set; } = DateTime.Now;

        public void ResetToDefaults()
        {
            BackgroundColor = "#1a1a2e";
            CardColor = "#16213e";
            AccentColor = "#00ff88";
            WarningColor = "#ffd700";
            DangerColor = "#e94560";
            EnableSounds = true;
            SoundOnPauseResume = true;
            SoundOn60Seconds = true;
            SoundOn10Seconds = true;
            SoundOnCountdown = true;
            SoundOnLevelChange = true;
            DefaultLevelDuration = 20;
            DefaultBreakDuration = 15;
            LastModified = DateTime.Now;
        }
    }
}
