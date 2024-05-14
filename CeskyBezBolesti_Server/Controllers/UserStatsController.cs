﻿using CeskyBezBolesti_Server.Database;
using CeskyBezBolesti_Server.DTO;
using CeskyBezBolesti_Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata.Ecma335;

namespace CeskyBezBolesti_Server.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class UserStatsController : ControllerBase
    {
        IDatabaseManager db = MyContainer.GetDbManager();

        [HttpPost("savesesiontime")]
        public async Task<ActionResult> SaveSesionTime(SaveSesionTimeDTO timeData)
        {
            if (timeData.SesionMinutes < 2) return Ok();

            string? token = HttpContext.Request.Cookies["jwtToken"];
            if (token == null) return Ok();
            User user = await GeneralFunctions.GetUser(token);

            string command = $"SELECT minutes FROM time_spent WHERE user_id={user.Id}";
            var reader = db.RunQuery(command);
            if (reader.HasRows)
            {
                // has some time registred
                reader.Read();
                int timeInDb = int.Parse(reader["minutes"].ToString()!);
                command = $"UPDATE time_spent SET minutes={timeInDb + timeData.SesionMinutes} WHERE user_id={user.Id}";
            }
            else
            {
                command = $"INSERT INTO time_spent(user_id, minutes) VALUES({user.Id}, {timeData.SesionMinutes})";
            }


            await db.RunNonQueryAsync(command);

            await reader.CloseAsync();
            await reader.DisposeAsync();
            return Ok();
        }

        [HttpGet("getuserstats")]
        public async Task<ActionResult> GetUserStats()
        {
            string? token = HttpContext.Request.Cookies["jwtToken"];
            if (token == null) return BadRequest("User not logged in!");
            User user = await GeneralFunctions.GetUser(token);

            // success rate
            string succesRate = string.Empty; // in percentages
            string command = "SELECT round(" +
                $"((SELECT SUM(count)*1.0 AS 'correct' FROM recorded_answers WHERE user_id = {user.Id} AND wasCorrect = 1) " +
                $"/ (SELECT SUM(count)*1.0 AS 'all' FROM recorded_answers WHERE user_id = {user.Id}))*100, 0)";
            var reader = db.RunQuery(command);
            if (reader.HasRows)
            {
                reader.Read();
                succesRate = reader[0].ToString() ?? "Chyba...";
            }

            // get worst doing category
            string worstCategory = string.Empty;
            command = "SELECT c.title AS category_title, " +
                "CAST(SUM(CASE WHEN ra.wasCorrect = 1 " +
                "THEN ra.count ELSE 0 END) AS FLOAT) / SUM(ra.count) AS success_rate " +
                "FROM " +
                "recorded_answers ra " +
                "JOIN " +
                "question q ON ra.quest_id = q.id " +
                "JOIN " +
                "subcategories sc ON q.sub_catg_id = sc.id " +
                "JOIN " +
                "categories c ON sc.catg_id = c.id " +
                "WHERE " +
                $"ra.user_id = {user.Id} " +
                "GROUP BY " +
                "c.title " +
                "ORDER BY " +
                "success_rate ASC " +
                "LIMIT 1;";
            reader = db.RunQuery(command);
            if (reader.HasRows)
            {
                reader.Read();
                worstCategory = reader[0].ToString() ?? "Chyba...";
            }

            // get best doing category
            string bestCategory = string.Empty;
            command = "SELECT c.title AS category_title, " +
                "CAST(SUM(CASE WHEN ra.wasCorrect = 1 " +
                "THEN ra.count ELSE 0 END) AS FLOAT) / SUM(ra.count) AS success_rate " +
                "FROM " +
                "recorded_answers ra " +
                "JOIN " +
                "question q ON ra.quest_id = q.id " +
                "JOIN " +
                "subcategories sc ON q.sub_catg_id = sc.id " +
                "JOIN " +
                "categories c ON sc.catg_id = c.id " +
                "WHERE " +
                $"ra.user_id = {user.Id} " +
                "GROUP BY " +
                "c.title " +
                "ORDER BY " +
                "success_rate DESC " +
                "LIMIT 1;";
            reader = db.RunQuery(command);
            if (reader.HasRows)
            {
                reader.Read();
                bestCategory = reader[0].ToString() ?? "Chyba...";
            }

            // get daily time spent average
            string dailyAvg = string.Empty;
            command = "SELECT " +
                $"(SELECT minutes FROM time_spent WHERE user_id = {user.Id})" +
                $" / (SELECT count(day) FROM user_day_history WHERE user_id = {user.Id}) as \"dailyavg\"";
            reader = db.RunQuery(command);
            if (reader.HasRows)
            {
                reader.Read();
                dailyAvg = reader[0].ToString() ?? string.Empty;
            }


            return Ok();
        }
    }
}
