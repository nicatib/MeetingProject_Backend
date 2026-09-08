using meeting_app.Hubs;
using Meeting_Project.Data;
using Meeting_Project.Entity;
using Meeting_Project.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Meeting_Project.Helper;

public class MeetingExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public MeetingExpirationService(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAutomaticMeetingsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Background Service Error: {ex.Message}"
                );
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken
                );
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessAutomaticMeetingsAsync()
    {
        using var scope =
            _serviceProvider.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var hubContext =
            scope.ServiceProvider
                .GetRequiredService<IHubContext<UserHub>>();

        var fcmService =
            scope.ServiceProvider
                .GetRequiredService<IFcmService>();

        var now = DateTime.Now;


        // =========================================================
        // 1. PENDING MEETINGS - STATUS CHECK
        // =========================================================
        //
        // accepted >= 2
        //      => Planned
        //
        // accepted < 2 && pending > 0
        //      => Pending
        //
        // accepted < 2 && pending == 0
        //      => Cancelled
        //
        // Əgər ən azı 2 iştirakçı qəbul edibsə,
        // qalan iştirakçı cavab verməsə belə Planned olur.
        //
        // =========================================================

        var pendingMeetings =
            await context.Meetings
                .Include(m => m.Participants)
                    .ThenInclude(p => p.Government)
                        .ThenInclude(g => g.Country)
                .Where(m =>
                    m.Status == MeetingStatus.Pending
                )
                .ToListAsync();


        foreach (var meeting in pendingMeetings)
        {
            var acceptedCount =
                meeting.Participants
                    .Count(p =>
                        p.isAccepted == true);

            var pendingCount =
                meeting.Participants
                    .Count(p =>
                        p.isAccepted == null);

            var declinedCount =
                meeting.Participants
                    .Count(p =>
                        p.isAccepted == false);


            // =====================================================
            // 1.1 ACCEPTED >= 2
            // =====================================================

            if (acceptedCount >= 2)
            {
                meeting.Status =
                    MeetingStatus.Planned;

                Console.WriteLine(
                    $"Meeting {meeting.Id}: " +
                    $"Accepted={acceptedCount}, " +
                    $"Pending={pendingCount}, " +
                    $"Declined={declinedCount} " +
                    $"=> PLANNED"
                );

                continue;
            }


            // =====================================================
            // 1.2 HƏLƏ PENDING VAR
            // =====================================================

            if (pendingCount > 0)
            {
                // Əgər görüşün başlama vaxtı artıq çatıbsa
                // və hələ 2 nəfər qəbul etməyibsə,
                // görüş avtomatik Cancelled olur.

                if (meeting.PlannedStartTime <= now)
                {
                    meeting.Status =
                        MeetingStatus.Cancelled;


                    // =================================================
                    // NOTIFICATIONS SIL
                    // =================================================

                    var oldNotifications =
                        await context.Notifitications
                            .Where(n =>
                                n.MeetingId ==
                                meeting.Id)
                            .ToListAsync();

                    if (oldNotifications.Any())
                    {
                        context.Notifitications
                            .RemoveRange(
                                oldNotifications
                            );
                    }


                    // =================================================
                    // TARGET USERS
                    // =================================================

                    var targetUserIds =
                        meeting.Participants
                            .Select(p =>
                                p.Government?
                                    .Country?
                                    .UserId)
                            .Where(uid =>
                                !string.IsNullOrEmpty(uid))
                            .Distinct()
                            .ToList();


                    // =================================================
                    // SIGNALR + FCM
                    // =================================================

                    foreach (var userId in targetUserIds)
                    {
                        // -------------------------------
                        // SignalR
                        // -------------------------------

                        await hubContext.Clients
                            .User(userId!)
                            .SendAsync(
                                "MeetingCancelled",
                                new
                                {
                                    meetingId =
                                        meeting.Id,

                                    message =
                                        $"'{meeting.Title}' görüşü " +
                                        "vaxtında ən azı 2 iştirakçı " +
                                        "tərəfindən qəbul edilmədiyi " +
                                        "üçün ləğv edildi."
                                }
                            );


                        // -------------------------------
                        // FCM
                        // -------------------------------

                        await fcmService
                            .SendNotificationAsync(
                                userId!,
                                "Görüş Ləğv Edildi",
                                $"'{meeting.Title}' görüşü " +
                                "vaxtında təsdiqlənmədiyi üçün " +
                                "ləğv olundu.",
                                new
                                {
                                    meetingId =
                                        meeting.Id,

                                    type =
                                        "MeetingCancelled"
                                }
                            );
                    }
                }

                continue;
            }


            // =====================================================
            // 1.3 BÜTÜN İŞTİRAKÇILAR CAVAB VERİB
            // =====================================================
            //
            // accepted < 2
            // pending == 0
            //
            // => Cancelled
            //
            // =====================================================

            if (acceptedCount < 2 &&
                pendingCount == 0)
            {
                meeting.Status =
                    MeetingStatus.Cancelled;


                // =================================================
                // NOTIFICATIONS SIL
                // =================================================

                var oldNotifications =
                    await context.Notifitications
                        .Where(n =>
                            n.MeetingId ==
                            meeting.Id)
                        .ToListAsync();

                if (oldNotifications.Any())
                {
                    context.Notifitications
                        .RemoveRange(
                            oldNotifications
                        );
                }


                // =================================================
                // TARGET USERS
                // =================================================

                var targetUserIds =
                    meeting.Participants
                        .Select(p =>
                            p.Government?
                                .Country?
                                .UserId)
                        .Where(uid =>
                            !string.IsNullOrEmpty(uid))
                        .Distinct()
                        .ToList();


                foreach (var userId in targetUserIds)
                {
                    // SignalR

                    await hubContext.Clients
                        .User(userId!)
                        .SendAsync(
                            "MeetingCancelled",
                            new
                            {
                                meetingId =
                                    meeting.Id,

                                message =
                                    $"'{meeting.Title}' görüşü " +
                                    "iştirakçıların cavabları " +
                                    "nəticəsində ləğv edildi."
                            }
                        );


                    // FCM

                    await fcmService
                        .SendNotificationAsync(
                            userId!,
                            "Görüş Ləğv Edildi",
                            $"'{meeting.Title}' görüşü " +
                            "ləğv edildi.",
                            new
                            {
                                meetingId =
                                    meeting.Id,

                                type =
                                    "MeetingCancelled"
                            }
                        );
                }
            }
        }


        // =========================================================
        // 2. PLANNED -> INPROGRESS
        // =========================================================
        //
        // MƏNTİQ:
        //
        // PlannedStartTime + 5 dəqiqə çatanda
        // görüş avtomatik başlayır.
        //
        // Məsələn:
        //
        // PlannedStartTime = 10:00
        //
        // 10:00 -> başlamır
        // 10:01 -> başlamır
        // 10:04 -> başlamır
        // 10:05 -> başlayır
        //
        // Background Service hər 1 dəqiqə işlədiyi üçün
        // real vaxtda maksimum təxminən 1 dəqiqə gecikmə
        // ola bilər.
        //
        // =========================================================

        var startThreshold =
            now.AddMinutes(-5);


        var meetingsToAutoStart =
            await context.Meetings
                .Include(m => m.Participants)
                    .ThenInclude(p => p.Government)
                        .ThenInclude(g => g.Country)
                .Where(m =>
                    m.Status ==
                        MeetingStatus.Planned &&
                    m.PlannedStartTime <=
                        startThreshold
                )
                .ToListAsync();


        foreach (var meeting in meetingsToAutoStart)
        {
            // =====================================================
            // ROOM CONFLICT
            // =====================================================

            var roomConflict =
                await context.Meetings
                    .AnyAsync(m =>
                        m.Id != meeting.Id &&
                        m.RoomId == meeting.RoomId &&
                        m.Status ==
                            MeetingStatus.InProgress &&
                        m.ActualStartTime != null &&
                        m.ActualEndTime != null &&
                        now >=
                            m.ActualStartTime &&
                        now <
                            m.ActualEndTime
                    );


            if (roomConflict)
            {
                Console.WriteLine(
                    $"Meeting {meeting.Id} auto-start edilmədi. " +
                    $"Room {meeting.RoomId} artıq məşğuldur."
                );

                continue;
            }


            // =====================================================
            // GOVERNMENT CONFLICT
            // =====================================================

            var participantGovIds =
                meeting.Participants
                    .Select(p =>
                        p.GovernmentId)
                    .Distinct()
                    .ToList();


            var governmentConflict =
                await context.Meetings
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
                Console.WriteLine(
                    $"Meeting {meeting.Id} auto-start edilmədi. " +
                    "İştirak edən qurumlardan biri başqa " +
                    "aktiv görüşdədir."
                );

                continue;
            }


            // =====================================================
            // START MEETING
            // =====================================================

            meeting.Status =
                MeetingStatus.InProgress;

            meeting.ActualStartTime =
                now;


            // Burada PlannedEndTime görüşün
            // avtomatik bitmə vaxtı kimi saxlanılır.

            meeting.ActualEndTime =
                meeting.PlannedEndTime;


            // =====================================================
            // ROOM BUSY
            // =====================================================

            var room =
                await context.HotelRooms
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            meeting.RoomId
                    );


            if (room != null)
            {
                room.isFree = false;
            }


            // =====================================================
            // TARGET USERS
            // =====================================================

            var targetUserIds =
                meeting.Participants
                    .Select(p =>
                        p.Government?
                            .Country?
                            .UserId)
                    .Where(uid =>
                        !string.IsNullOrEmpty(uid))
                    .Distinct()
                    .ToList();


            // =====================================================
            // SIGNALR + FCM
            // =====================================================

            foreach (var userId in targetUserIds)
            {
                // -------------------------------
                // SignalR
                // -------------------------------

                await hubContext.Clients
                    .User(userId!)
                    .SendAsync(
                        "OnMeetingStatusChanged",
                        new
                        {
                            meetingId =
                                meeting.Id,

                            status =
                                "InProgress",

                            roomId =
                                meeting.RoomId,

                            title =
                                meeting.Title
                        }
                    );


                // -------------------------------
                // FCM
                // -------------------------------

                await fcmService
                    .SendNotificationAsync(
                        userId!,
                        "Görüş Avtomatik Başladı",
                        $"'{meeting.Title}' görüşü " +
                        "başlama vaxtından 5 dəqiqə " +
                        "keçdiyi üçün sistem tərəfindən " +
                        "avtomatik başladıldı.",
                        new
                        {
                            meetingId =
                                meeting.Id,

                            type =
                                "MeetingStarted"
                        }
                    );
            }
        }



        var finishThreshold =
            now;


        var meetingsToAutoFinish =
            await context.Meetings
                .Include(m => m.Participants)
                    .ThenInclude(p => p.Government)
                        .ThenInclude(g => g.Country)
                .Where(m =>
                    m.Status ==
                        MeetingStatus.InProgress
                    &&
                    (
                        m.ActualEndTime ??
                        m.PlannedEndTime
                    ) <=
                        finishThreshold
                )
                .ToListAsync();


        foreach (var meeting in meetingsToAutoFinish)
        {
           
            meeting.Status =
                MeetingStatus.Finished;

            meeting.ActualEndTime =
                now;


        

            var room =
                await context.HotelRooms
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id ==
                            meeting.RoomId
                    );


            if (room != null)
            {
                room.isFree = true;
            }


         

            var targetUserIds =
                meeting.Participants
                    .Select(p =>
                        p.Government?
                            .Country?
                            .UserId)
                    .Where(uid =>
                        !string.IsNullOrEmpty(uid))
                    .Distinct()
                    .ToList();


            

            foreach (var userId in targetUserIds)
            {
             
                await hubContext.Clients
                    .User(userId!)
                    .SendAsync(
                        "OnMeetingStatusChanged",
                        new
                        {
                            meetingId =
                                meeting.Id,

                            status =
                                "Finished",

                            roomId =
                                meeting.RoomId,

                            title =
                                meeting.Title
                        }
                    );



                await fcmService
                    .SendNotificationAsync(
                        userId!,
                        "Görüş Bitdi",
                        $"'{meeting.Title}' görüşü " +
                        "avtomatik olaraq bitirildi.",
                        new
                        {
                            meetingId =
                                meeting.Id,

                            type =
                                "MeetingFinished"
                        }
                    );
            }
        }



        if (
            pendingMeetings.Any() ||
            meetingsToAutoStart.Any() ||
            meetingsToAutoFinish.Any()
        )
        {
            await context.SaveChangesAsync();
        }
    }
}

