using Microsoft.Identity.Client;

namespace Meeting_Project.Entity
{
    public class CircleTable:BaseEntity
    {
        public string Title { get; set; }
        public List<Seat>? Seats{ get; set; }
        public string? ImageUrl { get; set; }
        public CircleTable()
        {
            Seats = new List<Seat>();
        }
    }
}
