namespace Meeting_Project.Dtos.AccountDtos
{
    public class UpdateUserDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string? Desc { get; set; }
        public string? Role { get; set; }
        public string OldPass { get; set; }
        
        public string NewPass { get; set; }

    }
}
