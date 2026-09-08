namespace Meeting_Project.Entity
{
    public class Contact : BaseEntity
    {
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }

        public string Message { get; set; }
        public string MessageStatus { get; set; }

    }
}
