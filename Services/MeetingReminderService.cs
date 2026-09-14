using Meeting_Project.Data;
using Meeting_Project.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using meeting_app.Hubs;
using System.Collections.Concurrent;
using Meeting_Project.Entity;
using Meeting_Project.Helper;

public class MeetingReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    // Hər meeting üçün reminder-in göndərildiyi vaxtı saxlayırıq ki, təmizləyə bilək
    private static readonly ConcurrentDictionary<int, DateTime> _reminderSentMeetings = new();

    public MeetingReminderService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("==============================================");
        Console.WriteLine("MeetingReminderService STARTED");
        Console.WriteLine("10 dəqiqəlik meeting reminder aktivdir.");
        Console.WriteLine("==============================================");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMeetingRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ MeetingReminderService ERROR:");
                Console.WriteLine(ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        Console.WriteLine("MeetingReminderService STOPPED");
    }

    private async Task ProcessMeetingRemindersAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var fcmService = scope.ServiceProvider.GetRequiredService<IFcmService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<UserHub>>();

        var now = DateTime.Now;

        Console.WriteLine("----------------------------------------------");
        Console.WriteLine($"🔎 Reminder yoxlanılır: {now:yyyy-MM-dd HH:mm:ss}");

        // 10 dəqiqəyə yaxınlaşan görüşləri tapmaq üçün aralıq (məsələn: 9 ilə 11 dəqiqə arası)
        var reminderFrom = now.AddMinutes(9);
        var reminderTo = now.AddMinutes(11);

        Console.WriteLine($"🔎 Axtarış aralığı: {reminderFrom:HH:mm:ss} -> {reminderTo:HH:mm:ss}");

        var meetings = await context.Meetings
            .AsNoTracking()
            .Include(m => m.Participants)
                .ThenInclude(p => p.Government)
                    .ThenInclude(g => g.Country)
            .Where(m =>
                m.Status == MeetingStatus.Planned &&
                m.PlannedStartTime >= reminderFrom &&
                m.PlannedStartTime <= reminderTo
            )
            .ToListAsync(stoppingToken);

        Console.WriteLine($"🔎 Reminder üçün tapılan meeting sayı: {meetings.Count}");

        if (!meetings.Any())
        {
            Console.WriteLine("ℹ️ Bu yoxlamada reminder üçün meeting tapılmadı.");
            CleanupSentReminders(now);
            return;
        }

        foreach (var meeting in meetings)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            // Əgər bu meeting üçün artıq reminder göndərilibsə, yenidən göndərmə
            if (!_reminderSentMeetings.TryAdd(meeting.Id, now))
            {
                Console.WriteLine($"ℹ️ Meeting {meeting.Id}: reminder artıq göndərilib.");
                continue;
            }

            try
            {
                Console.WriteLine($"🚀 Meeting {meeting.Id}: reminder göndərilir...");

                await SendReminderAsync(meeting, fcmService, hubContext, now, stoppingToken);

                Console.WriteLine($"✅ Meeting {meeting.Id}: reminder göndərildi.");
            }
            catch (Exception ex)
            {
                // Xəta olarsa, siyahıdan çıxarırıq ki, növbəti dəfə yenidən cəhd edə bilsin
                _reminderSentMeetings.TryRemove(meeting.Id, out _);

                Console.WriteLine($"❌ Meeting {meeting.Id}: reminder göndərilmədi.");
                Console.WriteLine(ex);
            }
        }

        CleanupSentReminders(now);
    }

    private async Task SendReminderAsync(
        Meeting meeting,
        IFcmService fcmService,
        IHubContext<UserHub> hubContext,
        DateTime now,
        CancellationToken stoppingToken)
    {
        var targetUserIds = meeting.Participants
            .Select(p => p.Government?.Country)
            .Where(country => country != null)
            .SelectMany(country => new[]
            {
                country!.UserId,
                country.MemberId
            })
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct()
            .ToList();

        Console.WriteLine($"👥 Meeting {meeting.Id}: target user sayı = {targetUserIds.Count}");

        if (!targetUserIds.Any())
        {
            Console.WriteLine($"❌ Meeting {meeting.Id}: target user tapılmadı.");
            return;
        }

        // Qalan dəqiqəni 'now' dəyişəninə əsasən düzgün hesablayırıq
        var remainingMinutes = Math.Max(
            0,
            (meeting.PlannedStartTime - now).TotalMinutes
        );

        var startTime = meeting.PlannedStartTime.ToString("HH:mm");
        var title = "Görüş 10 dəqiqəyə başlayır";
        var body = $"'{meeting.Title}' görüşü {startTime}-da başlayacaq.";

        Console.WriteLine($"🔔 Meeting {meeting.Id} | Title: {meeting.Title} | Start: {startTime} | Remaining: {remainingMinutes:F1} dəqiqə");

        foreach (var userId in targetUserIds)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                // FCM Bildirişi
                Console.WriteLine($"📱 FCM göndərilir -> UserId={userId}");
                await fcmService.SendNotificationAsync(
                    userId!,
                    title,
                    body,
                    new
                    {
                        meetingId = meeting.Id,
                        meetingName = meeting.Title,
                        type = "MeetingReminder",
                        minutesRemaining = Math.Round(remainingMinutes, 1)
                    }
                );
                Console.WriteLine($"✅ FCM göndərildi -> UserId={userId}");

               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MeetingReminder ERROR | UserId={userId}");
                Console.WriteLine(ex);
            }
        }
    }

    private void CleanupSentReminders(DateTime now)
    {
        if (_reminderSentMeetings.IsEmpty)
        {
            return;
        }

        // 2 saatdan əvvəl göndərilmiş köhnə qeydləri yaddaşdan təmizləyirik ki, yaddaş dolmasın
        foreach (var kvp in _reminderSentMeetings)
        {
            if ((now - kvp.Value).TotalHours > 2)
            {
                _reminderSentMeetings.TryRemove(kvp.Key, out _);
            }
        }
    }
}