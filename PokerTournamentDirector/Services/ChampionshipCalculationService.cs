using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.Services
{
    /// <summary>
    /// Service dédié aux calculs de points et statistiques de championnat
    /// Centralise toute la logique métier pour éviter la duplication
    /// </summary>
    public class ChampionshipCalculationService
    {
        public int CalculateFullMatchPoints(Championship championship, ChampionshipMatch match, TournamentPlayer tp)
        {
            int basePoints = CalculateBasePoints(championship, tp.FinishPosition.Value);
            basePoints = (int)(basePoints * match.Coefficient);
            
            if (championship.CountBounties)
                basePoints += tp.BountyKills * championship.PointsPerBounty;
            
            if (tp.FinishPosition == 1 && championship.VictoryBonus > 0)
                basePoints += championship.VictoryBonus;
            
            if (tp.FinishPosition <= 3 && championship.Top3Bonus > 0)
                basePoints += championship.Top3Bonus;
            
            if (tp.RebuyCount > 0)
            {
                basePoints -= tp.RebuyCount * championship.RebuyPointsPenalty;
                basePoints = (int)(basePoints * championship.RebuyPointsMultiplier);
            }
            
            return basePoints;
        }

        public int CalculateBasePoints(Championship championship, int position)
        {
            int basePoints = championship.PointsMode switch
            {
                ChampionshipPointsMode.Linear => 
                    Math.Max(1, championship.LinearFirstPlacePoints - (position - 1)),
                ChampionshipPointsMode.FixedByPosition => 
                    GetFixedPointsForPosition(championship, position),
                ChampionshipPointsMode.ProportionalPrizePool => 
                    Math.Max(1, championship.LinearFirstPlacePoints - (position - 1)),
                _ => 0
            };

            if (championship.EnableParticipationPoints)
                basePoints += championship.ParticipationPoints;

            return basePoints;
        }

        private int GetFixedPointsForPosition(Championship championship, int position)
        {
            if (string.IsNullOrEmpty(championship.FixedPointsTable)) return 0;

            try
            {
                var table = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(championship.FixedPointsTable);
                if (table == null) return 0;

                if (table.ContainsKey(position.ToString()))
                    return table[position.ToString()];

                foreach (var kvp in table)
                {
                    if (kvp.Key.Contains("-"))
                    {
                        var parts = kvp.Key.Split('-');
                        if (int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int max))
                        {
                            if (position >= min && position <= max)
                                return kvp.Value;
                        }
                    }
                }
            }
            catch { }

            return 0;
        }
    }
}