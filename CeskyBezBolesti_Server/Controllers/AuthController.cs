using CeskyBezBolesti_Server.Database;
using CeskyBezBolesti_Server.DTO;
using CeskyBezBolesti_Server.Emailing;
using CeskyBezBolesti_Server.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Data;
using System.Data.SQLite;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
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
        IEmailSender emailSender = MyContainer.GetEmailSender();


        public AuthController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> RegisterUser(RegisterUserDTO req)
        {
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
            emailSender.SendEmail(new MailAddress(req.Email), "Potvrzení registrace",
                "Tímto potvrzujeme vaši úspěšnou registrace na stránkách ceskybezbolesti.com");

            // Get id of new user
            string getIdCommand = $"SELECT id FROM users WHERE email='{req.Email}' AND username='{req.Username}'";
            var reader = db.RunQuery(getIdCommand);
            string userId = string.Empty;
            if (reader.HasRows)
            {
                reader.Read();
                userId = reader[0].ToString() ?? string.Empty;
            }

            User tempUser = new User() { 
            FirstName = req.FirstName,
            LastName = req.LastName,
            Username= req.Username,
            Email= req.Email,
            Id = userId,
            };

            await GeneralFunctions.RegisterNewDayOfUsingUs(tempUser.Id!);

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
            var role = result["role"];
            string userRole;
            if(role is System.DBNull)
            {
                userRole = "student";
            }
            else
            {
                userRole = role.ToString()!;
            }
            User tempUser = new User()
            {
                Id = result["id"].ToString()!,
                FirstName = (string)result["first_name"],
                LastName = (string)result["last_name"],
                Username = (string)result["username"],
                Email = (string)result["email"],
                Role = userRole
            };
            result.Close();
            await result.DisposeAsync();

            // Verify password
            bool isPasswordCorrect = VerifyPassword(req.Password, Convert.FromBase64String(dbHash), Convert.FromBase64String(dbSalt));
            if(!isPasswordCorrect) return BadRequest(JsonConvert.SerializeObject("Wrong password!"));

            // Return JWT token
            string token = CreateToken(tempUser);
            Response.Headers.SetCookie = new Microsoft.Extensions.Primitives.StringValues($"jwtToken={token};httponly;path=/;samesite=none;secure");

            await GeneralFunctions.RegisterNewDayOfUsingUs(tempUser.Id!);

            return Ok(JsonConvert.SerializeObject(token));
        }

        [HttpGet("logout")]
        public async Task<ActionResult<string>> Logout()
        {
            string? token = HttpContext.Request.Cookies["jwtToken"];
            if (token == null) return Ok();

            Response.Cookies.Append("jwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(-1), // set expiration to past
                Secure = true,
                SameSite = SameSiteMode.None 
            });

            return Ok(new { message = "Odhlášení bylo úspěšné." });
        }

        [HttpPost("resetpassword")]
        public async Task<ActionResult> ResetPassword(ResetPasswordRequestDTO req)
        {
            // Check if user exists
            string query = $"SELECT id, email FROM users WHERE email = '{req.Email}'";
            var result = db.RunQuery(query);
            if (!result.HasRows)
            {
                return BadRequest("User does not exist.");
            }

            result.Read();
            string userId = result["id"].ToString()!;
            string userEmail = result["email"].ToString()!;

            // Generate a secure random reset token
            string resetToken = GenerateResetToken();
            DateTime createdAt = DateTime.Now;
            DateTime experationAt = DateTime.UtcNow.AddMinutes(15); // Token valid for 1 hour

            // Save the reset token and expiration to the database
            string command = $"INSERT INTO reset_password_tokens(user_id,token,was_used, expires_at,created_at) " +
                $"VALUES({userId},'{resetToken}',0,'{experationAt:yyyy-MM-dd HH:mm:ss}', '{createdAt:yyyy-MM-dd HH:mm:ss}')";
            db.RunNonQuery(command);

            // Send reset link to user's email
            string resetLink = $"https://ceskybezbolesti.cz/resetpassword/{resetToken}";
            string emailBody = $"Klikněte na následující odkaz pro obnovení hesla: {resetLink}. Tento odkaz je platný pouze 15 minut.";
            emailSender.SendEmail(new MailAddress(userEmail), "Obnovení hesla", emailBody);

            return Ok("Reset link has been sent to your email.");
        }

        [HttpPost("updatepassword")]
        public async Task<ActionResult> UpdatePassword(UpdatePasswordRequestDTO req)
        {
            // check if reset token is valid
            if (!VerifyResetPasswordToken(req.Token))
                return BadRequest("Reset token is invalid");

            // get user id
            string command = $"SELECT user_id FROM reset_password_tokens WHERE token = '{req.Token}'";
            var reader = db.RunQuery(command);
            if (!reader.HasRows)
                return BadRequest("An unexpected error has occured.");
            reader.Read();
            int userId = int.Parse(reader["user_id"].ToString()!);

            // generate new password hash and update 
            CreatePasswordHash(req.NewPassword, out byte[] passwordHash, out byte[] passwordSalt);

            // update users password in db
            command = $"UPDATE users SET password_hash = '{Convert.ToBase64String(passwordHash)}', password_salt = '{Convert.ToBase64String(passwordSalt)}' " +
                $"WHERE id = {userId}";
            db.RunNonQuery(command);

            // flag reset token as used
            command = $"UPDATE reset_password_tokens SET was_used=1 WHERE token = '{req.Token}'";
            db.RunNonQuery(command);

            return Ok();
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
            if (token2 == null) return BadRequest("Jwt Token not found!");

            User user = await GeneralFunctions.GetUser(token2);
            return Ok(JsonConvert.SerializeObject(user));
        }

        [HttpGet("/checkifadmin")]
        public async Task<ActionResult<bool>> CheckIfAdmin()
        {
            string? token2 = HttpContext.Request.Cookies["jwtToken"];
            if (token2 == null) return BadRequest("Jwt Token not found!");

            User user = await GeneralFunctions.GetUser(token2);
            return Ok(user.Role == "admin" ? true : false);
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
                new Claim(ClaimTypes.NameIdentifier, user.Id!),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role is null ? "student" : user.Role),
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

        private string GenerateResetToken()
        {
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes).Replace("+", "").Replace("/", "").Replace("=", ""); // Remove URL-unsafe chars
        }

        private bool VerifyResetPasswordToken(string token)
        {
            //Check if token exists and is still valid - gotta edit the database command
            string query = $"SELECT id, expires_at, was_used FROM reset_password_tokens WHERE token = '{token}'";
            var result = db.RunQuery(query);

            if (!result.HasRows)
            {
                return false;
            }

            result.Read();
            DateTime tokenExpiration = DateTime.Parse(result["expires_at"].ToString()!);
            bool wasUsed = bool.Parse(result["was_used"].ToString()!);

            // Check if the token has expired
            if (DateTime.UtcNow > tokenExpiration || wasUsed)
            {
                return false;
            }

            return true;

        }
    }
}
