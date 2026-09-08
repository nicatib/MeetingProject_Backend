namespace Meeting_Project.Dtos.MeetingDtos
{
    public class ReturnMeetingDto
    {
        public int  Id { get; set; }    
        public List<ReturnUserDto>?users { get; set; }
        public string RoomNumber { get; set; }
        public string Status { get; set; }
        public string PlannedStartTime { get; set; }
        public string PlannedEndTime { get; set; }  
        public string HotelName { get; set; }
        public string? ActualStartTime { get; set; }
        public string Title { get; set; }
        public string ? Description { get;set; }
        public string? ActualEndTime { get; set;}
        public List<ReturnParticipiantDto> Participiants { get; set;}

    }
}
