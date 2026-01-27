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
        private readonly AudioService _audioService;


        // Couleurs
        [ObservableProperty] private string _backgroundColor = "#1a1a2e";
        [ObservableProperty] private string _cardColor = "#16213e";
        [ObservableProperty] private string _accentColor = "#00ff88";
        [ObservableProperty] private string _warningColor = "#ffd700";
        [ObservableProperty] private string _dangerColor = "#e94560";

        // Sons
        [ObservableProperty] private bool _enableSounds = true;
        [ObservableProperty] private bool _soundOnPauseResume = true;
        [ObservableProperty] private bool _soundOn60Seconds = true;
        [ObservableProperty] private bool _soundOn10Seconds = true;
        [ObservableProperty] private bool _soundOnCountdown = true;
        [ObservableProperty] private bool _soundOnLevelChange = true;
        [ObservableProperty] private bool _soundOnWin = true;
        [ObservableProperty] private bool _soundOnBreak = true;
        [ObservableProperty] private bool _soundOnStart = true;
        [ObservableProperty] private bool _soundOnKill = true;
        [ObservableProperty] private bool _soundOnUndoKill = true;
        [ObservableProperty] private bool _soundOnRebuy = true;

        // Paramètres généraux
        [ObservableProperty] private int _defaultLevelDuration = 20;
        [ObservableProperty] private int _defaultBreakDuration = 15;

        // Paramètres administratifs
        [ObservableProperty] private int _fiscalYearStartDay = 1;
        [ObservableProperty] private int _fiscalYearStartMonth = 9;
        [ObservableProperty] private int _fiscalYearEndDay = 30;
        [ObservableProperty] private int _fiscalYearEndMonth = 6;
        [ObservableProperty] private int _administrativeDay = 1; // Lundi
        [ObservableProperty] private decimal _annualFee = 150m;
        [ObservableProperty] private int _trialPeriodWeeks = 4;
        [ObservableProperty] private string _installmentOptions = "2,3,4,6,10";
        [ObservableProperty] private bool _enableProrata = true;
        [ObservableProperty] private string _prorataMode = "monthly";

        // Brushes pour aperçu
        [ObservableProperty] private Brush _backgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e")!);
        [ObservableProperty] private Brush _cardBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e")!);
        [ObservableProperty] private Brush _accentBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88")!);

        public SettingsViewModel(SettingsService settingsService, AudioService audioService)
        {
            _settingsService = settingsService;
            _audioService = audioService;
        }

        public async Task InitializeAsync()
        {
            var settings = await _settingsService.GetSettingsAsync();

            // Couleurs
            BackgroundColor = settings.BackgroundColor;
            CardColor = settings.CardColor;
            AccentColor = settings.AccentColor;
            WarningColor = settings.WarningColor;
            DangerColor = settings.DangerColor;

            // Sons
            EnableSounds = settings.EnableSounds;
            SoundOnPauseResume = settings.SoundOnPauseResume;
            SoundOn60Seconds = settings.SoundOn60Seconds;
            SoundOn10Seconds = settings.SoundOn10Seconds;
            SoundOnCountdown = settings.SoundOnCountdown;
            SoundOnLevelChange = settings.SoundOnLevelChange;
            SoundOnStart = settings.SoundOnStart;
            SoundOnKill = settings.SoundOnKill;
            SoundOnUndoKill = settings.SoundOnUndoKill;
            SoundOnRebuy = settings.SoundOnRebuy;
            SoundOnWin = settings.SoundOnWin;
            SoundOnBreak = settings.SoundOnBreak;

            // Généraux
            DefaultLevelDuration = settings.DefaultLevelDuration;
            DefaultBreakDuration = settings.DefaultBreakDuration;

            // Administratifs
            FiscalYearStartDay = settings.FiscalYearStartDay;
            FiscalYearStartMonth = settings.FiscalYearStartMonth;
            FiscalYearEndDay = settings.FiscalYearEndDay;
            FiscalYearEndMonth = settings.FiscalYearEndMonth;
            AdministrativeDay = settings.AdministrativeDay;
            AnnualFee = settings.AnnualFee;
            TrialPeriodWeeks = settings.TrialPeriodWeeks;
            InstallmentOptions = settings.InstallmentOptions;
            EnableProrata = settings.EnableProrata;
            ProrataMode = settings.ProrataMode;

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
        private void PlaySound(string soundFileName)
        {
            if (!EnableSounds || string.IsNullOrEmpty(soundFileName)) return;
            _audioService?.PlaySound(soundFileName);
        }

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                // Couleurs
                settings.BackgroundColor = BackgroundColor;
                settings.CardColor = CardColor;
                settings.AccentColor = AccentColor;
                settings.WarningColor = WarningColor;
                settings.DangerColor = DangerColor;

                // Sons
                settings.EnableSounds = EnableSounds;
                settings.SoundOnPauseResume = SoundOnPauseResume;
                settings.SoundOn60Seconds = SoundOn60Seconds;
                settings.SoundOn10Seconds = SoundOn10Seconds;
                settings.SoundOnCountdown = SoundOnCountdown;
                settings.SoundOnStart = SoundOnStart;
                settings.SoundOnWin = SoundOnWin;
                settings.SoundOnKill = SoundOnKill;
                settings.SoundOnUndoKill = SoundOnUndoKill;
                settings.SoundOnBreak = SoundOnBreak;
                settings.SoundOnRebuy = SoundOnRebuy;
                settings.SoundOnLevelChange = SoundOnLevelChange;

                // Généraux
                settings.DefaultLevelDuration = DefaultLevelDuration;
                settings.DefaultBreakDuration = DefaultBreakDuration;

                // Administratifs
                settings.FiscalYearStartDay = FiscalYearStartDay;
                settings.FiscalYearStartMonth = FiscalYearStartMonth;
                settings.FiscalYearEndDay = FiscalYearEndDay;
                settings.FiscalYearEndMonth = FiscalYearEndMonth;
                settings.AdministrativeDay = AdministrativeDay;
                settings.AnnualFee = AnnualFee;
                settings.TrialPeriodWeeks = TrialPeriodWeeks;
                settings.InstallmentOptions = InstallmentOptions;
                settings.EnableProrata = EnableProrata;
                settings.ProrataMode = ProrataMode;

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
                PlaySound("test.mp3");
            }
        }
    }
}