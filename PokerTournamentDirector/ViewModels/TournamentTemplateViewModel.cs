using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace PokerTournamentDirector.ViewModels
{
    public partial class TournamentTemplateViewModel : ObservableObject
    {
        #region Propriétés
        private readonly TournamentTemplateService _templateService;
        private readonly BlindStructureService _blindService;

        // Collections et sélection
        [ObservableProperty] private ObservableCollection<TournamentTemplate> _templates = new();
        [ObservableProperty] private ObservableCollection<BlindStructure> _blindStructures = new();
        [ObservableProperty] private TournamentTemplate? _selectedTemplate;

        // Propriété calculée
        public bool ShowFinanceSection => EditType == TournamentTemplateType.Cash;

        // Champs d'édition
        [ObservableProperty] private string _editName = string.Empty;
        [ObservableProperty] private string _editDescription = string.Empty;
        [ObservableProperty] private TournamentTemplateType _editType = TournamentTemplateType.Cash;
        [ObservableProperty] private string _editCurrency = "EUR";
        [ObservableProperty] private int _editBuyIn = 20;
        [ObservableProperty] private int _editRake = 0;
        [ObservableProperty] private int _editStartingStack = 10000;
        [ObservableProperty] private int _editMaxPlayers = 50;
        [ObservableProperty] private int _editSeatsPerTable = 9;
        [ObservableProperty] private int _editLateRegLevels = 4;
        [ObservableProperty] private BlindStructure? _editSelectedBlindStructure;

        // Recaves
        [ObservableProperty] private bool _editAllowRebuys = false;
        [ObservableProperty] private decimal _editRebuyAmount = 20;
        [ObservableProperty] private int _editRebuyLimit = 3;
        [ObservableProperty] private int _editRebuyMaxLevel = 6;

        // Add-ons
        [ObservableProperty] private bool _editAllowAddOn = false;
        [ObservableProperty] private decimal _editAddOnAmount = 10;
        [ObservableProperty] private int _editAddOnStack = 10000;
        [ObservableProperty] private int _editAddOnAtLevel = 6;

        // Bounty
        [ObservableProperty] private bool _editAllowBounty = false;
        [ObservableProperty] private decimal _editBountyAmount = 5;
        #endregion
        public TournamentTemplateViewModel(
            TournamentTemplateService templateService,
            BlindStructureService blindService)
        {
            _templateService = templateService;
            _blindService = blindService;
        }

        #region Initialisation et chargement des données

        /// <summary>
        /// Charge les templates et les structures de blinds au démarrage de la vue
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadTemplatesAsync();
            await LoadBlindStructuresAsync();
        }

        /// <summary>
        /// Charge tous les templates depuis le service
        /// </summary>
        [RelayCommand]
        private async Task LoadTemplatesAsync()
        {
            var templates = await _templateService.GetAllTemplatesAsync();
            Templates.Clear();
            foreach (var template in templates)
            {
                Templates.Add(template);
            }
        }

        /// <summary>
        /// Charge toutes les structures de blinds disponibles
        /// </summary>
        private async Task LoadBlindStructuresAsync()
        {
            var structures = await _blindService.GetAllStructuresAsync();
            BlindStructures.Clear();
            foreach (var structure in structures)
            {
                BlindStructures.Add(structure);
            }
        }

        #endregion

        #region Gestion de la sélection et synchronisation des champs d'édition

        /// <summary>
        /// Déclenché quand un template est sélectionné dans la liste
        /// Charge ses valeurs dans les champs d'édition
        /// </summary>
        partial void OnSelectedTemplateChanged(TournamentTemplate? value)
        {
            if (value != null)
            {
                LoadTemplateForEditing(value);
            }
        }

        /// <summary>
        /// Remplit les champs d'édition avec les valeurs du template sélectionné
        /// </summary>
        private void LoadTemplateForEditing(TournamentTemplate template)
        {
            EditName = template.Name;
            EditDescription = template.Description ?? string.Empty;
            EditType = template.Type;
            EditCurrency = template.Currency;
            EditBuyIn = template.BuyIn;
            EditRake = template.Rake;
            EditStartingStack = template.StartingStack;
            EditMaxPlayers = template.MaxPlayers;
            EditSeatsPerTable = template.SeatsPerTable;
            EditLateRegLevels = template.LateRegLevels;
            EditSelectedBlindStructure = BlindStructures.FirstOrDefault(b => b.Id == template.BlindStructureId);
            EditAllowRebuys = template.AllowRebuys;
            EditRebuyAmount = template.RebuyAmount ?? template.BuyIn;
            EditRebuyLimit = template.RebuyLimit ?? 3;
            EditRebuyMaxLevel = template.RebuyMaxLevel ?? 6;
            EditAllowAddOn = template.AllowAddOn;
            EditAddOnAmount = template.AddOnAmount ?? template.BuyIn;
            EditAddOnStack = template.AddOnStack ?? 5000;
            EditAddOnAtLevel = template.AddOnAtLevel ?? 6;
            EditAllowBounty = template.AllowBounty;
            EditBountyAmount = template.BountyAmount ?? 5;
        }

        /// <summary>
        /// Affiche/masque la section financière selon le type de tournoi
        /// Remet à zéro les montants si ce n'est plus du Cash
        /// </summary>
        partial void OnEditTypeChanged(TournamentTemplateType value)
        {
            OnPropertyChanged(nameof(ShowFinanceSection));
            if (value != TournamentTemplateType.Cash)
            {
                EditBuyIn = 0;
                EditRake = 0;
                EditRebuyAmount = 0;
                EditAddOnAmount = 0;
                EditBountyAmount = 0;
            }
        }

        /// <summary>
        /// Synchronise les montants recave/add-on avec le buy-in quand celui-ci change
        /// </summary>
        partial void OnEditBuyInChanged(int value)
        {
            if (EditType == TournamentTemplateType.Cash)
            {
                EditRebuyAmount = value;
                EditAddOnAmount = value;
            }
        }

        #endregion

        #region Commandes CRUD (Création, Sauvegarde, Suppression, Duplication)

        /// <summary>
        /// Initialise un nouveau template vide pour l'édition
        /// </summary>
        [RelayCommand]
        private void NewTemplate()
        {
            SelectedTemplate = null;
            EditName = "Nouveau Template";
            EditDescription = string.Empty;
            EditType = TournamentTemplateType.Cash;
            EditCurrency = "EUR";
            EditBuyIn = 20;
            EditRake = 0;
            EditStartingStack = 10000;
            EditMaxPlayers = 50;
            EditSeatsPerTable = 9;
            EditLateRegLevels = 4;
            EditSelectedBlindStructure = BlindStructures.FirstOrDefault();
            EditAllowRebuys = false;
            EditRebuyAmount = 20;
            EditRebuyLimit = 3;
            EditRebuyMaxLevel = 6;
            EditAllowAddOn = false;
            EditAddOnAmount = 10;
            EditAddOnStack = 10000;
            EditAddOnAtLevel = 6;
            EditAllowBounty = false;
            EditBountyAmount = 5;

            // Crée un template temporaire pour activer l'édition
            SelectedTemplate = new TournamentTemplate
            {
                Id = 0,
                Name = "Nouveau Template"
            };
        }

        /// <summary>
        /// Sauvegarde ou met à jour le template en cours d'édition
        /// </summary>
        [RelayCommand]
        private async Task SaveTemplateAsync()
        {
            if (string.IsNullOrWhiteSpace(EditName))
            {
                CustomMessageBox.ShowWarning("Le nom est obligatoire.", "Erreur");
                return;
            }

            if (EditSelectedBlindStructure == null)
            {
                CustomMessageBox.ShowWarning("Sélectionnez une structure de blinds.", "Erreur");
                return;
            }

            try
            {
                if (SelectedTemplate == null || SelectedTemplate.Id == 0)
                {
                    // Création
                    var newTemplate = new TournamentTemplate
                    {
                        Name = EditName,
                        Description = EditDescription,
                        Type = EditType,
                        Currency = EditCurrency,
                        BuyIn = EditType == TournamentTemplateType.Cash ? EditBuyIn : 0,
                        Rake = EditType == TournamentTemplateType.Cash ? EditRake : 0,
                        StartingStack = EditStartingStack,
                        MaxPlayers = EditMaxPlayers,
                        SeatsPerTable = EditSeatsPerTable,
                        LateRegLevels = EditLateRegLevels,
                        BlindStructureId = EditSelectedBlindStructure.Id,
                        AllowRebuys = EditAllowRebuys,
                        RebuyAmount = EditAllowRebuys && EditType == TournamentTemplateType.Cash ? EditRebuyAmount : null,
                        RebuyLimit = EditAllowRebuys ? EditRebuyLimit : null,
                        RebuyMaxLevel = EditAllowRebuys ? EditRebuyMaxLevel : null,
                        AllowAddOn = EditAllowAddOn,
                        AddOnAmount = EditAllowAddOn && EditType == TournamentTemplateType.Cash ? EditAddOnAmount : null,
                        AddOnStack = EditAllowAddOn ? EditAddOnStack : null,
                        AddOnAtLevel = EditAllowAddOn ? EditAddOnAtLevel : null,
                        AllowBounty = EditAllowBounty,
                        BountyAmount = EditAllowBounty && EditType == TournamentTemplateType.Cash ? EditBountyAmount : null
                    };

                    await _templateService.CreateTemplateAsync(newTemplate);
                    CustomMessageBox.ShowSuccess("✅ Template créé !", "Succès");
                }
                else
                {
                    // Mise à jour
                    SelectedTemplate.Name = EditName;
                    SelectedTemplate.Description = EditDescription;
                    SelectedTemplate.Type = EditType;
                    SelectedTemplate.Currency = EditCurrency;
                    SelectedTemplate.BuyIn = EditType == TournamentTemplateType.Cash ? EditBuyIn : 0;
                    SelectedTemplate.Rake = EditType == TournamentTemplateType.Cash ? EditRake : 0;
                    SelectedTemplate.StartingStack = EditStartingStack;
                    SelectedTemplate.MaxPlayers = EditMaxPlayers;
                    SelectedTemplate.SeatsPerTable = EditSeatsPerTable;
                    SelectedTemplate.LateRegLevels = EditLateRegLevels;
                    SelectedTemplate.BlindStructureId = EditSelectedBlindStructure.Id;
                    SelectedTemplate.AllowRebuys = EditAllowRebuys;
                    SelectedTemplate.RebuyAmount = EditAllowRebuys && EditType == TournamentTemplateType.Cash ? EditRebuyAmount : null;
                    SelectedTemplate.RebuyLimit = EditAllowRebuys ? EditRebuyLimit : null;
                    SelectedTemplate.RebuyMaxLevel = EditAllowRebuys ? EditRebuyMaxLevel : null;
                    SelectedTemplate.AllowAddOn = EditAllowAddOn;
                    SelectedTemplate.AddOnAmount = EditAllowAddOn && EditType == TournamentTemplateType.Cash ? EditAddOnAmount : null;
                    SelectedTemplate.AddOnStack = EditAllowAddOn ? EditAddOnStack : null;
                    SelectedTemplate.AddOnAtLevel = EditAllowAddOn ? EditAddOnAtLevel : null;
                    SelectedTemplate.AllowBounty = EditAllowBounty;
                    SelectedTemplate.BountyAmount = EditAllowBounty && EditType == TournamentTemplateType.Cash ? EditBountyAmount : null;

                    await _templateService.UpdateTemplateAsync(SelectedTemplate);
                    CustomMessageBox.ShowSuccess("✅ Template modifié !", "Succès");
                }

                await LoadTemplatesAsync();
                SelectedTemplate = null; // Retour à l'état initial
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur : {ex.Message}", "Erreur");
            }
        }

        /// <summary>
        /// Annule l'édition en cours et désélectionne le template
        /// </summary>
        [RelayCommand]
        private void CancelEdit()
        {
            SelectedTemplate = null;
        }

        /// <summary>
        /// Supprime le template sélectionné après confirmation
        /// </summary>
        [RelayCommand]
        private async Task DeleteTemplateAsync()
        {
            if (SelectedTemplate == null || SelectedTemplate.Id == 0) return;

            var result = CustomMessageBox.ShowConfirmation(
                $"Supprimer le template '{SelectedTemplate.Name}' ?\n\nCette action est irréversible.",
                "Confirmation");

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _templateService.DeleteTemplateAsync(SelectedTemplate.Id);
                    await LoadTemplatesAsync();
                    SelectedTemplate = null;
                    CustomMessageBox.ShowSuccess("🗑️ Template supprimé.", "Succès");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.ShowError($"Erreur : {ex.Message}", "Erreur");
                }
            }
        }

        /// <summary>
        /// Crée une copie du template sélectionné
        /// </summary>
        [RelayCommand]
        private async Task DuplicateTemplateAsync()
        {
            if (SelectedTemplate == null || SelectedTemplate.Id == 0) return;

            try
            {
                var newName = $"{SelectedTemplate.Name} (Copie)";
                await _templateService.DuplicateTemplateAsync(SelectedTemplate.Id, newName);
                await LoadTemplatesAsync();

                // Sélectionne automatiquement la copie
                SelectedTemplate = Templates.FirstOrDefault(t => t.Name == newName);
                CustomMessageBox.ShowInformation($"📋 Template dupliqué : {newName}", "Succès");
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur : {ex.Message}", "Erreur");
            }
        }

        #endregion
    }
}