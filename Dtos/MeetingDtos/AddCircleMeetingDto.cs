namespace Meeting_Project.Dtos.MeetingDtos
{
    public class AddCircleMeetingDto
    {
        public string Title { get; set; }
        public List<AddSeatDto> dtos{ get; set; }
        public IFormFile File
        {
            get; set;
        }
        public AddCircleMeetingDto()
        {
            dtos = new List<AddSeatDto>();
        }
    }
}
