using CeskyBezBolesti_Server.Database;
using CeskyBezBolesti_Server.DTO;
using CeskyBezBolesti_Server.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;

namespace CeskyBezBolesti_Server.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        IDatabaseManager db = MyContainer.GetDbManager();
        int limit = 8;

        [HttpPost("getset")]
        public async Task<ActionResult<string>> GetSet(QuestionRequestDto request)
        {
            Question[] questions = new Question[limit];
            string command = $"SELECT * FROM question WHERE sub_catg_id = {request.SubCatgId}"
                   + $" ORDER BY RANDOM() LIMIT {limit}";

            // get the questions
            var reader = db.RunQuery(command);
            int questIndex = 0;
            foreach (var row in reader)
            {
                Question tempQuest = new Question()
                {
                    QuestionId = int.Parse(reader[0].ToString()!),
                    SubCatgId = int.Parse(reader[1].ToString()!),
                    QuestionText = reader[2].ToString()!
                };
                questions[questIndex] = tempQuest;
                questIndex++;
            }

            // get correct answer
            for(int i = 0; i < questions.Count(); i++)
            {
                if (questions[i] is null) continue;
                command = $"SELECT text FROM answers WHERE quest_id = {questions[i].QuestionId}" +
                    $" AND IsCorrect == 1";
                reader = db.RunQuery(command);
                if (!reader.HasRows) continue;
                reader.Read();
                questions[i].CorrectAnswer = reader[0].ToString();
            }

            //get the false answer
            for (int i = 0; i < questions.Count(); i++)
            {
                if (questions[i] is null) continue;
                command = $"SELECT text FROM answers WHERE quest_id = {questions[i].QuestionId}" +
                    $" AND IsCorrect == 0";
                reader = db.RunQuery(command);
                if (!reader.HasRows) continue;
                reader.Read();
                questions[i].FalseAnswer = reader[0].ToString();
            }

            reader.Close();
            await reader.DisposeAsync();
            QuestionResponseDto response = new QuestionResponseDto() {
                Questions = questions
            };
            return JsonConvert.SerializeObject(response);
        }

        [HttpGet("getfouryearmix")]
        public async Task<ActionResult<string>> Getfouryearmix()
        {
            int countPraceSTextem = 1;
            int countSlovniZasoba = 1;
            int countGramatika = 1;
            Question[] questions = new Question[3];

            // get questions from Prace s Textem category
            string command = "SELECT q.* " +
                "FROM question q " +
                "JOIN subcategories sc " +
                "ON q.sub_catg_id = sc.id " +
                "JOIN categories c ON " +
                "sc.catg_id = c.id " +
                "WHERE c.subject_id = 2 " +
                "ORDER BY RANDOM() " +
                $"LIMIT {countPraceSTextem};";

            // get the questions
            var reader = db.RunQuery(command);
            int questIndex = 0;
            foreach (var row in reader)
            {
                Question tempQuest = new Question()
                {
                    QuestionId = int.Parse(reader[0].ToString()!),
                    SubCatgId = int.Parse(reader[1].ToString()!),
                    QuestionText = reader[2].ToString()!
                };
                questions[questIndex] = tempQuest;
                questIndex++;
            }

            // get questions from Slovni zasoba category
            command = "SELECT q.* " +
                "FROM question q " +
                "JOIN subcategories sc " +
                "ON q.sub_catg_id = sc.id " +
                "JOIN categories c ON " +
                "sc.catg_id = c.id " +
                "WHERE c.subject_id = 3 " +
                "ORDER BY RANDOM() " +
                $"LIMIT {countSlovniZasoba};";

            // get the questions
            reader = db.RunQuery(command);
            foreach (var row in reader)
            {
                Question tempQuest = new Question()
                {
                    QuestionId = int.Parse(reader[0].ToString()!),
                    SubCatgId = int.Parse(reader[1].ToString()!),
                    QuestionText = reader[2].ToString()!
                };
                questions[questIndex] = tempQuest;
                questIndex++;
            }

            // get questions from Gramatika category
            command = "SELECT q.* " +
                "FROM question q " +
                "JOIN subcategories sc " +
                "ON q.sub_catg_id = sc.id " +
                "JOIN categories c ON " +
                "sc.catg_id = c.id " +
                "WHERE c.subject_id = 1 " +
                "ORDER BY RANDOM() " +
                $"LIMIT {countGramatika};";

            // get the questions
            reader = db.RunQuery(command);
            foreach (var row in reader)
            {
                Question tempQuest = new Question()
                {
                    QuestionId = int.Parse(reader[0].ToString()!),
                    SubCatgId = int.Parse(reader[1].ToString()!),
                    QuestionText = reader[2].ToString()!
                };
                questions[questIndex] = tempQuest;
                questIndex++;
            }

            // get correct answer
            for (int i = 0; i < questions.Count(); i++)
            {
                if (questions[i] is null) continue;
                command = $"SELECT text FROM answers WHERE quest_id = {questions[i].QuestionId}" +
                    $" AND IsCorrect == 1";
                reader = db.RunQuery(command);
                if (!reader.HasRows) continue;
                reader.Read();
                questions[i].CorrectAnswer = reader[0].ToString();
            }

            //get the false answer
            for (int i = 0; i < questions.Count(); i++)
            {
                if (questions[i] is null) continue;
                command = $"SELECT text FROM answers WHERE quest_id = {questions[i].QuestionId}" +
                    $" AND IsCorrect == 0";
                reader = db.RunQuery(command);
                if (!reader.HasRows) continue;
                reader.Read();
                questions[i].FalseAnswer = reader[0].ToString();
            }

            reader.Close();
            await reader.DisposeAsync();
            QuestionResponseDto response = new QuestionResponseDto()
            {
                Questions = questions
            };
            return Ok(JsonConvert.SerializeObject(response));
        }

        [HttpPost("reportmixresult")]
        public async Task<ActionResult> ReportMixResult(MixReportDTO report)
        {
            string? token = HttpContext.Request.Cookies["jwtToken"];
            if (token == null) return BadRequest("Jwt Token Not Found!");
            User user = await GeneralFunctions.GetUser(token);

            // save report
            string command;
            DateTime currentDateTime = DateTime.Now;
            string formattedDateTime = currentDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            command = $"INSERT INTO mix_reports(user_id, date) VALUES({user.Id},'{formattedDateTime}')";
            await db.RunNonQueryAsync(command);
            int newReportId;
            command = "SELECT last_insert_rowid();"; // TODO udělat to bezpečněji, zprovoznit returning
            var reader = db.RunQuery(command);
            if(!reader.HasRows) return BadRequest("Při ukladani došlo k neočekavane chybe.");
            reader.Read();
            newReportId = int.Parse(reader[0].ToString()!);

            // save answers
            foreach (var answer in report.Answers)
            {
                command = $"INSERT INTO mix_answers(mix_report_id, question_id, is_correct) " +
                    $"VALUES({newReportId}, {answer.QuestId}, {answer.IsCorrect})";
                await db.RunNonQueryAsync(command);
            }
            return Ok();
        }

        [HttpPost("recordmistake")]
        public async Task<ActionResult<string>> RecordMistake(RecordAnswerDTO answer)
        {
            string? token = HttpContext.Request.Cookies["jwtToken"];
            if (token == null) return BadRequest("Jwt Token Not Found!");
            User user = await GeneralFunctions.GetUser(token);

            //record to db
            string command = $"SELECT * FROM recorded_answers WHERE user_id={user.Id} AND quest_id={answer.QuestId} AND " +
                $"wasCorrect={(answer.WasCorrect ? 1 : 0)};";
            var reader = db.RunQuery(command);
            int count = 1;
            //has atleast once answered
            if(reader.HasRows)
            {
                reader.Read();
                count = int.Parse(reader["count"].ToString()!);
                count++;
                command = $"UPDATE recorded_answers SET count = {count} WHERE user_id = {user.Id} AND quest_id = {answer.QuestId}" +
                    $" AND wasCorrect={(answer.WasCorrect ? 1 : 0)}";

            }
            else
            {
                command = $"INSERT INTO recorded_answers(user_id, quest_id, wasCorrect, count) " +
                    $"VALUES({user.Id}, {answer.QuestId}, {(answer.WasCorrect ? 1 : 0)}, {count})";
            }
            await reader.CloseAsync();
            await reader.DisposeAsync();

            await db.RunNonQueryAsync(command);

            return Ok();
        }

        [HttpPost("addcategory")]
        public async Task<ActionResult> AddCategory(CategoryAddDTO newCategory)
        {
            // check if jwt token is valid
            string? token = HttpContext.Request.Cookies["jwtToken"];
            if (token == null) return BadRequest("Jwt Token Not Found!");

            if (!GeneralFunctions.IsJwtValid(token))
            {
                return BadRequest("Jwt token not valid!");
            }

            // check if he is admin
            User user = await GeneralFunctions.GetUser(token);
            if (user.Role != "admin")
            {
                return BadRequest("You must be admin!");
            }

            // TODO check if subject is unique

            string command = $"INSERT INTO categories(subject_id, title) VALUES({newCategory.SubjectId},'{newCategory.CategoryName}')";
            try
            {
                await db.RunNonQueryAsync(command);
            }
            catch (Exception ex)
            {
                return BadRequest("Saving to db has failed.");
            }
            
            return Ok();
        }

        [HttpPost("addsubcategory")]
        public async Task<ActionResult> AddSubcategory(SubcategoryAddDTO newSubcategory)
        {
            // check if jwt token is valid
            string? token = HttpContext.Request.Cookies["jwtToken"];
            if (token == null) return BadRequest("Jwt Token Not Found!");

            if (!GeneralFunctions.IsJwtValid(token))
            {
                return BadRequest("Jwt token not valid!");
            }

            // check if he is admin
            User user = await GeneralFunctions.GetUser(token);
            if (user.Role != "admin")
            {
                return BadRequest("You must be admin!");
            }

            // TODO check if subject is unique

            string command = $"INSERT INTO subcategories(catg_id, desc) VALUES({newSubcategory.CategoryId}," +
                $" '{newSubcategory.SubcategoryName}')";
            try
            {
                await db.RunNonQueryAsync(command);
            }
            catch (Exception ex)
            {
                return BadRequest("Saving to db has failed.");
            }

            return Ok();
        }

        [HttpPost("addquestion")]
        public async Task<ActionResult> AddQuestion(QuestionAddDTO newQuestion)
        {
            // check if jwt token is valid
            string? token = HttpContext.Request.Cookies["jwtToken"];
            if (token == null) return BadRequest("Jwt Token Not Found!");

            if (!GeneralFunctions.IsJwtValid(token))
            {
                return BadRequest("Jwt token not valid!");
            }

            // check if he is admin
            User user = await GeneralFunctions.GetUser(token);
            if (user.Role != "admin")
            {
                return BadRequest("You must be admin!");
            }

            // check if the question doesnt exits

            // record the answer
            string command = $"INSERT INTO question(sub_catg_id, text) VALUES({newQuestion.SubCatgId}," +
                $" '{newQuestion.QuestText}')";
            await db.RunNonQueryAsync(command);

            // get id of the newly added question
            int questId;
            command = "SELECT id FROM question" +
                $" WHERE sub_catg_id={newQuestion.SubCatgId} AND " +
                $" text='{newQuestion.QuestText}'";

            var reader = db.RunQuery(command);
            if (reader.HasRows)
            {
                await reader.ReadAsync();
                questId = int.Parse(reader["id"].ToString()!);
                reader.Close();
                await reader.DisposeAsync();
            }
            else
            {
                return BadRequest("Addidng question has failed.");
            }
            

            // save correct answer
            command = "INSERT INTO answers(quest_id, text, IsCorrect)" +
                $"VALUES({questId}, '{newQuestion!.Answers![0]}', 1)";
            await db.RunNonQueryAsync(command);

            // save all the wrong ones
            for(int i = 1; i < newQuestion.Answers.Length; i++)
            {
                command = "INSERT INTO answers(quest_id, text, IsCorrect)" +
                $"VALUES({questId}, '{newQuestion!.Answers![i]}', 0)";
                await db.RunNonQueryAsync(command);
            }

            return Ok();
        }

        [HttpPost("getallquestions")]
        public async Task<ActionResult> GetAllQuestions(AllQuestsRequestDTO req)
        {
            List<Question> questions = new List<Question>();
            string command = $"SELECT * FROM question WHERE sub_catg_id = {req.subCatgId}";
            var reader = db.RunQuery(command);
            foreach (var row in reader)
            {
                Question tempQuest = new Question()
                {
                    QuestionId = int.Parse(reader[0].ToString()!),
                    SubCatgId = int.Parse(reader[1].ToString()!),
                    QuestionText = reader[2].ToString()!
                };
                questions.Add(tempQuest);
            }
            return Ok(questions);
        }

    }
}
