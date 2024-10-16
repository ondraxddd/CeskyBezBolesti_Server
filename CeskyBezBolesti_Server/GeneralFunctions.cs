using CeskyBezBolesti_Server.Database;
using CeskyBezBolesti_Server.DTO;
using CeskyBezBolesti_Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CeskyBezBolesti_Server
{
    public static class GeneralFunctions
    {
        private static IConfiguration _configuration;

        private static IDatabaseManager db = MyContainer.GetDbManager();
        public static void Initialize(IConfiguration config)
        {
            _configuration = config;
        }

        public static async Task<User> GetUser(string token)
        {

            try
            {
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value));

                var tokenHandler = new JwtSecurityTokenHandler();

                // Nastavení validace tokenu
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false, // Můžete upravit podle potřeby
                    ValidateAudience = false, // Můžete upravit podle potřeby
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Žádná tolerance pro časové odchylky
                };

                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

                // Extrahování informací o uživateli z Claims
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = principal.FindFirst(ClaimTypes.Name)?.Value;
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                var role = principal.FindFirst(ClaimTypes.Role)?.Value;
                var firstName = principal.FindFirst(ClaimTypes.GivenName)?.Value;
                var lastName = principal.FindFirst(ClaimTypes.Surname)?.Value;

                // Vytvoření a naplnění instance UserDto
                var userDto = new User
                {
                    Id = userId!,
                    Username = username!,
                    Email = email!,
                    Role = role!,
                    FirstName = firstName!,
                    LastName = lastName!
                };

                await RegisterNewDayOfUsingUs(userDto.Id!);
                return userDto;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static async Task<bool> CheckAdmin(string token)
        {
            // asi by stačilo si získat usera a na tom udělat check
            throw new NotImplementedException();
        }

        public static bool IsJwtValid(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value));

                var tokenHandler = new JwtSecurityTokenHandler();

                // Nastavení validace tokenu
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false, // Můžete upravit podle potřeby
                    ValidateAudience = false, // Můžete upravit podle potřeby
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Žádná tolerance pro časové odchylky
                };

                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static async Task RegisterNewDayOfUsingUs(string userId)
        {
            DateTime myDateTime = DateTime.Now;
            string sqlFormattedDate = myDateTime.ToString("yyyy-MM-dd");
            string command = $"INSERT OR IGNORE INTO user_day_history(user_id, day) VALUES({userId}, '{sqlFormattedDate}')";
            await db.RunNonQueryAsync(command);
        }
    }
}
