using CeskyBezBolesti_Server.Database;
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
            if (token == null) return BadRequest("Jwt Token Not Found!");
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
    }
}
