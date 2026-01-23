using PokerTournamentDirector.ViewModels;
using System.Windows;

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
    }
}