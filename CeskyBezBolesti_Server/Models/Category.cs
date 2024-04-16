namespace CeskyBezBolesti_Server.Models
{
    public class Category
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
        public List<Subcategory> SubCatgs { get; set; }
    }
}
