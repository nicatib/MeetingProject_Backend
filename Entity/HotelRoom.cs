namespace Meeting_Project.Entity
{
    public class HotelRoom:BaseEntity
    {
        public string RoomNumber { get; set; }
        public int HotelId { get; set; }
        public Hotel Hotel { get; set; }
        public bool isFree { get; set; }
        public List<Meeting> ?Meetings { get; set; }
        public HotelRoom()
        {
            Meetings = new List<Meeting>();
        }
    }
}
