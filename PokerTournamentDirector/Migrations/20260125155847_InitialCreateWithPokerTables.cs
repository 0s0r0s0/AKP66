using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PokerTournamentDirector.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithPokerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BackgroundColor = table.Column<string>(type: "TEXT", nullable: false),
                    CardColor = table.Column<string>(type: "TEXT", nullable: false),
                    AccentColor = table.Column<string>(type: "TEXT", nullable: false),
                    WarningColor = table.Column<string>(type: "TEXT", nullable: false),
                    DangerColor = table.Column<string>(type: "TEXT", nullable: false),
                    EnableSounds = table.Column<bool>(type: "INTEGER", nullable: false),
                    SoundOnPauseResume = table.Column<bool>(type: "INTEGER", nullable: false),
                    SoundOn60Seconds = table.Column<bool>(type: "INTEGER", nullable: false),
                    SoundOn10Seconds = table.Column<bool>(type: "INTEGER", nullable: false),
                    SoundOnCountdown = table.Column<bool>(type: "INTEGER", nullable: false),
                    SoundOnLevelChange = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultLevelDuration = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultBreakDuration = table.Column<int>(type: "INTEGER", nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    FiscalYearStartDay = table.Column<int>(type: "INTEGER", nullable: false),
                    FiscalYearEndMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    FiscalYearEndDay = table.Column<int>(type: "INTEGER", nullable: false),
                    AdministrativeDay = table.Column<int>(type: "INTEGER", nullable: false),
                    AnnualFee = table.Column<decimal>(type: "TEXT", nullable: false),
                    TrialPeriodWeeks = table.Column<int>(type: "INTEGER", nullable: false),
                    InstallmentOptions = table.Column<string>(type: "TEXT", nullable: false),
                    EnableProrata = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProrataMode = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlindStructures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlindStructures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Championships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Season = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LogoPath = table.Column<string>(type: "TEXT", nullable: true),
                    ThemeColor = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodType = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableMonthlyStandings = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableQuarterlyStandings = table.Column<bool>(type: "INTEGER", nullable: false),
                    GenerateProvisionalAfterEachMatch = table.Column<bool>(type: "INTEGER", nullable: false),
                    PointsMode = table.Column<int>(type: "INTEGER", nullable: false),
                    LinearFirstPlacePoints = table.Column<int>(type: "INTEGER", nullable: false),
                    FixedPointsTable = table.Column<string>(type: "TEXT", nullable: true),
                    ProportionalTotalPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableParticipationPoints = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParticipationPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    Tiebreaker1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Tiebreaker2 = table.Column<int>(type: "INTEGER", nullable: true),
                    Tiebreaker3 = table.Column<int>(type: "INTEGER", nullable: true),
                    CountingMode = table.Column<int>(type: "INTEGER", nullable: false),
                    BestXOfSeason = table.Column<int>(type: "INTEGER", nullable: true),
                    BestXPerMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    BestXPerQuarter = table.Column<int>(type: "INTEGER", nullable: true),
                    ExcludeWorstX = table.Column<int>(type: "INTEGER", nullable: true),
                    CountBounties = table.Column<bool>(type: "INTEGER", nullable: false),
                    PointsPerBounty = table.Column<int>(type: "INTEGER", nullable: false),
                    VictoryBonus = table.Column<int>(type: "INTEGER", nullable: false),
                    Top3Bonus = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstEliminatedConsolation = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableSeasonPrizes = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeasonPrizePool = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrizeDistribution = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultMatchCoefficient = table.Column<decimal>(type: "TEXT", nullable: false),
                    FinalMatchCoefficient = table.Column<decimal>(type: "TEXT", nullable: false),
                    MainEventCoefficient = table.Column<decimal>(type: "TEXT", nullable: false),
                    RebuyMode = table.Column<int>(type: "INTEGER", nullable: false),
                    RebuyLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    RebuyPointsPenalty = table.Column<int>(type: "INTEGER", nullable: false),
                    RebuyPointsMultiplier = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsOpenChampionship = table.Column<bool>(type: "INTEGER", nullable: false),
                    QualificationTopX = table.Column<int>(type: "INTEGER", nullable: true),
                    QualificationMinPoints = table.Column<int>(type: "INTEGER", nullable: true),
                    QualificationMinMatches = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowLateRegistration = table.Column<bool>(type: "INTEGER", nullable: false),
                    LateRegistrationUntilMatch = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowRetroactivePoints = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Championships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Nickname = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PhotoPath = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    RegistrationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastTournamentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TrialEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalDue = table.Column<decimal>(type: "TEXT", nullable: false),
                    Paid = table.Column<decimal>(type: "TEXT", nullable: false),
                    InstallmentCount = table.Column<int>(type: "INTEGER", nullable: true),
                    NextDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalTournamentsPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalWins = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalITM = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalWinnings = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlindLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BlindStructureId = table.Column<int>(type: "INTEGER", nullable: false),
                    LevelNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    SmallBlind = table.Column<int>(type: "INTEGER", nullable: false),
                    BigBlind = table.Column<int>(type: "INTEGER", nullable: false),
                    Ante = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    IsBreak = table.Column<bool>(type: "INTEGER", nullable: false),
                    BreakName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlindLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlindLevels_BlindStructures_BlindStructureId",
                        column: x => x.BlindStructureId,
                        principalTable: "BlindStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    BuyIn = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rake = table.Column<decimal>(type: "TEXT", nullable: false),
                    RakeType = table.Column<int>(type: "INTEGER", nullable: false),
                    BlindStructureId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartingStack = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxPlayers = table.Column<int>(type: "INTEGER", nullable: false),
                    SeatsPerTable = table.Column<int>(type: "INTEGER", nullable: false),
                    LateRegLevels = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowRebuys = table.Column<bool>(type: "INTEGER", nullable: false),
                    RebuyAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    RebuyLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    RebuyLimitType = table.Column<int>(type: "INTEGER", nullable: false),
                    RebuyMaxLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    RebuyUntilPlayersLeft = table.Column<int>(type: "INTEGER", nullable: true),
                    RebuyStack = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxRebuysPerPlayer = table.Column<int>(type: "INTEGER", nullable: false),
                    RebuyPeriodMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowAddOn = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddOnAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    AddOnStack = table.Column<int>(type: "INTEGER", nullable: true),
                    AddOnAtLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowBounty = table.Column<bool>(type: "INTEGER", nullable: false),
                    BountyAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    BountyType = table.Column<int>(type: "INTEGER", nullable: false),
                    PayoutStructureJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentTemplates_BlindStructures_BlindStructureId",
                        column: x => x.BlindStructureId,
                        principalTable: "BlindStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChampionshipLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChampionshipId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BeforeData = table.Column<string>(type: "TEXT", nullable: true),
                    AfterData = table.Column<string>(type: "TEXT", nullable: true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionshipLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChampionshipLogs_Championships_ChampionshipId",
                        column: x => x.ChampionshipId,
                        principalTable: "Championships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChampionshipStandings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChampionshipId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPosition = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousPosition = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchesPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    Victories = table.Column<int>(type: "INTEGER", nullable: false),
                    Top3Finishes = table.Column<int>(type: "INTEGER", nullable: false),
                    AveragePosition = table.Column<decimal>(type: "TEXT", nullable: false),
                    BestPosition = table.Column<int>(type: "INTEGER", nullable: true),
                    WorstPosition = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalBounties = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalWinnings = table.Column<decimal>(type: "TEXT", nullable: false),
                    MonthlyPoints = table.Column<string>(type: "TEXT", nullable: true),
                    QuarterlyPoints = table.Column<string>(type: "TEXT", nullable: true),
                    RebuysUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionStdDev = table.Column<double>(type: "REAL", nullable: false),
                    ROI = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalMinutesPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageMinutesPerMatch = table.Column<int>(type: "INTEGER", nullable: false),
                    EliminatedMostByPlayerId = table.Column<string>(type: "TEXT", nullable: true),
                    EliminatedMostPlayerId = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsQualified = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionshipStandings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChampionshipStandings_Championships_ChampionshipId",
                        column: x => x.ChampionshipId,
                        principalTable: "Championships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChampionshipStandings_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsPaid = table.Column<bool>(type: "INTEGER", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentSchedules_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerLogs_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TemplateId = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    BuyIn = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rake = table.Column<decimal>(type: "TEXT", nullable: false),
                    RakeType = table.Column<int>(type: "INTEGER", nullable: false),
                    RebuyAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    AddOnAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    AllowRebuys = table.Column<bool>(type: "INTEGER", nullable: false),
                    RebuyLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    RebuyLimitType = table.Column<int>(type: "INTEGER", nullable: false),
                    RebuyMaxLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    RebuyUntilPlayersLeft = table.Column<int>(type: "INTEGER", nullable: true),
                    RebuyStack = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxRebuysPerPlayer = table.Column<int>(type: "INTEGER", nullable: false),
                    RebuyPeriodMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowAddOn = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddOnStack = table.Column<int>(type: "INTEGER", nullable: true),
                    AddOnAtLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowBounty = table.Column<bool>(type: "INTEGER", nullable: false),
                    BountyAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    BountyType = table.Column<int>(type: "INTEGER", nullable: false),
                    PayoutStructureJson = table.Column<string>(type: "TEXT", nullable: true),
                    StartingStack = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxPlayers = table.Column<int>(type: "INTEGER", nullable: false),
                    SeatsPerTable = table.Column<int>(type: "INTEGER", nullable: false),
                    LateRegistrationLevels = table.Column<int>(type: "INTEGER", nullable: false),
                    BlindStructureId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentLevelStartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalPrizePool = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalRebuys = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAddOns = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tournaments_BlindStructures_BlindStructureId",
                        column: x => x.BlindStructureId,
                        principalTable: "BlindStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tournaments_TournamentTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "TournamentTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChampionshipMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChampionshipId = table.Column<int>(type: "INTEGER", nullable: false),
                    TournamentId = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Coefficient = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsFinal = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMainEvent = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionshipMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChampionshipMatches_Championships_ChampionshipId",
                        column: x => x.ChampionshipId,
                        principalTable: "Championships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChampionshipMatches_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerRebuys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    TournamentId = table.Column<int>(type: "INTEGER", nullable: false),
                    RebuyDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    RebuyNumber = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRebuys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerRebuys_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerRebuys_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PokerTable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TournamentId = table.Column<int>(type: "INTEGER", nullable: false),
                    TableNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxSeats = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokerTable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PokerTable_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TournamentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayersRemaining = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentLogs_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TournamentId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    TableId = table.Column<int>(type: "INTEGER", nullable: true),
                    SeatNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentStack = table.Column<int>(type: "INTEGER", nullable: false),
                    RebuyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HasAddOn = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEliminated = table.Column<bool>(type: "INTEGER", nullable: false),
                    FinishPosition = table.Column<int>(type: "INTEGER", nullable: true),
                    EliminationTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EliminatedByPlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    EliminatedById = table.Column<int>(type: "INTEGER", nullable: true),
                    EliminatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Winnings = table.Column<decimal>(type: "TEXT", nullable: true),
                    ChampionshipPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    BountyKills = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentPlayers_PokerTable_TableId",
                        column: x => x.TableId,
                        principalTable: "PokerTable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TournamentPlayers_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "BlindStructures",
                columns: new[] { "Id", "CreatedDate", "Description", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 25, 16, 58, 46, 663, DateTimeKind.Local).AddTicks(4314), "Structure classique pour home games", "Standard (2h)" },
                    { 2, new DateTime(2026, 1, 25, 16, 58, 46, 663, DateTimeKind.Local).AddTicks(4478), "Structure rapide, niveaux de 12 minutes", "Turbo (1h30)" }
                });

            migrationBuilder.InsertData(
                table: "BlindLevels",
                columns: new[] { "Id", "Ante", "BigBlind", "BlindStructureId", "BreakName", "DurationMinutes", "IsBreak", "LevelNumber", "SmallBlind" },
                values: new object[,]
                {
                    { 1, 0, 50, 1, null, 20, false, 1, 25 },
                    { 2, 0, 100, 1, null, 20, false, 2, 50 },
                    { 3, 0, 150, 1, null, 20, false, 3, 75 },
                    { 4, 25, 200, 1, "Pause 15 min", 15, true, 4, 100 },
                    { 5, 25, 300, 1, null, 20, false, 5, 150 },
                    { 6, 50, 400, 1, null, 20, false, 6, 200 },
                    { 7, 75, 600, 1, null, 20, false, 7, 300 },
                    { 8, 100, 800, 1, null, 20, false, 8, 400 },
                    { 9, 100, 1000, 1, null, 20, false, 9, 500 },
                    { 10, 200, 1200, 1, null, 20, false, 10, 600 },
                    { 11, 0, 50, 2, null, 12, false, 1, 25 },
                    { 12, 0, 100, 2, null, 12, false, 2, 50 },
                    { 13, 25, 200, 2, null, 12, false, 3, 100 },
                    { 14, 25, 300, 2, "Pause 10 min", 10, true, 4, 150 },
                    { 15, 50, 400, 2, null, 12, false, 5, 200 },
                    { 16, 75, 600, 2, null, 12, false, 6, 300 },
                    { 17, 100, 1000, 2, null, 12, false, 7, 500 },
                    { 18, 200, 1600, 2, null, 12, false, 8, 800 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlindLevels_BlindStructureId_LevelNumber",
                table: "BlindLevels",
                columns: new[] { "BlindStructureId", "LevelNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipLogs_ChampionshipId",
                table: "ChampionshipLogs",
                column: "ChampionshipId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipMatches_ChampionshipId",
                table: "ChampionshipMatches",
                column: "ChampionshipId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipMatches_TournamentId",
                table: "ChampionshipMatches",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipStandings_ChampionshipId",
                table: "ChampionshipStandings",
                column: "ChampionshipId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipStandings_PlayerId",
                table: "ChampionshipStandings",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSchedules_PlayerId",
                table: "PaymentSchedules",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerLogs_PlayerId",
                table: "PlayerLogs",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRebuys_PlayerId",
                table: "PlayerRebuys",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRebuys_TournamentId",
                table: "PlayerRebuys",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Name",
                table: "Players",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PokerTable_TournamentId",
                table: "PokerTable",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentLogs_TournamentId",
                table: "TournamentLogs",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentPlayers_PlayerId",
                table: "TournamentPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentPlayers_TableId",
                table: "TournamentPlayers",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentPlayers_TournamentId",
                table: "TournamentPlayers",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_BlindStructureId",
                table: "Tournaments",
                column: "BlindStructureId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_Date",
                table: "Tournaments",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_TemplateId",
                table: "Tournaments",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTemplates_BlindStructureId",
                table: "TournamentTemplates",
                column: "BlindStructureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "BlindLevels");

            migrationBuilder.DropTable(
                name: "ChampionshipLogs");

            migrationBuilder.DropTable(
                name: "ChampionshipMatches");

            migrationBuilder.DropTable(
                name: "ChampionshipStandings");

            migrationBuilder.DropTable(
                name: "PaymentSchedules");

            migrationBuilder.DropTable(
                name: "PlayerLogs");

            migrationBuilder.DropTable(
                name: "PlayerRebuys");

            migrationBuilder.DropTable(
                name: "TournamentLogs");

            migrationBuilder.DropTable(
                name: "TournamentPlayers");

            migrationBuilder.DropTable(
                name: "Championships");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "PokerTable");

            migrationBuilder.DropTable(
                name: "Tournaments");

            migrationBuilder.DropTable(
                name: "TournamentTemplates");

            migrationBuilder.DropTable(
                name: "BlindStructures");
        }
    }
}
