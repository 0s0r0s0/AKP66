using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PokerTournamentDirector.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;

        [ObservableProperty]
        private string _backgroundColor = "#1a1a2e";

        [ObservableProperty]
        private string _cardColor = "#16213e";

        [ObservableProperty]
        private string _accentColor = "#00ff88";

        [ObservableProperty]
        private string _warningColor = "#ffd700";

        [ObservableProperty]
        private string _dangerColor = "#e94560";

        // Options sons
        [ObservableProperty]
        private bool _enableSounds = true;

        [ObservableProperty]
        private bool _soundOnPauseResume = true;

        [ObservableProperty]
        private bool _soundOn60Seconds = true;

        [ObservableProperty]
        private bool _soundOn10Seconds = true;

        [ObservableProperty]
        private bool _soundOnCountdown = true;

        [ObservableProperty]
        private bool _soundOnLevelChange = true;

        // Paramètres généraux
        [ObservableProperty]
        private int _defaultLevelDuration = 20;

        [ObservableProperty]
        private int _defaultBreakDuration = 15;

        [ObservableProperty]
        private Brush _backgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e")!);

        [ObservableProperty]
        private Brush _cardBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e")!);

        [ObservableProperty]
        private Brush _accentBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88")!);

        public SettingsViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task InitializeAsync()
        {
            var settings = await _settingsService.GetSettingsAsync();

            BackgroundColor = settings.BackgroundColor;
            CardColor = settings.CardColor;
            AccentColor = settings.AccentColor;
            WarningColor = settings.WarningColor;
            DangerColor = settings.DangerColor;
            EnableSounds = settings.EnableSounds;
            SoundOnPauseResume = settings.SoundOnPauseResume;
            SoundOn60Seconds = settings.SoundOn60Seconds;
            SoundOn10Seconds = settings.SoundOn10Seconds;
            SoundOnCountdown = settings.SoundOnCountdown;
            SoundOnLevelChange = settings.SoundOnLevelChange;
            DefaultLevelDuration = settings.DefaultLevelDuration;
            DefaultBreakDuration = settings.DefaultBreakDuration;

            UpdateBrushes();
        }

        partial void OnBackgroundColorChanged(string value) => UpdateBrushes();
        partial void OnCardColorChanged(string value) => UpdateBrushes();
        partial void OnAccentColorChanged(string value) => UpdateBrushes();

        private void UpdateBrushes()
        {
            try
            {
                BackgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BackgroundColor)!);
                CardBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(CardColor)!);
                AccentBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(AccentColor)!);
            }
            catch { }
        }

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                settings.BackgroundColor = BackgroundColor;
                settings.CardColor = CardColor;
                settings.AccentColor = AccentColor;
                settings.WarningColor = WarningColor;
                settings.DangerColor = DangerColor;
                settings.EnableSounds = EnableSounds;
                settings.SoundOnPauseResume = SoundOnPauseResume;
                settings.SoundOn60Seconds = SoundOn60Seconds;
                settings.SoundOn10Seconds = SoundOn10Seconds;
                settings.SoundOnCountdown = SoundOnCountdown;
                settings.SoundOnLevelChange = SoundOnLevelChange;
                settings.DefaultLevelDuration = DefaultLevelDuration;
                settings.DefaultBreakDuration = DefaultBreakDuration;

                await _settingsService.SaveSettingsAsync(settings);

                CustomMessageBox.ShowSuccess("Paramètres sauvegardés !", "Succès");
            }
            catch (System.Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur : {ex.Message}", "Erreur");
            }
        }

        [RelayCommand]
        private async Task ResetToDefaultsAsync()
        {
            if (CustomMessageBox.ShowConfirmation("Réinitialiser tous les paramètres ?", "Confirmation") == MessageBoxResult.Yes)
            {
                await _settingsService.ResetToDefaultsAsync();
                await InitializeAsync();
                CustomMessageBox.ShowSuccess("Paramètres réinitialisés !", "Succès");
            }
        }

        [RelayCommand]
        private void TestSound()
        {
            if (EnableSounds)
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
        }
    }
}
