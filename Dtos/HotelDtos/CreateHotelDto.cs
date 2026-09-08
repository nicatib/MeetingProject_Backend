namespace Meeting_Project.Dtos.HotelDtos
{
    public class CreateHotelDto
    {
        public string Name { get;set; }
        public string? Email { get;set; }
        public string? Phone{ get; set; }
        public string? Description { get; set; }
        public bool IsMain { get; set; }
        public string? Location { get; set; }
    }
}
