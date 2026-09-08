using meeting_app.Hubs;
using Meeting_Project.Data;
using Meeting_Project.Dtos.CountryDtos;
using Meeting_Project.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;
using System.Security.Claims;

namespace Meeting_Project.Contollers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<UserHub> _hubContext;
        public CountryController(AppDbContext context, UserManager<AppUser> userManager, IHubContext<UserHub> hubContext)
        {
            _context = context;
            this._userManager = userManager;
            _hubContext = hubContext;
        }


        [HttpGet("getAllCountry")]
        [Authorize]
        public IActionResult GetAllCountry()
        {
            var countries = _context.Countrys
                .Include(p => p.User)      // Admin
                .Include(p => p.Member)    // Member
                .Include(p => p.Hotel)
                .Include(p => p.MeetingRoom).ThenInclude(p=>p.Hotel)
                .Include(p => p.StateGovs)
                    .ThenInclude(x => x.Flights)
                .Where(p => !p.isDeleted)
                .OrderBy(p => p.Name)
                .ToList();

            var returnCountry = countries.Select(p => new ReturnCountryDto
            {
                // ================= COUNTRY =================

                Name = p.Name,

                Id = p.Id,
                MeetingRoomId = p.MeetingRoomId,
                // ================= ADMIN =================

                UserId = p.UserId,
                MeetingRoom = p.MeetingRoom != null
    ? $"{p.MeetingRoom.Hotel.Name} - {p.MeetingRoom.RoomNumber} nömrəli otaq"
    : "",
                FullName = p.User != null
                    ? p.User.FullName
                    : "",

                PhoneNumber = p.User != null
                    ? p.User.PhoneNumber
                    : "",

                
                // ================= MEMBER =================

                MemberId = p.MemberId,

                MemberFullName = p.Member != null
                    ? p.Member.FullName
                    : "",

                MemberPhoneNumber= p.Member != null
                    ? p.Member.PhoneNumber
                    : "",
                // ================= OTHER =================

                CreatedTime = p.CreatedTime,

                HotelId = p.HotelId,

                FlagUrl = p.FlagUrl,

                IsMain = p.IsMain,

                IsArrivedToHotel = p.IsArrivedToHotel,

                IsArrivedToBaku = p.IsArrivedToBaku,

                HotelName = p.Hotel != null
                    ? p.Hotel.Name
                    : "",


                // ================= GOVERNMENTS =================

                dtos = p.StateGovs
                    .Where(x => !x.isDeleted)
                    .OrderBy(x => x.Name)
                    .Select(x => new ReturnGovermentDto
                    {
                        Name = x.Name,

                        Id = x.Id,

                        IsMain = x.IsMain,

                        CountryId = x.CountryId,

                        CountryName = x.Country.Name,

                        FlightNumber = x.Flights != null
                            ? x.Flights.FlightNumber
                            : null,

                        PlannedArrivedTime = x.Flights != null
                            ? x.Flights.PlannedArrivedTime
                            : null,

                        RealArrivedTime = x.Flights != null
                            ? x.Flights.RealArrivedTime
                            : null
                    })
                    .ToList()
            });

            return Ok(returnCountry);
        }

        [HttpPost("CreateCountry")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateCountry([FromForm] CreateCountryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto == null)
                return BadRequest("Məlumat göndərilməyib");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Ölkə adı boş ola bilməz");

            dto.Name = dto.Name.Trim();

            // =========================
            // USER ID-LƏRİ TƏMİZLƏ
            // =========================

            dto.UserId = dto.UserId?.Trim();
            dto.MemberId = dto.MemberId?.Trim();

            if (dto.UserId == "null" ||
                string.IsNullOrWhiteSpace(dto.UserId))
            {
                dto.UserId = null;
            }

            if (dto.MemberId == "null" ||
                string.IsNullOrWhiteSpace(dto.MemberId))
            {
                dto.MemberId = null;
            }

            // =========================
            // ÖLKƏNİN MÖVCUDLUĞUNU YOXLAYIRIQ
            // =========================

            var existCountry = await _context.Countrys
                .AnyAsync(x =>
                    !x.isDeleted &&
                    x.Name.ToLower() == dto.Name.ToLower());

            if (existCountry)
                return BadRequest("Bu adda ölkə mövcuddur");


            // =========================
            // ADMIN YOXLAMASI
            // =========================

            if (dto.UserId != null)
            {
                var admin = await _userManager.FindByIdAsync(dto.UserId);

                if (admin == null)
                    return BadRequest("Admin tapılmadı");

                // User həqiqətən Admin rolundadır?
                var isAdmin = await _userManager.IsInRoleAsync(
                    admin,
                    "Admin"
                );

                if (!isAdmin)
                    return BadRequest(
                        "Seçilmiş istifadəçi Admin rolunda deyil"
                    );

            
            }


            // =========================
            // MEMBER YOXLAMASI
            // =========================

            if (dto.MemberId != null)
            {
                var member = await _userManager.FindByIdAsync(
                    dto.MemberId
                );

                if (member == null)
                    return BadRequest("Member tapılmadı");

                // User həqiqətən Member rolundadır?
                var isMember = await _userManager.IsInRoleAsync(
                    member,
                    "Member"
                );

                if (!isMember)
                    return BadRequest(
                        "Seçilmiş istifadəçi Member rolunda deyil"
                    );

                // Member başqa ölkəyə təyin olunub?
                var memberAlreadyUsed = await _context.Countrys
                    .AnyAsync(x =>
                        !x.isDeleted &&
                        x.MemberId == dto.MemberId);

                if (memberAlreadyUsed)
                    return BadRequest(
                        "Bu Member artıq bir ölkəyə təyin olunub"
                    );
            }


            // =========================
            // ADMIN VƏ MEMBER EYNİ OLA BİLMƏZ
            // =========================

            if (dto.UserId != null &&
                dto.MemberId != null &&
                dto.UserId == dto.MemberId)
            {
                return BadRequest(
                    "Admin və Member eyni istifadəçi ola bilməz"
                );
            }


            // =========================
            // COUNTRY YARAT
            // =========================

            var country = new Country
            {
                Name = dto.Name,

                CreatedTime = DateTime.Now.ToString("dd/MM/yyyy"),

                isDeleted = false,
                MeetingRoomId=dto.MeetingRoomId==null ? null : dto.MeetingRoomId,

                FlagUrl = dto.FlagUrl,

                UserId = dto.UserId,

                MemberId = dto.MemberId,

                IsArrivedToBaku = false,
                IsArrivedToHotel = false,

                HotelId = dto.HotelId,

                IsMain = dto.IsMain
            };


            await _context.Countrys.AddAsync(country);

            await _context.SaveChangesAsync();


            return Ok(new
            {
                message = "Ölkə yaradıldı",
                countryId = country.Id
            });
        }


        [HttpGet("GetAvailableAdmins")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAvailableAdmins()
        {
            var users = await _userManager.GetUsersInRoleAsync("Admin");

            var result = users
                .Where(u => !u.isDeleted)
                .Select(u => new
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,

                    CountryId = _context.Countrys
                        .Where(c => !c.isDeleted && c.UserId == u.Id)
                        .Select(c => (int?)c.Id)
                        .FirstOrDefault()
                })
                .ToList();

            return Ok(result);
        }

        [HttpGet("GetAvailableMembers")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAvailableMembers()
        {
            var users = await _userManager.GetUsersInRoleAsync("Member");

            var result = users
                .Where(u => !u.isDeleted)
                .Select(u => new
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,

                    CountryId = _context.Countrys
                        .Where(c => !c.isDeleted && c.MemberId == u.Id)
                        .Select(c => (int?)c.Id)
                        .FirstOrDefault()
                })
                .ToList();

            return Ok(result);
        }


        [HttpPut("UpdateCountry")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateCountry(
    [FromForm] UpdateCountryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // =========================================================
            // IDENTITY / NULL TƏMİZLƏMƏ
            // =========================================================

            dto.UserId = dto.UserId?.Trim();
            dto.MemberId = dto.MemberId?.Trim();

            if (dto.UserId == "null" ||
                string.IsNullOrWhiteSpace(dto.UserId))
            {
                dto.UserId = null;
            }

            if (dto.MemberId == "null" ||
                string.IsNullOrWhiteSpace(dto.MemberId))
            {
                dto.MemberId = null;
            }


            // =========================================================
            // ÖLKƏNİ TAP
            // =========================================================

            var country = await _context.Countrys
                .FirstOrDefaultAsync(x =>
                    !x.isDeleted &&
                    x.Id == dto.Id);

            if (country == null)
                return NotFound("Ölkə tapılmadı");


            // Köhnə əlaqələri yadda saxlayırıq
            var oldAdminId = country.UserId;
            var oldMemberId = country.MemberId;


            // =========================================================
            // ÖLKƏ ADI YOXLAMASI
            // =========================================================

            if (!string.Equals(
                country.Name,
                dto.Name,
                StringComparison.OrdinalIgnoreCase))
            {
                var nameExists = await _context.Countrys
                    .AnyAsync(x =>
                        !x.isDeleted &&
                        x.Id != dto.Id &&
                        x.Name.ToLower() == dto.Name.Trim().ToLower());

                if (nameExists)
                {
                    return BadRequest("Bu adda ölkə mövcuddur");
                }
            }


            // =========================================================
            // ADMIN YOXLAMASI
            // =========================================================

            if (dto.UserId != null)
            {
                var admin = await _userManager.FindByIdAsync(dto.UserId);

                if (admin == null)
                {
                    return BadRequest("Admin tapılmadı");
                }

                var isAdmin = await _userManager.IsInRoleAsync(
                    admin,
                    "Admin");

                if (!isAdmin)
                {
                    return BadRequest(
                        "Seçilən istifadəçi Admin deyil");
                }

                // BURADA ƏVVƏLKİ KİMİ
                // "başqa ölkəyə təyin olunub" YOXLAMASI YOXDUR.
                //
                // Çünki Admin bir neçə ölkəni idarə edə bilər.
            }


            // =========================================================
            // MEMBER YOXLAMASI
            // =========================================================

            if (dto.MemberId != null)
            {
                var member = await _userManager.FindByIdAsync(
                    dto.MemberId);

                if (member == null)
                {
                    return BadRequest("Member tapılmadı");
                }

                var isMember = await _userManager.IsInRoleAsync(
                    member,
                    "Member");

                if (!isMember)
                {
                    return BadRequest(
                        "Seçilən istifadəçi Member deyil");
                }

                // Member başqa ölkəyə təyin olunub?
                //
                // Cari ölkəni nəzərə almırıq.
                // Buna görə edit zamanı öz Member-i seçmək mümkündür.

                var memberUsedInAnotherCountry =
                    await _context.Countrys
                        .AnyAsync(x =>
                            !x.isDeleted &&
                            x.Id != dto.Id &&
                            x.MemberId == dto.MemberId);

                if (memberUsedInAnotherCountry)
                {
                    return BadRequest(
                        "Bu Member artıq başqa ölkəyə təyin olunub");
                }
            }


            // =========================================================
            // ÖLKƏ MƏLUMATLARINI YENİLƏ
            // =========================================================

            country.Name = dto.Name.Trim();

            country.UserId = dto.UserId;
            country.MemberId = dto.MemberId;

            country.FlagUrl = dto.FlagUrl;

            country.IsMain = dto.IsMain;
            country.MeetingRoomId = dto.MeetingRoomId;
            country.HotelId = dto.HotelId;

            country.UpdatedTime =
                DateTime.Now.ToString("dd/MM/yyyy");


            // =========================================================
            // KÖHNƏ MEMBER ƏLAQƏSİNİ SİL
            // =========================================================

            if (oldMemberId != null &&
                oldMemberId != dto.MemberId)
            {
                var oldMember =
                    await _userManager.FindByIdAsync(oldMemberId);

                if (oldMember != null)
                {
                    // Member artıq heç bir ölkəyə bağlı deyil
                    oldMember.CountryId = null;

                    await _userManager.UpdateAsync(oldMember);
                }
            }


            // =========================================================
            // YENİ MEMBER ƏLAQƏSİNİ YARAT
            // =========================================================

            if (dto.MemberId != null)
            {
                var newMember =
                    await _userManager.FindByIdAsync(dto.MemberId);

                if (newMember != null)
                {
                    newMember.CountryId = dto.Id;

                    await _userManager.UpdateAsync(newMember);
                }
            }


            // =========================================================
            // DATABASE SAVE
            // =========================================================

            await _context.SaveChangesAsync();


            // =========================================================
            // SIGNALR PROFILE UPDATE
            // =========================================================

            // Köhnə Member
            if (oldMemberId != null &&
                oldMemberId != dto.MemberId)
            {
                await _hubContext.Clients
                    .User(oldMemberId)
                    .SendAsync("ReceiveProfileUpdate");
            }

            // Yeni Member
            if (dto.MemberId != null)
            {
                await _hubContext.Clients
                    .User(dto.MemberId)
                    .SendAsync("ReceiveProfileUpdate");
            }


            // Admin üçün artıq CountryId dəyişmirik.
            //
            // Çünki Admin bir neçə ölkəyə sahib ola bilər.
            // Admin-in ölkələri Country.UserId üzərindən
            // tapılacaq.


            return Ok("Ölkə uğurla yeniləndi");
        }





        [Authorize(Roles ="SuperAdmin")]
        [HttpDelete("deleteCountry")]
        public IActionResult DeleteCountry(int id)
        {
            if (id <= 0) return BadRequest("Id yoxdur");

            var existCountry = _context.Countrys
                .Include(x => x.StateGovs)
                .FirstOrDefault(p => !p.isDeleted && p.Id == id);

            if (existCountry == null) return BadRequest("Olke yoxdur");

            if (existCountry.StateGovs != null)
            {
                foreach (var gov in existCountry.StateGovs)
                {
                    _context.StateGovs.Remove(gov);
                };
            }

            _context.Countrys.Remove(existCountry);
            _context.SaveChanges();

            return Ok();
        }
        [Authorize(Roles ="SuperAdmin")]

        [HttpPost("addGoverment")]
        public async Task<IActionResult> AddGoverment([FromForm] AddGov dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existCountry = await _context.Countrys
                .FirstOrDefaultAsync(p => !p.isDeleted && p.Id == dto.CountryId);

            if (existCountry == null)
                return BadRequest("Belə bir ölkə yoxdur");

            // ✅ DÜZGÜN YOXLAMA
            var existGov = await _context.StateGovs.AnyAsync(g =>
                !g.isDeleted &&
                g.CountryId == dto.CountryId &&
                g.Name.ToLower() == dto.Name.ToLower()
            );

            if (existGov)
                return BadRequest("Bu adda qurum artıq bu ölkədə mövcuddur");

            StateGov newGov = new()
            {
                Name = dto.Name.Trim(),
                IsMain=existCountry.IsMain,
                CountryId = dto.CountryId,
                CreatedTime = DateTime.Now.ToString("dd/MM/yyyy")
            };

            await _context.StateGovs.AddAsync(newGov);
            await _context.SaveChangesAsync();

            // ✅ Flight əlavə
            if (!string.IsNullOrWhiteSpace(dto.FlightNumber) && dto.FlightNumber != "undefined")
            {
                var newFlight = new Flight
                {
                    FlightNumber = dto.FlightNumber,
                    StateGovId = newGov.Id,
                    IsArrived = false,
                    CreatedTime = DateTime.Now.ToString("dd/MM/yyyy")
                };

                await _context.Flights.AddAsync(newFlight);
                await _context.SaveChangesAsync();
            }

            return Ok("Goverment created successfully");
        }
        [HttpDelete("deleteGov")]
        [Authorize(Roles = "SuperAdmin")]

        public IActionResult DeleteGov(int id)
        {
            if (id ==null)
                return BadRequest("Id yanlışdır");

            var existStateGov = _context.StateGovs
                .Include(p => p.Flights)
                .FirstOrDefault(p => p.Id == id);

            if (existStateGov == null)
                return BadRequest("Qurum tapılmadı");

            // 🔥 flight-ları sil
            if (existStateGov.Flights != null)
            {
                _context.Flights.RemoveRange(existStateGov.Flights);
            }

            _context.StateGovs.Remove(existStateGov);
            _context.SaveChanges();

            return Ok("Silindi");
        }
        [Authorize(Roles = "SuperAdmin")]
        [HttpPut("updateGov")]
        public async Task<IActionResult> UpdateGov([FromForm] UpdateGovDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existGov = await _context.StateGovs
                .Include(x => x.Flights)
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (existGov == null)
                return BadRequest("Gov tapilmadi");

            var nameExists = await _context.StateGovs
                .AnyAsync(x => x.Id != dto.Id &&
                               x.Name.ToLower() == dto.Name.ToLower());

            if (nameExists)
                return BadRequest("Bu adda qurum artıq mövcuddur");

            existGov.Name = dto.Name;
            existGov.UpdatedTime = DateTime.Now.ToString("dd/MM/yyyy");


            if (!string.IsNullOrWhiteSpace(dto.FlightNumber) &&
                dto.FlightNumber != "undefined")
            {
                if (existGov.Flights != null)
                {
                    existGov.Flights.FlightNumber = dto.FlightNumber;
                }
                else
                {
                    var newFlight = new Flight
                    {
                        FlightNumber = dto.FlightNumber,
                        StateGovId = existGov.Id,
                        IsArrived = false,
                        CreatedTime = DateTime.Now.ToString("dd/MM/yyyy")
                    };

                    await _context.Flights.AddAsync(newFlight);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Updated successfully",
                existGov.Id
            });
        }

        [HttpGet("getgoverments")]
        public IActionResult GetGov()
        {
            var goverments = _context.StateGovs
                .Include(p => p.Country)
                .Include(p => p.Flights)
                .Where(p => !p.isDeleted)
                .ToList();

            var returnGov = goverments.Select(p => new ReturnGovermentDto()
            {
                CountryId = p.CountryId,
                Name = p.Name,
                Id = p.Id,
                IsMain=p.Country.IsMain,
                FlightNumber = p.Flights != null ? p.Flights.FlightNumber : null,
                CountryName=p.Country.Name,
                
                PlannedArrivedTime = p.Flights?.PlannedArrivedTime != null
                    ? p.Flights.PlannedArrivedTime.ToString()
                    : null,

                RealArrivedTime = p.Flights?.RealArrivedTime != null
                    ? p.Flights.RealArrivedTime.ToString()
                    : null,
            });

            return Ok(returnGov);
        }
        [Authorize]
        [Authorize]
        [HttpPost("changePlannedToBaku")]
        public async Task<IActionResult> ChangePlannedToBaku(int id)
        {
            var existCountry = await _context.Countrys
                .FirstOrDefaultAsync(x => x.Id == id);

            if (existCountry == null)
            {
                return NotFound(new
                {
                    message = "Ölkə tapılmadı."
                });
            }

            existCountry.IsArrivedToBaku = !existCountry.IsArrivedToBaku;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = existCountry.Id,
                isArrivedToBaku = existCountry.IsArrivedToBaku
            });
        }


        [Authorize]
        [HttpPost("changePlannedToHotel")]
        public async Task<IActionResult> ChangePlannedToHotel(int id)
        {
            var existCountry = await _context.Countrys
                .FirstOrDefaultAsync(x => x.Id == id);

            if (existCountry == null)
            {
                return NotFound(new
                {
                    message = "Ölkə tapılmadı."
                });
            }

            existCountry.IsArrivedToHotel = !existCountry.IsArrivedToHotel;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = existCountry.Id,
                isArrivedToHotel = existCountry.IsArrivedToHotel
            });
        }


        [Authorize]
        [HttpGet("getCurrentCountries")]
        public async Task<IActionResult> GetCurrentCountries()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.isDeleted)
                return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault();

            List<Country> countries;

            if (roleName == "Admin")
            {
                countries = await _context.Countrys
                    .Include(c => c.User)
                    .Include(c => c.Member) // Member məlumatını gətiririk
                    .Include(c => c.Hotel)
                    .Include(c => c.StateGovs)
                        .ThenInclude(s => s.Flights)
                    .Where(c =>
                        !c.isDeleted &&
                        c.UserId == user.Id
                    )
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }
            else if (roleName == "Member")
            {
                countries = await _context.Countrys
                    .Include(c => c.User)
                    .Include(c => c.Member) // Member məlumatını gətiririk
                    .Include(c => c.Hotel)
                    .Include(c => c.StateGovs)
                        .ThenInclude(s => s.Flights)
                    .Where(c =>
                        !c.isDeleted &&
                        c.MemberId == user.Id
                    )
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }
            else
            {
                return Forbid();
            }

            var result = countries.Select(country => new ReturnCountryDto
            {
                Id = country.Id,

                Name = country.Name,

                FlagUrl = country.FlagUrl,

                CreatedTime = country.CreatedTime,

                IsMain = country.IsMain,

                IsArrivedToBaku = country.IsArrivedToBaku,

                IsArrivedToHotel = country.IsArrivedToHotel,

                // ADMIN
                UserId = country.UserId,

                FullName = country.User != null
                    ? country.User.FullName
                    : null,

                PhoneNumber = country.User != null
                    ? country.User.PhoneNumber
                    : "",

                // MEMBER
                MemberId = country.MemberId,

                MemberFullName = country.Member != null
                    ? country.Member.FullName
                    : null,

                HotelId = country.HotelId,

                HotelName = country.Hotel != null
                    ? country.Hotel.Name
                    : null,

                dtos = country.StateGovs != null
                    ? country.StateGovs
                        .Where(x => !x.isDeleted)
                        .OrderBy(x => x.Name)
                        .Select(x => new ReturnGovermentDto
                        {
                            Id = x.Id,

                            Name = x.Name,

                            IsMain = x.IsMain,

                            CountryId = x.CountryId,

                            CountryName = x.Country != null
                                ? x.Country.Name
                                : country.Name,

                            FlightNumber = x.Flights != null
                                ? x.Flights.FlightNumber
                                : "No flight data",

                            PlannedArrivedTime = x.Flights != null
                                ? x.Flights.PlannedArrivedTime
                                : null,

                            RealArrivedTime = x.Flights != null
                                ? x.Flights.RealArrivedTime
                                : null
                        })
                        .ToList()
                    : new List<ReturnGovermentDto>()
            }).ToList();

            return Ok(result);
        }
    }
}
