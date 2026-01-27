using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.ViewModels
{
    public partial class ChampionshipManagementViewModel : ObservableObject
    {
        private ChampionshipService _championshipService;

        [ObservableProperty]
        private ObservableCollection<Championship> _championships = new();

        [ObservableProperty]
        private Championship? _selectedChampionship;

        [ObservableProperty]
        private bool _showArchived = false;

        [ObservableProperty]
        private string _searchText = string.Empty;

        public ChampionshipManagementViewModel(ChampionshipService championshipService)
        {
            _championshipService = championshipService;
        }

        public async Task InitializeAsync()
        {
            await LoadChampionshipsAsync();
        }

        [RelayCommand]
        private async Task LoadChampionshipsAsync()
        {
            var championships = await _championshipService.GetAllChampionshipsAsync(!ShowArchived);

            Championships.Clear();
            foreach (var c in championships)
            {
                Championships.Add(c);
            }
        }

        [RelayCommand]

        private async Task SaveNewChampionshipAsync(Championship championship)
        {
            try
            {
                var created = await _championshipService.CreateChampionshipAsync(championship);
                Championships.Insert(0, created);
                CustomMessageBox.ShowSuccess(
                    $"Championnat '{championship.Name}' créé avec succès !",
                    "Succès");
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur: {ex.Message}", "Erreur");
            }
        }


        [RelayCommand]
        private void CreateChampionship()
        {
            // ✅ Passer un nouveau championnat ET le paramètre isEditMode
            var dialog = new ChampionshipConfigView(new Championship(), false);
            if (dialog.ShowDialog() == true && dialog.Championship != null)
            {
                _ = SaveNewChampionshipAsync(dialog.Championship);
            }
        }

        [RelayCommand]
        private void EditChampionship()
        {
            if (SelectedChampionship == null) return;

            // ✅ Passer le championnat sélectionné ET isEditMode = true
            var dialog = new ChampionshipConfigView(SelectedChampionship, true);
            if (dialog.ShowDialog() == true && dialog.Championship != null)
            {
                _ = UpdateChampionshipAsync(dialog.Championship);
            }
        }

        private async Task UpdateChampionshipAsync(Championship championship)
        {
            try
            {
                await _championshipService.UpdateChampionshipAsync(championship);
                await LoadChampionshipsAsync();
                CustomMessageBox.ShowSuccess("Championnat mis à jour !", "Succès");
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur: {ex.Message}", "Erreur");
            }
        }

        [RelayCommand]
        private async Task ArchiveChampionshipAsync()
        {
            if (SelectedChampionship == null)
            {
                CustomMessageBox.ShowWarning("Veuillez sélectionner un championnat.", "Sélection requise");
                return;
            }

            var result = CustomMessageBox.ShowConfirmation(
                $"Archiver le championnat '{SelectedChampionship.Name}' ?\n\n" +
                "Il restera accessible dans les archives.",
                "Confirmer l'archivage");

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    await _championshipService.ArchiveChampionshipAsync(SelectedChampionship.Id);
                    await LoadChampionshipsAsync();
                    CustomMessageBox.ShowSuccess("Championnat archivé.", "Succès");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.ShowError($"Erreur: {ex.Message}", "Erreur");
                }
            }
        }

        [RelayCommand]
        private async Task DeleteChampionshipAsync()
        {
            if (SelectedChampionship == null)
            {
                CustomMessageBox.ShowWarning("Veuillez sélectionner un championnat.", "Sélection requise");
                return;
            }

            var result = CustomMessageBox.ShowConfirmation(
                $"⚠️ SUPPRIMER DÉFINITIVEMENT le championnat '{SelectedChampionship.Name}' ?\n\n" +
                "Cette action est IRRÉVERSIBLE.\n" +
                "Toutes les données (manches, classements, logs) seront perdues.",
                "⚠️ ATTENTION - Suppression définitive");

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // Double confirmation
                var confirm = CustomMessageBox.ShowConfirmation(
                    "Êtes-vous ABSOLUMENT SÛR ?\n\nTapez OUI pour confirmer.",
                    "Dernière confirmation");

                if (confirm == System.Windows.MessageBoxResult.Yes)
                {
                    try
                    {
                        await _championshipService.DeleteChampionshipAsync(SelectedChampionship.Id);
                        Championships.Remove(SelectedChampionship);
                        SelectedChampionship = null;
                        CustomMessageBox.ShowInformation("Championnat supprimé.", "Suppression effectuée");
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.ShowError($"Erreur: {ex.Message}", "Erreur");
                    }
                }
            }
        }

        [RelayCommand]
        private void OpenDashboard()
        {
            if (SelectedChampionship == null)
            {
                CustomMessageBox.ShowWarning("Veuillez sélectionner un championnat.", "Sélection requise");
                return;
            }

            var dashboard = new ChampionshipDashboardView(SelectedChampionship.Id, _championshipService);
            dashboard.Show();
        }

        partial void OnShowArchivedChanged(bool value)
        {
            _ = LoadChampionshipsAsync();
        }
    }
}
