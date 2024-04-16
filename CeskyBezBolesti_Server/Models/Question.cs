namespace CeskyBezBolesti_Server.Models
{
    public class Question
    {
        public int QuestionId { get; set; }
        public int SubCatgId { get; set; }
        public string QuestionText { get; set; }
        public string FalseAnswer { get; set; }
        public string CorrectAnswer { get; set; }
    }
}
