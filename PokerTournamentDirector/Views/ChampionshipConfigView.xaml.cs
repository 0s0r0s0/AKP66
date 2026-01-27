using System.Windows;
using System.Windows.Controls;
using PokerTournamentDirector.ViewModels;
using PokerTournamentDirector.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PokerTournamentDirector.Views
{
    public partial class ChampionshipConfigView : Window
    {
        public class PointsRow
        {
            public string Position { get; set; } = "";
            public int Points { get; set; } = 0;
        }

        private ChampionshipConfigViewModel ViewModel => (ChampionshipConfigViewModel)DataContext;
        private ObservableCollection<PointsRow> _fixedPointsRows = new();

        public Championship Championship => ViewModel.Championship;

        public ChampionshipConfigView()
        {
            InitializeComponent();
            DataContext = new ChampionshipConfigViewModel();
            InitializeFixedPointsGrid();
        }

        public ChampionshipConfigView(Championship championship, bool isEditMode)
        {
            InitializeComponent();
            DataContext = new ChampionshipConfigViewModel(championship, isEditMode);
            InitializeFixedPointsGrid();
            LoadFixedPointsFromViewModel();
        }

        private void InitializeFixedPointsGrid()
        {
            _fixedPointsRows.Add(new PointsRow { Position = "1", Points = 100 });
            _fixedPointsRows.Add(new PointsRow { Position = "2", Points = 80 });
            FixedPointsItemsControl.ItemsSource = _fixedPointsRows;
        }

        private void LoadFixedPointsFromViewModel()
        {
            if (!string.IsNullOrEmpty(ViewModel.FixedPointsTable))
            {
                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, int>>(ViewModel.FixedPointsTable);
                    if (dict != null && dict.Count > 0)
                    {
                        _fixedPointsRows.Clear();
                        foreach (var kvp in dict)
                        {
                            _fixedPointsRows.Add(new PointsRow { Position = kvp.Key, Points = kvp.Value });
                        }
                    }
                }
                catch { }
            }
        }

        // Navigation entre steps
        private void Step0_Click(object sender, RoutedEventArgs e) => ShowStep(0);
        private void Step1_Click(object sender, RoutedEventArgs e) => ShowStep(1);
        private void Step2_Click(object sender, RoutedEventArgs e) => ShowStep(2);
        private void Step3_Click(object sender, RoutedEventArgs e) => ShowStep(3);
        private void Step4_Click(object sender, RoutedEventArgs e) => ShowStep(4);
        private void Step5_Click(object sender, RoutedEventArgs e) => ShowStep(5);

        private void ShowStep(int stepIndex)
        {
            Step0.Visibility = Visibility.Collapsed;
            Step1.Visibility = Visibility.Collapsed;
            Step2.Visibility = Visibility.Collapsed;
            Step3.Visibility = Visibility.Collapsed;
            Step4.Visibility = Visibility.Collapsed;
            Step5.Visibility = Visibility.Collapsed;

            switch (stepIndex)
            {
                case 0: Step0.Visibility = Visibility.Visible; break;
                case 1: Step1.Visibility = Visibility.Visible; break;
                case 2: Step2.Visibility = Visibility.Visible; break;
                case 3: Step3.Visibility = Visibility.Visible; break;
                case 4: Step4.Visibility = Visibility.Visible; break;
                case 5: Step5.Visibility = Visibility.Visible; break;
            }

            BtnPrev.Visibility = stepIndex > 0 ? Visibility.Visible : Visibility.Collapsed;
            BtnNext.Visibility = stepIndex < 5 ? Visibility.Visible : Visibility.Collapsed;
            BtnSave.Visibility = stepIndex == 5 ? Visibility.Visible : Visibility.Collapsed;

            ViewModel.CurrentStep = stepIndex;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CurrentStep < 5)
                ShowStep(ViewModel.CurrentStep + 1);
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CurrentStep > 0)
                ShowStep(ViewModel.CurrentStep - 1);
        }

        // Points fixes - Presets
        private void LoadPreset100_Click(object sender, RoutedEventArgs e)
        {
            _fixedPointsRows.Clear();
            _fixedPointsRows.Add(new PointsRow { Position = "1", Points = 100 });
            _fixedPointsRows.Add(new PointsRow { Position = "2", Points = 80 });
            _fixedPointsRows.Add(new PointsRow { Position = "3", Points = 65 });
            _fixedPointsRows.Add(new PointsRow { Position = "4", Points = 55 });
            _fixedPointsRows.Add(new PointsRow { Position = "5", Points = 45 });
            _fixedPointsRows.Add(new PointsRow { Position = "6", Points = 35 });
            _fixedPointsRows.Add(new PointsRow { Position = "7", Points = 30 });
            _fixedPointsRows.Add(new PointsRow { Position = "8", Points = 25 });
            _fixedPointsRows.Add(new PointsRow { Position = "9", Points = 20 });
            _fixedPointsRows.Add(new PointsRow { Position = "10", Points = 15 });
            _fixedPointsRows.Add(new PointsRow { Position = "11-20", Points = 10 });
            _fixedPointsRows.Add(new PointsRow { Position = "21+", Points = 5 });
        }

        private void LoadPresetAK66_Click(object sender, RoutedEventArgs e)
        {
            _fixedPointsRows.Clear();
            _fixedPointsRows.Add(new PointsRow { Position = "1", Points = 66 });
            _fixedPointsRows.Add(new PointsRow { Position = "2", Points = 52 });
            _fixedPointsRows.Add(new PointsRow { Position = "3", Points = 42 });
            _fixedPointsRows.Add(new PointsRow { Position = "4", Points = 35 });
            _fixedPointsRows.Add(new PointsRow { Position = "5", Points = 29 });
            _fixedPointsRows.Add(new PointsRow { Position = "6", Points = 24 });
            _fixedPointsRows.Add(new PointsRow { Position = "7", Points = 20 });
            _fixedPointsRows.Add(new PointsRow { Position = "8", Points = 16 });
            _fixedPointsRows.Add(new PointsRow { Position = "9", Points = 13 });
            _fixedPointsRows.Add(new PointsRow { Position = "10", Points = 10 });
            _fixedPointsRows.Add(new PointsRow { Position = "11-15", Points = 7 });
            _fixedPointsRows.Add(new PointsRow { Position = "16-20", Points = 5 });
            _fixedPointsRows.Add(new PointsRow { Position = "21+", Points = 3 });
        }

        private void AddFixedPointRow_Click(object sender, RoutedEventArgs e)
        {
            _fixedPointsRows.Add(new PointsRow { Position = "", Points = 0 });
        }

        private void RemoveFixedPointRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PointsRow row)
            {
                _fixedPointsRows.Remove(row);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Sauvegarder la table de points fixes si mode = 1 (Points fixes)
            if (ViewModel.SelectedPointsModeIndex == 1)
            {
                var dict = new Dictionary<string, int>();
                foreach (var row in _fixedPointsRows)
                {
                    if (!string.IsNullOrWhiteSpace(row.Position))
                    {
                        dict[row.Position.Trim()] = row.Points;
                    }
                }

                if (dict.Count > 0)
                {
                    ViewModel.FixedPointsTable = JsonConvert.SerializeObject(dict);
                }
            }

            ViewModel.SaveToChampionship();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}