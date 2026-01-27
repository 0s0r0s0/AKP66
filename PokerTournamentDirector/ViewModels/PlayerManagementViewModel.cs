using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PokerTournamentDirector.ViewModels
{
    public partial class PlayerManagementViewModel : ObservableObject
    {
        private readonly PlayerService _playerService;
        private readonly SettingsService _settingsService;
        private AppSettings? _settings;

        [ObservableProperty] private ObservableCollection<Player> _players = new();
        [ObservableProperty] private Player? _selectedPlayer;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _showActiveOnly = true;

        // Formulaire d'édition
        [ObservableProperty] private bool _isEditing = false;
        [ObservableProperty] private string _editName = string.Empty;
        [ObservableProperty] private string _editNickname = string.Empty;
        [ObservableProperty] private string _editEmail = string.Empty;
        [ObservableProperty] private string _editPhone = string.Empty;
        [ObservableProperty] private string _editCity = string.Empty;
        [ObservableProperty] private string _editNotes = string.Empty;

        // Cotisation
        [ObservableProperty] private int _selectedPaymentTypeIndex = 3; // None par défaut
        [ObservableProperty] private int _selectedInstallmentIndex = 0; // 2 fois par défaut
        [ObservableProperty] private decimal _calculatedFee = 0;
        [ObservableProperty] private string _feeExplanation = "";
        [ObservableProperty] private bool _showInstallmentOptions = false;

        [ObservableProperty] private ObservableCollection<string> _installmentOptions = new();

        // Stats
        [ObservableProperty] private int _totalPlayers = 0;
        [ObservableProperty] private int _activePlayers = 0;
        [ObservableProperty] private int _alertCount = 0;

        // Paiement
        [ObservableProperty] private bool _showPaymentDialog = false;
        [ObservableProperty] private decimal _paymentAmount = 0;

        // Historique
        [ObservableProperty] private bool _showHistory = false;
        [ObservableProperty] private ObservableCollection<PlayerLog> _historyLogs = new();

        public ObservableCollection<string> PaymentTypes { get; } = new()
        {
            "Réglée",
            "Essai",
            "Mensualités",
            "Non réglée"
        };

        public PlayerManagementViewModel(PlayerService playerService, SettingsService settingsService)
        {
            _playerService = playerService;
            _settingsService = settingsService;
        }

        public async Task InitializeAsync()
        {
            _settings = await _settingsService.GetSettingsAsync();
            LoadInstallmentOptions();
            await LoadPlayersAsync();
            CheckAlerts();
        }

        private void LoadInstallmentOptions()
        {
            if (_settings == null) return;

            InstallmentOptions.Clear();
            var options = _settings.InstallmentOptions.Split(',');
            foreach (var opt in options)
            {
                if (int.TryParse(opt.Trim(), out int value))
                {
                    InstallmentOptions.Add($"{value} fois");
                }
            }
        }

        [RelayCommand]
        private async Task LoadPlayersAsync()
        {
            var players = await _playerService.GetAllPlayersAsync(!ShowActiveOnly);

            Players.Clear();
            foreach (var player in players)
            {
                Players.Add(player);
            }

            UpdateStats();
            CheckAlerts();
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadPlayersAsync();
                return;
            }

            var players = await _playerService.SearchPlayersAsync(SearchText);

            Players.Clear();
            foreach (var player in players)
            {
                Players.Add(player);
            }

            UpdateStats();
        }

        [RelayCommand]
        private void NewPlayer()
        {
            SelectedPlayer = null;
            EditName = string.Empty;
            EditNickname = string.Empty;
            EditEmail = string.Empty;
            EditPhone = string.Empty;
            EditCity = string.Empty;
            EditNotes = string.Empty;
            SelectedPaymentTypeIndex = 3; // Non réglée
            SelectedInstallmentIndex = 0;

            CalculateFee();
            IsEditing = true;
        }

        [RelayCommand]
        private void EditPlayer()
        {
            if (SelectedPlayer == null) return;

            EditName = SelectedPlayer.Name;
            EditNickname = SelectedPlayer.Nickname ?? string.Empty;
            EditEmail = SelectedPlayer.Email ?? string.Empty;
            EditPhone = SelectedPlayer.Phone ?? string.Empty;
            EditCity = SelectedPlayer.City ?? string.Empty;
            EditNotes = SelectedPlayer.Notes ?? string.Empty;

            // Déterminer le type de paiement
            SelectedPaymentTypeIndex = SelectedPlayer.PaymentStatus switch
            {
                PaymentStatus.Paid => 0,
                PaymentStatus.Trial => 1,
                PaymentStatus.InProgress => 2,
                _ => 3
            };

            IsEditing = true;
        }

        partial void OnSelectedPaymentTypeIndexChanged(int value)
        {
            ShowInstallmentOptions = (value == 2); // Mensualités
            CalculateFee();
        }

        partial void OnSelectedInstallmentIndexChanged(int value)
        {
            CalculateFee();
        }

        private void CalculateFee()
        {
            if (_settings == null) return;

            var now = DateTime.Now;
            var registrationMonth = now.Month;
            decimal baseFee = _settings.AnnualFee;

            // Si septembre ou août, montant complet
            if (registrationMonth == 9 || registrationMonth == 8)
            {
                CalculatedFee = baseFee;
                FeeExplanation = $"Inscription en {(registrationMonth == 9 ? "septembre" : "août")} : cotisation annuelle complète = {baseFee}€";
                return;
            }

            // Sinon, calcul prorata si activé
            if (_settings.EnableProrata)
            {
                if (_settings.ProrataMode == "monthly")
                {
                    // Calcul mensuel : nombre de mois restants jusqu'à fin d'exercice
                    var fiscalYearEnd = _settings.FiscalYearEnd;
                    var monthsRemaining = ((fiscalYearEnd.Year - now.Year) * 12) + fiscalYearEnd.Month - now.Month + 1;
                    if (monthsRemaining < 0) monthsRemaining = 0;

                    CalculatedFee = Math.Round((baseFee / 10m) * monthsRemaining, 0); // 10 mois (sept à juin)
                    FeeExplanation = $"Prorata mensuel : {monthsRemaining} mois restants × {baseFee / 10m:F2}€ = {CalculatedFee}€";
                }
                else // percentage
                {
                    var fiscalYearStart = _settings.FiscalYearStart;
                    var fiscalYearEnd = _settings.FiscalYearEnd;
                    var totalDays = (fiscalYearEnd - fiscalYearStart).Days;
                    var remainingDays = (fiscalYearEnd - now).Days;

                    var percentage = (decimal)remainingDays / totalDays;
                    CalculatedFee = Math.Round(baseFee * percentage, 0);
                    FeeExplanation = $"Prorata proportionnel : {percentage:P0} de l'exercice restant = {CalculatedFee}€";
                }
            }
            else
            {
                CalculatedFee = baseFee;
                FeeExplanation = $"Cotisation annuelle standard = {baseFee}€";
            }
        }

        [RelayCommand]
        private async Task SavePlayerAsync()
        {
            if (string.IsNullOrWhiteSpace(EditName))
            {
                CustomMessageBox.ShowWarning("Le nom est obligatoire.", "Erreur");
                return;
            }

            if (_settings == null) return;

            try
            {
                Player player;
                bool isNew = SelectedPlayer == null;

                // 1. D'ABORD créer ou récupérer le joueur
                if (isNew)
                {
                    player = new Player
                    {
                        RegistrationDate = DateTime.Now,
                        Name = EditName,
                        Nickname = string.IsNullOrWhiteSpace(EditNickname) ? null : EditNickname,
                        Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail,
                        Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone,
                        City = string.IsNullOrWhiteSpace(EditCity) ? null : EditCity,
                        Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes,
                        TotalDue = CalculatedFee
                    };

                    // SAUVEGARDER D'ABORD pour avoir un ID
                    await _playerService.CreatePlayerAsync(player);

                    // Maintenant on peut logger la création
                    await LogAction(player, "Création du joueur", $"Inscription le {player.RegistrationDate:dd/MM/yyyy}");
                }
                else
                {
                    player = SelectedPlayer!;

                    // Mise à jour des infos de base
                    player.Name = EditName;
                    player.Nickname = string.IsNullOrWhiteSpace(EditNickname) ? null : EditNickname;
                    player.Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail;
                    player.Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone;
                    player.City = string.IsNullOrWhiteSpace(EditCity) ? null : EditCity;
                    player.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes;
                    player.TotalDue = CalculatedFee;
                }

                // 2. Gestion du paiement (APRES que le joueur existe en base)
                string paymentAction = "";
                string paymentDetails = "";

                switch (SelectedPaymentTypeIndex)
                {
                    case 0: // Réglée
                        player.PaymentStatus = PaymentStatus.Paid;
                        player.Paid = CalculatedFee;
                        player.NextDueDate = null;
                        player.TrialEnd = null;
                        paymentAction = "Cotisation réglée intégralement";
                        paymentDetails = $"Montant : {CalculatedFee}€";
                        break;

                    case 1: // Essai
                        player.PaymentStatus = PaymentStatus.Trial;
                        player.TrialEnd = DateTime.Now.AddDays(_settings.TrialPeriodWeeks * 7);
                        player.Paid = 0;
                        player.NextDueDate = null;
                        paymentAction = "Période d'essai";
                        paymentDetails = $"Fin le {player.TrialEnd:dd/MM/yyyy}";
                        break;

                    case 2: // Mensualités
                        player.PaymentStatus = PaymentStatus.InProgress;
                        player.Paid = 0;

                        // Créer l'échéancier
                        var installmentText = InstallmentOptions[SelectedInstallmentIndex];
                        var installmentCount = int.Parse(installmentText.Replace(" fois", ""));
                        player.InstallmentCount = installmentCount;

                        await CreatePaymentSchedule(player, installmentCount);
                        paymentAction = "Paiement échelonné";
                        paymentDetails = $"{installmentCount} mensualités de {CalculatedFee / installmentCount:F0}€ environ";
                        break;

                    case 3: // Non réglée
                        player.PaymentStatus = PaymentStatus.None;
                        player.Paid = 0;
                        player.NextDueDate = null;
                        player.TrialEnd = null;
                        paymentAction = "Inscription sans paiement";
                        paymentDetails = "";
                        break;
                }

                // 3. Mettre à jour le joueur avec les infos de paiement
                if (isNew)
                {
                    await _playerService.UpdatePlayerAsync(player);
                }
                else
                {
                    await _playerService.UpdatePlayerAsync(player);
                    await LogAction(player, "Modification du joueur", "");
                }

                // 4. Logger l'action de paiement (si applicable)
                if (!string.IsNullOrEmpty(paymentAction))
                {
                    await LogAction(player, paymentAction, paymentDetails);
                }

                IsEditing = false;
                await LoadPlayersAsync();

                CustomMessageBox.ShowSuccess(
                    isNew ? "Joueur créé avec succès !" : "Joueur modifié avec succès !",
                    "Succès"
                );
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur lors de la sauvegarde : {ex.Message}", "Erreur");
            }
        }

        private async Task CreatePaymentSchedule(Player player, int installmentCount)
        {
            if (_settings == null) return;

            // Supprimer l'ancien échéancier si existe
            await _playerService.DeletePaymentSchedulesAsync(player.Id);

            var totalAmount = CalculatedFee;
            var baseAmount = Math.Floor(totalAmount / installmentCount / 5) * 5; // Arrondi à 5€
            var remainder = totalAmount - (baseAmount * (installmentCount - 1));

            var adminDay = _settings.AdministrativeDayOfWeek; // Au lieu de _settings.AdministrativeDay
            var currentDate = GetNextAdministrativeDay(DateTime.Now, adminDay);

            var schedules = new List<PaymentSchedule>();

            for (int i = 0; i < installmentCount; i++)
            {
                var amount = (i == installmentCount - 1) ? remainder : baseAmount;

                schedules.Add(new PaymentSchedule
                {
                    PlayerId = player.Id,
                    DueDate = currentDate,
                    Amount = amount,
                    IsPaid = false
                });

                // Prochaine date administrative (1 mois plus tard)
                currentDate = currentDate.AddMonths(1);
                currentDate = GetNextAdministrativeDay(currentDate, adminDay); ;
            }

            await _playerService.CreatePaymentSchedulesAsync(schedules);

            // Mettre à jour la prochaine échéance
            player.NextDueDate = schedules.First().DueDate;
        }

        private DateTime GetNextAdministrativeDay(DateTime from, DayOfWeek targetDay)
        {
            var daysUntilTarget = ((int)targetDay - (int)from.DayOfWeek + 7) % 7;
            if (daysUntilTarget == 0 && from.TimeOfDay > TimeSpan.Zero)
                daysUntilTarget = 7;

            return from.Date.AddDays(daysUntilTarget);
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            SelectedPlayer = null;
        }

        [RelayCommand]
        private async Task DeletePlayerAsync()
        {
            if (SelectedPlayer == null) return;

            var result = CustomMessageBox.ShowConfirmation(
                $"Voulez-vous vraiment désactiver le joueur '{SelectedPlayer.Name}' ?",
                "Confirmation");

            if (result == MessageBoxResult.Yes)
            {
                SelectedPlayer.Status = PlayerStatus.Inactive;
                await _playerService.UpdatePlayerAsync(SelectedPlayer);
                await LogAction(SelectedPlayer, "Désactivation", "Joueur désactivé");
                await LoadPlayersAsync();
            }
        }

        [RelayCommand]
        private void OpenPayment()
        {
            if (SelectedPlayer == null) return;

            var remaining = SelectedPlayer.TotalDue - SelectedPlayer.Paid;
            PaymentAmount = remaining > 0 ? remaining : 0;
            ShowPaymentDialog = true;
        }

        [RelayCommand]
        private async Task ProcessPaymentAsync()
        {
            if (SelectedPlayer == null || PaymentAmount <= 0) return;

            try
            {
                SelectedPlayer.Paid += PaymentAmount;

                // Marquer les échéances comme payées
                var schedules = await _playerService.GetPaymentSchedulesAsync(SelectedPlayer.Id);
                decimal amountToDistribute = PaymentAmount;

                foreach (var schedule in schedules.Where(s => !s.IsPaid).OrderBy(s => s.DueDate))
                {
                    if (amountToDistribute >= schedule.Amount)
                    {
                        schedule.IsPaid = true;
                        schedule.PaidDate = DateTime.Now;
                        amountToDistribute -= schedule.Amount;
                        await _playerService.UpdatePaymentScheduleAsync(schedule);
                    }
                    else if (amountToDistribute > 0)
                    {
                        // Paiement partiel - on laisse l'échéance non payée mais on note
                        break;
                    }
                }

                // Mettre à jour la prochaine échéance
                var nextUnpaid = schedules.Where(s => !s.IsPaid).OrderBy(s => s.DueDate).FirstOrDefault();
                SelectedPlayer.NextDueDate = nextUnpaid?.DueDate;

                // Vérifier si tout est payé
                if (SelectedPlayer.Paid >= SelectedPlayer.TotalDue)
                {
                    SelectedPlayer.PaymentStatus = PaymentStatus.Paid;
                }

                await _playerService.UpdatePlayerAsync(SelectedPlayer);
                await LogAction(SelectedPlayer, "Encaissement", $"Montant : {PaymentAmount}€ - Total payé : {SelectedPlayer.Paid}€/{SelectedPlayer.TotalDue}€");

                ShowPaymentDialog = false;
                await LoadPlayersAsync();

                CustomMessageBox.ShowSuccess($"Paiement de {PaymentAmount}€ enregistré !", "Succès");
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur : {ex.Message}", "Erreur");
            }
        }

        [RelayCommand]
        private void CancelPayment()
        {
            ShowPaymentDialog = false;
        }

        [RelayCommand]
        private async Task ShowHistoryAsync()
        {
            if (SelectedPlayer == null) return;

            var logs = await _playerService.GetPlayerLogsAsync(SelectedPlayer.Id);
            HistoryLogs.Clear();
            foreach (var log in logs.OrderByDescending(l => l.Timestamp))
            {
                HistoryLogs.Add(log);
            }

            ShowHistory = true;
        }

        [RelayCommand]
        private void CloseHistory()
        {
            ShowHistory = false;
        }

        private async Task LogAction(Player player, string action, string? details = null)
        {
            var log = new PlayerLog
            {
                PlayerId = player.Id,
                Action = action,
                Details = details,
                Timestamp = DateTime.Now
            };

            await _playerService.CreateLogAsync(log);
        }

        [RelayCommand]
        private async Task ImportCsvAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Fichiers CSV (*.csv)|*.csv|Tous les fichiers (*.*)|*.*",
                Title = "Importer des joueurs depuis un CSV"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var csvContent = await File.ReadAllTextAsync(openFileDialog.FileName);
                    int importedCount = await _playerService.ImportPlayersFromCsvAsync(csvContent);

                    CustomMessageBox.ShowInformation($"{importedCount} joueur(s) importé(s) avec succès !", "Import réussi");

                    await LoadPlayersAsync();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.ShowError($"Erreur lors de l'import : {ex.Message}", "Erreur");
                }
            }
        }

        [RelayCommand]
        private async Task ExportCsvAsync()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Fichiers CSV (*.csv)|*.csv",
                Title = "Exporter les joueurs",
                FileName = $"joueurs_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var csv = "Nom,Pseudo,Email,Téléphone,Ville,Date Inscription,Statut,Cotisation,Payé\n";

                    foreach (var player in Players)
                    {
                        csv += $"{player.Name},{player.Nickname},{player.Email},{player.Phone},{player.City}," +
                               $"{player.RegistrationDate:dd/MM/yyyy},{player.Status},{player.TotalDue},{player.Paid}\n";
                    }

                    await File.WriteAllTextAsync(saveFileDialog.FileName, csv);

                    CustomMessageBox.ShowSuccess("Export réussi !", "Succès");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.ShowError($"Erreur lors de l'export : {ex.Message}", "Erreur");
                }
            }
        }

        partial void OnShowActiveOnlyChanged(bool value)
        {
            _ = LoadPlayersAsync();
        }

        private void UpdateStats()
        {
            TotalPlayers = Players.Count;
            ActivePlayers = Players.Count(p => p.Status == PlayerStatus.Active);
        }

        private void CheckAlerts()
        {
            if (_settings == null) return;

            AlertCount = 0;

            foreach (var player in Players)
            {
                // Période d'essai terminée
                if (player.PaymentStatus == PaymentStatus.Trial &&
                    player.TrialEnd.HasValue &&
                    player.TrialEnd.Value < DateTime.Now)
                {
                    AlertCount++;
                }

                // Aucun paiement après 1 mois
                if (player.PaymentStatus == PaymentStatus.None &&
                    player.Paid == 0 &&
                    (DateTime.Now - player.RegistrationDate).TotalDays > 30)
                {
                    AlertCount++;
                }

                // Mensualité en retard d'une semaine administrative
                if (player.NextDueDate.HasValue)
                {
                    var weeksPassed = (DateTime.Now - player.NextDueDate.Value).TotalDays / 7;
                    if (weeksPassed >= 1)
                    {
                        AlertCount++;
                    }
                }
            }
        }
    }
}