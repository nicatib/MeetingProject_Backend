namespace Meeting_Project.Dtos.MeetingDtos
{
    public class GetMeetingDtoByCountryId
    {
        public int page { get; set; } = 1;

        public int take { get; set; } = 5000;

        public string? DateTime { get; set; }

        public List<int>? countryIds { get; set; }

        public int? Status { get; set; }
    }
}