using PokerTournamentDirector.Models;
using PokerTournamentDirector.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PokerTournamentDirector.Views
{
    public partial class TournamentTemplateView : Window
    {
        private readonly TournamentTemplateViewModel _viewModel;

        public TournamentTemplateView(TournamentTemplateViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }

        private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TournamentTemplate template)
            {
                await _viewModel.ToggleFavoriteCommand.ExecuteAsync(template);
            }
        }
    }

}