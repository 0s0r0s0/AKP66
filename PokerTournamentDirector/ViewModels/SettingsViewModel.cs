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

        // MAIL
        [ObservableProperty] private string _smtpServer = "smtp.gmail.com";
        [ObservableProperty] private int _smtpPort = 587;
        [ObservableProperty] private bool _smtpEnableSsl = true;
        [ObservableProperty] private string _smtpUsername = "";
        [ObservableProperty] private string _smtpPassword = "";
        [ObservableProperty] private string _smtpFromEmail = "";
        private readonly EmailService _emailService;

        // Paramètres généraux
        [ObservableProperty] private int _defaultLevelDuration = 20;
        [ObservableProperty] private int _defaultBreakDuration = 15;

        // Paramètres administratifs
        [ObservableProperty]
        private DateTime _fiscalYearStartDate = new DateTime(DateTime.Now.Year, 9, 1);

        [ObservableProperty]
        private DateTime _fiscalYearEndDate = new DateTime(DateTime.Now.Year + 1, 6, 30);
        [ObservableProperty] private int _administrativeDay = 1; // Lundi
        [ObservableProperty] private int _annualFee = 100;
        [ObservableProperty] private int _trialPeriodWeeks = 4;
        [ObservableProperty] private string _installmentOptions = "2,3,4,6,10";
        [ObservableProperty] private bool _enableProrata = true;
        [ObservableProperty] private string _prorataMode = "monthly";

        // Brushes pour aperçu
        [ObservableProperty] private Brush _backgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e")!);
        [ObservableProperty] private Brush _cardBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e")!);
        [ObservableProperty] private Brush _accentBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88")!);

        public SettingsViewModel(SettingsService settingsService, AudioService audioService, EmailService emailService)
        {
            _settingsService = settingsService;
            _audioService = audioService;
            _emailService = emailService;
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

            // Mails
            SmtpServer = settings.SmtpServer;
            SmtpPort = settings.SmtpPort;
            SmtpEnableSsl = settings.SmtpEnableSsl;
            SmtpUsername = settings.SmtpUsername;
            SmtpPassword = settings.SmtpPassword;
            SmtpFromEmail = settings.SmtpFromEmail;

            // Généraux
            DefaultLevelDuration = settings.DefaultLevelDuration;
            DefaultBreakDuration = settings.DefaultBreakDuration;

            // NOUVEAU : Charger les dates depuis jour/mois
            FiscalYearStartDate = new DateTime(
                DateTime.Now.Year,
                settings.FiscalYearStartMonth,
                settings.FiscalYearStartDay);

            FiscalYearEndDate = new DateTime(
                FiscalYearStartDate.AddMonths(9).Year, // Année suivante probable
                settings.FiscalYearEndMonth,
                settings.FiscalYearEndDay);

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
            settings.SoundOnLevelChange = SoundOnLevelChange;
            settings.SoundOnStart = SoundOnStart;
            settings.SoundOnKill = SoundOnKill;
            settings.SoundOnUndoKill = SoundOnUndoKill;
            settings.SoundOnRebuy = SoundOnRebuy;
            settings.SoundOnWin = SoundOnWin;
            settings.SoundOnBreak = SoundOnBreak;

            // Mails
            settings.SmtpServer = SmtpServer;
            settings.SmtpPort = SmtpPort;
            settings.SmtpEnableSsl = SmtpEnableSsl;
            settings.SmtpUsername = SmtpUsername;
            settings.SmtpPassword = SmtpPassword;
            settings.SmtpFromEmail = SmtpFromEmail;

            // Généraux
            settings.DefaultLevelDuration = DefaultLevelDuration;
            settings.DefaultBreakDuration = DefaultBreakDuration;

            // NOUVEAU : Sauvegarder jour/mois depuis DateTime
            settings.FiscalYearStartDay = FiscalYearStartDate.Day;
            settings.FiscalYearStartMonth = FiscalYearStartDate.Month;
            settings.FiscalYearEndDay = FiscalYearEndDate.Day;
            settings.FiscalYearEndMonth = FiscalYearEndDate.Month;

            settings.AdministrativeDay = AdministrativeDay;
            settings.AnnualFee = AnnualFee;
            settings.TrialPeriodWeeks = TrialPeriodWeeks;
            settings.InstallmentOptions = InstallmentOptions;
            settings.EnableProrata = EnableProrata;
            settings.ProrataMode = ProrataMode;

            await _settingsService.SaveSettingsAsync(settings);

            CustomMessageBox.ShowSuccess("✅ Paramètres sauvegardés avec succès !", "Succès");
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

        [RelayCommand]
        private async Task TestEmailConnection()
        {
            var (success, message) = await _emailService.TestConnectionAsync();

            if (success)
                CustomMessageBox.ShowSuccess(message, "Test Email");
            else
                CustomMessageBox.ShowError(message, "Erreur Email");
        }
    }
}