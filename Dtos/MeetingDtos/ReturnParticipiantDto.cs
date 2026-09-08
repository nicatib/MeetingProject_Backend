namespace Meeting_Project.Dtos.MeetingDtos
{
    public class ReturnParticipiantDto
    {
        public int Id { get; set; } 
        public string CountryName { get; set; }
        public string GovermentName { get; set; }
        public bool? isAccespted { get; set; }
        public int? CountryId { get; set; } 
    }
}
