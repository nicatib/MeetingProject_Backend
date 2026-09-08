namespace Meeting_Project.Dtos.AccountDtos
{
    public class GetRoleDto
    {
        public string RoleName { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; }
        public string Id { get; set; }
        public string CreatedTime { get; set; }
        public int? userCount { get; set; }
        public bool isDeleted { get; set; }
        public string? DeletedTime { get; set; }
    }
}
