namespace Meeting_Project.Entity
{
    public class Hotel:BaseEntity
    {
        public string Name { get; set; }
        public List<HotelRoom>? Rooms { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public List<Country>? Countries { get; set; }
        public bool IsMain { get; set; }    
        public Hotel()
        {
            Rooms = new List<HotelRoom>();
            Countries=new List<Country>();
        }
    }
}
