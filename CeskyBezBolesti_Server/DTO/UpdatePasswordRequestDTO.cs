namespace CeskyBezBolesti_Server.DTO
{
    public class UpdatePasswordRequestDTO
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }

    }
}
