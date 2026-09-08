namespace Meeting_Project.Entity
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public string CreatedTime { get; set; }
        public bool isDeleted { get; set; }
        public string? DeletedTime { get; set; }
        public string? UpdatedTime { get; set; }
    }
}
