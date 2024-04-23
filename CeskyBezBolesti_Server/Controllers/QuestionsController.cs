﻿using CeskyBezBolesti_Server.Database;
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
            //string? token = HttpContext.Request.Cookies["jwtToken"];
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
                    $" AND isCorrect == 1";
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
                    $" AND isCorrect == 0";
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
    }
}
