using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

/// v2.0 - GEstion des structures de blinds
namespace PokerTournamentDirector.ViewModels
{
    /// <summary>
    /// ViewModel principal pour l'éditeur de structures de blinds.
    /// Gère le chargement, l'édition, la création, la suppression et la génération automatique de structures.
    /// </summary>
    public partial class BlindStructureEditorViewModel : ObservableObject
    {
        private readonly BlindStructureService _blindService;

        #region Propriétés observables - Liste et sélection

        [ObservableProperty]
        private ObservableCollection<BlindStructure> structures = new();

        [ObservableProperty]
        private BlindStructure? selectedStructure;

        [ObservableProperty]
        private ObservableCollection<BlindLevel> currentLevels = new();

        #endregion

        #region Propriétés observables - Informations structure

        [ObservableProperty]
        private string structureName = string.Empty;

        [ObservableProperty]
        private string structureDescription = string.Empty;

        [ObservableProperty]
        private int totalDuration;

        [ObservableProperty]
        private bool isEditing;

        #endregion

        #region Propriétés observables - Générateur automatique

        [ObservableProperty]
        private int autoTargetDuration = 120;

        [ObservableProperty]
        private int autoStartingBlind = 25;

        [ObservableProperty]
        private int autoLevelDuration = 20;

        [ObservableProperty]
        private bool autoWithAnte;

        [ObservableProperty]
        private int autoNumberOfBreaks = 2;

        [ObservableProperty]
        private int autoBreakDuration = 15;

        [ObservableProperty]
        private int autoStartingStack = 10000;

        [ObservableProperty]
        private int autoAveragePlayers = 10;

        #endregion

        #region Constructeur et initialisation

        public BlindStructureEditorViewModel(BlindStructureService blindService)
        {
            _blindService = blindService;
        }

        /// <summary>
        /// Initialise le ViewModel en chargeant les structures existantes.
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadStructuresAsync();
        }

        #endregion

        #region Chargement et sélection des structures

        [RelayCommand]
        private async Task LoadStructuresAsync()
        {
            var structures = await _blindService.GetAllStructuresAsync();
            Structures.Clear();

            foreach (var structure in structures)
            {
                Structures.Add(structure);
            }
        }

        partial void OnSelectedStructureChanged(BlindStructure? value)
        {
            if (value != null)
            {
                LoadStructure(value);
            }
        }

        private void LoadStructure(BlindStructure structure)
        {
            StructureName = structure.Name;
            StructureDescription = structure.Description ?? string.Empty;

            CurrentLevels.Clear();
            foreach (var level in structure.Levels.OrderBy(l => l.LevelNumber))
            {
                CurrentLevels.Add(level);
            }

            CalculateTotalDuration();
            IsEditing = true;
        }

        #endregion

        #region Création et modification manuelle



        [RelayCommand]
        private void NewStructure()
        {
            SelectedStructure = null;
            StructureName = "Nouvelle structure";
            StructureDescription = string.Empty;
            CurrentLevels.Clear();

            // Ajouter un premier niveau par défaut
            AddLevel();
            IsEditing = true;
        }

        [RelayCommand]
        private void AddLevel()
        {
            int nextLevelNumber = CurrentLevels.Any() ? CurrentLevels.Max(l => l.LevelNumber) + 1 : 1;

            // Suggestion basée sur le dernier niveau
            int sb = 25, bb = 50, ante = 0;
            if (CurrentLevels.Any())
            {
                var lastLevel = CurrentLevels.OrderBy(l => l.LevelNumber).Last();
                if (!lastLevel.IsBreak)
                {
                    sb = lastLevel.SmallBlind * 2;
                    bb = lastLevel.BigBlind * 2;
                    ante = lastLevel.Ante > 0 ? lastLevel.Ante * 2 : 0;
                }
            }

            CurrentLevels.Add(new BlindLevel
            {
                LevelNumber = nextLevelNumber,
                SmallBlind = sb,
                BigBlind = bb,
                Ante = ante,
                DurationMinutes = 20,
                IsBreak = false
            });

            CalculateTotalDuration();
        }

        [RelayCommand]
        private void AddBreak()
        {
            int nextLevelNumber = CurrentLevels.Any() ? CurrentLevels.Max(l => l.LevelNumber) + 1 : 1;

            CurrentLevels.Add(new BlindLevel
            {
                LevelNumber = nextLevelNumber,
                SmallBlind = 0,
                BigBlind = 0,
                Ante = 0,
                DurationMinutes = 15,
                IsBreak = true,
                BreakName = "Pause"
            });

            CalculateTotalDuration();
        }

        [RelayCommand]
        private void RemoveLevel(BlindLevel level)
        {
            CurrentLevels.Remove(level);

            // Renumérotation des niveaux
            int levelNum = 1;
            foreach (var l in CurrentLevels.OrderBy(x => x.LevelNumber))
            {
                l.LevelNumber = levelNum++;
            }

            CalculateTotalDuration();
        }

