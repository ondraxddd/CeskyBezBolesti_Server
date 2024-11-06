namespace CeskyBezBolesti_Server.DTO
{
    public class RecordedMixAnswerDTO
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public List<string> Answers { get; set; } = new List<string>(); // first = correct
        public bool WasUserCorrect { get; set; }

    }
}
