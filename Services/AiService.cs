//using Meeting_Project.Data;

//namespace Meeting_Project.Services
//{
//    public class AiService
//    {
//        private readonly HttpClient _http;
//        private readonly IConfiguration _config;
//        private readonly MeetingContextService _contextService;
//        private readonly PromptEngine _promptEngine;

//        public AiService(
//            HttpClient http,
//            IConfiguration config,
//            MeetingContextService contextService,
//            PromptEngine promptEngine)
//        {
//            _http = http;
//            _config = config;
//            _contextService = contextService;
//            _promptEngine = promptEngine;
//        }

//        public async Task<string> AskAsync(string prompt)
//        {
//            var context = await _contextService.BuildContextAsync();

//            var finalPrompt = _promptEngine.Build(prompt, context);

//            var request = new
//            {
//                model = "gpt-4o-mini",
//                messages = new[]
//                {
//                new { role = "system", content = "You are a BI assistant." },
//                new { role = "user", content = finalPrompt }
//            }
//            };

//            _http.DefaultRequestHeaders.Authorization =
//                new System.Net.Http.Headers.AuthenticationHeaderValue(
//                    "Bearer",
//                    _config["OpenAI:Key"]
//                );

//            var res = await _http.PostAsJsonAsync(
//                "https://api.openai.com/v1/chat/completions",
//                request
//            );

//            return await res.Content.ReadAsStringAsync();
//        }
//    }
//}
