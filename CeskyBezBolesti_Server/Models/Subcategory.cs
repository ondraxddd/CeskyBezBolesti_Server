namespace CeskyBezBolesti_Server.Models
{
    public class Subcategory
    {
        public int Id { get; set; }
        public int ParentCatgId { get; set; }
        public string Desc { get; set; }
    }
}
