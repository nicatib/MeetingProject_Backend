namespace Meeting_Project.Dtos.AIDtos
{
    public class AiResponseDto
    {
        public string Summary { get; set; }
        public List<string> Insights { get; set; }
        public List<string> Warnings { get; set; }  
    }
}
