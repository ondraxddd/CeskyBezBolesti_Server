namespace CeskyBezBolesti_Server.DTO
{
    public class MixOverviewResponseDTO
    {
        public int Id { get; set; }
        public DateTime Recorded { get; set; }
        public List<RecordedMixAnswerDTO> Answers { get; set; }

    }
}