        #endregion

        #region Sauvegarde, suppression et duplication

        [RelayCommand]
        private async Task SaveStructureAsync()
        {
            if (string.IsNullOrWhiteSpace(StructureName))
            {
                CustomMessageBox.ShowWarning("Le nom de la structure est obligatoire.", "Erreur");
                return;
            }

            if (!CurrentLevels.Any())
            {
                CustomMessageBox.ShowWarning("Ajoutez au moins un niveau.", "Erreur");
                return;
            }

            try
            {
                if (SelectedStructure == null)
                {
                    // Nouvelle structure
                    var newStructure = new BlindStructure
                    {
                        Name = StructureName,
                        Description = StructureDescription
                    };

                    var created = await _blindService.CreateStructureAsync(newStructure);

                    foreach (var level in CurrentLevels)
                    {
                        level.BlindStructureId = created.Id;
                        created.Levels.Add(level);  // ← Ajout crucial : associe les niveaux à la structure
                    }

                    await _blindService.UpdateStructureAsync(created);
                }
                else
                {
                    // Modification existante
                    SelectedStructure.Name = StructureName;
                    SelectedStructure.Description = StructureDescription;
                    SelectedStructure.Levels = CurrentLevels.ToList();

                    await _blindService.UpdateStructureAsync(SelectedStructure);
                }

                CustomMessageBox.ShowSuccess("Structure sauvegardée avec succès !", "Succès");
                await LoadStructuresAsync();
                IsEditing = false;
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur lors de la sauvegarde : {ex.Message}", "Erreur");
            }
        }

        [RelayCommand]
        private async Task DeleteStructureAsync()
        {
            if (SelectedStructure == null) return;

            var result = CustomMessageBox.ShowConfirmation(
                $"Voulez-vous vraiment supprimer la structure '{SelectedStructure.Name}' ?",
                "Confirmation");

            if (result == MessageBoxResult.Yes)
            {
                await _blindService.DeleteStructureAsync(SelectedStructure.Id);
                await LoadStructuresAsync();
                IsEditing = false;
                CustomMessageBox.ShowSuccess("Structure supprimée.", "Succès");
            }
        }

        [RelayCommand]
        private async Task DuplicateStructureAsync()
        {
            if (SelectedStructure == null) return;

            var newName = $"{SelectedStructure.Name} (Copie)";
            await _blindService.DuplicateStructureAsync(SelectedStructure.Id, newName);
            await LoadStructuresAsync();

            CustomMessageBox.ShowSuccess($"Structure dupliquée : {newName}", "Succès");
        }

        [RelayCommand]
        private async Task ToggleFavorite(BlindStructure structure)
        {
            try
            {
                // Si on met en favori, retirer le favori des autres
                if (!structure.IsFavorite)
                {
                    var allStructures = await _blindService.GetAllStructuresAsync();
                    foreach (var s in allStructures.Where(s => s.IsFavorite && s.Id != structure.Id))
                    {
                        s.IsFavorite = false;
                        await _blindService.UpdateStructureAsync(s);
                    }
                }

                structure.IsFavorite = !structure.IsFavorite;
                await _blindService.UpdateStructureAsync(structure);
                await LoadStructuresAsync();

                CustomMessageBox.ShowSuccess(
                    structure.IsFavorite
                        ? $"⭐ '{structure.Name}' défini comme favori !"
                        : $"'{structure.Name}' retiré des favoris.",
                    "Favori");
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Génération automatique

        [RelayCommand]
        private void GenerateAuto()
        {
            var generated = _blindService.GenerateStructure(
                $"Auto {AutoTargetDuration}min",
                AutoTargetDuration,
                AutoStartingBlind,
                AutoLevelDuration,
                AutoWithAnte,
                AutoNumberOfBreaks,
                AutoBreakDuration,
                AutoStartingStack,
                AutoAveragePlayers);

            StructureName = generated.Name;
            StructureDescription = generated.Description ?? string.Empty;

            CurrentLevels.Clear();
            foreach (var level in generated.Levels)
            {
                CurrentLevels.Add(level);
            }

            CalculateTotalDuration();
            IsEditing = true;

            CustomMessageBox.ShowSuccess(
                $"Structure générée automatiquement !\n\n" +
                $"Niveaux de jeu : {CurrentLevels.Count(l => !l.IsBreak)}\n" +
                $"Pauses : {CurrentLevels.Count(l => l.IsBreak)}\n" +
                $"Durée totale : {TotalDuration} min ({TotalDuration / 60}h{TotalDuration % 60:D2})",
                "Structure générée");
        }

        #endregion

        #region Utilitaires

        private void CalculateTotalDuration()
        {
            TotalDuration = CurrentLevels.Sum(l => l.DurationMinutes);
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            SelectedStructure = null;
            CurrentLevels.Clear();
        }

        #endregion
    }
}