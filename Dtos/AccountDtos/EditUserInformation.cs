namespace Meeting_Project.Dtos.AccountDtos
{
    public class EditUserInformation
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string? Desc { get; set; }
        public string? RoleName { get; set; }
        public int? status { get; set; }
    }
}
