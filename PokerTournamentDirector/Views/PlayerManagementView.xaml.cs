using PokerTournamentDirector.ViewModels;
using System.Windows;

namespace PokerTournamentDirector.Views
{
    public partial class PlayerManagementView : Window
    {
        private readonly PlayerManagementViewModel _viewModel;

        public PlayerManagementView(PlayerManagementViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }
    }
}