namespace Meeting_Project.Dtos.CountryDtos
{
    public class ReturnCountryDto
    {
        public string Name { get; set; }
        public string? FullName{ get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsMain { get; set; }

        public int Id { get; set; }
        public string FlagUrl { get; set; }
        public string HotelName { get; set; }
        public int HotelId{ get; set; }
        public string? MemberId { get; set; }
        public string? MemberFullName { get; set; }
        public string? MemberPhoneNumber { get; set; }
        public bool IsArrivedToBaku { get; set; }
        public bool IsArrivedToHotel { get; set; }
        public string? MeetingRoom { get;set; }
        public int? MeetingRoomId { get; set; }
        public string? UserId { get; set; }
        public string CreatedTime { get; set; } 
        public List<ReturnGovermentDto>? dtos { get; set; }
        public ReturnCountryDto()
        {
            dtos = new List<ReturnGovermentDto>();
        }
    }
}
