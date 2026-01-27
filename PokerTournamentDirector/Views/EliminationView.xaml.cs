using PokerTournamentDirector.ViewModels;
using System.Windows;

namespace PokerTournamentDirector.Views
{
    public partial class EliminationView : Window
    {
        private readonly EliminationViewModel _viewModel;

        public EliminationView(EliminationViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.InitializeAsync();

            // S'abonner à l'événement de fin de tournoi
            _viewModel.TournamentFinished += (s, winnerName) =>
            {
                // Fermer automatiquement après validation
                DialogResult = true;
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
