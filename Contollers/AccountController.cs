using Meeting_Project.Data;
using Meeting_Project.Dtos.AccountDtos;
using Meeting_Project.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Mail;
using System.Net;
using System.Net.Http;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Meeting_Project.Helper;
using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace Meeting_Project.Contollers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _web;
        private readonly IConfiguration _config;
        public AccountController(AppDbContext context,  IWebHostEnvironment web, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, SignInManager<AppUser> signInManager, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _web = web;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _config = config;
        }
        //[HttpGet("GetRoles")]
        //public async Task<IActionResult> getRoles()
        //{
        //    await _roleManager.CreateAsync(new AppRole
        //    {
        //        Name = "Admin"
        //    });
        //    await _roleManager.CreateAsync(new AppRole
        //    {
        //        Name = "SuperAdmin"
        //    });
        //    await _roleManager.CreateAsync(new AppRole
        //    {
        //        Name = "Member"
        //    });
        //    return Ok();
        //}

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm]RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "User already exists" });

            var user = new AppUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                CreatedTime = DateTime.Now.ToString("MM/dd/yyyy"),
                isDeleted = false,
                Status = Helper.UserStatus.Waiting
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(e => e.Description));
            }

            // Role əlavə et
            await _userManager.AddToRoleAsync(user, "Admin");

            //// 🔐 Email confirmation token
            //var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            //var encodedToken = Uri.EscapeDataString(token);

            //var link = Url.Action(
            //    "ConfirmEmail",
            //    "Account",
            //    new { userId = user.Id, token = encodedToken },
            //    Request.Scheme
            //);

            //// 📧 Email body oxu
            //string body;
            //using (StreamReader reader = new StreamReader("wwwroot/verification/VerificationEmail.html"))
            //{
            //    body = await reader.ReadToEndAsync();
            //}

            //body = body.Replace("{{link}}", link);
            //body = body.Replace("{{userName}}", user.FullName);

            //// 📧 Email göndər
            //try
            //{
            //    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
            //    {
            //        smtp.EnableSsl = true;
            //        smtp.Credentials = new NetworkCredential("ibrahimovnicat15987@gmail.com", "dlkz wmmx ibla hudz\r\n");

            //        var mail = new MailMessage
            //        {
            //            From = new MailAddress("ibrahimovnicat15987@gmail.com", "Meeting Project"),
            //            Subject = "Verify your email",
            //            Body = body,
            //            IsBodyHtml = true
            //        };

            //        mail.To.Add(user.Email);

            //        await smtp.SendMailAsync(mail); // ✅ async
            //    }
            //}
            //catch (Exception ex)
            //{
            //    return Ok(new
            //    {
            //        message = "User created but email sending failed",
            //        error = ex.Message
            //    });
            //}

            return Ok(new
            {
                message = "User registered successfully. Please verify your email."
            });
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return BadRequest("Invalid user");

            // 🔥 IMPORTANT: decode et
            var decodedToken = Uri.UnescapeDataString(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            user.Status = Helper.UserStatus.Active;
            await _userManager.UpdateAsync(user);

            return Ok("Email confirmed successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null)
                return BadRequest("User not found");

            var checkPassword = await _userManager.CheckPasswordAsync(user, loginDto.Password);

            if (!checkPassword)
                return BadRequest("Incorrect password");
            if (user.Status == UserStatus.Passive)
            {
                return BadRequest("User is deactive");
            }
            //if (!await _userManager.IsEmailConfirmedAsync(user))
            //    return BadRequest("Please confirm your email first");

            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"])
            );

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );

            var roles = await _userManager.GetRolesAsync(user);
            string roleId = "";
            var roleName = roles.FirstOrDefault();

            if (!string.IsNullOrEmpty(roleName))
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                roleId = role?.Id ?? "";
            }
            var userCountry = _context.Countrys.FirstOrDefault(c => c.UserId == user.Id);
            string countryIdStr = userCountry != null ? userCountry.Id.ToString() : "0";
            var claims = new List<Claim>
    {
                new Claim("FullName", user.FullName ?? ""),
                new Claim("Email", user.Email ?? ""),
              new Claim(ClaimTypes.NameIdentifier, user.Id ?? ""),
                new Claim("Desc", user.Description?? ""),
                new Claim("RoleId", roleId),
             new Claim("CountryId", countryIdStr),

                new Claim("Phone", user.PhoneNumber ?? ""),
                new Claim("Role", roles.FirstOrDefault() ?? ""),
                new Claim("Status", user.Status.ToString())
    };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(15),
                signingCredentials: credentials
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }

        [Authorize]
        [HttpGet("getcurrentUser")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.isDeleted)
                return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault();

            // User-in bağlı olduğu Country-ləri tapırıq
            var countries = await _context.Countrys
                .Where(c =>
                    !c.isDeleted &&
                    (
                        c.UserId == user.Id ||
                        c.MemberId == user.Id
                    )
                )
                .Select(c => new
                {
                    c.Id,
                    c.MemberId
                })
                .ToListAsync();

            var memberIds = countries
                .Where(c => !string.IsNullOrEmpty(c.MemberId))
                .Select(c => c.MemberId!)
                .Distinct()
                .ToList();

            var members = await _userManager.Users
                .Where(u =>
                    !u.isDeleted &&
                    memberIds.Contains(u.Id)
                )
                .Select(u => new
                {
                    u.Id,
                    u.FullName
                })
                .ToListAsync();

            var memberName = countries
                .Where(c => !string.IsNullOrEmpty(c.MemberId))
                .Select(c => members
                    .FirstOrDefault(m => m.Id == c.MemberId)?.FullName)
                .FirstOrDefault(name => !string.IsNullOrEmpty(name));

            var result = new GetUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? "",

                CreatedTime = user.CreatedTime,
                DeletedTime = user.DeletedTime,



                Description = user.Description,
                fcmToken = user.FcmToken,

                isDeleted = user.isDeleted,
                Status = user.Status,

                RoleName = roleName,

                CountryIds = countries
                    .Select(c => c.Id)
                    .ToList()
            };

            return Ok(result);
        }

        [HttpPost("save-fcm-token")]
        [Authorize]
        public async Task<IActionResult> SaveFcmToken([FromBody] string fcmToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound("User not found");

            user.FcmToken = fcmToken;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Token updated successfully" });
        }

        [Authorize]
        [HttpGet("get-user/{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Id is required");

            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.isDeleted)
                return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault();

            var countryIds = await _context.Countrys
                .Where(c =>
                    !c.isDeleted &&
                    (
                        c.UserId == user.Id ||
                        c.MemberId == user.Id
                    )
                )
                .Select(c => c.Id)
                .ToListAsync();

            var result = new GetUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? "",

                CreatedTime = user.CreatedTime,
                DeletedTime = user.DeletedTime,

                Status = user.Status,
                Description = user.Description,

                isDeleted = user.isDeleted,
                fcmToken = user.FcmToken,

                RoleName = roleName,

                CountryIds = countryIds
            };

            return Ok(result);
        }



        [Authorize]
        [HttpGet("get-all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Where(u => !u.isDeleted)
                .ToListAsync();

            var result = new List<GetUserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault();

                var countryIds = await _context.Countrys
                    .Where(c =>
                        !c.isDeleted &&
                        (
                            c.UserId == user.Id ||
                            c.MemberId == user.Id
                        )
                    )
                    .Select(c => c.Id)
                    .ToListAsync();

                result.Add(new GetUserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? "",

                    CreatedTime = user.CreatedTime,
                    DeletedTime = user.DeletedTime,

                    Status = user.Status,
                    Description = user.Description,

                    isDeleted = user.isDeleted,
                    fcmToken = user.FcmToken,

                    RoleName = roleName,

                    CountryIds = countryIds
                });
            }

            return Ok(result);
        }




        [HttpPost("UpdateUserinSettings")]
        [Authorize(Roles ="SuperAdmin")]
        public async Task<IActionResult> UpdateUser([FromForm] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
                return NotFound("User not found");

            // 🔍 Email dəyişilibsə yoxla
            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existUserWithEmail = await _userManager.FindByEmailAsync(dto.Email);
                if (existUserWithEmail != null && existUserWithEmail.Id != user.Id)
                    return BadRequest("This email is already taken");

                user.Email = dto.Email;
                user.UserName = dto.Email;

                // 🔐 Email dəyişibsə confirm false olsun
                user.EmailConfirmed = false;
            }
          
            if (!string.IsNullOrWhiteSpace(dto.OldPass) && !string.IsNullOrWhiteSpace(dto.NewPass))
            {
                var passwordCheck = await _userManager.CheckPasswordAsync(user, dto.OldPass);
                if (!passwordCheck)
                    return BadRequest("Incorrect old password");

                var changePasswordResult = await _userManager.ChangePasswordAsync(user, dto.OldPass, dto.NewPass);
                if (!changePasswordResult.Succeeded)
                    return BadRequest(changePasswordResult.Errors.Select(e => e.Description));
            }

            // 🧾 digər fieldlər
            user.FullName = dto.FullName;
            user.PhoneNumber = dto.Phone;
            user.Description = dto.Desc;

            // 🎭 ROLE UPDATE
            if (!string.IsNullOrEmpty(dto.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);

                if (!currentRoles.Contains(dto.Role))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, dto.Role);
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return BadRequest(updateResult.Errors.Select(e => e.Description));

            // 📧 EMAIL CONFIRMATION (əgər email dəyişibsə)
            //if (!user.EmailConfirmed)
            //{
            //    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            //    var encodedToken = Uri.EscapeDataString(token);

            //    var link = Url.Action(
            //        "ConfirmEmail",
            //        "Account",
            //        new { userId = user.Id, token = encodedToken },
            //        Request.Scheme
            //    );

            //    string body;
            //    using (StreamReader reader = new StreamReader("wwwroot/verification/VerificationEmail.html"))
            //    {
            //        body = await reader.ReadToEndAsync();
            //    }

            //    body = body.Replace("{{link}}", link);
            //    body = body.Replace("{{userName}}", user.FullName);

            //    try
            //    {
            //        using (var smtp = new SmtpClient("smtp.gmail.com", 587))
            //        {
            //            smtp.EnableSsl = true;
            //            smtp.Credentials = new NetworkCredential("ibrahimovnicat15987@gmail.com", "dlkz wmmx ibla hudz\r\n");

            //            var mail = new MailMessage
            //            {
            //                From = new MailAddress("ibrahimovnicat15987@gmail.com", "Meeting Project"),
            //                Subject = "Confirm your updated email",
            //                Body = body,
            //                IsBodyHtml = true
            //            };

            //            mail.To.Add(user.Email);

            //            await smtp.SendMailAsync(mail);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        return Ok(new
            //        {
            //            message = "User updated but email sending failed",
            //            error = ex.Message
            //        });
            //    }
            //}

            return Ok(new
            {
                message = "User updated successfully",
                requireEmailConfirmation = !user.EmailConfirmed
            });
        }

        [HttpPost("EditUserInformation")]
        [Authorize(Roles ="SuperAdmin")]
        public async Task<IActionResult> EditUserInformation([FromForm]EditUserInformation dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
                return NotFound("User not found");

            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existUserWithEmail = await _userManager.FindByEmailAsync(dto.Email);
                if (existUserWithEmail != null && existUserWithEmail.Id != user.Id)
                    return BadRequest("This email is already taken");

                user.Email = dto.Email;
                user.UserName = dto.Email;

                user.EmailConfirmed = true;
            }
            user.FullName = dto.FullName;
            user.PhoneNumber = dto.Phone;
            user.Description = dto.Desc;
            user.Status = dto.status == 0
            ? UserStatus.Active
            : dto.status == 2
                ? UserStatus.Waiting
                : dto.status == 3
                    ? UserStatus.Passive
                    : UserStatus.Passive; 
            if (!string.IsNullOrEmpty(dto.RoleName))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);

                if (!currentRoles.Contains(dto.RoleName))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, dto.RoleName);
                }
            }

            // 💾 SAVE
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return BadRequest(updateResult.Errors.Select(e => e.Description));
            return Ok(new
            {
                message = "User updated successfully",
            });
        }

        [HttpPut("EditUserInMobile")]
        [Authorize]
        public async Task<IActionResult> EditUserInMobile(EditUserMobileDto dto)
        {
            if (dto == null)
                return BadRequest("Məlumat göndərilməyib");


            // ============================
            // CURRENT USER ID
            // ============================

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();


            // ============================
            // FIND USER
            // ============================

            var existUser = await _userManager.FindByIdAsync(userId);

            if (existUser == null)
                return NotFound("User not found");


            // ============================
            // UPDATE USER
            // ============================

            existUser.PhoneNumber = dto.Phone;

            existUser.Email = dto.Email;

            existUser.FullName = dto.FullName;


            var updateResult = await _userManager.UpdateAsync(existUser);


            if (!updateResult.Succeeded)
            {
                return BadRequest(updateResult.Errors);
            }


            // ============================
            // ROLE
            // ============================

            var roleName = await _context.UserRoles
                .Where(ur => ur.UserId == existUser.Id)
                .Join(
                    _context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name
                )
                .FirstOrDefaultAsync();


            // ============================
            // COUNTRY IDS
            // ============================

            List<int> countryIds = new List<int>();


            // ADMIN
            if (roleName == "Admin")
            {
                countryIds = await _context.Countrys
                    .Where(c => c.UserId == existUser.Id)
                    .Select(c => c.Id)
                    .ToListAsync();
            }

            // MEMBER
            else if (roleName == "Member")
            {
                countryIds = await _context.Countrys
                    .Where(c => c.MemberId == existUser.Id)
                    .Select(c => c.Id)
                    .ToListAsync();
            }

            // SUPERADMIN
            // heç nə etmirik -> []


            int countryId = countryIds.FirstOrDefault();


            // ============================
            // RESPONSE
            // ============================

            return Ok(new GetUserDto
            {
                Id = existUser.Id,

                FullName = existUser.FullName,

                Email = existUser.Email,

                PhoneNumber = existUser.PhoneNumber,

                CreatedTime = existUser.CreatedTime,

                DeletedTime = existUser.DeletedTime,

                Description = existUser.Description,

                fcmToken = existUser.FcmToken,


                CountryIds = countryIds,

                isDeleted = existUser.isDeleted,

                RoleName = roleName,

                Status = existUser.Status
            });
        }

        [Authorize]
        [HttpPut("changePass")]
        public async Task<IActionResult> UpdatePass(ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return BadRequest("Id not found");
            var existUser = await _userManager.FindByIdAsync(userId);
            if (existUser == null) return BadRequest("User not found");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _userManager.ChangePasswordAsync(existUser, dto.OldPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }
                return BadRequest(ModelState);
            }

            return Ok(new { message = "Password updated successfully." });
        }

        [HttpPost("add-role")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AddRole([FromForm] AddRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RoleName))
                return BadRequest("Role name is required");

            var existRole = await _roleManager.FindByNameAsync(dto.RoleName);

            if (existRole != null)
                return BadRequest("Role already exists");

            var role = new AppRole
            {
                Name = dto.RoleName,
                CreatedTime= DateTime.Now.ToString("MM/dd/yyyy"),
                Status = dto.Status,
                isDeleted=false,
                Description=dto.Description,
                DeletedTime=null,
                UpdatedTime=null
                
            };

            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new
            {
                message = "Role created successfully"
            });
        }

        [HttpPut("update-role")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateRole([FromForm] UpdateRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var role = await _roleManager.FindByIdAsync(dto.Id);

            if (role == null)
                return NotFound("Role not found");

            var roleWithSameName = await _roleManager.FindByNameAsync(dto.RoleName);

            if (roleWithSameName != null && roleWithSameName.Id != dto.Id)
                return BadRequest("Role name already exists");

            role.Name = dto.RoleName;
            role.NormalizedName = dto.RoleName.ToUpper();
            role.Description = dto.Description;
            if (role is AppRole appRole)
            {
                appRole.Status = dto.Status;

                appRole.UpdatedTime = DateTime.Now.ToString("MM/dd/yyyy");
            }

            var result = await _roleManager.UpdateAsync(role);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new
            {
                message = "Role updated successfully"
            });
        }
        [HttpGet("get-role")]
        public async Task<IActionResult> GetRoleWithId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Id not found");

            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
                return NotFound("Role not found");

            if (role is AppRole appRole)
            {
                // 🔥 USER COUNT
                var users = _userManager.Users.ToList();

                int userCount = 0;

                foreach (var u in users)
                {
                    if (await _userManager.IsInRoleAsync(u, appRole.Name))
                    {
                        userCount++;
                    }
                }

                var result = new GetRoleDto
                {
                    Id = appRole.Id,
                    RoleName = appRole.Name,
                    Description = appRole.Description,
                    Status = appRole.Status,
                    CreatedTime = appRole.CreatedTime,
                    isDeleted = appRole.isDeleted,
                    DeletedTime = appRole.DeletedTime,
                    userCount = userCount
                };

                return Ok(result);
            }

            return BadRequest("Invalid role type");
        }
        [Authorize()]
        [HttpGet("getallroles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = _roleManager.Roles.ToList();

            var result = roles.Select(role =>
            {
                var appRole = role as AppRole;

                // 🔥 USER COUNT (FAST)
                var userCount = _context.UserRoles
                    .Count(x => x.RoleId == role.Id);

                return new GetRoleDto
                {
                    Id = role.Id,
                    RoleName = role.Name,
                    Description = appRole?.Description,
                    Status = appRole?.Status,
                    CreatedTime = appRole?.CreatedTime,
                    isDeleted = appRole?.isDeleted ?? false,
                    DeletedTime = appRole?.DeletedTime,
                    userCount = userCount
                };
            }).ToList();

            return Ok(result);
        }
        [Authorize(Roles ="SuperAdmin")]
        [HttpDelete("DeleteRole")]
        public async Task<IActionResult> deleteRole(string roleId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var existRole=await _roleManager.FindByIdAsync(roleId);
            if(existRole== null) return BadRequest("ROle is not exist");
            existRole.isDeleted = true;
            existRole.DeletedTime = DateTime.Now.ToString("MM/dd/yyyy");
            existRole.Status = UserStatus.Passive.ToString();
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
