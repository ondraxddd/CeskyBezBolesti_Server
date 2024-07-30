namespace CeskyBezBolesti_Server.DTO
{
    public class UserFullStatsDTO
    {
        public string SuccessRate { get; set; } = "0";
        public string BestCategory { get; set; } = string.Empty;
        public string WorstCategory { get; set; } = string.Empty;
        public string DailyAvg { get; set; } = "0";
        public List<Dictionary<string, string>> ShouldRemember { get; set; } = new List<Dictionary<string, string>>();
    }
}
