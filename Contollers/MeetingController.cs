using meeting_app.Hubs;
using Meeting_Project.Data;
using Meeting_Project.Dtos.MeetingDtos;
using Meeting_Project.Dtos.NotificationDtos;
using Meeting_Project.Entity;
using Meeting_Project.Helper;
using Meeting_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Meeting_Project.Contollers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MeetingController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IHubContext<UserHub> _hubContext;
        private readonly IFcmService _fcmService;
        public MeetingController(AppDbContext context, UserManager<AppUser> userManager, IHubContext<UserHub> hubContext, IFcmService fcmService)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _fcmService = fcmService;
        }

        private async Task<HotelRoom?> GetPermanentAvailableRoom(
    List<int> countryIds,
    DateTime start,
    DateTime end)
        {
            foreach (var countryId in countryIds)
            {
                var country = await _context.Countrys
                    .Include(c => c.MeetingRoom)
                    .FirstOrDefaultAsync(c =>
                        c.Id == countryId &&
                        !c.isDeleted &&
                        c.MeetingRoomId != null
                    );

                if (country == null || country.MeetingRoom == null)
                    continue;

                var roomId = country.MeetingRoomId.Value;

                var isBusy = await _context.Meetings
                    .AnyAsync(m =>
                        m.RoomId == roomId &&
                        m.Status != MeetingStatus.Cancelled &&

                        start < (m.ActualEndTime ?? m.PlannedEndTime) &&
                        end > (m.ActualStartTime ?? m.PlannedStartTime)
                    );

                if (!isBusy)
                {
                    return country.MeetingRoom;
                }

            }

            return null;
        }

        private async Task<bool> HasConflict(
    int roomId,
    DateTime start,
    DateTime end)
        {
            return await _context.Meetings
                .AnyAsync(m =>
                    m.RoomId == roomId &&
                    m.Status != MeetingStatus.Cancelled &&

                    start <
                        (m.ActualEndTime ?? m.PlannedEndTime) &&

                    end >
                        (m.ActualStartTime ?? m.PlannedStartTime)
                );
        }

        private async Task<HotelRoom?> GetRandomAvailableRoom(
        int hotelId,
        DateTime start,
        DateTime end)
        {
            var availableRooms = await _context.HotelRooms
                .Where(r =>
                    r.HotelId == hotelId &&
                    !r.isDeleted &&

                    !_context.Meetings.Any(m =>
                        m.RoomId == r.Id &&
                        m.Status != MeetingStatus.Cancelled &&

                        start <
                            (m.ActualEndTime ?? m.PlannedEndTime) &&

                        end >
                            (m.ActualStartTime ?? m.PlannedStartTime)
                    )
                )
                .ToListAsync();


            if (availableRooms.Count == 0)
                return null;


            var randomIndex =
                Random.Shared.Next(availableRooms.Count);


            return availableRooms[randomIndex];
        }

        [Authorize]
        [HttpPost("Create")]
        public async Task<IActionResult> CreateMeeting(
            [FromBody] CreateMeetingDto dto)
        {
            if (dto == null)
                return BadRequest("DTO is null");


            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Title required");


            if (dto.StartTime <= DateTime.Now)
            {
                return BadRequest(
                    "Keçmiş tarixə və ya cari vaxtdan əvvələ görüş təyin etmək olmaz."
                );
            }


            if (dto.EndTime <= dto.StartTime)
            {
                return BadRequest(
                    "Görüşün bitmə vaxtı başlanğıc vaxtından böyük olmalıdır."
                );
            }


            if (dto.Participants == null ||
                dto.Participants.Count < 2)
            {
                return BadRequest(
                    "Minimum 2 government required"
                );
            }


          
            var govIds = dto.Participants
                .Select(x => x.GovernmentId)
                .Distinct()
                .ToList();


            if (govIds.Count < 2)
            {
                return BadRequest(
                    "Minimum 2 fərqli qurum seçilməlidir"
                );
            }


       

            var govs = await _context.StateGovs
                .Where(x =>
                    govIds.Contains(x.Id) &&
                    !x.isDeleted
                )
                .Select(x => new
                {
                    x.Id,
                    x.CountryId
                })
                .ToListAsync();


            if (govs.Count != govIds.Count)
            {
                return BadRequest(
                    "Bəzi qurumlar tapılmadı"
                );
            }


          

            var participantCountryIds = dto.Participants
                .Select(p =>
                    govs.First(g =>
                        g.Id == p.GovernmentId
                    ).CountryId
                )
                .Distinct()
                .ToList();


          

            var selectedHotel = await _context.Hotels
                .FirstOrDefaultAsync(h =>
                    h.Id == dto.HotelId &&
                    !h.isDeleted
                );


            if (selectedHotel == null)
            {
                return BadRequest(
                    "Seçilmiş hotel tapılmadı"
                );
            }


            using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable
                );


            try
            {
              

                var governmentConflict =
                    await _context.Meetings
                        .Where(m =>
                            m.Status == MeetingStatus.Pending ||
                            m.Status == MeetingStatus.Planned ||
                            m.Status == MeetingStatus.InProgress
                        )
                        .AnyAsync(m =>
                            m.Participants.Any(p =>
                                govIds.Contains(p.GovernmentId) &&
                                p.isAccepted != false
                            )
                            &&
                            dto.StartTime < m.PlannedEndTime
                            &&
                            dto.EndTime > m.PlannedStartTime
                        );


                if (governmentConflict)
                {
                    return BadRequest(
                        "Seçilmiş qurumlardan biri həmin vaxt başqa görüşdədir"
                    );
                }



                var duration =
                    (int)(
                        dto.EndTime -
                        dto.StartTime
                    ).TotalMinutes;


              

                HotelRoom? room = null;


               
                room = await GetPermanentAvailableRoom(
                    participantCountryIds,
                    dto.StartTime,
                    dto.EndTime
                );


                int hotelIdToUse;


                if (room != null)
                {
                    hotelIdToUse = room.HotelId;
                }


           
                else
                {
                    hotelIdToUse = dto.HotelId;

                    room = await GetRandomAvailableRoom(
                        hotelIdToUse,
                        dto.StartTime,
                        dto.EndTime
                    );


                    if (room == null)
                    {
                        return BadRequest(
                            "Bu vaxt aralığında seçilmiş hoteldə boş otaq yoxdur"
                        );
                    }
                }


                

                var roomConflict = await HasConflict(
                    room.Id,
                    dto.StartTime,
                    dto.EndTime
                );


                if (roomConflict)
                {
                    return BadRequest(
                        "Seçilmiş otaq artıq həmin vaxt aralığında doludur"
                    );
                }


             
                var meeting = new Meeting
                {
                    RoomId = room.Id,

                    Title = dto.Title,

                    Description = dto.Description,

                    PlannedStartTime = dto.StartTime,

                    PlannedEndTime = dto.EndTime,

                    DurationMinutes = duration,

                    Status = MeetingStatus.Pending,

                 

                    Participants = govIds
                        .Select(id => new MeetingParticipant
                        {
                            GovernmentId = id,

                            isAccepted = null
                        })
                        .ToList()
                };


                _context.Meetings.Add(meeting);


                await _context.SaveChangesAsync();


             

                var govInfos = await _context.StateGovs
                    .Where(x =>
                        govIds.Contains(x.Id)
                    )
                    .Select(x => new
                    {
                        x.Id,

                        GovName = x.Name,

                        CountryName = x.Country.Name,

                        x.CountryId
                    })
                    .ToListAsync();


              
                var participantsText =
                    string.Join(
                        " - ",
                        govInfos.Select(x =>
                            $"{x.GovName} ({x.CountryName})"
                        )
                    );


                var roomInfo = await _context.HotelRooms
                    .Where(r =>
                        r.Id == room.Id
                    )
                    .Select(r => new
                    {
                        r.RoomNumber,

                        HotelName = r.Hotel.Name
                    })
                    .FirstOrDefaultAsync();


                var hotelRoomText =
                    roomInfo != null
                        ? $"{roomInfo.HotelName}, Otaq №{roomInfo.RoomNumber}"
                        : "Təyin olunmuş otaq";


          

                var notificationsToCreate =
                    new List<MeetingNotifitication>();


                var formattedPlannedTime =
                    $"{meeting.PlannedStartTime:dd.MM.yyyy HH:mm} - " +
                    $"{meeting.PlannedEndTime:HH:mm}";


                var currentTimestamp =
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm"
                    );


               

                var notificationCountryIds = govInfos
                    .Select(x => x.CountryId)
                    .Distinct()
                    .ToList();



                var countryUsers = await _context.Countrys
                    .Where(x =>
                        notificationCountryIds.Contains(x.Id) &&
                        !x.isDeleted
                    )
                    .Select(x => new
                    {
                        CountryId = x.Id,

                        AdminUserId = x.UserId,

                        MemberUserId = x.MemberId
                    })
                    .ToListAsync();


               
                var detailedMessage =
                    $"İştirakçılar: {participantsText}\n" +
                    $"Yer: {hotelRoomText}";


               

                foreach (var gov in govInfos)
                {
                   
                    var countryInfo =
                        countryUsers.FirstOrDefault(x =>
                            x.CountryId == gov.CountryId
                        );


                    if (countryInfo == null)
                        continue;


                   

                    var userIdsForThisCountry =
                        new List<string>();



                    if (!string.IsNullOrWhiteSpace(
                        countryInfo.AdminUserId
                    ))
                    {
                        userIdsForThisCountry.Add(
                            countryInfo.AdminUserId
                        );
                    }



                    if (!string.IsNullOrWhiteSpace(
                        countryInfo.MemberUserId
                    ))
                    {
                        userIdsForThisCountry.Add(
                            countryInfo.MemberUserId
                        );
                    }



                    userIdsForThisCountry =
                        userIdsForThisCountry
                            .Distinct()
                            .ToList();


                    if (!userIdsForThisCountry.Any())
                        continue;



                    foreach (var currentUserId
                        in userIdsForThisCountry)
                    {
                        var newNotif =
                            new MeetingNotifitication
                            {
                                Title =
                                    meeting.Title,

                                CreatedAt =
                                    currentTimestamp,

                                IsRead = false,

                                IsShown = false,
                                isDeleted=false,
                                MeetingId =
                                    meeting.Id,

                                UserId =
                                    currentUserId,

                              
                                GovermentName =
                                    gov.GovName,

                                GovernmentId =
                                    gov.Id,

                           
                                IsAccepted = null,

                                Text =
                                    participantsText,

                                Message =
                                    detailedMessage,

                                plannedTime =
                                    formattedPlannedTime,

                                Type =
                                    "MeetingCreated"
                            };


                        notificationsToCreate.Add(
                            newNotif
                        );
                    }
                }


             

                notificationsToCreate =
                    notificationsToCreate
                        .GroupBy(x => new
                        {
                            x.UserId,

                            x.GovernmentId,

                            x.MeetingId
                        })
                        .Select(x => x.First())
                        .ToList();


                if (notificationsToCreate.Any())
                {
                    await _context.Notifitications
                        .AddRangeAsync(
                            notificationsToCreate
                        );


                    await _context.SaveChangesAsync();
                }



                await transaction.CommitAsync();


                _ = Task.Run(async () =>
                {
                    try
                    {
                        var tasks =
                            notificationsToCreate
                                .Select(async notif =>
                                {
                                    try
                                    {
                                       
                                        await _hubContext.Clients
                                            .User(
                                                notif.UserId.ToString()
                                            )
                                            .SendAsync(
                                                "MeetingCreated",
                                                new
                                                {
                                                    id =
                                                        notif.Id,

                                                    meetingId =
                                                        notif.MeetingId,

                                                    title =
                                                        notif.Title,

                                                    message =
                                                        notif.Message,

                                                    plannedTime =
                                                        notif.plannedTime,

                                                    governmentName =
                                                        notif.GovermentName,

                                                    isShown =
                                                        notif.IsShown,

                                                    createdAt =
                                                        notif.CreatedAt,

                                                    userId =
                                                        notif.UserId,

                                                    governmentId =
                                                        notif.GovernmentId,

                                                    isAccepted =
                                                        notif.IsAccepted,

                                                    isRead =
                                                        notif.IsRead
                                                }
                                            );


                                        Console.WriteLine($"Gelen data {notif.MeetingId}");

                                        await _fcmService
                                            .SendNotificationAsync(
                                                notif.UserId.ToString(),

                                                "Yeni Görüş",

                                                notif.Message,

                                                new
                                                {
                                                    meetingId =
                                                        notif.MeetingId,

                                                    type =
                                                        notif.Type,

                                                    governmentId =
                                                        notif.GovernmentId
                                                }
                                            );
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(
                                            $"Bildiriş göndərilmə xətası " +
                                            $"(User: {notif.UserId}): " +
                                            $"{ex.Message}"
                                        );
                                    }
                                });


                        await Task.WhenAll(tasks);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"Global Notification Error: " +
                            $"{ex.Message}"
                        );
                    }
                });


                // =====================================================
                // RESPONSE
                // =====================================================

                return Ok(new
                {
                    message =
                        "Meeting created successfully",

                    meetingId =
                        meeting.Id,

                    roomId =
                        room.Id,

                    hotelUsed =
                        hotelIdToUse,

                    roomNumber =
                        room.RoomNumber,

                    isPermanentRoom =
                        participantCountryIds.Any(
                            countryId =>
                                _context.Countrys.Any(c =>
                                    c.Id == countryId &&
                                    c.MeetingRoomId == room.Id
                                )
                        )
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(
                    500,
                    new
                    {
                        ex.Message,

                        ex.StackTrace
                    }
                );
            }
        }


        [Authorize]
        [HttpPost("StartMeeting")]
        public async Task<IActionResult> StartMeeting(int id)
        {
            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var meeting = await _context.Meetings
                    .Include(x => x.Participants)
                        .ThenInclude(p => p.Government)
                            .ThenInclude(g => g.Country)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (meeting == null)
                    return BadRequest("Meeting not found");


                if (meeting.Status == MeetingStatus.Finished)
                    return BadRequest("Meeting artıq bitib");

                if (meeting.Status == MeetingStatus.InProgress)
                    return BadRequest("Meeting artıq başlayıb");

                if (meeting.Status == MeetingStatus.Cancelled)
                    return BadRequest("Görüş ləğv edilib");

                if (
                    meeting.Status != MeetingStatus.Pending &&
                    meeting.Status != MeetingStatus.Planned
                )
                {
                    return BadRequest(
                        "Bu görüşü başlatmaq mümkün deyil"
                    );
                }

                var now = DateTime.Now;

                if (now < meeting.PlannedStartTime)
                {
                    return BadRequest(
                        $"Bu görüşün başlama vaxtı hələ çatmayıb. " +
                        $"Başlama vaxtı: " +
                        $"{meeting.PlannedStartTime:dd.MM.yyyy HH:mm}"
                    );
                }


                var roomConflict = await _context.Meetings
                    .AnyAsync(m =>
                        m.Id != meeting.Id &&
                        m.RoomId == meeting.RoomId &&
                        m.Status == MeetingStatus.InProgress &&
                        m.ActualStartTime != null &&
                        m.ActualEndTime != null &&
                        now >= m.ActualStartTime &&
                        now < m.ActualEndTime
                    );

                if (roomConflict)
                {
                    return BadRequest(
                        "Bu otaqda artıq aktiv görüş var."
                    );
                }

              
                var participantGovIds = meeting.Participants
                    .Select(p => p.GovernmentId)
                    .Distinct()
                    .ToList();

                var governmentConflict =
                    await _context.Meetings
                        .Where(m =>
                            m.Id != meeting.Id &&
                            m.Status ==
                                MeetingStatus.InProgress
                        )
                        .AnyAsync(m =>
                            m.Participants.Any(p =>
                                participantGovIds.Contains(
                                    p.GovernmentId
                                )
                                &&
                                p.isAccepted != false
                            )
                        );

                if (governmentConflict)
                {
                    return BadRequest(
                        "Bu görüşdə iştirak edən qurumlardan biri " +
                        "artıq başqa aktiv görüşdədir."
                    );
                }


                meeting.Status =
                    MeetingStatus.InProgress;

                meeting.ActualStartTime = now;

              
                meeting.ActualEndTime =
                    meeting.PlannedEndTime;

              
                var room = await _context.HotelRooms
                    .FirstOrDefaultAsync(
                        x => x.Id == meeting.RoomId
                    );

                if (room != null)
                {
                    room.isFree = false;
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                var targetUserIds = meeting.Participants
                    .Select(p =>
                        p.Government?
                            .Country?
                            .UserId
                    )
                    .Where(uid =>
                        !string.IsNullOrEmpty(uid))
                    .Distinct()
                    .ToList();

                foreach (var userId in targetUserIds)
                {
                   

                    await _hubContext.Clients
                        .User(userId!)
                        .SendAsync(
                            "OnMeetingStatusChanged",
                            new
                            {
                                meetingId = meeting.Id,
                                status = "InProgress",
                                roomId = meeting.RoomId,
                                title = meeting.Title
                            }
                        );

                    await _fcmService.SendNotificationAsync(
                        userId!,
                        "İclas başladı",
                        $"{meeting.Title} adlı iclas başladı.",
                        new
                        {
                            meetingId = meeting.Id,
                            type = "MeetingStatusChanged",
                            status = "InProgress",
                            roomId = meeting.RoomId
                        }
                    );
                }

                return Ok(new
                {
                    message =
                        "Meeting started successfully",

                    meetingId = meeting.Id,

                    status = "InProgress",

                    actualStartTime =
                        meeting.ActualStartTime
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            "Görüş başladılarkən xəta baş verdi",

                        error = ex.Message
                    }
                );
            }
        }


        [Authorize]
        [HttpPost("FinishMeeting")]
        public async Task<IActionResult> FinishMeeting(
            int meetingId)
        {
            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var meeting = await _context.Meetings
                    .Include(x => x.Participants)
                        .ThenInclude(p => p.Government)
                            .ThenInclude(g => g.Country)
                    .FirstOrDefaultAsync(
                        x => x.Id == meetingId
                    );

                if (meeting == null)
                    return BadRequest("Meeting not found");

                // =========================================================
                // STATUS CHECK
                // =========================================================

                if (meeting.Status == MeetingStatus.Finished)
                    return BadRequest(
                        "Meeting artıq bitib"
                    );

                if (meeting.Status == MeetingStatus.Cancelled)
                    return BadRequest(
                        "Görüş ləğv edilib"
                    );

                if (meeting.Status != MeetingStatus.InProgress)
                {
                    return BadRequest(
                        "Yalnız davam edən (InProgress) " +
                        "görüşləri bitirmək olar."
                    );
                }


                var now = DateTime.Now;

              

                var allowedEndTimeForManualFinish =
                    meeting.PlannedEndTime.AddMinutes(15);

                if (now > allowedEndTimeForManualFinish)
                {
                    return BadRequest(
                        "Əllə bitirmək üçün 15 dəqiqəlik " +
                        "vaxt pəncərəsi keçib. " +
                        "Bu görüş artıq avtomatik bitirilməlidir."
                    );
                }

             

                meeting.Status =
                    MeetingStatus.Finished;

                // Əsl faktiki bitmə vaxtı
                meeting.ActualEndTime = now;


                var room = await _context.HotelRooms
                    .FirstOrDefaultAsync(
                        x => x.Id == meeting.RoomId
                    );

                if (room != null)
                {
                    room.isFree = true;
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // =========================================================
                // SIGNALR
                // =========================================================

                var targetUserIds = meeting.Participants
                    .Select(p =>
                        p.Government?
                            .Country?
                            .UserId
                    )
                    .Where(uid =>
                        !string.IsNullOrEmpty(uid))
                    .Distinct()
                    .ToList();

                foreach (var userId in targetUserIds)
                {

                    await _hubContext.Clients
                        .User(userId!)
                        .SendAsync(
                            "OnMeetingStatusChanged",
                            new
                            {
                                meetingId = meeting.Id,
                                status = "Finished",
                                roomId = meeting.RoomId,
                                title = meeting.Title
                            }
                        );

                    await _fcmService.SendNotificationAsync(
                        userId!,
                        "İclas bitdi",
                        $"{meeting.Title} adlı iclas başa çatdı.",
                        new
                        {
                            meetingId = meeting.Id,
                            type = "MeetingStatusChanged",
                            status = "Finished",
                            roomId = meeting.RoomId
                        }
                    );
                }
                return Ok(new
                {
                    message =
                        "Meeting finished successfully",

                    meetingId = meeting.Id,

                    status = "Finished",

                    actualEndTime =
                        meeting.ActualEndTime
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            "Görüş bitirilərkən xəta baş verdi",

                        error = ex.Message
                    }
                );
            }
        }



        [Authorize]
        [HttpGet("GetAllMeeting")]
        public async Task<IActionResult> GetAllMeeting()
        {
            var meetings = await _context.Meetings
      .Include(x => x.Room)
          .ThenInclude(r => r.Hotel)
      .Include(x => x.Participants)
          .ThenInclude(x => x.Government)
              .ThenInclude(g => g.Country)
      .OrderByDescending(x => x.CreatedAt)
      .ToListAsync();

            var result = meetings.Select(m => new ReturnMeetingDto
            {
                Id = m.Id,

                RoomNumber = m.Room.RoomNumber,

                Status = m.Status.ToString(),

                Title = m.Title,
                HotelName = m.Room.Hotel.Name,

                Description = m.Description,

                PlannedStartTime =
                    m.PlannedStartTime.ToString("yyyy-MM-dd HH:mm"),

                PlannedEndTime =
                    m.PlannedEndTime.ToString("yyyy-MM-dd HH:mm"),

                ActualStartTime =
                    m.ActualStartTime?.ToString("yyyy-MM-dd HH:mm"),

                ActualEndTime =
                    m.ActualEndTime?.ToString("yyyy-MM-dd HH:mm"),

                Participiants = m.Participants
                    .Select(p => new ReturnParticipiantDto
                    {
                        Id = p.Government.Id,

                        GovermentName = p.Government.Name,

                        CountryName = p.Government.Country.Name,
                        CountryId=p.Government.CountryId,
                        isAccespted=p.isAccepted==null? null :p.isAccepted
                    })
                    .ToList()

            }).ToList();

            return Ok(result);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("deleteMeeting")]
        public async Task<IActionResult> DeleteMeeting(int id)
        {
            // 1. Görüşü bazadan tapırıq
            var existMeeting = await _context.Meetings.FirstOrDefaultAsync(x => x.Id == id);
            if (existMeeting == null)
                return BadRequest("Görüş tapılmadı.");

            var relatedNotifications = await _context.Notifitications
                .Where(n => n.MeetingId == id)
                .ToListAsync();

            if (relatedNotifications.Any())
            {
                _context.Notifitications.RemoveRange(relatedNotifications);
            }

            var relatedParticipants = await _context.MeetingParticipant
                .Where(p => p.MeetingId == id)
                .ToListAsync();

            if (relatedParticipants.Any())
            {
                _context.MeetingParticipant.RemoveRange(relatedParticipants);
            }

            _context.Meetings.Remove(existMeeting);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Görüş, ona aid bildirişlər və iştirakçılar uğurla silindi." });
        }


        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateMeeting(
    int id,
    [FromBody] CreateMeetingDto dto)
        {
            // ================= VALIDATION =================

            if (dto == null)
                return BadRequest("DTO is null");

            var existMeeting = await _context.Meetings
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (existMeeting == null)
                return BadRequest("Meeting tapılmadı");

            if (existMeeting.Status == MeetingStatus.Finished || existMeeting.Status == MeetingStatus.InProgress || existMeeting.Status == MeetingStatus.Cancelled)
                return BadRequest("Yalniz baslanmayan gorus dəyişdirilə bilər");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Title required");

            if (dto.Participants == null ||
                dto.Participants.Count < 2)
                return BadRequest("Minimum 2 government required");

            if (dto.StartTime >= dto.EndTime)
                return BadRequest("Başlanğıc və bitmə vaxtı düzgün deyil");

            if (dto.EndTime <= DateTime.Now)
                return BadRequest("End time düzgün deyil");

            // ================= GOVERNMENT IDS =================

            var govIds = dto.Participants
                .Select(x => x.GovernmentId)
                .Distinct()
                .ToList();

            if (govIds.Count < 2)
                return BadRequest("Minimum 2 fərqli qurum seçilməlidir");

            // ================= GOVERNMENT VALIDATION =================

            var govs = await _context.StateGovs
                .Where(x =>
                    govIds.Contains(x.Id) &&
                    !x.isDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.CountryId
                })
                .ToListAsync();

            if (govs.Count != govIds.Count)
                return BadRequest("Bəzi qurumlar tapılmadı");

            // ================= SAME COUNTRY CHECK =================

            var sameCountry = govs
                .GroupBy(x => x.CountryId)
                .Any(g => g.Count() > 1);

            //if (sameCountry)
            //    return BadRequest(
            //        "Eyni ölkəyə aid qurumlar seçilə bilməz");

            // ================= GOVERNMENT CONFLICT =================

            var governmentConflict = await _context.Meetings
                .Where(m =>
                    m.Id != id &&
                    m.Status != MeetingStatus.Cancelled &&

                    m.Participants.Any(p =>
                        govIds.Contains(p.GovernmentId)) &&

                    dto.StartTime < m.PlannedEndTime &&
                    dto.EndTime > m.PlannedStartTime
                )
                .AnyAsync();

            if (governmentConflict)
            {
                return BadRequest(
                    "Seçilmiş qurumlardan biri həmin vaxt başqa görüşdədir");
            }

            // ================= TRANSACTION =================

            using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                var duration =
                    (int)(dto.EndTime - dto.StartTime).TotalMinutes;

                // ================= ROOM =================

                HotelRoom? room = null;

                // əvvəlki otaq boşdursa onu saxla
                var currentRoomConflict = await _context.Meetings
                    .AnyAsync(m =>
                        m.Id != id &&
                        m.RoomId == existMeeting.RoomId &&
                        m.Status != MeetingStatus.Cancelled &&
                        dto.StartTime < m.PlannedEndTime &&
                        dto.EndTime > m.PlannedStartTime
                    );

                if (!currentRoomConflict)
                {
                    room = await _context.HotelRooms
                        .FirstOrDefaultAsync(r =>
                            r.Id == existMeeting.RoomId &&
                            !r.isDeleted);
                }
                else
                {
                    room = await GetRandomAvailableRoom(
                        dto.HotelId,
                        dto.StartTime,
                        dto.EndTime);
                }

                if (room == null)
                    return BadRequest(
                        "Bu vaxt aralığında boş otaq yoxdur");

                // ================= UPDATE =================

                existMeeting.RoomId = room.Id;

                existMeeting.Title = dto.Title;
                existMeeting.Description = dto.Description;

                existMeeting.PlannedStartTime = dto.StartTime;
                existMeeting.PlannedEndTime = dto.EndTime;

                existMeeting.DurationMinutes = duration;

                // ================= PARTICIPANTS RESET =================

                _context.MeetingParticipant.RemoveRange(
                    existMeeting.Participants);

                existMeeting.Participants = govIds
                    .Select(x => new MeetingParticipant
                    {
                        GovernmentId = x
                    })
                    .ToList();

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Meeting updated successfully",
                    meetingId = existMeeting.Id,
                    roomId = room.Id
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "Server error",
                    error = ex.Message
                });
            }
        }


        [HttpPost("AddCircleMeeting")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AddCircleMeeting(
     [FromForm] AddCircleMeetingDto dto)
        {
            try
            {

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (dto.File == null || dto.File.Length == 0)
                    return BadRequest("File yoxdur");

               
                var fileName =
                    Guid.NewGuid() + Path.GetExtension(dto.File.FileName);

                var path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/meetings"
                );

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var fullPath = Path.Combine(path, fileName);

                await using (var stream = new FileStream(
                    fullPath,
                    FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }

               
                var newTable = new CircleTable
                {
                    Title = dto.Title,
                    ImageUrl = "/uploads/meetings/" + fileName,
                    CreatedTime = DateTime.Now.ToString()
                };

                await _context.CircleTables.AddAsync(newTable);

                await _context.SaveChangesAsync();

               
                var govIds = dto.dtos
                    .Select(item => item.StateGovId)
                    .Distinct()
                    .ToList();

               
                foreach (var item in dto.dtos)
                {
                    var seat = new Seat
                    {
                        SeatIndex = item.SeatIndex,
                        StateGovId = item.StateGovId,
                        CircleTableId = newTable.Id,
                        CreatedTime = DateTime.Now.ToString()
                    };

                    await _context.Seats.AddAsync(seat);
                }

                var result = await _context.SaveChangesAsync();

                var countryIds = await _context.StateGovs
                    .Where(x => govIds.Contains(x.Id))
                    .Select(x => x.CountryId)
                    .Distinct()
                    .ToListAsync();

               

                var countryUsers = await _context.Countrys
                    .Where(x => countryIds.Contains(x.Id))
                    .Select(x => new
                    {
                        x.UserId,
                        x.MemberId
                    })
                    .ToListAsync();


                var userIds = countryUsers
                    .SelectMany(x => new[]
                    {
                x.UserId,
                x.MemberId
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

    

                Console.WriteLine(
                    "=============================================="
                );

                Console.WriteLine(
                    $"CircleMeeting Created. TableId: {newTable.Id}"
                );

                Console.WriteLine(
                    $"CountryIds: {string.Join(", ", countryIds)}"
                );

                Console.WriteLine(
                    $"UserIds: {string.Join(", ", userIds)}"
                );

                Console.WriteLine(
                    $"GovernmentIds: {string.Join(", ", govIds)}"
                );

                Console.WriteLine(
                    "=============================================="
                );

               
                var govInfos = await _context.StateGovs
                    .Where(x => govIds.Contains(x.Id))
                    .Select(x => new
                    {
                        GovName = x.Name,
                        CountryName = x.Country.Name
                    })
                    .ToListAsync();

                var participantsText = string.Join(
                    " - ",
                    govInfos.Select(x =>
                        $"{x.GovName} ({x.CountryName})"
                    )
                );

                var currentTimestamp =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm");


                foreach (var userId in userIds)
                {
                    try
                    {
                        Console.WriteLine(
                            $"=============================================="
                        );

                        Console.WriteLine(
                            $"CircleMeeting notification -> UserId: {userId}"
                        );

                        try
                        {
                            await _hubContext.Clients
                                .User(userId.ToString())
                                .SendAsync(
                                    "CircleMeetingCreated",
                                    new
                                    {
                                        tableId = newTable.Id,
                                        title = newTable.Title,
                                        message = participantsText,
                                        createdAt = currentTimestamp,
                                        type = "CircleMeetingCreated"
                                    }
                                );

                            Console.WriteLine(
                                $"✅ SignalR göndərildi -> UserId: {userId}"
                            );
                        }
                        catch (Exception signalREx)
                        {
                            Console.WriteLine(
                                $"❌ SignalR Error -> UserId: {userId}"
                            );

                            Console.WriteLine(
                                signalREx.Message
                            );
                        }


                        try
                        {
                            Console.WriteLine(
                                $"📱 FCM göndərilir -> UserId: {userId}"
                            );

                            await _fcmService.SendNotificationAsync(
                                userId.ToString(),

                                "Yeni dairəvi görüş",

                                $"{newTable.Title}: {participantsText}",

                                new
                                {
                                    tableId = newTable.Id,
                                    title = newTable.Title,
                                    message = participantsText,
                                    createdAt = currentTimestamp,
                                    type = "CircleMeetingCreated"
                                }
                            );

                            Console.WriteLine(
                                $"✅ FCM göndərildi -> UserId: {userId}"
                            );
                        }
                        catch (Exception fcmEx)
                        {
                            Console.WriteLine(
                                $"❌ FCM Error -> UserId: {userId}"
                            );

                            Console.WriteLine(
                                fcmEx.Message
                            );
                        }

                        Console.WriteLine(
                            $"=============================================="
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"❌ CircleMeeting notification Error -> UserId: {userId}"
                        );

                        Console.WriteLine(
                            ex.Message
                        );
                    }
                }


                return Ok(new
                {
                    message = "Success",
                    insertedSeats = result,
                    tableId = newTable.Id
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"❌ AddCircleMeeting Error:"
                );

                Console.WriteLine(ex);

                return BadRequest(
                    ex.InnerException?.Message ?? ex.Message
                );
            }
        }









        [HttpGet("GetallCircleMeeting")]
        [Authorize]
        public IActionResult GetAllCircleMeeting()
        {
            // 1. Məlumatları bazadan çəkirik
            var existMeetings = _context.CircleTables
                .Include(p => p.Seats)
                    .ThenInclude(p => p.StateGov)
                        .ThenInclude(p => p.Country)
                .ToList();

            var returnMeetings = existMeetings.Select(p => new ReturnCircleDto()
            {
                Title = p.Title,
                Id = p.Id,
                ImageUrl = p.ImageUrl,
                Seats = p.Seats?.Select(x => new ReturnSeatDto()
                {
                    SeatIndex = x.SeatIndex,
                    StateGovId = x.StateGovId,
                    StateName = x.StateGov?.Name,
                    CountryName = x.StateGov?.Country?.Name
                }).ToList() ?? new List<ReturnSeatDto>() 
            }).ToList();

            return Ok(returnMeetings);
        }


[Authorize]
[HttpGet("GetallCircleMeetingByCountryId")]
public async Task<IActionResult> GetAllCircleMeetingByCountryId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User Id not found");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.isDeleted)
                return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault();

            // ============================================================
            // USER-IN AID OLDUGU COUNTRY-LER
            // ============================================================

            List<int> countryIds;

            if (roleName == "Admin")
            {
                countryIds = await _context.Countrys
                    .Where(c =>
                        !c.isDeleted &&
                        c.UserId == user.Id
                    )
                    .Select(c => c.Id)
                    .ToListAsync();
            }
            else if (roleName == "Member")
            {
                countryIds = await _context.Countrys
                    .Where(c =>
                        !c.isDeleted &&
                        c.MemberId == user.Id
                    )
                    .Select(c => c.Id)
                    .ToListAsync();
            }
            else
            {
                return Forbid();
            }

            // ============================================================
            // COUNTRY YOXDURSA
            // ============================================================

            if (countryIds.Count == 0)
                return Ok(new List<ReturnCircleDto>());

            // ============================================================
            // CIRCLE TABLE-LARI TAP
            //
            // Masa user-in country-lerinden HEC OLMAZSA BIRINE
            // aid olan StateGov/Seat varsa masa qaytarilir.
            // ============================================================

            var returnMeetings = await _context.CircleTables
                .Where(circle =>
                    circle.Seats.Any(seat =>
                        seat.StateGov != null &&
                        !seat.StateGov.isDeleted &&
                        countryIds.Contains(seat.StateGov.CountryId)
                    )
                )
                .Include(circle => circle.Seats)
                    .ThenInclude(seat => seat.StateGov)
                        .ThenInclude(stateGov => stateGov.Country)
                .Select(circle => new ReturnCircleDto
                {
                    Id = circle.Id,

                    Title = circle.Title,

                    ImageUrl = circle.ImageUrl,

                    // ====================================================
                    // VACIB:
                    // BURADA ARTIG FILTER YOXDUR.
                    //
                    // Masa tapildisa, onun BUTUN oturacaqlari qaytarilir.
                    // ====================================================

                    Seats = circle.Seats
                        .Select(seat => new ReturnSeatDto
                        {
                            SeatIndex = seat.SeatIndex,

                            StateGovId = seat.StateGovId,

                            StateName = seat.StateGov != null
                                ? seat.StateGov.Name
                                : null,

                            CountryName =
                                seat.StateGov != null &&
                                seat.StateGov.Country != null
                                    ? seat.StateGov.Country.Name
                                    : null
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(returnMeetings);
        }




        [HttpDelete("deletecircleMeeting")]
        [Authorize]
        public IActionResult DeleteCircleMeet(int id)
        {
            var existMeeting = _context.CircleTables
                .Include(x => x.Seats)
                .FirstOrDefault(p => p.Id == id);

            if (existMeeting == null)
                return BadRequest("Meeting not found");

            // 🔥 1. əvvəl seats sil
            if (existMeeting.Seats != null && existMeeting.Seats.Any())
            {
                _context.Seats.RemoveRange(existMeeting.Seats);
            }

            // 🔥 2. sonra table sil
            _context.CircleTables.Remove(existMeeting);

            _context.SaveChanges();

            return Ok();
        }



        [Authorize]
        [HttpPost("getMeetingsWithFilter")]
        public async Task<IActionResult> GetMeetingInCountry(
            GetMeetingDtoByCountryId filterDto)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("İstifadəçi tapılmadı.");

            var existUser = await _userManager.FindByIdAsync(userId);

            if (existUser == null)
                return Unauthorized("User not found");

            var role = User.FindFirstValue(ClaimTypes.Role);

            if (filterDto.page < 1)
                filterDto.page = 1;

            if (filterDto.take < 1)
                filterDto.take = 5000;


            List<int> ownCountryIds;

            if (role == "Admin")
            {
                ownCountryIds = await _context.Countrys
                    .AsNoTracking()
                    .Where(c =>
                        !c.isDeleted &&
                        c.UserId == userId)
                    .Select(c => c.Id)
                    .ToListAsync();
            }
            else if (role == "Member")
            {
                var memberCountryId = await _context.Countrys
                    .AsNoTracking()
                    .Where(c =>
                        !c.isDeleted &&
                        c.MemberId == userId)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefaultAsync();

                if (!memberCountryId.HasValue)
                {
                    return Ok(new
                    {
                        TotalCount = 0,
                        Meetings = new List<ReturnMeetingDto>()
                    });
                }

                ownCountryIds = new List<int>
        {
            memberCountryId.Value
        };
            }
            else
            {
                return Forbid();
            }


            if (!ownCountryIds.Any())
            {
                return Ok(new
                {
                    TotalCount = 0,
                    Meetings = new List<ReturnMeetingDto>()
                });
            }


            var query = _context.Meetings
                .AsNoTracking()
                .AsQueryable();

           

            query = query.Where(m =>
                m.Status != MeetingStatus.Pending &&
                m.Participants.Any(p =>
                    ownCountryIds.Contains(
                        p.Government.CountryId
                    )
                )
            );

           

            if (filterDto.countryIds != null &&
                filterDto.countryIds.Any())
            {
                var selectedCountryIds =
                    filterDto.countryIds
                        .Distinct()
                        .ToList();

                query = query.Where(m =>
                    m.Participants.Any(p =>
                        selectedCountryIds.Contains(
                            p.Government.CountryId
                        )
                    )
                );
            }

            // =========================================================
            // 8. STATUS FILTER
            //
            // 4 = bütün statuslar
            // =========================================================

            if (filterDto.Status.HasValue &&
                filterDto.Status.Value != 4)
            {
                query = query.Where(m =>
                    (int)m.Status == filterDto.Status.Value
                );
            }

            // =========================================================
            // 9. DATE FILTER
            // =========================================================

            if (!string.IsNullOrEmpty(filterDto.DateTime) &&
                DateTime.TryParse(
                    filterDto.DateTime,
                    null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out DateTime searchDate))
            {
                var targetDate = searchDate.Date;
                var nextDay = targetDate.AddDays(1);

                query = query.Where(m =>
                    m.PlannedStartTime >= targetDate &&
                    m.PlannedStartTime < nextDay
                );
            }

            // =========================================================
            // 10. TOTAL COUNT
            // =========================================================

            var totalCount = await query.CountAsync();

            // =========================================================
            // 11. DATA
            // =========================================================

            var data = await query
                .OrderByDescending(m => m.CreatedAt)

                .Skip(
                    (filterDto.page - 1) *
                    filterDto.take
                )

                .Take(filterDto.take)

                .Select(m => new ReturnMeetingDto
                {
                    Id = m.Id,

                    RoomNumber =
                        m.Room.RoomNumber,

                    Status =
                        m.Status.ToString(),

                    Title =
                        m.Title,

                    HotelName =
                        m.Room.Hotel.Name,

                    Description =
                        m.Description,

                    PlannedStartTime =
                        m.PlannedStartTime
                            .ToString("yyyy-MM-dd HH:mm"),

                    PlannedEndTime =
                        m.PlannedEndTime
                            .ToString("yyyy-MM-dd HH:mm"),

                    ActualStartTime =
                        m.ActualStartTime.HasValue
                            ? m.ActualStartTime.Value
                                .ToString("yyyy-MM-dd HH:mm")
                            : null,

                    ActualEndTime =
                        m.ActualEndTime.HasValue
                            ? m.ActualEndTime.Value
                                .ToString("yyyy-MM-dd HH:mm")
                            : null,

                    Participiants = m.Participants
                        .Select(p => new ReturnParticipiantDto
                        {
                            Id =
                                p.Government.Id,

                            GovermentName =
                                p.Government.Name,

                            CountryId =
                                p.Government.CountryId,

                            CountryName =
                                p.Government.Country.Name,

                            isAccespted =
                                p.isAccepted
                        })
                        .ToList()
                })

                .ToListAsync();

            // =========================================================
            // 12. RESPONSE
            // =========================================================

            return Ok(new
            {
                TotalCount = totalCount,
                Meetings = data
            });
        }




        [Authorize]
        [HttpGet("getMeetingWithId/{id}")]
        public async Task<IActionResult> GetById(int? id)
        {
            if (id == null)
                return BadRequest("Id not found");

            var meeting = await _context.Meetings
                .Where(m => m.Id == id)
                .Select(m => new ReturnMeetingDto
                {
                    Id = m.Id,

                    RoomNumber = m.Room.RoomNumber,

                    Status = m.Status.ToString(),

                    Title = m.Title,

                    HotelName = m.Room.Hotel.Name,

                    Description = m.Description,

                    PlannedStartTime = m.PlannedStartTime
                        .ToString("yyyy-MM-dd HH:mm"),

                    PlannedEndTime = m.PlannedEndTime
                        .ToString("yyyy-MM-dd HH:mm"),

                    ActualStartTime = m.ActualStartTime.HasValue
                        ? m.ActualStartTime.Value.ToString("yyyy-MM-dd HH:mm")
                        : null,

                    ActualEndTime = m.ActualEndTime.HasValue
                        ? m.ActualEndTime.Value.ToString("yyyy-MM-dd HH:mm")
                        : null,

                    Participiants = m.Participants
                        .Select(p => new ReturnParticipiantDto
                        {
                            Id = p.Government.Id,

                            GovermentName = p.Government.Name,

                            CountryId = p.Government.CountryId,

                            CountryName = p.Government.Country.Name,

                            isAccespted = p.isAccepted
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (meeting == null)
                return NotFound("Meeting not found");

            return Ok(meeting);
        }


        ////////////////////////////////////////////////////////////////////

        [Authorize]
        [HttpGet("getAllMeetingInCountry")]
        public async Task<IActionResult> GetAllMeetingInCountry()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not found");


            var role = User.FindFirstValue(
                ClaimTypes.Role
            );


            IQueryable<Meeting> query =
                _context.Meetings.AsNoTracking();



            if (role == "Admin")
            {
                var countryIds = await _context.Countrys
                    .AsNoTracking()
                    .Where(c =>
                        !c.isDeleted &&
                        c.UserId == userId
                    )
                    .Select(c => c.Id)
                    .ToListAsync();


                if (!countryIds.Any())
                {
                    return Ok(new
                    {
                        data = new List<ReturnMeetingDto>()
                    });
                }


                query = query.Where(m =>
                    m.Participants.Any(p =>
                        countryIds.Contains(
                            p.Government.CountryId
                        )
                    )
                );
            }


            else if (role == "Member")
            {
                var countryIds = await _context.Countrys
                    .AsNoTracking()
                    .Where(c =>
                        !c.isDeleted &&
                        c.MemberId == userId
                    )
                    .Select(c => c.Id)
                    .ToListAsync();


                if (!countryIds.Any())
                {
                    return Ok(new
                    {
                        data = new List<ReturnMeetingDto>()
                    });
                }


                query = query.Where(m =>
                    m.Participants.Any(p =>
                        countryIds.Contains(
                            p.Government.CountryId
                        )
                    )
                );
            }


           

            else
            {
                return Forbid();
            }


            // =====================================================
            // DATA
            // =====================================================

            var data = await query

                .OrderByDescending(m => m.PlannedStartTime)

                .Select(m => new ReturnMeetingDto
                {
                    Id = m.Id,

                    RoomNumber = m.Room.RoomNumber,

                    Status = m.Status.ToString(),

                    Title = m.Title,

                    HotelName = m.Room.Hotel.Name,

                    Description = m.Description,

                    PlannedStartTime =
                        m.PlannedStartTime
                            .ToString("yyyy-MM-dd HH:mm"),

                    PlannedEndTime =
                        m.PlannedEndTime
                            .ToString("yyyy-MM-dd HH:mm"),

                    ActualStartTime =
                        m.ActualStartTime.HasValue
                            ? m.ActualStartTime.Value
                                .ToString("yyyy-MM-dd HH:mm")
                            : null,

                    ActualEndTime =
                        m.ActualEndTime.HasValue
                            ? m.ActualEndTime.Value
                                .ToString("yyyy-MM-dd HH:mm")
                            : null,

                    Participiants = m.Participants
                        .Select(p => new ReturnParticipiantDto
                        {
                            Id = p.Government.Id,

                            GovermentName = p.Government.Name,

                            CountryId = p.Government.CountryId,

                            CountryName = p.Government.Country.Name
                        })
                        .ToList()
                })

                .ToListAsync();


            return Ok(new
            {
                data
            });
        }


        ////////////////////////////////////////////////////////////////////

        [Authorize]
        [HttpGet("getallNotification")]
        public async Task<IActionResult> getAllNotification()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var existUser = await _userManager.FindByIdAsync(userId);

            if (existUser == null)
                return BadRequest("User not found");

            var returnNotification = await _context.Notifitications
                .Where(p =>
                    p.UserId == existUser.Id &&
                    p.isDeleted != true)
                .OrderByDescending(p => p.Id)
                .Select(p => new ReturnNotificationDto
                {
                    // =============================================
                    // BASIC
                    // =============================================

                    Id = p.Id,

                    Title = p.Title,

                    Message = p.Message,

                    UserId = p.UserId,

                    IsRead = p.IsRead,

                    CreatedAt = p.CreatedAt,

                    Reason = p.Reason,

                    plannedTime = p.plannedTime,

                    MeetingId = p.MeetingId,

                    Type = p.Type,

                    IsShown = p.IsShown,

                    // =============================================
                    // GOVERNMENT
                    // =============================================

                    GovernmentId = p.GovernmentId,

                    GovermentName = p.GovermentName,

                    // =============================================
                    // ACCEPTED
                    // =============================================

                    IsAccepted =
                        p.Meeting != null &&
                        p.Meeting.Status == MeetingStatus.Cancelled
                            ? false
                            : p.IsAccepted
                })
                .ToListAsync();

            return Ok(returnNotification);
        }
        ////////////////////////////////////////////////////////////////////
        [Authorize]
        [HttpGet("getArchivedNotification")]
        public async Task<IActionResult> GetArchivedNotification()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var existUser = await _userManager.FindByIdAsync(userId);

            if (existUser == null)
                return BadRequest("User not found");

            var returnNotification = await _context.Notifitications
                .Where(p =>
                    p.UserId == existUser.Id &&p.isDeleted==true)
                .OrderByDescending(p => p.Id)
                .Select(p => new ReturnNotificationDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Message = p.Message,
                    UserId = p.UserId,
                    IsRead = p.IsRead,
                    CreatedAt = p.CreatedAt,
                    Reason = p.Reason,
                    plannedTime = p.plannedTime,
                    MeetingId = p.MeetingId,
                    Type = p.Type,
                    IsShown = p.IsShown,
                    GovernmentId = p.GovernmentId,
                    GovermentName = p.GovermentName,

                    IsAccepted =
                        p.Meeting != null &&
                        p.Meeting.Status == MeetingStatus.Cancelled
                            ? false
                            : p.IsAccepted
                })
                .ToListAsync();

            return Ok(returnNotification);
        }

        [Authorize]
        [HttpPost("DeclineNotification")]
        public async Task<IActionResult> DeclineNotification(
      DeclineNotification notDto)
        {
            // =========================================================
            // 1. LOGIN USER
            // =========================================================

            var userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("İstifadəçi tapılmadı.");


            // =========================================================
            // 2. NOTIFICATION
            // =========================================================

            var existNotification =
                await _context.Notifitications
                    .FirstOrDefaultAsync(n => n.Id == notDto.Id);

            if (existNotification == null)
                return BadRequest("Bildiriş tapılmadı.");


            // =========================================================
            // 3. NOTIFICATION USER CHECK
            // =========================================================

            if (existNotification.UserId?.ToString() != userIdClaim)
            {
                return BadRequest(
                    "Bu bildiriş sizə aid deyil.");
            }



            var role =
                User.FindFirstValue(ClaimTypes.Role);

            if (role != "Admin" &&
                role != "Member")
            {
                return Forbid();
            }


            // =========================================================
            // 5. MEMBER DECLINE EDƏ BİLMƏZ
            // =========================================================

            if (role == "Member")
            {
                return BadRequest(
                    "Member istifadəçisi görüş iştirakını Decline edə bilməz. " +
                    "Qərarı yalnız həmin Country-nin Admin-i verə bilər.");
            }



            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Id == userIdClaim);

            if (user == null)
            {
                return Unauthorized(
                    "İstifadəçi tapılmadı.");
            }


            // =========================================================
            // 7. MEETING
            // =========================================================

            var existMeeting =
                await _context.Meetings
                    .Include(m => m.Participants)
                    .FirstOrDefaultAsync(
                        m => m.Id == existNotification.MeetingId);

            if (existMeeting == null)
            {
                return BadRequest(
                    "Görüş tapılmadı.");
            }


            // =========================================================
            // 8. MEETING CANCELLED CHECK
            // =========================================================

            if (existMeeting.Status ==
                MeetingStatus.Cancelled)
            {
                return BadRequest(
                    "Bu görüş artıq ləğv olunub.");
            }


            // =========================================================
            // 9. NOTIFICATION GOVERNMENT ID
            // =========================================================

            var notificationGovernmentId =
                existNotification.GovernmentId;

            if (notificationGovernmentId <= 0)
            {
                return BadRequest(
                    "Bildirişə aid dövlət qurumu tapılmadı.");
            }


            // =========================================================
            // 10. GOVERNMENT
            // =========================================================

            var government =
                await _context.StateGovs
                    .Include(g => g.Country)
                    .FirstOrDefaultAsync(g =>
                        g.Id == notificationGovernmentId &&
                        !g.isDeleted);

            if (government == null)
            {
                return BadRequest(
                    "Dövlət qurumu tapılmadı.");
            }


            // =========================================================
            // 11. COUNTRY
            // =========================================================

            if (government.Country == null)
            {
                return BadRequest(
                    "Dövlət qurumunun Country məlumatı tapılmadı.");
            }

            var country =
                government.Country;


            // =========================================================
            // 12. ADMIN OWNERSHIP
            // =========================================================

            if (country.UserId != userIdClaim)
            {
                return BadRequest(
                    "Siz bu dövlət qurumunun bağlı olduğu Country-nin Admin-i deyilsiniz.");
            }


            // =========================================================
            // 13. PARTICIPANT
            // =========================================================

            var participant =
                existMeeting.Participants
                    .FirstOrDefault(p =>
                        p.GovernmentId ==
                        notificationGovernmentId);

            if (participant == null)
            {
                return BadRequest(
                    "Bu dövlət qurumu görüş iştirakçısı deyil.");
            }


            // =========================================================
            // 14. ALREADY DECLINED
            // =========================================================

            if (participant.isAccepted == false)
            {
                return BadRequest(
                    "Bu dövlət qurumu artıq Decline edilib.");
            }


            // =========================================================
            // 15. DECLINE ONLY THIS GOVERNMENT
            // =========================================================

            participant.isAccepted = false;


           

            var relatedNotifications =
                await _context.Notifitications
                    .Where(n =>
                        n.MeetingId == existMeeting.Id &&
                        n.GovernmentId == notificationGovernmentId)
                    .ToListAsync();


            foreach (var notification in relatedNotifications)
            {
                notification.IsAccepted = false;
                notification.IsRead = true;
                notification.Reason = notDto.Reason;
            }


            // =========================================================
            // 17. COUNTS
            // =========================================================

            var acceptedGovernmentCount =
                existMeeting.Participants
                    .Count(p =>
                        p.isAccepted == true);


            var pendingGovernmentCount =
                existMeeting.Participants
                    .Count(p =>
                        p.isAccepted == null);


            var declinedGovernmentCount =
                existMeeting.Participants
                    .Count(p =>
                        p.isAccepted == false);



            bool isCancelled = false;
            bool isPlanned = false;


            if (acceptedGovernmentCount >= 2 &&
                pendingGovernmentCount == 0)
            {
                existMeeting.Status =
                    MeetingStatus.Planned;

                isPlanned = true;
            }


            // ---------------------------------------------------------
            // CANCELLED
            // ---------------------------------------------------------

            else if (pendingGovernmentCount == 0 &&
                     acceptedGovernmentCount < 2)
            {
                existMeeting.Status =
                    MeetingStatus.Cancelled;

                isCancelled = true;
            }


            // ---------------------------------------------------------
            // PENDING
            // ---------------------------------------------------------

            else
            {
                existMeeting.Status =
                    MeetingStatus.Pending;
            }


          

            if (isCancelled)
            {
                var otherNotifications =
                    await _context.Notifitications
                        .Where(n =>
                            n.MeetingId == existMeeting.Id &&
                            !relatedNotifications
                                .Select(x => x.Id)
                                .Contains(n.Id))
                        .ToListAsync();


                foreach (var notification in otherNotifications)
                {
                    notification.IsRead = false;

                    // Meeting artıq Cancelled olduğu üçün
                    // notification frontend-də də qəbul edilmiş
                    // kimi görünməsin.
                    notification.IsAccepted = false;
                }
            }



            await _context.SaveChangesAsync();


            // =========================================================
            // 21. SIGNALR
            // =========================================================

            try
            {
                var meetingGovernmentIds =
                    existMeeting.Participants
                        .Select(p => p.GovernmentId)
                        .Distinct()
                        .ToList();


                var targetUserIds =
                    await _context.StateGovs
                        .Where(g =>
                            meetingGovernmentIds.Contains(g.Id) &&
                            !g.isDeleted)
                        .Join(
                            _context.Countrys
                                .Where(c => !c.isDeleted),

                            government => government.CountryId,

                            country => country.Id,

                            (government, country) =>
                                new
                                {
                                    country.UserId,
                                    country.MemberId
                                }
                        )
                        .SelectMany(x =>
                            new[]
                            {
                        x.UserId,
                        x.MemberId
                            })
                        .Where(id =>
                            !string.IsNullOrEmpty(id) &&
                            id != userIdClaim)
                        .Distinct()
                        .ToListAsync();


                foreach (var targetUserId in targetUserIds)
                {
                    await _hubContext.Clients
                        .User(targetUserId)
                        .SendAsync(
                            "declineStatusUpdate",
                            new
                            {
                                meetingId =
                                    existMeeting.Id,

                                governmentId =
                                    notificationGovernmentId,

                                countryId =
                                    country.Id,

                                isAccepted =
                                    false,

                                status =
                                    existMeeting.Status.ToString(),

                                isMeetingCancelled =
                                    isCancelled,

                                isMeetingPlanned =
                                    isPlanned,

                                acceptedCount =
                                    acceptedGovernmentCount,

                                pendingCount =
                                    pendingGovernmentCount,

                                declinedCount =
                                    declinedGovernmentCount,

                                reason =
                                    notDto.Reason
                            });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"SignalR Decline Göndərmə Xətası: {ex.Message}");
            }


            // =========================================================
            // 22. RESPONSE
            // =========================================================

            return Ok(
                new
                {
                    message =
                        isCancelled
                            ? "Bütün iştirakçılar cavab verdi və qəbul edənlərin sayı 2-dən aşağı olduğu üçün görüş ləğv edildi."
                            : isPlanned
                                ? "Dövlət qurumu görüşdə iştirakdan imtina etdi. Digər iştirakçılarla birlikdə görüş Planned olaraq qalır."
                                : "Dövlət qurumu görüşdə iştirakdan imtina etdi. Digər iştirakçıların cavabı gözlənilir.",

                    meetingId =
                        existMeeting.Id,

                    governmentId =
                        notificationGovernmentId,

                    governmentName =
                        government.Name,

                    countryId =
                        country.Id,

                    countryName =
                        country.Name,

                    isAccepted =
                        false,

                    acceptedCount =
                        acceptedGovernmentCount,

                    pendingCount =
                        pendingGovernmentCount,

                    declinedCount =
                        declinedGovernmentCount,

                    isMeetingCancelled =
                        isCancelled,

                    isMeetingPlanned =
                        isPlanned,

                    meetingStatus =
                        existMeeting.Status.ToString(),

                    reason =
                        notDto.Reason
                });
        }

        [Authorize]
        [HttpPost("AcceptNotification")]
        public async Task<IActionResult> AcceptNotification(int id)
        {
            // =========================================================
            // 1. LOGIN USER
            // =========================================================

            var userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(
                    "İstifadəçi tapılmadı.");
            }


            // =========================================================
            // 2. ROLE
            // =========================================================

            var role =
                User.FindFirstValue(ClaimTypes.Role);

            // Bu endpoint yalnız Admin üçündür.
            if (role != "Admin")
            {
                if (role == "Member")
                {
                    return BadRequest(
                        "Member istifadəçisi görüşü qəbul edə bilməz. " +
                        "Görüşü yalnız Country Admin-i qəbul edə bilər.");
                }

                return Forbid();
            }


            // =========================================================
            // 3. NOTIFICATION
            // =========================================================

            var existNotification =
                await _context.Notifitications
                    .FirstOrDefaultAsync(n => n.Id == id);

            if (existNotification == null)
            {
                return BadRequest(
                    "Bildiriş tapılmadı.");
            }


            // =========================================================
            // 4. NOTIFICATION USER CHECK
            // =========================================================

            if (existNotification.UserId?.ToString() != userIdClaim)
            {
                return BadRequest(
                    "Bu bildiriş sizə aid deyil.");
            }


            // =========================================================
            // 5. USER
            // =========================================================

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.Id == userIdClaim);

            if (user == null)
            {
                return Unauthorized(
                    "İstifadəçi tapılmadı.");
            }


            // =========================================================
            // 6. ADMIN-IN COUNTRY-LƏRİ
            // =========================================================

            // Admin bir neçə Country idarə edə bilər.
            var userCountryIds =
                await _context.Countrys
                    .Where(c =>
                        !c.isDeleted &&
                        c.UserId == userIdClaim)
                    .Select(c => c.Id)
                    .ToListAsync();

            if (!userCountryIds.Any())
            {
                return BadRequest(
                    "Admin-in idarə etdiyi Country tapılmadı.");
            }


            // =========================================================
            // 7. MEETING
            // =========================================================

            var existMeeting =
                await _context.Meetings
                    .Include(m => m.Participants)
                    .FirstOrDefaultAsync(
                        m => m.Id == existNotification.MeetingId);

            if (existMeeting == null)
            {
                return BadRequest(
                    "Görüş tapılmadı.");
            }


            // =========================================================
            // 8. CANCELLED CHECK
            // =========================================================

            if (existMeeting.Status ==
                MeetingStatus.Cancelled)
            {
                return BadRequest(
                    "Bu görüş artıq ləğv olunub.");
            }


            // =========================================================
            // 9. NOTIFICATION GOVERNMENT
            // =========================================================

            var notificationGovernmentId =
                existNotification.GovernmentId;

            if (notificationGovernmentId <= 0)
            {
                return BadRequest(
                    "Bildirişə aid dövlət qurumu tapılmadı.");
            }


            // =========================================================
            // 10. GOVERNMENT
            // =========================================================

            var government =
                await _context.StateGovs
                    .Include(g => g.Country)
                    .FirstOrDefaultAsync(g =>
                        g.Id == notificationGovernmentId &&
                        !g.isDeleted);

            if (government == null)
            {
                return BadRequest(
                    "Dövlət qurumu tapılmadı.");
            }


            // =========================================================
            // 11. COUNTRY
            // =========================================================

            if (government.Country == null)
            {
                return BadRequest(
                    "Dövlət qurumunun Country məlumatı tapılmadı.");
            }

            var country =
                government.Country;


            // =========================================================
            // 12. ADMIN COUNTRY CHECK
            // =========================================================

            // Notification-da gələn Government-in Country-si
            // mütləq bu Admin-in idarə etdiyi Country olmalıdır.

            if (!userCountryIds.Contains(country.Id))
            {
                return BadRequest(
                    "Bu dövlət qurumu sizin idarə etdiyiniz Country-yə aid deyil.");
            }


            // =========================================================
            // 13. PARTICIPANT
            // =========================================================

            // YALNIZ notification-da olan Government-i tapırıq.
            //
            // Məsələn:
            //
            // Meeting:
            // Government 15
            // Government 16
            // Government 8
            //
            // Notification GovernmentId = 16-dırsa,
            // yalnız 16 dəyişəcək.

            var participant =
                existMeeting.Participants
                    .FirstOrDefault(p =>
                        p.GovernmentId ==
                        notificationGovernmentId);

            if (participant == null)
            {
                return BadRequest(
                    "Bu dövlət qurumu görüş iştirakçısı deyil.");
            }


            // =========================================================
            // 14. ALREADY DECLINED
            // =========================================================

            if (participant.isAccepted == false)
            {
                return BadRequest(
                    "Bu dövlət qurumu artıq Decline edilib və yenidən Accept edilə bilməz.");
            }


            // =========================================================
            // 15. ALREADY ACCEPTED CHECK
            // =========================================================

            if (participant.isAccepted == true)
            {
                return BadRequest(
                    "Bu dövlət qurumu artıq Accept edilib.");
            }


            // =========================================================
            // 16. ACCEPT ONLY THIS GOVERNMENT
            // =========================================================

            participant.isAccepted = true;


            // =========================================================
            // 17. SAME GOVERNMENT NOTIFICATIONS
            // =========================================================

            // Eyni Meeting + eyni Government üçün yaradılmış
            // bütün notification-ları sync edirik.
            //
            // Burada Admin və Member notification-ları ola bilər.

            var relatedNotifications =
                await _context.Notifitications
                    .Where(n =>
                        n.MeetingId == existMeeting.Id &&
                        n.GovernmentId == notificationGovernmentId)
                    .ToListAsync();


            foreach (var notification in relatedNotifications)
            {
                notification.IsAccepted = true;
                notification.IsRead = true;
            }


            // =========================================================
            // 18. COUNTS
            // =========================================================

            var acceptedCount =
                existMeeting.Participants
                    .Count(p =>
                        p.isAccepted == true);


            var pendingCount =
                existMeeting.Participants
                    .Count(p =>
                        p.isAccepted == null);


            var declinedCount =
                existMeeting.Participants
                    .Count(p =>
                        p.isAccepted == false);


            // =========================================================
            // 19. MEETING STATUS
            // =========================================================
            //
            // QAYDA:
            //
            // accepted >= 2 && pending == 0
            //                  => Planned
            //
            // pending > 0
            //                  => Pending
            //
            // pending == 0 && accepted < 2
            //                  => Cancelled
            //
            // =========================================================

            bool isFullyConfirmed = false;
            bool isCancelled = false;


            // ---------------------------------------------------------
            // PLANNED
            // ---------------------------------------------------------

            if (acceptedCount >= 2 &&
                pendingCount == 0)
            {
                existMeeting.Status =
                    MeetingStatus.Planned;

                isFullyConfirmed = true;
            }


            // ---------------------------------------------------------
            // CANCELLED
            // ---------------------------------------------------------

            else if (pendingCount == 0 &&
                     acceptedCount < 2)
            {
                existMeeting.Status =
                    MeetingStatus.Cancelled;

                isCancelled = true;
            }


            // ---------------------------------------------------------
            // PENDING
            // ---------------------------------------------------------

            else
            {
                existMeeting.Status =
                    MeetingStatus.Pending;
            }


            // =========================================================
            // 20. SAVE DATABASE
            // =========================================================

            await _context.SaveChangesAsync();


            // =========================================================
            // 21. SIGNALR
            // =========================================================

            try
            {
                // Meeting-də iştirak edən bütün Government-lər.
                var meetingGovernmentIds =
                    existMeeting.Participants
                        .Select(p => p.GovernmentId)
                        .Distinct()
                        .ToList();


                // Həmin Government-lərin bağlı olduğu
                // Country Admin + Member-ləri tapılır.

                var targetUserIds =
                    await _context.StateGovs
                        .Where(g =>
                            meetingGovernmentIds.Contains(g.Id) &&
                            !g.isDeleted)
                        .Join(
                            _context.Countrys
                                .Where(c => !c.isDeleted),

                            government => government.CountryId,

                            country => country.Id,

                            (government, country) =>
                                new
                                {
                                    country.UserId,
                                    country.MemberId
                                }
                        )
                        .SelectMany(x =>
                            new[]
                            {
                        x.UserId,
                        x.MemberId
                            })
                        .Where(uid =>
                            !string.IsNullOrEmpty(uid) &&
                            uid != userIdClaim)
                        .Distinct()
                        .ToListAsync();


                // Hər relevant istifadəçiyə SignalR göndərilir.

                foreach (var targetUserId in targetUserIds)
                {
                    await _hubContext.Clients
                        .User(targetUserId)
                        .SendAsync(
                            "acceptStatusUpdate",
                            new
                            {
                                meetingId =
                                    existMeeting.Id,

                                // Yalnız Accept edilən Government.
                                governmentId =
                                    notificationGovernmentId,

                                countryId =
                                    country.Id,

                                isAccepted =
                                    true,

                                status =
                                    existMeeting.Status.ToString(),

                                acceptedCount =
                                    acceptedCount,

                                pendingCount =
                                    pendingCount,

                                declinedCount =
                                    declinedCount,

                                isFullyConfirmed =
                                    isFullyConfirmed,

                                isMeetingCancelled =
                                    isCancelled
                            });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"SignalR Accept Göndərmə Xətası: {ex.Message}");
            }


     

            return Ok(
                new
                {
                    message =
                        isFullyConfirmed
                            ? "Bütün iştirakçılar cavab verdi və qəbul edənlərin sayı ən azı 2 olduğu üçün görüş Planned oldu."
                            : isCancelled
                                ? "Qəbul edən iştirakçıların sayı 2-dən aşağı olduğu üçün görüş ləğv edildi."
                                : "Dövlət qurumunun iştirakı qəbul edildi. Digər iştirakçıların cavabı gözlənilir.",

                    meetingId =
                        existMeeting.Id,

                    governmentId =
                        notificationGovernmentId,

                    governmentName =
                        government.Name,

                    countryId =
                        country.Id,

                    countryName =
                        country.Name,

                    isAccepted =
                        true,

                    acceptedCount =
                        acceptedCount,

                    pendingCount =
                        pendingCount,

                    declinedCount =
                        declinedCount,

                    isFullyConfirmed =
                        isFullyConfirmed,

                    isMeetingCancelled =
                        isCancelled,

                    meetingStatus =
                        existMeeting.Status.ToString()
                });
        }



        [Authorize]
        [HttpDelete("DeleteNotification/{id}")]
        public async Task<IActionResult> DeleteNotification(
     int id,
     [FromQuery] int statusCode)
        {
            try
            {

                var userIdClaim =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(
                        "İstifadəçi tapılmadı.");
                }


                var role =
                    User.FindFirstValue(ClaimTypes.Role);

                if (role != "Admin" && role != "Member")
                {
                    return Forbid();
                }


                if (statusCode != 1 && statusCode != 2)
                {
                    return BadRequest(
                        "statusCode yalnız 1 və ya 2 ola bilər.");
                }


                var existNotification =
                    await _context.Notifitications
                        .FirstOrDefaultAsync(p =>
                            p.Id == id);

                if (existNotification == null)
                {
                    return NotFound(
                        "Bildiriş tapılmadı.");
                }


                if (existNotification.UserId?.ToString()
                    != userIdClaim)
                {
                    return Forbid(
                        "Bu bildiriş üzərində əməliyyat aparmaq səlahiyyətiniz yoxdur.");
                }


                if (statusCode == 1)
                {
                    // Artıq arxivdədirsə
                    if (existNotification.isDeleted == true)
                    {
                        return BadRequest(
                            "Bu bildiriş artıq arxivdədir.");
                    }

                    existNotification.isDeleted = true;

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        message = "Bildiriş arxivləndi.",
                        notificationId = id,
                        statusCode = 1,
                        isDeleted = true,
                        deletedFromDatabase = false
                    });
                }


                if (statusCode == 2)
                {
                    _context.Notifitications.Remove(
                        existNotification);

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        message = "Bildiriş database-dən silindi.",
                        notificationId = id,
                        statusCode = 2,
                        isDeleted = false,
                        deletedFromDatabase = true
                    });
                }

                return BadRequest(
                    "Yanlış statusCode.");
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    $"Daxili xəta baş verdi: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPut("RestoreNotification/{id}")]
        public async Task<IActionResult> RestoreNotification(int id)
        {
            try
            {
             
                var userIdClaim =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized("İstifadəçi tapılmadı.");

              

                var role =
                    User.FindFirstValue(ClaimTypes.Role);

                if (role != "Admin" && role != "Member")
                {
                    return Forbid();
                }

              

                var existNotification =
                    await _context.Notifitications
                        .FirstOrDefaultAsync(p =>
                            p.Id == id);

                if (existNotification == null)
                {
                    return NotFound(
                        "Bildiriş tapılmadı.");
                }

               

                if (existNotification.UserId?.ToString() != userIdClaim)
                {
                    return Forbid(
                        "Bu bildirişi bərpa etmək səlahiyyətiniz yoxdur.");
                }


                if (existNotification.isDeleted == false)
                {
                    return BadRequest("Bu bildiriş artıq aktivdir.");
                }


                existNotification.isDeleted = false;


                await _context.SaveChangesAsync();


                return Ok(new
                {
                    message = "Bildiriş arxivdən geri qaytarıldı.",
                    notificationId = id,
                    isDeleted = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    $"Daxili xəta baş verdi: {ex.Message}");
            }
        }
    }


 
        

}