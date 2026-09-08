using Meeting_Project.Helper;
using Microsoft.AspNetCore.Identity;

namespace Meeting_Project.Entity
{
    public class AppRole: IdentityRole
    {
        public string Status{ get; set; }
        public string? Description { get; set; }
        public string CreatedTime { get; set; }
        public bool isDeleted { get; set; }
        public string? DeletedTime { get; set; }
        public string? UpdatedTime { get; set; }
    }
}
