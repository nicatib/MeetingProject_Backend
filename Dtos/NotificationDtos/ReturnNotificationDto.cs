namespace Meeting_Project.Dtos.NotificationDtos
{
    public class ReturnNotificationDto
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public string UserId { get; set; }

        public bool IsRead { get; set; }

        public string GovermentName { get; set; }

        public int? GovernmentId { get; set; }

        public string CreatedAt { get; set; }

        public string? Reason { get; set; }

        public string plannedTime { get; set; }

        public int? MeetingId { get; set; }

        public string Type { get; set; }

        public bool IsShown { get; set; }

        public bool? IsAccepted { get; set; }
    }
}