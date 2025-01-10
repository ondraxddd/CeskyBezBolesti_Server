using CeskyBezBolesti_Server.Database;
using CeskyBezBolesti_Server.DTO;
using CeskyBezBolesti_Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CeskyBezBolesti_Server.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class GeneralController : ControllerBase
    {
        IDatabaseManager db = MyContainer.GetDbManager();
        private static readonly IConfiguration _configuration;

        // vrátí kategorie i podkategorie
        [HttpGet("getallcategories")]
        public async Task<ActionResult<string>> GetAllCategories()
        {
            string getCtgs = "SELECT id, subject_id, title FROM categories";
            var result = db.RunQuery(getCtgs);
            if (!result.HasRows)
                return BadRequest(JsonConvert.SerializeObject("No categories were loaded!"));

            List<Category> categories = new List<Category>();
            foreach(var res in result)
            {
                Category tempCatg = new Category()
                {
                    Id = int.Parse(result["id"].ToString()!),
                    SubjectId = int.Parse(result["subject_id"].ToString()!),
                    Title = result["title"].ToString()!,
                   // Desc = result["desc"].ToString(),
                };
                categories.Add(tempCatg);
            }
            result.Close();
            await result.DisposeAsync();

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

        // vráti subjekty
        [HttpGet("getsubjects")]
        public async Task<ActionResult<string>> GetSubjects()
        {
            string getCtgs = "SELECT id, title FROM Subjects";
            var result = db.RunQuery(getCtgs);
            if (!result.HasRows)
                return BadRequest(JsonConvert.SerializeObject("No subjects were loaded!"));

            List<Subject> subjects = new List<Subject>();
            foreach (var res in result)
            {
                Subject tempSub = new Subject()
                {
                    Id = int.Parse(result["id"].ToString()!),
                    Title = result["title"].ToString()!,
                };
                subjects.Add(tempSub);
            }
            result.Close();
            await result.DisposeAsync();

            return JsonConvert.SerializeObject(subjects);

        }

        // vrátí subjekty i s jejich kategoriemi i podkategoriemi
        [HttpGet("getallsubjects")]
        public async Task<ActionResult<string>> GetAllSubjects()
        {
            // get subjects
            string command = "SELECT id, title FROM Subjects";
            var reader = db.RunQuery(command);
            if (!reader.HasRows)
                return BadRequest(JsonConvert.SerializeObject("No subjects were loaded!"));

            List<FullSubjectsResponse> subjects = new List<FullSubjectsResponse>();
            foreach (var res in reader)
            {
                FullSubjectsResponse tempSub = new FullSubjectsResponse()
                {
                    Id = int.Parse(reader["id"].ToString()!),
                    Title = reader["title"].ToString()!,
                };

                // get count of free and paid questions
                string commandCounts = @"SELECT 
    s.id, 
    s.title,
    COUNT(CASE WHEN q.subscription = 0 THEN 1 END) AS FreeQuestionsCount,
    COUNT(CASE WHEN q.subscription > 0 THEN 1 END) AS PaidQuestionsCount
FROM 
    Subjects s
JOIN 
    categories c ON c.subject_id = s.id
JOIN 
    subcategories sc ON sc.catg_id = c.id
JOIN 
    question q ON q.sub_catg_id = sc.id
WHERE " +
                $" s.id = {tempSub.Id} " +
                "GROUP BY " +
                "s.id, s.title;";
                var readerCounts = db.RunQuery(commandCounts);
                if (readerCounts.HasRows)
                {
                    readerCounts.Read();
                    tempSub.FreeQuestionsCount = int.Parse(readerCounts["FreeQuestionsCount"].ToString()!);
                    tempSub.PaidQuestionsCount = int.Parse(readerCounts["PaidQuestionsCount"].ToString()!);
                }
                readerCounts.Close();
                await readerCounts.DisposeAsync();
                subjects.Add(tempSub);
            }

            // get categories per each subject
            foreach(var subject in subjects)
            {
                command = $"SELECT * FROM categories WHERE subject_id = {subject.Id}";
                reader = db.RunQuery(command);
                subject.Categories = new List<Category>();
                foreach(var catg in reader)
                {
                    Category tempCatg = new Category()
                    {
                        Id = int.Parse(reader["id"].ToString()!),
                        Title = reader["title"].ToString()!,
                        Desc = reader["desc"].ToString()!,
                    };

                    // find all its subcategories per category
                    string subCatgsCommand = $"SELECT * FROM subcategories WHERE catg_id = {tempCatg.Id}";
                    var subCatgReader = db.RunQuery(subCatgsCommand);
                    tempCatg.SubCatgs = new List<Subcategory>();
                    foreach(var subCatg in subCatgReader)
                    {
                        Subcategory tempSubcatg = new Subcategory()
                        {
                            Id = int.Parse(subCatgReader["id"].ToString()),
                            Desc = subCatgReader["desc"].ToString()!
                        };
                        tempCatg.SubCatgs.Add(tempSubcatg);
                    }

                    subject.Categories.Add(tempCatg);
                }
            }


            reader.Close();
            await reader.DisposeAsync();

            return JsonConvert.SerializeObject(subjects);
        }

        [HttpGet("getsubcategorydetails")]
        public async Task<ActionResult<string>> GetSubcategoryDetails(int subCatgId)
        {
            // get details about subcategory
            string command = $"SELECT * FROM subcategories WHERE id = {subCatgId}";
            var reader = db.RunQuery(command);
            if (!reader.HasRows)
                return BadRequest(JsonConvert.SerializeObject("No subjects were loaded!"));

            
            SubcategoryDTO subCategory = new SubcategoryDTO();
            foreach (var res in reader)
            {
                subCategory.Id = subCatgId;
                subCategory.ParentCatgId = int.Parse(reader["catg_id"].ToString()!);
                subCategory.Desc = reader["desc"].ToString()!;
            }

            // get parent category name
            command = $"SELECT title FROM categories WHERE id = {subCategory.ParentCatgId}";
            reader = db.RunQuery(command);
            reader.Read();
            subCategory.ParentCatgName = reader["title"].ToString()!;


            reader.Close();
            await reader.DisposeAsync();

            return JsonConvert.SerializeObject(subCategory);
        }
    }
}
