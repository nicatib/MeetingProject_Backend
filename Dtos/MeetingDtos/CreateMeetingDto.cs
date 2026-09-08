namespace Meeting_Project.Dtos.MeetingDtos
{
    public class CreateMeetingDto
    {
        public int HotelId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public List<ParticipantDto> Participants { get; set; }
    }
}
