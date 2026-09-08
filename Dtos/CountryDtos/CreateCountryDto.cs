public class CreateCountryDto
{
    public string Name { get; set; }
    public string? FlagUrl { get; set; }

    public string? UserId { get; set; }

    public string? MemberId { get; set; }

    public int HotelId { get; set; }

    public bool IsMain { get; set; }
    public int? MeetingRoomId { get; set; }  
}