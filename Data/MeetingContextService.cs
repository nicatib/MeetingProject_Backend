//using Microsoft.EntityFrameworkCore;
//using Meeting_Project.Helper;

//namespace Meeting_Project.Data
//{
//    public class MeetingContextService
//    {
//        private readonly AppDbContext _context;

//        public MeetingContextService(AppDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<object> BuildContextAsync()
//        {
//            var meetings = await _context.Meetings
//                .Select(m => new
//                {
//                    m.Title,
//                    m.CreatedAt,
//                    m.DurationMinutes,
//                    m.Status
//                })
//                .ToListAsync();

//            var stats = new
//            {
//                Completed = meetings.Count(x => x.Status == MeetingStatus.Finished),
//                Cancelled = meetings.Count(x => x.Status == MeetingStatus.Cancelled),
//                Total = meetings.Count()
//            };

//            return new
//            {
//                Stats = stats,
//                Meetings = meetings
//            };
//        }
//    }
//}