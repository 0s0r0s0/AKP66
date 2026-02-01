using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.Services
{
    public class SettingsService
    {
        private readonly PokerDbContext _context;
        private AppSettings? _cachedSettings;

        public SettingsService(PokerDbContext context)
        {
            _context = context;
        }

        public async Task<AppSettings> GetSettingsAsync()
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            var settings = await _context.AppSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new AppSettings();
                _context.AppSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            _cachedSettings = settings;
            return settings;
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            _context.AppSettings.Update(settings);
            await _context.SaveChangesAsync();
            _cachedSettings = settings;
        }

        public async Task ResetToDefaultsAsync()
        {
            var settings = await GetSettingsAsync();

            // Réinitialiser aux valeurs par défaut
            settings.BackgroundColor = "#1a1a2e";
            settings.CardColor = "#16213e";
            settings.AccentColor = "#00ff88";
            settings.WarningColor = "#ffd700";
            settings.DangerColor = "#e94560";
            settings.EnableSounds = true;
            settings.SoundOnPauseResume = true;
            settings.SoundOn60Seconds = true;
            settings.SoundOn10Seconds = true;
            settings.SoundOnCountdown = true;
            settings.SoundOnLevelChange = true;
            settings.SoundOnBreak = true;
            settings.SoundOnKill = true;
            settings.SoundOnUndoKill = true;
            settings.SoundOnRebuy = true;
            settings.SoundOnWin = true;
            settings.SoundOnStart = true;
            settings.DefaultLevelDuration = 20;
            settings.DefaultBreakDuration = 15;

            // Paramètres administratifs
            settings.FiscalYearStartMonth = 9;
            settings.FiscalYearStartDay = 1;
            settings.FiscalYearEndMonth = 6;
            settings.FiscalYearEndDay = 30;
            settings.AdministrativeDay = 1;
            settings.AnnualFee = 100;
            settings.TrialPeriodWeeks = 4;
            settings.InstallmentOptions = "2,3,4,6,10";
            settings.EnableProrata = true;
            settings.ProrataMode = "monthly";

            await SaveSettingsAsync(settings);
        }
    }
}