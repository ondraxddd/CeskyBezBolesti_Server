namespace CeskyBezBolesti_Server.Models
{
    public class User
    {
        public string? Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Email { get; set; }
        public string? Role { get; set; }
        public TimeSpan? Sub_exp { get; set; }
        public string? SupervisorId { get; set; }
        public bool IsVerified { get; set; }
        /* public byte[]? PasswordHash { get; set; }
        public byte[]? PasswordSalt { get; set; } */

    }
}
