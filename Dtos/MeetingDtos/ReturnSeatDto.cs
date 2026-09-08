namespace Meeting_Project.Dtos.MeetingDtos
{
    public class ReturnSeatDto
    {
        public int SeatIndex { get; set; }
        public int? StateGovId { get; set; }
        public string CountryName { get; set; }
        public string? StateName { get; set; }
    }
}
