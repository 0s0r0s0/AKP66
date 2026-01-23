using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PokerTournamentDirector.Views
{
    public partial class CustomMessageBox : Window
    {
        public enum MessageBoxType
        {
            Information,
            Question,
            Warning,
            Error,
            Success
        }

        public enum MessageBoxButtons
        {
            OK,
            OKCancel,
            YesNo,
            YesNoCancel
        }

        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        private CustomMessageBox(string title, string message, MessageBoxType type, MessageBoxButtons buttons)
        {
            InitializeComponent();

            // ✅ CORRECTION 1 : Centrer sur la fenêtre parente
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            Title = title;
            MessageText.Text = message;

            ConfigureMessageType(type);
            ConfigureButtons(buttons);
        }

        private void ConfigureMessageType(MessageBoxType type)
        {
            switch (type)
            {
                case MessageBoxType.Information:
                    IconText.Text = "ℹ️";
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                    break;

                case MessageBoxType.Question:
                    IconText.Text = "❓";
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(0, 255, 136));
                    break;

                case MessageBoxType.Warning:
                    IconText.Text = "⚠️";
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    break;

                case MessageBoxType.Error:
                    IconText.Text = "❌";
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(233, 69, 96));
                    break;

                case MessageBoxType.Success:
                    IconText.Text = "✅";
                    IconBorder.Background = new SolidColorBrush(Color.FromRgb(0, 255, 136));
                    break;
            }
        }

        private void ConfigureButtons(MessageBoxButtons buttons)
        {
            ButtonsPanel.Children.Clear();

            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    AddButton("OK", MessageBoxResult.OK, isPrimary: true);
                    break;

                case MessageBoxButtons.OKCancel:
                    AddButton("Annuler", MessageBoxResult.Cancel);
                    AddButton("OK", MessageBoxResult.OK, isPrimary: true);
                    break;

                case MessageBoxButtons.YesNo:
                    AddButton("Non", MessageBoxResult.No);
                    AddButton("Oui", MessageBoxResult.Yes, isPrimary: true);
                    break;

                case MessageBoxButtons.YesNoCancel:
                    AddButton("Annuler", MessageBoxResult.Cancel);
                    AddButton("Non", MessageBoxResult.No);
                    AddButton("Oui", MessageBoxResult.Yes, isPrimary: true);
                    break;
            }
        }

        private void AddButton(string text, MessageBoxResult result, bool isPrimary = false)
        {
            var button = new Button
            {
                Content = text,
                Width = 110,
                Height = 42,
                Margin = new Thickness(10, 0, 0, 0),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Style = (Style)FindResource("ModernButton")
            };

            if (isPrimary)
            {
                button.Background = new SolidColorBrush(Color.FromRgb(0, 255, 136));
                button.Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46));
                button.IsDefault = true;
            }
            else
            {
                button.Background = new SolidColorBrush(Color.FromRgb(30, 42, 70));
                button.Foreground = Brushes.White;

                if (result == MessageBoxResult.Cancel)
                    button.IsCancel = true;
            }

            button.Click += (s, e) =>
            {
                Result = result;
                DialogResult = true;
                Close();
            };

            ButtonsPanel.Children.Add(button);
        }

        // Méthodes statiques
        public static MessageBoxResult Show(string message, string title = "Information",
            MessageBoxType type = MessageBoxType.Information,
            MessageBoxButtons buttons = MessageBoxButtons.OK)
        {
            // ✅ CORRECTION 2 : Toujours définir Owner avant ShowDialog
            var dlg = new CustomMessageBox(title, message, type, buttons);

            // Essayer d'utiliser la fenêtre active comme Owner
            if (Application.Current.Windows.Count > 0)
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.IsActive)
                    {
                        dlg.Owner = window;
                        break;
                    }
                }

                // Si aucune fenêtre active, utiliser la MainWindow
                if (dlg.Owner == null)
                {
                    dlg.Owner = Application.Current.MainWindow;
                }
            }

            // ✅ CORRECTION 3 : Forcer le Topmost temporairement
            dlg.Topmost = true;
            dlg.ShowDialog();

            return dlg.Result;
        }

        public static MessageBoxResult ShowInformation(string message, string title = "Information")
            => Show(message, title, MessageBoxType.Information, MessageBoxButtons.OK);

        public static MessageBoxResult ShowQuestion(string message, string title = "Question")
            => Show(message, title, MessageBoxType.Question, MessageBoxButtons.YesNo);

        public static MessageBoxResult ShowWarning(string message, string title = "Attention")
            => Show(message, title, MessageBoxType.Warning, MessageBoxButtons.OK);

        public static MessageBoxResult ShowError(string message, string title = "Erreur")
            => Show(message, title, MessageBoxType.Error, MessageBoxButtons.OK);

        public static MessageBoxResult ShowSuccess(string message, string title = "Succès")
            => Show(message, title, MessageBoxType.Success, MessageBoxButtons.OK);

        public static MessageBoxResult ShowConfirmation(string message, string title = "Confirmation")
            => Show(message, title, MessageBoxType.Question, MessageBoxButtons.YesNo);

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }
    }
}