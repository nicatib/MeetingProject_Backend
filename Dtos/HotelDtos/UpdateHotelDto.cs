namespace Meeting_Project.Dtos.HotelDtos
{
    public class UpdateHotelDto
    {
        public string Name { get; set; }
        public string? Email { get; set; }
        public string? Phone{ get; set; }
        public int Id { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsMain { get; set; }


    }
}
