using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokerTournamentDirector.Migrations
{
    /// <inheritdoc />
    public partial class AddNewSoundSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Rake",
                table: "TournamentTemplates",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "BuyIn",
                table: "TournamentTemplates",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<bool>(
                name: "SoundOnBreak",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SoundOnKill",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SoundOnRebuy",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SoundOnStart",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SoundOnUndoKill",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SoundOnWin",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "BlindStructures",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 27, 13, 11, 48, 87, DateTimeKind.Local).AddTicks(9295));

            migrationBuilder.UpdateData(
                table: "BlindStructures",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 27, 13, 11, 48, 87, DateTimeKind.Local).AddTicks(9460));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoundOnBreak",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "SoundOnKill",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "SoundOnRebuy",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "SoundOnStart",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "SoundOnUndoKill",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "SoundOnWin",
                table: "AppSettings");

            migrationBuilder.AlterColumn<decimal>(
                name: "Rake",
                table: "TournamentTemplates",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyIn",
                table: "TournamentTemplates",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.UpdateData(
                table: "BlindStructures",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 25, 16, 58, 46, 663, DateTimeKind.Local).AddTicks(4314));

            migrationBuilder.UpdateData(
                table: "BlindStructures",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 25, 16, 58, 46, 663, DateTimeKind.Local).AddTicks(4478));
        }
    }
}
