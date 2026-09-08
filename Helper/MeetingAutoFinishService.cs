//using Microsoft.Extensions.Hosting;
//using Microsoft.EntityFrameworkCore;
//using Meeting_Project.Data;
//using Meeting_Project.Helper;

//public class MeetingAutoFinishService : BackgroundService
//{
//    private readonly IServiceScopeFactory _scopeFactory;

//    public MeetingAutoFinishService(IServiceScopeFactory scopeFactory)
//    {
//        _scopeFactory = scopeFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            using var scope = _scopeFactory.CreateScope();
//            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//            var now = DateTime.Now;

//            // 🔥 ONLY EXPIRED MEETINGS (END TIME PASSED)
//            var expiredMeetings = await db.Meetings
//                .Include(x => x.Room)
//                .Where(m =>
//                    m.Status != MeetingStatus.Finished &&
//                    m.PlannedEndTime <= now
//                )
//                .ToListAsync();

//            foreach (var meeting in expiredMeetings)
//            {
//                meeting.Status = MeetingStatus.Finished;

//                // əgər manual finish edilməyibsə
//                if (meeting.ActualEndTime == null)
//                {
//                    meeting.ActualEndTime = now;
//                }

//                if (meeting.Room != null)
//                {
//                    meeting.Room.isFree = true;
//                }
//            }

//            if (expiredMeetings.Any())
//                await db.SaveChangesAsync();

//            await Task.Delay(30000, stoppingToken);
//        }
//    }
//}