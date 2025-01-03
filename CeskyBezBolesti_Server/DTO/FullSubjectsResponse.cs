using CeskyBezBolesti_Server.Models;

namespace CeskyBezBolesti_Server.DTO
{
    public class FullSubjectsResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public List<Category> Categories { get; set; }
        public int FreeQuestionsCount { get; set; }
        public int PaidQuestionsCount { get; set; }

    }
}
