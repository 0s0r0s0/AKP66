using PokerTournamentDirector.Models;
using PokerTournamentDirector.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PokerTournamentDirector.Views
{
    public partial class BlindStructureEditorView : Window
    {
        private readonly BlindStructureEditorViewModel _viewModel;

        public BlindStructureEditorView(BlindStructureEditorViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }

        private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is BlindStructure structure)
            {
                await _viewModel.ToggleFavoriteCommand.ExecuteAsync(structure);
            }
        }
    }
}