namespace CeskyBezBolesti_Server.DTO
{
    public class SubcategoryDTO
    {
        public int Id { get; set; }
        public int ParentCatgId { get; set; }
        public string? ParentCatgName { get; set; }
        public string Desc { get; set; }
    }
}
