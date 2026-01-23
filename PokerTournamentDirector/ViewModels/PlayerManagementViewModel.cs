using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System;
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

        [ObservableProperty]
        private ObservableCollection<Player> _players = new();

        [ObservableProperty]
        private Player? _selectedPlayer;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _showActiveOnly = true;

        // Formulaire d'édition
        [ObservableProperty]
        private bool _isEditing = false;

        [ObservableProperty]
        private string _editName = string.Empty;

        [ObservableProperty]
        private string _editNickname = string.Empty;

        [ObservableProperty]
        private string _editEmail = string.Empty;

        [ObservableProperty]
        private string _editPhone = string.Empty;

        [ObservableProperty]
        private string _editNotes = string.Empty;

        [ObservableProperty]
        private int _totalPlayers = 0;

        [ObservableProperty]
        private int _activePlayers = 0;

        public PlayerManagementViewModel(PlayerService playerService)
        {
            _playerService = playerService;
        }

        public async Task InitializeAsync()
        {
            await LoadPlayersAsync();
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
            EditNotes = string.Empty;
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
            EditNotes = SelectedPlayer.Notes ?? string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        private async Task SavePlayerAsync()
        {
            if (string.IsNullOrWhiteSpace(EditName))
            {
                CustomMessageBox.ShowWarning("Le nom est obligatoire.", "Erreur");
                return;
            }

            try
            {
                if (SelectedPlayer == null)
                {
                    // Nouveau joueur
                    var newPlayer = new Player
                    {
                        Name = EditName,
                        Nickname = string.IsNullOrWhiteSpace(EditNickname) ? null : EditNickname,
                        Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail,
                        Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone,
                        Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes
                    };

                    await _playerService.CreatePlayerAsync(newPlayer);
                }
                else
                {
                    // Modification
                    SelectedPlayer.Name = EditName;
                    SelectedPlayer.Nickname = string.IsNullOrWhiteSpace(EditNickname) ? null : EditNickname;
                    SelectedPlayer.Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail;
                    SelectedPlayer.Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone;
                    SelectedPlayer.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes;

                    await _playerService.UpdatePlayerAsync(SelectedPlayer);
                }

                IsEditing = false;
                await LoadPlayersAsync();
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur lors de la sauvegarde : {ex.Message}", "Erreur");
            }
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
                $"Voulez-vous vraiment désactiver le joueur '{SelectedPlayer.Name}' ?\n\n" +
                "Le joueur sera marqué comme inactif mais ses données seront conservées.",
                "Confirmation");

            if (result == MessageBoxResult.Yes)
            {
                await _playerService.DeletePlayerAsync(SelectedPlayer.Id);
                await LoadPlayersAsync();
            }
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
                    var csv = "Nom,Pseudo,Email,Téléphone,Tournois,Victoires,ITM,Gains\n";

                    foreach (var player in Players)
                    {
                        csv += $"{player.Name},{player.Nickname},{player.Email},{player.Phone}," +
                               $"{player.TotalTournamentsPlayed},{player.TotalWins},{player.TotalITM},{player.TotalWinnings}\n";
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
            ActivePlayers = Players.Count(p => p.IsActive);
        }
    }
}