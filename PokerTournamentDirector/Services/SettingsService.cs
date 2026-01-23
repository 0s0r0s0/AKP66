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
            settings.ResetToDefaults();
            await SaveSettingsAsync(settings);
        }
    }
}