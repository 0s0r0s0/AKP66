using PokerTournamentDirector.Models;
using PokerTournamentDirector.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PokerTournamentDirector.Converters
{
    // ========== BOOL → VISIBILITY ==========

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }

    public class BoolToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Collapsed;
        }
    }

    // ========== NULL → VISIBILITY ==========

    public class NullToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool inverse = parameter?.ToString() == "Inverse";
            bool isNull = value == null;
            return (inverse ? isNull : !isNull) ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NullToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null || (value is string s && string.IsNullOrWhiteSpace(s))
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // ========== NULL → BOOL ==========

    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NotNullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // ========== INT → VISIBILITY ==========

    public class ZeroToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i) return i > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (value is decimal d) return d > 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class IndexToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i && parameter is string s && i.ToString() == s
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class IndexGreaterThanZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i && i > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // ========== INT → BOOL ==========

    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i && parameter is string s && i.ToString() == s;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b && parameter is string s && int.TryParse(s, out int i) ? i : 0;
        }
    }

    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i && parameter is string s && i.ToString() == s
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }


    // ========== BOOL → STRING ==========

    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && parameter is string options)
            {
                var parts = options.Split('|');
                return parts.Length == 2 ? (b ? parts[0] : parts[1]) : string.Empty;
            }
            return string.Empty;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToActiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? "✓ Actif" : "✗ Inactif";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToHistoryTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool show && parameter is string text)
            {
                var parts = text.Split('|');
                return show && parts.Length > 1 ? parts[1] : parts[0];
            }
            return "📋 Historique";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // ========== COLOR ==========

    public class BoolToActiveColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush(value is bool b && b
                ? Color.FromRgb(0, 255, 136)
                : Color.FromRgb(233, 69, 96));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class HexToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try { return (Color)ColorConverter.ConvertFromString(hex); }
                catch { return Colors.Black; }
            }
            return Colors.Black;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Color c ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : "#000000";
        }
    }

    // ========== STRING ==========

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string str && parameter is string target && str == target
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // ========== CUSTOM ==========

    public class PlayerEditTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Player p ? $"Modifier {p.Name}" : "Nouveau Joueur";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class MinutesToHoursConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int minutes)
            {
                int hours = minutes / 60;
                int mins = minutes % 60;
                return $"{hours}h{mins:D2}";
            }
            return "0h00";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class PositionChangeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2)
            {
                int current = values[0] is int c ? c : (int.TryParse(values[0]?.ToString(), out c) ? c : 0);
                int? previous = values[1] is int p ? p : (int.TryParse(values[1]?.ToString(), out p) ? p : (int?)null);

                if (!previous.HasValue) return "NEW";

                int diff = previous.Value - current;
                if (diff > 0) return $"↑{diff}";
                if (diff < 0) return $"↓{Math.Abs(diff)}";
                return "=";
            }
            return "";
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }
    }

    // Convertit PaymentStatus en icône
    public class PaymentStatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PaymentStatus status)
            {
                return status switch
                {
                    PaymentStatus.Paid => "✅",
                    PaymentStatus.InProgress => "⏳",
                    PaymentStatus.Trial => "🐟",
                    PaymentStatus.None => "❌",
                    _ => "?"
                };
            }
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Convertit PaymentStatus en couleur
    public class PaymentStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PaymentStatus status)
            {
                return status switch
                {
                    PaymentStatus.Paid => new SolidColorBrush(Color.FromRgb(0, 255, 136)), // #00ff88
                    PaymentStatus.InProgress => new SolidColorBrush(Color.FromRgb(255, 215, 0)), // #ffd700
                    PaymentStatus.Trial => new SolidColorBrush(Color.FromRgb(135, 206, 250)), // Light blue
                    PaymentStatus.None => new SolidColorBrush(Color.FromRgb(233, 69, 96)), // #e94560
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Convertit PlayerStatus en texte
    public class PlayerStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PlayerStatus status)
            {
                return status switch
                {
                    PlayerStatus.Active => "Actif",
                    PlayerStatus.Inactive => "Inactif",
                    _ => "?"
                };
            }
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Convertit PlayerStatus en couleur
    public class PlayerStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PlayerStatus status)
            {
                return status switch
                {
                    PlayerStatus.Active => new SolidColorBrush(Color.FromRgb(0, 255, 136)), // #00ff88
                    PlayerStatus.Inactive => new SolidColorBrush(Color.FromRgb(160, 160, 160)), // #a0a0a0
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Retourne l'icône d'alerte pour un joueur
    public class PlayerAlertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Player player)
                return "";

            // Période d'essai terminée
            if (player.PaymentStatus == PaymentStatus.Trial &&
                player.TrialEnd.HasValue &&
                player.TrialEnd.Value < DateTime.Now)
            {
                return "🐟";
            }

            // Aucun paiement après 1 mois
            if (player.PaymentStatus == PaymentStatus.None &&
                player.Paid == 0 &&
                (DateTime.Now - player.RegistrationDate).TotalDays > 30)
            {
                return "💸";
            }

            // Mensualité en retard
            if (player.NextDueDate.HasValue)
            {
                var daysPassed = (DateTime.Now - player.NextDueDate.Value).TotalDays;
                if (daysPassed >= 7) // 1 semaine de retard
                {
                    return "⏰";
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Détermine si un joueur a une alerte (pour colorer la ligne)
    public class PlayerHasAlertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Player player)
                return false;

            // Période d'essai terminée
            if (player.PaymentStatus == PaymentStatus.Trial &&
                player.TrialEnd.HasValue &&
                player.TrialEnd.Value < DateTime.Now)
            {
                return true;
            }

            // Aucun paiement après 1 mois
            if (player.PaymentStatus == PaymentStatus.None &&
                player.Paid == 0 &&
                (DateTime.Now - player.RegistrationDate).TotalDays > 30)
            {
                return true;
            }

            // Mensualité en retard
            if (player.NextDueDate.HasValue)
            {
                var daysPassed = (DateTime.Now - player.NextDueDate.Value).TotalDays;
                if (daysPassed >= 7)
                {
                    return true;
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ProrataModeToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string mode)
            {
                return mode switch
                {
                    "monthly" => 0,
                    "percentage" => 1,
                    _ => 0
                };
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return index switch
                {
                    0 => "monthly",
                    1 => "percentage",
                    _ => "monthly"
                };
            }
            return "monthly";
        }
    }

    public class IntGreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //Kills EMOJI éliminations
    public class KillCountToEmojiConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int killCount)
            {
                return new string('☠', killCount);

            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class KillCountToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count switch
                {
                    >= 8 => new SolidColorBrush(Colors.DarkRed),
                    >= 6 => new SolidColorBrush(Colors.Red),
                    >= 4 => new SolidColorBrush(Colors.Orange),
                    >= 2 => new SolidColorBrush(Colors.Yellow),
                    _ => Brushes.White
                };
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //Générateur de blinds
    // BB = SB × 2 (affichage seule)
    public class MultiplyBy2Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int sb && sb > 0)
                return (sb * 2).ToString();
            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // pas besoin car read-only
        }
    }

    // Affiche "Stack de départ ≈ xx BB" (gère le cas SB=0)
    public class StartingStackToBBConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Length == 2 &&
                values[0] is int stack && stack > 0 &&
                values[1] is int sb && sb > 0)
            {
                int bbCount = stack / (sb * 2);
                return $"Stack de départ ≈ {bbCount} BB";
            }
            return "Stack de départ ≈ — BB";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}