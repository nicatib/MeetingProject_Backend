namespace Meeting_Project.Dtos.MeetingDtos
{
    public class ReturnCircleDto
    {
        public int Id { get; set; } 
        public string Title { get; set; }
        public string? ImageUrl { get; set; }
        public List<ReturnSeatDto>? Seats { get; set; }
        public ReturnCircleDto()
        {
            Seats = new List<ReturnSeatDto>();
        }
    }
}
