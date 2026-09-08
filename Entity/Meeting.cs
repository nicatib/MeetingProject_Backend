using Meeting_Project.Helper;

namespace Meeting_Project.Entity
{
    public class Meeting
    {
        public int Id { get; set; }

        public DateTime PlannedStartTime { get; set; }
        public DateTime PlannedEndTime { get; set; }

        public int DurationMinutes { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int RoomId { get; set; }
        public HotelRoom Room { get; set; }

        public MeetingStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ActualEndTime { get; set; }
        public DateTime? ActualStartTime { get; set; }

        public List<MeetingParticipant> Participants { get; set; }
    }
}
