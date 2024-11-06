using CeskyBezBolesti_Server.Database;
using CeskyBezBolesti_Server.DTO;
using CeskyBezBolesti_Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Reflection.PortableExecutable;

namespace CeskyBezBolesti_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MixController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        IDatabaseManager db = MyContainer.GetDbManager();

        int reportCount = 15; // how many mix reports to be returned

        public MixController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        [HttpGet("getallmixes")]
        public async Task<ActionResult> GetAllMixes() // Get all reported mixes within count limit
        {
            string? token = HttpContext.Request.Cookies["jwtToken"];
            if (token == null) return BadRequest("Jwt Token Not Found!");
            User user = await GeneralFunctions.GetUser(token);

            string command = $"SELECT * FROM mix_reports WHERE user_id = {user.Id} " +
                $"ORDER BY date DESC LIMIT {reportCount}";
            var reader = db.RunQuery(command);

            if (!reader?.HasRows == null || reader == null) return Ok();
            List<OneMixReportOverview> reports = new List<OneMixReportOverview>();
            foreach (var report in reader)
            {
                OneMixReportOverview tempReport = new OneMixReportOverview();
                tempReport.Id = int.Parse(reader["id"].ToString()!);
                string sqlDateTime = reader["date"].ToString()!;
                tempReport.DateTime = DateTime.ParseExact(sqlDateTime, "M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
                reports.Add(tempReport);
            }
            reader.Close();
            await reader.DisposeAsync();

            MixReportsOverviewDTO reportsResponse = new MixReportsOverviewDTO(reports.ToArray());
            return Ok(JsonConvert.SerializeObject(reportsResponse));
        }

        [HttpPost("getreport")]
        public async Task<ActionResult> GetReport(MixOverviewRequestDTO req)
        {
            string? token = HttpContext.Request.Cookies["jwtToken"];
            if (token == null) return BadRequest("Jwt Token Not Found!");

            User user = await GeneralFunctions.GetUser(token);

            // Verify report ownership
            string command = $"SELECT date FROM mix_reports WHERE id = {req.ReportId} AND user_id = {user.Id}";
            var reader = db.RunQuery(command);
            if (reader == null || !reader.HasRows) return BadRequest();

            // Get report date
            reader.Read();
            string sqlDateTime = reader["date"].ToString()!;
            DateTime reportDate = DateTime.ParseExact(sqlDateTime, "M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
            reader.Close();
            await reader.DisposeAsync();

            // Prepare response DTO
            var response = new MixOverviewResponseDTO
            {
                Id = req.ReportId,
                Recorded = reportDate,
                Answers = new List<RecordedMixAnswerDTO>()
            };

            // Query for mix answers
            command = $"SELECT id, question_id, is_correct FROM mix_answers WHERE mix_report_id = {req.ReportId}";
            var answersReader = db.RunQuery(command);
            if (answersReader == null || !answersReader.HasRows) return BadRequest("No answers found for the report.");

            // Process each answer
            while (answersReader.Read())
            {
                int answerId = int.Parse(answersReader["id"].ToString() ?? "0");
                int questionId = int.Parse(answersReader["question_id"].ToString() ?? "0");
                bool isUserCorrect = (bool)answersReader["is_correct"];

                // Get question text
                command = $"SELECT text FROM question WHERE id = {questionId}";
                var questionReader = db.RunQuery(command);
                questionReader.Read();
                string questionText = questionReader["text"].ToString()!;
                questionReader.Close();
                await questionReader.DisposeAsync();

                // Get possible answers, with the correct answer first
                command = $"SELECT text, isCorrect FROM answers WHERE quest_id = {questionId} ORDER BY isCorrect DESC";
                var possibleAnswersReader = db.RunQuery(command);

                var possibleAnswers = new List<string>();
                while (possibleAnswersReader.Read())
                {
                    possibleAnswers.Add(possibleAnswersReader["text"].ToString()!);
                }
                possibleAnswersReader.Close();
                await possibleAnswersReader.DisposeAsync();

                // Construct answer DTO with IsUserCorrect
                var answerDto = new RecordedMixAnswerDTO
                {
                    Id = answerId,
                    Question = questionText,
                    Answers = possibleAnswers,
                    WasUserCorrect = isUserCorrect
                };

                response.Answers.Add(answerDto);
            }

            answersReader.Close();
            await answersReader.DisposeAsync();

            return Ok(response);
        }







    }
}
