using CeskyBezBolesti_Server.Database;
using CeskyBezBolesti_Server.DTO;
using CeskyBezBolesti_Server.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Data.SQLite;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace CeskyBezBolesti_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        IDatabaseManager db = MyContainer.GetDbManager();

        public AuthController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> RegisterUser(UserDto req)
        {
            //TODO Defened against the SQL injection!!!!!!!

            // Check if user exists
            string loadRecords = $"SELECT id FROM users WHERE email = '{req.Email}'";
            if (db.RunQuery(loadRecords).HasRows)
            {
                return BadRequest(JsonConvert.SerializeObject("User already exists."));
            }

            CreatePasswordHash(req.Password, out byte[] passwordHash, out byte[] passwordSalt);         
            // Save user to db
            string saveUserCommand = "INSERT INTO users(username, last_name, first_name, email, password_hash, password_salt, created_at) " +
                $" VALUES('{req.Username}', '{req.LastName}', '{req.FirstName}', '{req.Email}', '{Convert.ToBase64String(passwordHash)}', '{Convert.ToBase64String(passwordSalt)}', '{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}')";
            db.RunNonQuery(saveUserCommand);

            User tempUser = new User() { 
            FirstName = req.FirstName,
            LastName = req.LastName,
            Username= req.Username,
            Email= req.Email,
            };

            return Ok(JsonConvert.SerializeObject(CreateToken(tempUser))); 
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDto req)
        {
            // Check if user exists
            string loadRecords = $"SELECT * FROM users WHERE email = '{req.Email}'";
            var result = db.RunQuery(loadRecords);
            if(!result.HasRows)
                return BadRequest(JsonConvert.SerializeObject("User doesnt exists!"));

            result.Read();
            string dbHash = (string)result["password_hash"];
            string dbSalt = (string)result["password_salt"];
            User tempUser = new User()
            {
                FirstName = (string)result["first_name"],
                LastName = (string)result["last_name"],
                Username = (string)result["username"],
                Email = (string)result["email"]
            };
            result.Close();
            result.DisposeAsync();

            // Verify password
            bool isPasswordCorrect = VerifyPassword(req.Password, Convert.FromBase64String(dbHash), Convert.FromBase64String(dbSalt));
            if(!isPasswordCorrect) return BadRequest(JsonConvert.SerializeObject("Wrong password!"));

            // Return JWT token
            string token = CreateToken(tempUser);
            Response.Headers.SetCookie = new Microsoft.Extensions.Primitives.StringValues($"jwtToken={token};httponly;path=/;samesite=none;secure");
            return Ok(JsonConvert.SerializeObject(token));
        }

        [HttpPost("verifyjwttoken")]
        public async Task<ActionResult<string>> VerifyJwtToken(string token)
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

                // V případě úspěšné validace můžete provádět další akce nebo vrátit informace o uživateli
                // např. principal.Claims.Select(c => new { c.Type, c.Value });

                return Ok("Token je platný.");
            }
            catch (Exception ex)
            {
                return BadRequest("Neplatný token. " + ex.Message);
            }
        }

        [HttpGet("/getuser")]
        public async Task<ActionResult<string>> GetUser()
        {
            string? token2 = HttpContext.Request.Cookies["jwtToken"];

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
                var principal = tokenHandler.ValidateToken(token2, validationParameters, out validatedToken);

                // Extrahování informací o uživateli z Claims
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = principal.FindFirst(ClaimTypes.Name)?.Value;
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                var role = principal.FindFirst(ClaimTypes.Role)?.Value;
                var firstName = principal.FindFirst(ClaimTypes.GivenName)?.Value;
                var lastName = principal.FindFirst(ClaimTypes.Surname)?.Value;

                // Vytvoření a naplnění instance UserDto
                var userDto = new UserDto
                {
                    Id = userId,
                    Username = username,
                    Email = email,
                    Role = role,
                    FirstName = firstName,
                    LastName = lastName
                };

                return Ok(JsonConvert.SerializeObject(userDto));
            }
            catch (Exception ex)
            {
                return BadRequest("Neplatný token. " + ex.Message);
            }
        }


        [HttpGet("isjwtincluded")]
        public IActionResult IsJwtIncuded()
        {
            // Získání hodnoty cookie z požadavku
            var jwtTokenCookie = Request.Headers.Cookie.FirstOrDefault(x => x.StartsWith("jwtToken"));

            if (jwtTokenCookie != null)
            {
                // Zde můžete provádět další logiku, například ověření platnosti tokenu
                // nebo vrácení nějaké odpovědi na základě existence tokenu.

                return Ok(new { Message = "Token found", Token = jwtTokenCookie });
            }
            else
            {
                return BadRequest("Token not found");
            }
        }

        [HttpGet("/ping")]
        public void Ping()
        {
            Response.Headers.SetCookie = "jwtToken=ahoj; httpOnly=true; secure; samesite=none; path=/";
            Response.StatusCode = 200;
        }
        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role is null ? "Student" : user.Role),
                new Claim(ClaimTypes.Email, user.Email)
            };
           var key = new SymmetricSecurityKey(System.Text.
              Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using(var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private static bool VerifyPassword(string enteredPassword, byte[] storedHash, byte[] storedSalt)
        {
            using (var hmac = new HMACSHA512(storedSalt))
            {
                byte[] computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(enteredPassword));
                string newHash = Convert.ToBase64String(computedHash);
                string oldHash = Convert.ToBase64String(storedHash);
                return newHash == oldHash;
                // return computedHash.Equals(storedHash); - proč porovnávání bytes nefunguje???
            }
        }
    }
}
