//using Meeting_Project.Dtos.AIDtos;
//using Meeting_Project.Services;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace Meeting_Project.Contollers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class AiController : ControllerBase
//    {
//        private readonly AiService _ai;

//        public AiController(AiService ai)
//        {
//            _ai = ai;
//        }

//        [HttpPost("ask")]
//        public async Task<IActionResult> Ask([FromBody] AiQueryRequest req)
//        {
//            var result = await _ai.AskAsync(req.Prompt);
//            return Ok(result);
//        }
//    }
//}
