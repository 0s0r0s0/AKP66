using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.MediaFoundation;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PokerTournamentDirector.ViewModels
{
    public partial class TournamentTemplateViewModel : ObservableObject
    {
        private readonly TournamentTemplateService _templateService;
        private readonly BlindStructureService _blindService;

        [ObservableProperty]
        private ObservableCollection<TournamentTemplate> _templates = new();

        [ObservableProperty]
        private ObservableCollection<BlindStructure> _blindStructures = new();

        [ObservableProperty]
        private TournamentTemplate? _selectedTemplate;

        [ObservableProperty]
        private bool _isEditing = false;

        // Édition
        [ObservableProperty]
        private string _editName = string.Empty;

        [ObservableProperty]
        private string _editDescription = string.Empty;

        [ObservableProperty]
        private TournamentTemplateType _editType = TournamentTemplateType.Cash;

        [ObservableProperty]
        private string _editCurrency = "EUR";

        [ObservableProperty]
        private decimal _editBuyIn = 20;

        [ObservableProperty]
        private decimal _editRake = 0;

        [ObservableProperty]
        private int _editStartingStack = 10000;

        [ObservableProperty]
        private int _editMaxPlayers = 50;

        [ObservableProperty]
        private int _editSeatsPerTable = 9;

        [ObservableProperty]
        private int _editLateRegLevels = 4;

        [ObservableProperty]
        private BlindStructure? _editSelectedBlindStructure;

        // Recaves
        [ObservableProperty]
        private bool _editAllowRebuys = false;

        [ObservableProperty]
        private decimal _editRebuyAmount = 20;

        [ObservableProperty]
        private int _editRebuyLimit = 3;

        [ObservableProperty]
        private int _editRebuyMaxLevel = 6;

        // Add-ons
        [ObservableProperty]
        private bool _editAllowAddOn = false;

        [ObservableProperty]
        private decimal _editAddOnAmount = 10;

        [ObservableProperty]
        private int _editAddOnStack = 5000;

        [ObservableProperty]
        private int _editAddOnAtLevel = 6;

        // Bounty
        [ObservableProperty]
        private bool _editAllowBounty = false;

        [ObservableProperty]
        private decimal _editBountyAmount = 5;

        public TournamentTemplateViewModel(TournamentTemplateService templateService, BlindStructureService blindService)
        {
            _templateService = templateService;
            _blindService = blindService;
        }

        public async Task InitializeAsync()
        {
            await LoadTemplatesAsync();
            await LoadBlindStructuresAsync();
        }

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

        private async Task LoadBlindStructuresAsync()
        {
            var structures = await _blindService.GetAllStructuresAsync();
            BlindStructures.Clear();
            foreach (var structure in structures)
            {
                BlindStructures.Add(structure);
            }
        }

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
            EditAllowAddOn = false;
            EditAllowBounty = false;
            IsEditing = true;
        }

        [RelayCommand]
        private void EditTemplate()
        {
            if (SelectedTemplate == null) return;

            EditName = SelectedTemplate.Name;
            EditDescription = SelectedTemplate.Description ?? string.Empty;
            EditType = SelectedTemplate.Type;
            EditCurrency = SelectedTemplate.Currency;
            EditBuyIn = SelectedTemplate.BuyIn;
            EditRake = SelectedTemplate.Rake;
            EditStartingStack = SelectedTemplate.StartingStack;
            EditMaxPlayers = SelectedTemplate.MaxPlayers;
            EditSeatsPerTable = SelectedTemplate.SeatsPerTable;
            EditLateRegLevels = SelectedTemplate.LateRegLevels;
            EditSelectedBlindStructure = BlindStructures.FirstOrDefault(b => b.Id == SelectedTemplate.BlindStructureId);

            EditAllowRebuys = SelectedTemplate.AllowRebuys;
            EditRebuyAmount = SelectedTemplate.RebuyAmount ?? 20;
            EditRebuyLimit = SelectedTemplate.RebuyLimit ?? 3;
            EditRebuyMaxLevel = SelectedTemplate.RebuyMaxLevel ?? 6;

            EditAllowAddOn = SelectedTemplate.AllowAddOn;
            EditAddOnAmount = SelectedTemplate.AddOnAmount ?? 10;
            EditAddOnStack = SelectedTemplate.AddOnStack ?? 5000;
            EditAddOnAtLevel = SelectedTemplate.AddOnAtLevel ?? 6;

            EditAllowBounty = SelectedTemplate.AllowBounty;
            EditBountyAmount = SelectedTemplate.BountyAmount ?? 5;

            IsEditing = true;
        }

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
                if (SelectedTemplate == null)
                {
                    // Nouveau template
                    var newTemplate = new TournamentTemplate
                    {
                        Name = EditName,
                        Description = EditDescription,
                        Type = EditType,
                        Currency = EditCurrency,
                        BuyIn = EditBuyIn,
                        Rake = EditRake,
                        StartingStack = EditStartingStack,
                        MaxPlayers = EditMaxPlayers,
                        SeatsPerTable = EditSeatsPerTable,
                        LateRegLevels = EditLateRegLevels,
                        BlindStructureId = EditSelectedBlindStructure.Id,
                        AllowRebuys = EditAllowRebuys,
                        RebuyAmount = EditAllowRebuys ? EditRebuyAmount : null,
                        RebuyLimit = EditAllowRebuys ? EditRebuyLimit : null,
                        RebuyMaxLevel = EditAllowRebuys ? EditRebuyMaxLevel : null,
                        AllowAddOn = EditAllowAddOn,
                        AddOnAmount = EditAllowAddOn ? EditAddOnAmount : null,
                        AddOnStack = EditAllowAddOn ? EditAddOnStack : null,
                        AddOnAtLevel = EditAllowAddOn ? EditAddOnAtLevel : null,
                        AllowBounty = EditAllowBounty,
                        BountyAmount = EditAllowBounty ? EditBountyAmount : null
                    };

                    await _templateService.CreateTemplateAsync(newTemplate);
                }
                else
                {
                    // Modification
                    SelectedTemplate.Name = EditName;
                    SelectedTemplate.Description = EditDescription;
                    SelectedTemplate.Type = EditType;
                    SelectedTemplate.Currency = EditCurrency;
                    SelectedTemplate.BuyIn = EditBuyIn;
                    SelectedTemplate.Rake = EditRake;
                    SelectedTemplate.StartingStack = EditStartingStack;
                    SelectedTemplate.MaxPlayers = EditMaxPlayers;
                    SelectedTemplate.SeatsPerTable = EditSeatsPerTable;
                    SelectedTemplate.LateRegLevels = EditLateRegLevels;
                    SelectedTemplate.BlindStructureId = EditSelectedBlindStructure.Id;
                    SelectedTemplate.AllowRebuys = EditAllowRebuys;
                    SelectedTemplate.RebuyAmount = EditAllowRebuys ? EditRebuyAmount : null;
                    SelectedTemplate.RebuyLimit = EditAllowRebuys ? EditRebuyLimit : null;
                    SelectedTemplate.RebuyMaxLevel = EditAllowRebuys ? EditRebuyMaxLevel : null;
                    SelectedTemplate.AllowAddOn = EditAllowAddOn;
                    SelectedTemplate.AddOnAmount = EditAllowAddOn ? EditAddOnAmount : null;
                    SelectedTemplate.AddOnStack = EditAllowAddOn ? EditAddOnStack : null;
                    SelectedTemplate.AddOnAtLevel = EditAllowAddOn ? EditAddOnAtLevel : null;
                    SelectedTemplate.AllowBounty = EditAllowBounty;
                    SelectedTemplate.BountyAmount = EditAllowBounty ? EditBountyAmount : null;

                    await _templateService.UpdateTemplateAsync(SelectedTemplate);
                }

                CustomMessageBox.ShowSuccess("Template sauvegardé !", "Succès");
                await LoadTemplatesAsync();
                IsEditing = false;
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur : {ex.Message}", "Erreur");
            }
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            SelectedTemplate = null;
        }

        [RelayCommand]
        private async Task DeleteTemplateAsync()
        {
            if (SelectedTemplate == null) return;

            var result = CustomMessageBox.ShowConfirmation(
                $"Supprimer le template '{SelectedTemplate.Name}' ?",
                "Confirmation");

            if (result == MessageBoxResult.Yes)
            {
                await _templateService.DeleteTemplateAsync(SelectedTemplate.Id);
                await LoadTemplatesAsync();
                CustomMessageBox.ShowSuccess("Template supprimé.", "Succès");
            }
        }

        [RelayCommand]
        private async Task DuplicateTemplateAsync()
        {
            if (SelectedTemplate == null) return;

            var newName = $"{SelectedTemplate.Name} (Copie)";
            await _templateService.DuplicateTemplateAsync(SelectedTemplate.Id, newName);
            await LoadTemplatesAsync();
            CustomMessageBox.ShowInformation($"Template dupliqué : {newName}", "Succès");
        }
    }
}