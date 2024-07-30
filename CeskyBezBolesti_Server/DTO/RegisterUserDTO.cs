using System.Globalization;

namespace CeskyBezBolesti_Server.DTO
{
    public class RegisterUserDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

    }
}
