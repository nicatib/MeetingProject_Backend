namespace Meeting_Project.Dtos.HotelDtos
{
    public class GetOtelDto
    {
        public List<GetRoomDto>? RoomDtos { get; set; }
        public string Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Description { get; set; }
        public int Id { get; set; }
        public string CreatedTime { get; set; }
        public string? Location { get; set; }
        public bool IsMain { get; set; }

        public bool isDeleted { get; set; }
        public string? DeletedTime { get; set; }
        public GetOtelDto()
        {
            RoomDtos = new List<GetRoomDto>();
        }
    }
}
