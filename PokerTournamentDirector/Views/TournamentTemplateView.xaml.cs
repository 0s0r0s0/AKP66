using PokerTournamentDirector.ViewModels;
using System.Windows;

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
    }
}