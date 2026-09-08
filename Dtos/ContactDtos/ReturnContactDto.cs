namespace Meeting_Project.Dtos.ContactDtos
{
    public class ReturnContactDto
    {
        public string ToUseName { get; set; }
        public string Message { get; set; }
        public string FromUserName { get; set; }
        public int Id { get; set; }
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public string CreatedTime { get; set; }
        public string MessageStatus { get; set; }

    }
}
