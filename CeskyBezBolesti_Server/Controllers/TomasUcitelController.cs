using CeskyBezBolesti_Server.DTO;
using CeskyBezBolesti_Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

namespace CeskyBezBolesti_Server.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class TomasUcitelController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public TomasUcitelController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        [HttpPost("askTomas")]
        public async Task<ActionResult<string>> AskTomasQuestion(TomasOtazkaRequestDTO prompt)
        {
            prompt.questionPrompt = prompt.questionPrompt.Replace("\"", "'");
            // get api key
            var apiKey = _configuration.GetSection("AppSettings:geminiApiKey").Value;
            // request url
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
            // payload
            string jsonBody = $@"
        {{
            ""system_instruction"": {{
                ""parts"": {{
                    ""text"": ""Budeš zastávat roli učitele Tomáše. Představ si, že jsi učitel českého jazyka na základní škole v pátem ročníku v české republice. Své studenty si tento měsíc učil psaní I/Y, například po vyjmenovaných slovech, psáních velkých a malých písmen, číslovek, psaní ě/je podle vzoru petr/petrovi a psaní ú/ů. Děti se tě budou ptát, ať jim vysvětlíš látku nebo odůvodníš správnou odpověď. Proto všechny tvé odpovědi musí být naprosto přesné a spolehlivé, spravností svých odpovědí si musíš být na 100000% jistý.""
                }}
            }},
            ""contents"": {{
                ""parts"": {{
                    ""text"": ""{prompt.questionPrompt}""
                }}
            }}
        }}";

            using HttpClient client = new HttpClient();
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            string responseString = await response.Content.ReadAsStringAsync();

            // Parsování JSON odpovědi
            using JsonDocument doc = JsonDocument.Parse(responseString);
            string responseText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();


            return JsonConvert.SerializeObject(responseText ?? "Chyba");
        }

        [HttpPost("askRizzler")]
        public async Task<ActionResult<string>> AskRizzlerQuestion(TomasOtazkaRequestDTO prompt)
        {
            prompt.questionPrompt = prompt.questionPrompt.Replace("\"", "'");
            // get api key
            var apiKey = _configuration.GetSection("AppSettings:geminiApiKey").Value;
            // request url
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
            // payload
            string jsonBody = $@"
        {{
            ""system_instruction"": {{
                ""parts"": {{
                    ""text"": ""Budeš zastávat roli učitele který se jmenuje Rizzler. Představ si, že jsi učitel českého jazyka na základní škole v pátem ročníku v české republice. Své studenty si tento měsíc učil psaní I/Y, například po vyjmenovaných slovech, psáních velkých a malých písmen, číslovek, psaní ě/je podle vzoru petr/petrovi a psaní ú/ů. Děti se tě budou ptát, ať jim vysvětlíš látku nebo odůvodníš správnou odpověď. Proto všechny tvé odpovědi musí být naprosto přesné a spolehlivé, spravností svých odpovědí si musíš být na 100000% jistý.
Taky nezapomeň, že se snažíš zaujmout generaci Alpha. Proto používej online slovník generace alpha a mluvenou řeč a nářečí generace Alpha. Používej maximální počet slov jako rizzler, skibidi, gyat, alpha, sigma, delulu atd.. Zkrátka maximální počet slov z brainrot slangu. Také se snaž být vtipný s těmito slovy, klidně tak moc, až to bude trapné. Ty si rizzler a toho, kdo se ptá, oslovuj jako sigma. Oslovuj ho často, aby věděl, že je sigma, a velmi ho povzbuzuj a chval ho, že správné sigmy pokládají hodně otázek ohledně spisovné češtiny. Tvé odpovědi však musí krátké a stručné. V každé odpovědi musíš použít minimálně jednou slovo 'skibidi'. Neboj se cokoliv označit jako 'skibidi' věc, i přesto, že to nedává absolutně žádný smysl.""
                }}
            }},
            ""contents"": {{
                ""parts"": {{
                    ""text"": ""{prompt.questionPrompt}""
                }}
            }}
        }}";

            using HttpClient client = new HttpClient();
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            string responseString = await response.Content.ReadAsStringAsync();

            // Parsování JSON odpovědi
            using JsonDocument doc = JsonDocument.Parse(responseString);
            string responseText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();


            return JsonConvert.SerializeObject(responseText ?? "Chyba");
        }
    }
}
