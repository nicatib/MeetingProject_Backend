namespace Meeting_Project.Dtos.CountryDtos
{
    public class UpdateCountryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? MemberId { get; set; }

        public string FlagUrl { get; set; }
        public bool IsMain { get; set; }
        public int HotelId { get; set; }
        public int? MeetingRoomId { get; set; }

        public string? UserId { get; set; }
    }
}
