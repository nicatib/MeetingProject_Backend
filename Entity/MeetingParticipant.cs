namespace Meeting_Project.Entity
{
    public class MeetingParticipant
    {
        public int Id { get; set; }

        public int MeetingId { get; set; }
        public Meeting Meeting { get; set; }
        public int GovernmentId { get; set; }
        public StateGov Government { get; set; }
        public bool? isAccepted { get; set; } = null;
    }
}
