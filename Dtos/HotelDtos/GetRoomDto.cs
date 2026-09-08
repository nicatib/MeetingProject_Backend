namespace Meeting_Project.Dtos.HotelDtos
{
    public class GetRoomDto
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public bool isFree { get; set; }
        public string? Description { get; set; }
        public string CreatedTime { get; set; }
        public bool isDeleted { get; set; }
        public string? DeletedTime { get; set; }
        public string HotelName { get; set; }
    }
}
