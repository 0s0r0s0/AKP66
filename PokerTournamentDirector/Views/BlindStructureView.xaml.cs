using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using PokerTournamentDirector.Models;

namespace PokerTournamentDirector.Views
{
    public partial class BlindStructureView : Window
    {
        public BlindStructureView()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    /// <summary>
    /// Convertisseur pour afficher soit "PAUSE - Nom" soit "Niveau X"
    /// </summary>
    public class LevelDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 3) return "—";

            bool isBreak = values[0] is bool b && b;
            string? breakName = values[1] as string;
            int levelNumber = values[2] is int num ? num : 0;

            if (isBreak)
            {
                return string.IsNullOrEmpty(breakName) ? "🕐 PAUSE" : $"🕐 {breakName}";
            }

            return $"Niveau {levelNumber}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ViewModel pour la fenêtre de structure des blinds
    /// </summary>
    public class BlindStructureViewModel
    {
        public string TournamentName { get; set; } = string.Empty;
        public int CurrentLevel { get; set; }
        public int TotalLevels { get; set; }
        public ObservableCollection<BlindLevelDisplay> BlindLevels { get; set; } = new();
    }

    /// <summary>
    /// Modèle d'affichage pour un niveau de blind
    /// </summary>
    public class BlindLevelDisplay
    {
        public int LevelNumber { get; set; }
        public int SmallBlind { get; set; }
        public int BigBlind { get; set; }
        public int Ante { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsBreak { get; set; }
        public string? BreakName { get; set; }
        public bool IsCurrentLevel { get; set; }
    }
}