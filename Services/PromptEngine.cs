//namespace Meeting_Project.Services
//{
//    public class PromptEngine
//    {
//        public string Build(string userPrompt, object context)
//        {
//            return $"""
//You are a senior business intelligence analyst.

//RULES:
//- Use ONLY provided data
//- Do NOT hallucinate
//- Be structured
//- Be concise

//USER QUESTION:
//{userPrompt}

//DATA CONTEXT:
//{System.Text.Json.JsonSerializer.Serialize(context)}

//OUTPUT FORMAT:
//- Summary
//- Insights (bullet points)
//- Warnings (if any)
//""";
//        }
//    }
//}
