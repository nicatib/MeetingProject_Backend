using Meeting_Project.Helper;

namespace Meeting_Project.Dtos.AccountDtos
{
    public class GetUserDto
    {
        public string CreatedTime { get; set; }
        public string FullName { get; set; }
        public bool isDeleted { get; set; }
        public string? DeletedTime { get; set; }
        public UserStatus Status { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string RoleName { get; set; }
        public string Id { get; set; }

        public List<int> CountryIds { get; set; } = new();


        public string? Description { get; set; }
        public string ? fcmToken { get; set; }
    }
}
