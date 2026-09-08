using System.ComponentModel.DataAnnotations;

namespace Meeting_Project.Dtos.AccountDtos
{
    public class ChangePasswordDto
    {
        [Required]
        public string OldPassword { get; set; } // oldPassword yox, OldPass

        [Required]
        public string NewPassword { get; set; } // newPassword yox, NewPass
    }
}
