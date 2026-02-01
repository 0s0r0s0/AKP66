using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PokerTournamentDirector.Services
{
    public class EmailService
    {
        private readonly SettingsService _settingsService;
        private SmtpClient? _smtpClient;

        public EmailService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        /// <summary>
        /// Envoie un email simple
        /// </summary>
        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                if (!ValidateEmailSettings(settings))
                {
                    throw new InvalidOperationException("Configuration email incomplète. Configurez d'abord les paramètres SMTP.");
                }

                using var client = CreateSmtpClient(settings);
                using var message = CreateMailMessage(settings.SmtpFromEmail!, to, subject, body, isHtml);

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur envoi email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envoie un email à plusieurs destinataires
        /// </summary>
        public async Task<bool> SendBulkEmailAsync(string[] recipients, string subject, string body, bool isHtml = true)
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                if (!ValidateEmailSettings(settings))
                {
                    throw new InvalidOperationException("Configuration email incomplète.");
                }

                using var client = CreateSmtpClient(settings);

                foreach (var recipient in recipients)
                {
                    if (string.IsNullOrWhiteSpace(recipient)) continue;

                    using var message = CreateMailMessage(settings.SmtpFromEmail!, recipient, subject, body, isHtml);
                    await client.SendMailAsync(message);
                    await Task.Delay(100); // Anti-spam delay
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur envoi bulk email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envoie une notification de tournoi aux joueurs inscrits
        /// </summary>
        public async Task<bool> SendTournamentNotificationAsync(string[] playerEmails, string tournamentName, DateTime tournamentDate)
        {
            var subject = $"🎲 Tournoi : {tournamentName}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; color: #333;'>
                    <div style='background: linear-gradient(135deg, #0f3460 0%, #16213e 100%); padding: 30px; border-radius: 10px;'>
                        <h1 style='color: #00ff88; text-align: center;'>♠️ POKER TOURNAMENT ♣️</h1>
                        <h2 style='color: white; text-align: center;'>{tournamentName}</h2>
                    </div>
                    <div style='padding: 20px;'>
                        <p style='font-size: 16px;'>Bonjour,</p>
                        <p style='font-size: 16px;'>Vous êtes inscrit(e) au tournoi :</p>
                        <div style='background: #f5f5f5; padding: 15px; border-left: 4px solid #00ff88; margin: 20px 0;'>
                            <p style='margin: 5px 0;'><strong>📅 Date :</strong> {tournamentDate:dd/MM/yyyy à HH:mm}</p>
                            <p style='margin: 5px 0;'><strong>🎯 Tournoi :</strong> {tournamentName}</p>
                        </div>
                        <p style='font-size: 14px; color: #666;'>Bonne chance ! 🍀</p>
                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='font-size: 12px; color: #999; text-align: center;'>Los Reneg'As ¡Hasta la victoria siempre!</p>
                    </div>
                </body>
                </html>";

            return await SendBulkEmailAsync(playerEmails, subject, body, isHtml: true);
        }

        /// <summary>
        /// Envoie un rappel de cotisation
        /// </summary>
        public async Task<bool> SendPaymentReminderAsync(string playerEmail, string playerName, decimal amountDue, DateTime dueDate)
        {
            var subject = "💰 Rappel de cotisation - Los Reneg'As";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='background: #0f3460; padding: 20px; border-radius: 10px;'>
                        <h2 style='color: #00ff88;'>Rappel de cotisation</h2>
                    </div>
                    <div style='padding: 20px;'>
                        <p>Bonjour {playerName},</p>
                        <p>Votre cotisation arrive à échéance :</p>
                        <div style='background: #fff3cd; padding: 15px; border-left: 4px solid #ffb703; margin: 20px 0;'>
                            <p><strong>💵 Montant :</strong> {amountDue:C}</p>
                            <p><strong>📅 Échéance :</strong> {dueDate:dd/MM/yyyy}</p>
                        </div>
                        <p>Merci de régulariser votre situation.</p>
                        <p style='font-size: 12px; color: #999; margin-top: 30px;'>Los Reneg'As ♠️♣️♥️♦️</p>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(playerEmail, subject, body, isHtml: true);
        }

        /// <summary>
        /// Envoie les résultats d'un tournoi
        /// </summary>
        public async Task<bool> SendTournamentResultsAsync(string[] playerEmails, string tournamentName, string resultsHtml)
        {
            var subject = $"📊 Résultats : {tournamentName}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='background: linear-gradient(135deg, #0f3460 0%, #16213e 100%); padding: 30px; border-radius: 10px;'>
                        <h1 style='color: #00ff88; text-align: center;'>🏆 RÉSULTATS DU TOURNOI</h1>
                        <h2 style='color: white; text-align: center;'>{tournamentName}</h2>
                    </div>
                    <div style='padding: 20px;'>
                        {resultsHtml}
                        <hr style='margin: 30px 0;'>
                        <p style='font-size: 12px; color: #999; text-align: center;'>Los Reneg'As ¡Hasta la victoria siempre!</p>
                    </div>
                </body>
                </html>";

            return await SendBulkEmailAsync(playerEmails, subject, body, isHtml: true);
        }

        /// <summary>
        /// Test de connexion SMTP
        /// </summary>
        public async Task<(bool success, string message)> TestConnectionAsync()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                if (!ValidateEmailSettings(settings))
                {
                    return (false, "Configuration email incomplète. Vérifiez les paramètres SMTP.");
                }

                using var client = CreateSmtpClient(settings);

                // Test simple sans envoi réel
                await Task.Run(() => client.Timeout = 5000);

                return (true, "Connexion SMTP réussie ✅");
            }
            catch (SmtpException ex)
            {
                return (false, $"Erreur SMTP : {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        private bool ValidateEmailSettings(Models.AppSettings settings)
        {
            return !string.IsNullOrWhiteSpace(settings.SmtpServer) &&
                   settings.SmtpPort > 0 &&
                   !string.IsNullOrWhiteSpace(settings.SmtpUsername) &&
                   !string.IsNullOrWhiteSpace(settings.SmtpPassword) &&
                   !string.IsNullOrWhiteSpace(settings.SmtpFromEmail);
        }

        private SmtpClient CreateSmtpClient(Models.AppSettings settings)
        {
            return new SmtpClient(settings.SmtpServer)
            {
                Port = settings.SmtpPort,
                Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword),
                EnableSsl = settings.SmtpEnableSsl,
                Timeout = 10000
            };
        }

        private MailMessage CreateMailMessage(string from, string to, string subject, string body, bool isHtml)
        {
            var message = new MailMessage(from, to, subject, body)
            {
                IsBodyHtml = isHtml
            };

            return message;
        }
    }
}