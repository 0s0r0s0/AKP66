using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace PokerTournamentDirector.ViewModels
{
    public partial class ChampionshipConfigViewModel : ObservableObject
    {
        [ObservableProperty] private int _currentStep = 0;
        [ObservableProperty] private bool _isLastStep;

        // Step 0
        [ObservableProperty] private string _name = "";
        [ObservableProperty] private string _season = "";
        [ObservableProperty] private string _description = "";
        [ObservableProperty] private DateTime _startDate;
        [ObservableProperty] private DateTime _endDate;
        [ObservableProperty] private int _selectedPeriodTypeIndex;
        [ObservableProperty] private bool _enableMonthlyStandings;
        [ObservableProperty] private bool _enableQuarterlyStandings;

        // Step 1
        [ObservableProperty] private int _selectedPointsModeIndex;
        [ObservableProperty] private int _linearFirstPlacePoints = 100;
        [ObservableProperty] private string? _fixedPointsTable; // JSON
        [ObservableProperty] private string _fixedPointsSummary = "Aucune configuration";
        [ObservableProperty] private int _proportionalTotalPoints = 1000;
        [ObservableProperty] private bool _enableParticipationPoints;
        [ObservableProperty] private int _participationPoints = 10;
        [ObservableProperty] private int _tiebreaker1Index;

        // Step 2
        [ObservableProperty] private int _selectedCountingModeIndex;
        [ObservableProperty] private int? _bestXOfSeason;
        [ObservableProperty] private int _bestXPeriodTypeIndex; // 0=mois, 1=trimestre, 2=année
        [ObservableProperty] private int? _bestXPerPeriod;
        [ObservableProperty] private int? _excludeWorstX;

        // Step 3
        [ObservableProperty] private bool _countBounties;
        [ObservableProperty] private int _pointsPerBounty = 5;
        [ObservableProperty] private int _victoryBonus;
        [ObservableProperty] private int _top3Bonus;
        [ObservableProperty] private int _firstEliminatedConsolation;
        [ObservableProperty] private decimal _defaultMatchCoefficient = 1.0m;
        [ObservableProperty] private decimal _finalMatchCoefficient = 2.0m;
        [ObservableProperty] private decimal _mainEventCoefficient = 1.5m;

        // Step 4
        [ObservableProperty] private int _selectedRebuyModeIndex;
        [ObservableProperty] private int? _rebuyLimit;
        [ObservableProperty] private int _rebuyPointsPenalty;
        [ObservableProperty] private decimal _rebuyPointsMultiplier = 1.0m;

        // Step 5
        [ObservableProperty] private bool _isOpenChampionship = true;
        [ObservableProperty] private int? _qualificationTopX;
        [ObservableProperty] private int? _qualificationMinPoints;
        [ObservableProperty] private int? _qualificationMinMatches;
        [ObservableProperty] private bool _allowLateRegistration = true;
        [ObservableProperty] private int? _lateRegistrationUntilMatch;
        [ObservableProperty] private bool _allowRetroactivePoints;

        public ObservableCollection<string> PeriodTypes { get; } = new() { "Annuel", "Trimestriel", "Mensuel", "Personnalisé" };
        public ObservableCollection<string> PointsModes { get; } = new() { "Linéaire (100→1)", "Points fixes par position", "Proportionnel au prize pool" };
        public ObservableCollection<string> CountingModes { get; } = new() { "Tous les tournois", "Meilleurs X de la saison", "Meilleurs X par période" };
        public ObservableCollection<string> RebuyModes { get; } = new() { "Aucune recave", "Illimitées", "Limitées par manche", "Limitées par mois", "Limitées par trimestre", "Limitées par saison" };
        public ObservableCollection<string> TiebreakerOptions { get; } = new() { "Nombre de victoires", "Meilleur résultat", "Confrontation directe", "Somme des positions", "Plus de manches" };

        // Propriété pour stocker le championnat
        public Championship Championship { get; set; }

        // Constructeur par défaut
        public ChampionshipConfigViewModel()
        {
            InitializeDefaults();
        }

        // Constructeur avec paramètres pour éditer un championnat existant
        public ChampionshipConfigViewModel(Championship championship, bool isEditMode)
        {
            Championship = championship;

            if (isEditMode)
            {
                LoadFromChampionship(championship);
            }
            else
            {
                InitializeDefaults();
            }
        }

        private void InitializeDefaults()
        {
            var now = DateTime.Now;
            var currentYear = now.Year;
            var nextYear = now.Month >= 9 ? currentYear + 1 : currentYear;

            StartDate = new DateTime(nextYear, 9, 1);  // Sept année en cours/suivante
            EndDate = new DateTime(nextYear + 1, 6, 30);  // Juin année suivante
            Season = $"{nextYear}-{nextYear + 1}";
        }

        // Charger les données depuis un championnat existant
        private void LoadFromChampionship(Championship championship)
        {
            // Step 0 - Informations générales
            Name = championship.Name ?? "";
            Season = championship.Season ?? "";
            Description = championship.Description ?? "";
            StartDate = championship.StartDate;
            EndDate = championship.EndDate;
            SelectedPeriodTypeIndex = (int)championship.PeriodType;
            EnableMonthlyStandings = championship.EnableMonthlyStandings;
            EnableQuarterlyStandings = championship.EnableQuarterlyStandings;

            // Step 1 - Système de points
            SelectedPointsModeIndex = (int)championship.PointsMode;
            LinearFirstPlacePoints = championship.LinearFirstPlacePoints;
            FixedPointsTable = championship.FixedPointsTable;
            UpdateFixedPointsSummary();
            EnableParticipationPoints = championship.EnableParticipationPoints;
            ParticipationPoints = championship.ParticipationPoints;
            Tiebreaker1Index = (int)championship.Tiebreaker1;

            // Step 2 - Comptage
            SelectedCountingModeIndex = (int)championship.CountingMode;
            BestXOfSeason = championship.BestXOfSeason;

            // Détecter le type de période pour BestXPerPeriod
            if (championship.BestXPerMonth.HasValue)
            {
                BestXPeriodTypeIndex = 0; // Mois
                BestXPerPeriod = championship.BestXPerMonth;
            }
            else if (championship.BestXPerQuarter.HasValue)
            {
                BestXPeriodTypeIndex = 1; // Trimestre
                BestXPerPeriod = championship.BestXPerQuarter;
            }
            else
            {
                BestXPeriodTypeIndex = 2; // Année
                BestXPerPeriod = null;
            }

            ExcludeWorstX = championship.ExcludeWorstX;

            // Step 3 - Bonus
            CountBounties = championship.CountBounties;
            PointsPerBounty = championship.PointsPerBounty;
            VictoryBonus = championship.VictoryBonus;
            Top3Bonus = championship.Top3Bonus;
            FirstEliminatedConsolation = championship.FirstEliminatedConsolation;
            DefaultMatchCoefficient = championship.DefaultMatchCoefficient;
            FinalMatchCoefficient = championship.FinalMatchCoefficient;
            MainEventCoefficient = championship.MainEventCoefficient;

            // Step 4 - Recaves
            SelectedRebuyModeIndex = (int)championship.RebuyMode;
            RebuyLimit = championship.RebuyLimit;
            RebuyPointsPenalty = championship.RebuyPointsPenalty;
            RebuyPointsMultiplier = championship.RebuyPointsMultiplier;

            // Step 5 - Qualification
            IsOpenChampionship = championship.IsOpenChampionship;
            QualificationTopX = championship.QualificationTopX;
            QualificationMinPoints = championship.QualificationMinPoints;
            QualificationMinMatches = championship.QualificationMinMatches;
            AllowLateRegistration = championship.AllowLateRegistration;
            LateRegistrationUntilMatch = championship.LateRegistrationUntilMatch;
            AllowRetroactivePoints = championship.AllowRetroactivePoints;
        }

        // Sauvegarder dans l'objet Championship
        public void SaveToChampionship()
        {
            if (Championship == null)
                Championship = new Championship();

            // Step 0 - Informations générales
            Championship.Name = Name;
            Championship.Season = Season;
            Championship.Description = Description;
            Championship.StartDate = StartDate;
            Championship.EndDate = EndDate;
            Championship.PeriodType = (ChampionshipPeriodType)SelectedPeriodTypeIndex;
            Championship.EnableMonthlyStandings = EnableMonthlyStandings;
            Championship.EnableQuarterlyStandings = EnableQuarterlyStandings;

            // Step 1 - Configuration du système de points
            Championship.PointsMode = (ChampionshipPointsMode)SelectedPointsModeIndex;
            Championship.LinearFirstPlacePoints = LinearFirstPlacePoints;
            Championship.FixedPointsTable = FixedPointsTable;
            Championship.EnableParticipationPoints = EnableParticipationPoints;
            Championship.ParticipationPoints = ParticipationPoints;
            Championship.Tiebreaker1 = (ChampionshipTiebreaker)Tiebreaker1Index;

            // Step 2 - Comptage
            Championship.CountingMode = (ChampionshipCountingMode)SelectedCountingModeIndex;
            Championship.BestXOfSeason = BestXOfSeason;

            // Sauvegarder BestXPerPeriod selon le type choisi
            Championship.BestXPerMonth = null;
            Championship.BestXPerQuarter = null;

            if (BestXPerPeriod.HasValue)
            {
                switch (BestXPeriodTypeIndex)
                {
                    case 0: Championship.BestXPerMonth = BestXPerPeriod; break;
                    case 1: Championship.BestXPerQuarter = BestXPerPeriod; break;
                }
            }

            Championship.ExcludeWorstX = ExcludeWorstX;

            // Step 3 - Bonus
            Championship.CountBounties = CountBounties;
            Championship.PointsPerBounty = PointsPerBounty;
            Championship.VictoryBonus = VictoryBonus;
            Championship.Top3Bonus = Top3Bonus;
            Championship.FirstEliminatedConsolation = FirstEliminatedConsolation;
            Championship.DefaultMatchCoefficient = DefaultMatchCoefficient;
            Championship.FinalMatchCoefficient = FinalMatchCoefficient;
            Championship.MainEventCoefficient = MainEventCoefficient;

            // Step 4 - Recaves
            Championship.RebuyMode = (ChampionshipRebuyMode)SelectedRebuyModeIndex;
            Championship.RebuyLimit = RebuyLimit;
            Championship.RebuyPointsPenalty = RebuyPointsPenalty;
            Championship.RebuyPointsMultiplier = RebuyPointsMultiplier;

            // Step 5 - Qualification
            Championship.IsOpenChampionship = IsOpenChampionship;
            Championship.QualificationTopX = QualificationTopX;
            Championship.QualificationMinPoints = QualificationMinPoints;
            Championship.QualificationMinMatches = QualificationMinMatches;
            Championship.AllowLateRegistration = AllowLateRegistration;
            Championship.LateRegistrationUntilMatch = LateRegistrationUntilMatch;
            Championship.AllowRetroactivePoints = AllowRetroactivePoints;

            // Mettre à jour le timestamp
            Championship.UpdatedAt = DateTime.Now;
            ExportPdf();
            CustomMessageBox.ShowSuccess($"Saison {Season} créée avec succès ! Good Luck 🍀", "Succès");
        }

        partial void OnCurrentStepChanged(int value)
        {
            IsLastStep = value == 5;
        }

        [RelayCommand]
        private void GoToStep(int step)
        {
            if (step >= 0 && step <= 5)
                CurrentStep = step;
        }

        [RelayCommand]
        private void NextStep()
        {
            if (CurrentStep < 5)
                CurrentStep++;
        }

        [RelayCommand]
        private void PreviousStep()
        {
            if (CurrentStep > 0)
                CurrentStep--;
        }

        [RelayCommand]
        private void ExportPdf()
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Reglement_{Name.Replace(" ", "_")}.pdf",
                    Title = "Enregistrer le règlement"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    GeneratePdfRules(saveDialog.FileName);
                    MessageBox.Show($"Règlement généré !\n\n{saveDialog.FileName}", "Export PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur génération PDF :\n\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GeneratePdfRules(string filePath)
        {
            Document doc = new Document(PageSize.A4, 40, 40, 50, 50);
            PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
            doc.Open();

            // ======================== COULEURS ========================
            var darkBlue = new BaseColor(15, 52, 96);   // #0f3460
            var neonGreen = new BaseColor(0, 255, 136);  // #00ff88
            var lightGray = new BaseColor(248, 248, 255); // fond très léger pour la colonne gauche

            // ======================== POLICES ========================
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 26, darkBlue);
            var subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 16, darkBlue);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, darkBlue);
            var subHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, darkBlue);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.BLACK);
            var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.DARK_GRAY);
            var bulletFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, neonGreen); // vert pour les puces

            // ======================== EN-TÊTE ========================
            var title = new Paragraph($"CHAMPIONNAT {Name.ToUpper()}", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 0f;
            doc.Add(title);

            var subtitle = new Paragraph($"Saison {Season}", subtitleFont);
            subtitle.Alignment = Element.ALIGN_CENTER;
            subtitle.SpacingAfter = 4f;
            doc.Add(subtitle);

            var dates = new Paragraph($"{StartDate:dd/MM/yyyy} → {EndDate:dd/MM/yyyy}", smallFont);
            dates.Alignment = Element.ALIGN_CENTER;
            dates.SpacingAfter = 30f;
            doc.Add(dates);

            // ======================== TABLEAU 2 COLONNES ========================
            PdfPTable mainTable = new PdfPTable(2);
            mainTable.WidthPercentage = 100;
            mainTable.SetWidths(new float[] { 48f, 52f }); // un peu plus d'espace à droite
            mainTable.SpacingBefore = 10f;
            mainTable.DefaultCell.Border = Rectangle.NO_BORDER;
            mainTable.DefaultCell.Padding = 8f;

            // ------------------- COLONNE GAUCHE : SYSTÈME DE POINTS -------------------
            PdfPCell leftCell = new PdfPCell { Border = Rectangle.NO_BORDER, BackgroundColor = lightGray, Padding = 12f };

            leftCell.AddElement(new Paragraph("SYSTÈME DE POINTS", headerFont));

            Paragraph pointsModeTitle = SelectedPointsModeIndex switch
            {
                0 => new Paragraph($"Linéaire dégressif (1er = {LinearFirstPlacePoints} pts)", subHeaderFont),
                1 => new Paragraph("Points fixes par position", subHeaderFont),
                _ => new Paragraph("Proportionnel au prize pool (ICM)", subHeaderFont)
            };
            pointsModeTitle.SpacingBefore = 12f;
            pointsModeTitle.SpacingAfter = 8f;
            leftCell.AddElement(pointsModeTitle);

            PdfPTable pointsTable = new PdfPTable(2);
            pointsTable.WidthPercentage = 100;
            pointsTable.SetWidths(SelectedPointsModeIndex == 1 ? new float[] { 1.6f, 1f } : new float[] { 1f, 1f });
            pointsTable.SpacingBefore = 4f;

            AddTableCell(pointsTable, "Position", boldFont, new BaseColor(220, 220, 220), Element.ALIGN_CENTER);
            AddTableCell(pointsTable, "Points", boldFont, new BaseColor(220, 220, 220), Element.ALIGN_CENTER);

            if (SelectedPointsModeIndex == 0) // Linéaire
            {
                for (int i = 1; i <= 15; i++)
                {
                    int pts = LinearFirstPlacePoints - (i - 1);
                    if (pts < 1) pts = 1;
                    string posStr = i == 1 ? "1er" : $"{i}ème";
                    AddTableCell(pointsTable, posStr, normalFont, BaseColor.WHITE, Element.ALIGN_CENTER);
                    AddTableCell(pointsTable, $"{pts} pts", normalFont, BaseColor.WHITE, Element.ALIGN_CENTER);
                }
                AddTableCell(pointsTable, "Puis -1 pt/place", smallFont, new BaseColor(240, 240, 240), Element.ALIGN_CENTER);
                AddTableCell(pointsTable, "", smallFont, new BaseColor(240, 240, 240), Element.ALIGN_CENTER);
            }
            else if (SelectedPointsModeIndex == 1) // Points fixes
            {
                if (!string.IsNullOrEmpty(FixedPointsTable))
                {
                    try
                    {
                        var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(FixedPointsTable);
                        if (dict?.Count > 0)
                        {
                            foreach (var kvp in dict.OrderBy(x => ParsePositionForSort(x.Key)))
                            {
                                string posDisplay = FormatPosition(kvp.Key);
                                AddTableCell(pointsTable, posDisplay, normalFont, BaseColor.WHITE, Element.ALIGN_CENTER);
                                AddTableCell(pointsTable, $"{kvp.Value} pts", normalFont, BaseColor.WHITE, Element.ALIGN_CENTER);
                            }
                        }
                        else
                        {
                            AddTableCell(pointsTable, "Non configuré", normalFont, BaseColor.WHITE, Element.ALIGN_CENTER, 2);
                        }
                    }
                    catch
                    {
                        AddTableCell(pointsTable, "Erreur de configuration", normalFont, BaseColor.WHITE, Element.ALIGN_CENTER, 2);
                    }
                }
                else
                {
                    AddTableCell(pointsTable, "Non configuré", normalFont, BaseColor.WHITE, Element.ALIGN_CENTER, 2);
                }
            }
            else // ICM
            {
                AddTableCell(pointsTable, "Total points", normalFont, BaseColor.WHITE);
                AddTableCell(pointsTable, $"{ProportionalTotalPoints} pts", normalFont, BaseColor.WHITE);
                AddTableCell(pointsTable, "Répartition", normalFont, BaseColor.WHITE);
                AddTableCell(pointsTable, "Selon ICM", normalFont, BaseColor.WHITE);
            }

            leftCell.AddElement(pointsTable);

            if (EnableParticipationPoints)
            {
                var part = new Paragraph($"\n+ {ParticipationPoints} points de participation par tournoi", normalFont);
                part.SpacingBefore = 12f;
                leftCell.AddElement(part);
            }

            mainTable.AddCell(leftCell);

            // ------------------- COLONNE DROITE : DÉTAILS RÈGLEMENT -------------------
            PdfPCell rightCell = new PdfPCell { Border = Rectangle.NO_BORDER, Padding = 12f };

            rightCell.AddElement(new Paragraph("DÉTAILS DU CHAMPIONNAT", headerFont));

            // Helper pour ajouter une puce verte stylée
            void AddBulletLine(string text)
            {
                var bullet = new Chunk("»  ", bulletFont); // » est parfaitement supporté et très propre
                var content = new Chunk(text, normalFont);

                var phrase = new Phrase();
                phrase.Add(bullet);
                phrase.Add(content);

                var p = new Paragraph(phrase)
                {
                    SpacingBefore = 5f,
                    SpacingAfter = 3f,
                    FirstLineIndent = -16f,   // pour aligner la puce à gauche
                    IndentationLeft = 20f     // décalage du texte
                };
                rightCell.AddElement(p);
            }

            // 1. Méthode de comptage
            rightCell.AddElement(new Paragraph("\nMéthode de comptage", subHeaderFont));
            string countingDesc = GetCountingDescription();
            AddBulletLine(countingDesc);
            if (ExcludeWorstX.HasValue && ExcludeWorstX > 0)
                AddBulletLine($"Les {ExcludeWorstX} pires résultats sont exclus");

            // 2. Bonus (si présents)
            if (VictoryBonus > 0 || Top3Bonus > 0 || FirstEliminatedConsolation > 0 || CountBounties)
            {
                rightCell.AddElement(new Paragraph("\nBonus et primes", subHeaderFont));
                if (VictoryBonus > 0) AddBulletLine($"Victoire : +{VictoryBonus} pts");
                if (Top3Bonus > 0) AddBulletLine($"Top 3 : +{Top3Bonus} pts");
                if (FirstEliminatedConsolation > 0) AddBulletLine($"1er éliminé (consolation) : {FirstEliminatedConsolation} pts");
                if (CountBounties) AddBulletLine($"Bounty : +{PointsPerBounty} pts par bounty");
            }

            // 3. Pondération (si présente)
            if (FinalMatchCoefficient != 1.0m || MainEventCoefficient != 1.0m || DefaultMatchCoefficient != 1.0m)
            {
                rightCell.AddElement(new Paragraph("\nPondération des manches", subHeaderFont));
                AddBulletLine($"Standard : ×{DefaultMatchCoefficient}");
                if (FinalMatchCoefficient != 1.0m) AddBulletLine($"Finale : ×{FinalMatchCoefficient}");
                if (MainEventCoefficient != 1.0m) AddBulletLine($"Main Event : ×{MainEventCoefficient}");
            }

            // 4. Recaves
            rightCell.AddElement(new Paragraph("\nGestion des recaves", subHeaderFont));
            AddBulletLine(RebuyModes[SelectedRebuyModeIndex]);
            if (RebuyLimit.HasValue && SelectedRebuyModeIndex > 1)
                AddBulletLine($"Max : {RebuyLimit} recave(s)");
            if (RebuyPointsPenalty > 0)
                AddBulletLine($"Pénalité : -{RebuyPointsPenalty} pts / recave");
            else if (RebuyPointsMultiplier != 1.0m)
                AddBulletLine($"Points × {RebuyPointsMultiplier} si recave");

            // 5. Qualification
            if (!IsOpenChampionship)
            {
                rightCell.AddElement(new Paragraph("\nQualification", subHeaderFont));
                if (QualificationTopX.HasValue) AddBulletLine($"Top {QualificationTopX} qualifiés");
                if (QualificationMinPoints.HasValue) AddBulletLine($"Min {QualificationMinPoints} pts");
                if (QualificationMinMatches.HasValue) AddBulletLine($"Min {QualificationMinMatches} manches jouées");
            }

            // 6. Départage
            rightCell.AddElement(new Paragraph("\nDépartage en cas d'égalité", subHeaderFont));
            AddBulletLine(TiebreakerOptions[Tiebreaker1Index]);
            // Ajoute les autres critères de départage si tu en as plus

            mainTable.AddCell(rightCell);

            doc.Add(mainTable);

            // ======================== PIED DE PAGE ========================
            var separator = new Paragraph(new Chunk("────────────────────────────────────────", smallFont));
            separator.Alignment = Element.ALIGN_CENTER;
            separator.SpacingBefore = 20f;
            separator.SpacingAfter = 8f;
            doc.Add(separator);

            var footer = new Paragraph($"Règlement généré le {DateTime.Now:dd/MM/yyyy à HH:mm}", smallFont);
            footer.Alignment = Element.ALIGN_CENTER;
            doc.Add(footer);

            doc.Close();
        }

        // Méthode helper pour formater les positions
        private string FormatPosition(string key)
        {
            if (key.Contains("-")) return key;  
            if (key.Contains("+")) return key;  

            if (int.TryParse(key, out int pos))
            {
                if (pos == 1) return "1er";
                return pos.ToString();  
            }

            return key;
        }

        // Méthode helper pour le texte de comptage
        private string GetCountingDescription()
        {
            switch (SelectedCountingModeIndex)
            {
                case 0: return "Tous les tournois comptent";
                case 1:
                    return BestXOfSeason.HasValue
                    ? $"Seuls les {BestXOfSeason} meilleurs résultats comptent"
                    : "Meilleurs X résultats (non configuré)";
                case 2:
                    string period = BestXPeriodTypeIndex switch
                    {
                        0 => "mois",
                        1 => "trimestre",
                        _ => "année"
                    };
                    return BestXPerPeriod.HasValue
                        ? $"Les {BestXPerPeriod} meilleurs résultats par {period} sont conservés"
                        : $"Meilleurs X résultats par {period} (non configuré)";
                default: return "Configuration inconnue";
            }
        }

        // Méthode helper pour ajouter une cellule proprement
        private void AddTableCell(PdfPTable table, string text, Font font, BaseColor bgColor,
                                 int horizontalAlignment = Element.ALIGN_LEFT, int colspan = 1)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = bgColor;
            cell.HorizontalAlignment = horizontalAlignment;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Padding = 6f;
            cell.BorderWidth = 0.5f;
            cell.BorderColor = new BaseColor(180, 180, 180);
            if (colspan > 1) cell.Colspan = colspan;
            table.AddCell(cell);
        }

        private int ParsePositionForSort(string pos)
        {
            if (pos.Contains("-"))
            {
                var parts = pos.Split('-');
                if (int.TryParse(parts[0], out int start))
                    return start;
            }
            else if (pos.Contains("+"))
            {
                var num = pos.Replace("+", "");
                if (int.TryParse(num, out int val))
                    return val;
            }
            else if (int.TryParse(pos, out int single))
            {
                return single;
            }
            return 999;
        }

        private void AddTableCell(PdfPTable table, string text, Font font, BaseColor bgColor)
        {
            var cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = bgColor;
            cell.Padding = 8f;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(cell);
        }

        private void UpdateFixedPointsSummary()
        {
            if (string.IsNullOrEmpty(FixedPointsTable))
            {
                FixedPointsSummary = "Aucune configuration";
                return;
            }

            try
            {
                var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(FixedPointsTable);
                if (dict != null && dict.Count > 0)
                {
                    FixedPointsSummary = $"✓ {dict.Count} position(s) configurée(s)";
                }
                else
                {
                    FixedPointsSummary = "Aucune configuration";
                }
            }
            catch
            {
                FixedPointsSummary = "Configuration invalide";
            }
        }
    }
}