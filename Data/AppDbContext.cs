using Meeting_Project.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Meeting_Project.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<HotelRoom> HotelRooms { get; set; }
        public DbSet<Meeting> Meetings { get; set; }

        public DbSet<Country> Countrys { get; set; }
        public DbSet<StateGov> StateGovs { get; set; }

        public DbSet<Flight> Flights { get; set; }
        public DbSet<MeetingParticipant> MeetingParticipant { get; set; }

        public DbSet<Seat> Seats { get; set; }
        public DbSet<CircleTable> CircleTables { get; set; }

        public DbSet<MeetingNotifitication> Notifitications { get; set; }
        public DbSet<Contact> ContactMessages { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // =====================================================
            // COUNTRY → HOTEL
            // =====================================================

            builder.Entity<Country>()
                .HasOne(c => c.Hotel)
                .WithMany(h => h.Countries)
                .HasForeignKey(c => c.HotelId)
                .OnDelete(DeleteBehavior.NoAction);


            // =====================================================
            // COUNTRY → ADMIN
            // Country.UserId → AspNetUsers.Id
            // =====================================================

            builder.Entity<Country>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);


            // =====================================================
            // COUNTRY → MEMBER
            // Country.MemberId → AspNetUsers.Id
            // =====================================================

            builder.Entity<Country>()
                .HasOne(c => c.Member)
                .WithMany()
                .HasForeignKey(c => c.MemberId)
                .OnDelete(DeleteBehavior.NoAction);


            // =====================================================
            // COUNTRY → DEFAULT MEETING ROOM
            // Country.MeetingRoomId → HotelRooms.Id
            // =====================================================

            builder.Entity<Country>()
                .HasOne(c => c.MeetingRoom)
                .WithMany()
                .HasForeignKey(c => c.MeetingRoomId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}