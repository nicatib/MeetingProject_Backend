using Meeting_Project.Helper;
using Microsoft.AspNetCore.Identity;

namespace Meeting_Project.Entity
{
    public class AppUser:IdentityUser
    {
        public string CreatedTime { get; set; }
        public string? UpdatedTime { get; set; }
        public string FullName { get; set; }
        public bool isDeleted { get; set; }
        public string? DeletedTime { get; set; }
        public UserStatus Status { get; set; }
        public string? Description { get; set; }
        public int? CountryId { get; set; }
        public string? FcmToken { get; set; }

    }
}
