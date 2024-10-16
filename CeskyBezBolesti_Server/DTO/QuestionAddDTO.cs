namespace CeskyBezBolesti_Server.DTO
{
    public class QuestionAddDTO
    {
        public int SubCatgId { get; set; }
        public string? QuestText { get; set; }
        public string[]? Answers { get; set; } // first gotta be correct!
    }
}
