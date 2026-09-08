using Meeting_Project.Entity;

public class Country : BaseEntity
{
    public string Name { get; set; }
    public string FlagUrl { get; set; }

    public List<StateGov>? StateGovs { get; set; }

    // ADMIN
    public string? UserId { get; set; }
    public AppUser? User { get; set; }

    // MEMBER
    public string? MemberId { get; set; }
    public AppUser? Member { get; set; }

    public int HotelId { get; set; }
    public Hotel Hotel { get; set; }

    public bool IsMain { get; set; }
    public bool IsArrivedToBaku { get; set; }
    public bool IsArrivedToHotel { get; set; }
    public int? MeetingRoomId { get; set; }

    public HotelRoom? MeetingRoom { get; set; }

    public Country()
    {
        StateGovs = new List<StateGov>();
    }
}