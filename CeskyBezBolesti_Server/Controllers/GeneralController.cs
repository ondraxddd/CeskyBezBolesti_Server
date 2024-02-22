using CeskyBezBolesti_Server.Database;
using CeskyBezBolesti_Server.DTO;
using CeskyBezBolesti_Server.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CeskyBezBolesti_Server.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class GeneralController : ControllerBase
    {
        IDatabaseManager db = MyContainer.GetDbManager();

        [HttpGet("getcategories")]
        public async Task<ActionResult<string>> GetCategories()
        {
            string getCtgs = "SELECT id, title FROM categories";
            var result = db.RunQuery(getCtgs);
            if (!result.HasRows)
                return BadRequest(JsonConvert.SerializeObject("No categories were loaded!"));

            List<Category> categories = new List<Category>();
            foreach(var res in result)
            {
                Category tempCatg = new Category()
                {
                    Id = int.Parse(result["id"].ToString()!),
                    Title = result["title"].ToString()!,
                   // Desc = result["desc"].ToString(),
                };
                categories.Add(tempCatg);
            }
            result.Close();
            result.DisposeAsync();

            string getSubctgs = "SELECT id, catg_id, desc FROM subcategories";
            result = db.RunQuery(getSubctgs);
            if (!result.HasRows)
                return BadRequest(JsonConvert.SerializeObject("No subcategories were loaded!"));

            List<Subcategory> subcategories = new List<Subcategory>();
            foreach (var res in result)
            {
                Subcategory tempSubcategory = new Subcategory()
                {
                    Id = int.Parse(result["id"].ToString()!),
                    ParentCatgId = int.Parse(result["catg_id"].ToString()!),
                    Desc = result["desc"].ToString(),
                };
                subcategories.Add(tempSubcategory);
            }

            foreach (var category in categories)
            {
                // Vytvoření nového seznamu pro podkategorie této kategorie
                category.SubCatgs = new List<Subcategory>();

                // Iterace přes všechny podkategorie a přiřazení podle ID rodičovské kategorie
                foreach (var subcategory in subcategories)
                {
                    if (subcategory.ParentCatgId == category.Id)
                    {
                        // Přiřazení podkategorie do seznamu podkategorií dané kategorie
                        category.SubCatgs.Add(subcategory);
                    }
                }
            }

            return JsonConvert.SerializeObject(categories);
            
        }
    }
}
